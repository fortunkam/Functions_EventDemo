using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using HandleEvents.Support;
using Microsoft.AspNet.SignalR.Client;
using System.Configuration;

namespace HandleEvents
{
    [StorageAccount("StorageConnectionString")]
    public static class MailboxHandler
    {
        [FunctionName("MailboxHandler")]
        public static void Run([QueueTrigger("mailboxhandler")]string mailboxName, TraceWriter log)
        {
            log.Info($"MailboxHandler Started: {mailboxName}");
            try
            {

                //Has a task to wait for messages
                //Listens for SignalR events
                //Restart, kills existing task and restarts
                //MonitorStart, Sends events to SignalR
                //MonitorStop, Stops events

                var broadcastEvents = false;

                var hubConnection = new HubConnection(ConfigurationManager.AppSettings["SignalRUrl"], false);
                var proxy = hubConnection.CreateHubProxy("monitorhub");


                var mailboxEvents = new BackgroundEventGenerator();
                mailboxEvents.NewMessage += (message) =>
                {
                    var logMessage = $"New Message arrived in mailbox {mailboxName}";
                    log.Info(logMessage);
                    if(broadcastEvents)
                    {
                        proxy.Invoke("SendMailboxLog", mailboxName, logMessage, DateTime.UtcNow.ToString("s"));
                    }
                };


                proxy.On<string>("RestartMailbox", (name) =>
                {
                    if (name == mailboxName)
                    {
                        var logMessage = $"****MailboxHandler {name}: Restart mailbox event****";
                        log.Warning(logMessage);
                        if (broadcastEvents)
                        {
                            proxy.Invoke("SendMailboxLog", mailboxName, logMessage, DateTime.UtcNow.ToString("s"));
                        }
                        mailboxEvents.Stop();
                        mailboxEvents.Start(mailboxName);
                    }
                });

                proxy.On<string>("MailboxMonitorStart", (name) =>
                {
                    if(name == mailboxName)
                    {
                        broadcastEvents = true;
                    }
                });

                proxy.On<string>("MailboxMonitorEnd", (name) =>
                {
                    if (name == mailboxName)
                    {
                        broadcastEvents = false;
                    }
                });

                hubConnection.StateChanged += (state) =>
                {
                    log.Info($"Handler: State change from {state.OldState} to {state.NewState}");
                    if (state.NewState == ConnectionState.Connected)
                    {
                        proxy.Invoke("SendMailboxRegistered", mailboxName);
                    }
                };


                var task = hubConnection.Start();

                mailboxEvents.Start(mailboxName);

                var eventWait = new System.Threading.SemaphoreSlim(0,1);
                Task.WaitAll(task, eventWait.WaitAsync());
                //task.Wait();

                log.Info($"MailboxHandler Stopped: {mailboxName}");
            }
            catch(Exception ex)
            {
                log.Error(ex.ToString());
                
            }

        }
    }
}
