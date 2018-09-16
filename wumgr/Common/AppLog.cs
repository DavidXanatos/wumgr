using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class AppLog
{
    private List<string> mLogList = new List<string>();

    static public void Line(String line)
    {
        if (mInstance != null)
            mInstance.logLine(line);
    }

    public void logLine(String line)
    {
        if (Logger != null)
        {
            mLogList.Add(line);
            while (mLogList.Count > 100)
                mLogList.RemoveAt(0);

            LogEventArgs args = new LogEventArgs();
            args.line = line;
            Logger(this, args);
        }
    }

    static public List<string> GetLog() { return mInstance.mLogList; }

    public class LogEventArgs : EventArgs
    {
        public string line { get; set; }
    }

    static public event EventHandler<LogEventArgs> Logger;

    static void LineLogger(object sender, LogEventArgs args)
    {
        Console.WriteLine("LOG: " + args.line);
    }

    static private AppLog mInstance = null;

    public static AppLog GetInstance() { return mInstance; }

    public AppLog()
    {
        mInstance = this;

        Logger += LineLogger;
    }
}
