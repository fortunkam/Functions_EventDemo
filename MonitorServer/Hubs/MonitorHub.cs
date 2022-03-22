using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MonitorServer.Hubs
{
    public class MonitorHub : Hub
    {
        public async Task SendMailboxLog(string name, string message)
        {
            await Clients.All.MailboxLog(name, message);
        }

        public async Task SendRestartMailbox(string name)
        {
            await Clients.All.RestartMailbox(name);
        }

        public async Task SendMailboxMonitorStart(string name)
        {
            await Clients.All.MailboxMonitorStart(name);
        }

        public async Task SendMailboxMonitorEnd(string name)
        {
            await Clients.All.MailboxMonitorEnd(name);
        }

        public async Task SendMailboxRegistered(string name)
        {
            await Clients.All.MailboxRegistered(name);
        }

        public async Task SendAddMailbox(string name)
        {
            await Clients.All.AddMailbox(name);
        }
    }
}