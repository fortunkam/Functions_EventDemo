using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using HandleEvents.Support;
using Microsoft.AspNet.SignalR.Client;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace HandleEvents
{
    [StorageAccount("StorageConnectionString")]
    public static class MailboxOrchestrator
    {

        [FunctionName("MailboxOrchestrator")]
        public static void Run([QueueTrigger("mailboxorchestrator")]string myQueueItem, TraceWriter log)
        {
            log.Info($"MailboxOrchestrator Started");


            try
            {
                //Loop through mailboxes and post a new message per mailbox
                //(this way you get the scale of functions)
                // Monitor SignalR for requests to start new mailbox
                var url = ConfigurationManager.AppSettings["SignalRUrl"];

                var hubConnection = new HubConnection(url,false);
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue queue = queueClient.GetQueueReference("mailboxhandler");
                queue.CreateIfNotExists();

                var proxy = hubConnection.CreateHubProxy("monitorhub");             

                proxy.On<string>("AddMailbox", (name) =>
                {
                    log.Info($"MailboxOrchestrator Adding new Mailbox {name}");
                    CloudQueueMessage message = new CloudQueueMessage(name);
                    queue.AddMessage(message);
                });

                hubConnection.StateChanged += (state) =>
                { 
                     log.Info($"Orchestrator: State change from {state.OldState} to {state.NewState}");
                };

                hubConnection.Error += (ex) =>
                {
                    log.Error("Ooops", ex);
                };

                var task = hubConnection.Start();

                var iLoader = new InstanceLoader();
                
                foreach(var i in iLoader.GetInstances())
                {
                    CloudQueueMessage message = new CloudQueueMessage(i);
                    queue.AddMessage(message);
                }

                var eventWait = new System.Threading.SemaphoreSlim(0, 1);
                Task.WaitAll(task, eventWait.WaitAsync());


                log.Info($"MailboxOrchestrator Ended");


            }
            catch(Exception ex)
            {
                log.Error(ex.ToString());
                
            }
        }
    }
}
