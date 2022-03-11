using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using HandleEvents.Support;

namespace HandleEvents
{
    [StorageAccount("StorageConnectionString")]
    public static class Function1
    {
        [FunctionName("Function1")]
        [return: Queue("mailbox")]
        public static string Run([QueueTrigger("mailbox")]string myQueueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
            try
            {
                var eventGenerator = new BackgroundEventGenerator();
                eventGenerator.NewMessage += (message) =>
                {
                    log.Info($"New Message arrived from mailbox {message}");
                };

                //var HubConnection = new SignalRHubConnection();
                //HubConnection.On("Restart")(s =>
                //{
                //    eventGenerator.Stop();
                //    return myQueueItem;
                //});
                //HubConnection.Start();

                eventGenerator.Start(myQueueItem);

                //Collect 1 minutes worth of events
                Thread.Sleep(new TimeSpan(0, 1, 0));

                eventGenerator.Stop();
            }
            catch(Exception ex)
            {
                log.Error(ex.ToString());
                
            }

            return myQueueItem;
        }
    }
}
