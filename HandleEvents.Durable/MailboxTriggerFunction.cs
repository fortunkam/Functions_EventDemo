using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HandleEvents.Support;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace HandleEvents.Durable
{
    public static class MailboxTriggerFunction
    {
        [FunctionName("MailboxTriggerFunction")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase starter,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string instanceId = "af9bf7e6-b909-4611-a497-0c95cfa58bfc";

            await starter.StartNewAsync("MailBoxListOrchestration", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }



        [FunctionName("MailBoxListOrchestration")]
        public static async Task<HttpResponseMessage> MailBoxListOrchestration([OrchestrationTrigger] DurableOrchestrationContext context, TraceWriter log)
        {
            var mailboxes = await context.CallActivityAsync<string[]>("LoadMailboxes", null);

            var tasks = new Dictionary<string, Task>();
            foreach(var mailbox in mailboxes)
            {
                tasks.Add(mailbox, context.CallSubOrchestratorAsync("MailBoxOrchestration", mailbox));
            }

            await Task.WhenAll(tasks.Values);



            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [FunctionName("LoadMailboxes")]
        public static string[] LoadMailboxes(
        [ActivityTrigger] DurableActivityContext context)
        {
            var iLoader = new InstanceLoader();
            return iLoader.GetInstances();
        }

        [FunctionName("MailBoxOrchestration")]
        public static async Task MailBoxOrchestration([OrchestrationTrigger] DurableOrchestrationContext context, TraceWriter log)
        {
            var mailData = new MailboxData()
            {
                Name = context.GetInput<string>(),
                runDuration = new TimeSpan(0,5,0)
            };

            var tasks = new List<Task>();
            tasks.Add(context.WaitForExternalEvent($"EndProcess_{mailData.Name}"));
            tasks.Add(context.CallActivityAsync("ProcessMailbox", mailData));



            var completedTask = await Task.WhenAny(tasks);
            await completedTask;


            context.ContinueAsNew(null);
        }

        [FunctionName("ProcessMailbox")]
        public static void ProcessMailbox(
        [ActivityTrigger] DurableActivityContext context, TraceWriter log)
        {
            var mailboxData = context.GetInput<MailboxData>();
            var mailbox = mailboxData.Name;

            var eventGenerator = new BackgroundEventGenerator();
            eventGenerator.NewMessage += (message) =>
            {
                log.Info($"New Message arrived from mailbox {message}");
            };

            eventGenerator.Start(mailbox);

            Thread.Sleep(mailboxData.runDuration);

            eventGenerator.Stop();


        }
    }
}
