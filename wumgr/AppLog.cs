using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wumgr
{
    class AppLog
    {
        static public void Line(String line)
        {
            if (mInstance != null)
                mInstance.logLine(line);
        }

        public void logLine(String line)
        {
            if (Logger != null)
            {
                LogEventArgs args = new LogEventArgs();
                args.line = line;
                Logger(this, args);
            }
        }

        public class LogEventArgs : EventArgs
        {
            public string line { get; set; }
        }

        static public event EventHandler<LogEventArgs> Logger;

        static void LineLogger(object sender, LogEventArgs args)
        {
            Console.WriteLine(args.line);
        }

        static private AppLog mInstance = null;

        public static AppLog GetInstance() { return mInstance; }

        public AppLog()
        {
            mInstance = this;

            Logger += LineLogger;
        }
    }

}
