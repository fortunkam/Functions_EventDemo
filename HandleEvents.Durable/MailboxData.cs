using System;

namespace HandleEvents.Durable
{
    public class MailboxData
    {
        public string Name { get; set; }
        public TimeSpan runDuration { get; set; }
    }
}
