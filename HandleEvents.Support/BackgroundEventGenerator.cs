using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HandleEvents.Support
{

    public delegate void NewMessageEventHandler(string message);

    public class BackgroundEventGenerator
    {
        private Thread _thread;

        public void Start(string mailbox)
        {
            _thread = new Thread(new ParameterizedThreadStart(BackgroundThread));
            _thread.Start(mailbox);
        }

        public void Stop()
        {
            _thread.Abort();
        }

        public event NewMessageEventHandler NewMessage;

        private void BackgroundThread(object param)
        {
            var mb = param.ToString();

            while (true)
            {
                if(NewMessage != null)
                {
                    NewMessage.Invoke(mb);
                }

                Thread.Sleep(3000);
            }
        }

    }
}
