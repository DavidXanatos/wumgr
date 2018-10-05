using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;


class HttpTask
{
    //const int DefaultTimeout = 2 * 60 * 1000; // 2 minutes timeout
    const int BUFFER_SIZE = 1024;
    private byte[] BufferRead;
    private HttpWebRequest request;
    private HttpWebResponse response;
    private Stream streamResponse;
    private Stream streamWriter;
    private Dispatcher mDispatcher;
    private string mUrl;
    private string mDlPath;
    private string mDlName;
    private int mLength = -1;
    private int mOffset = -1;
    private bool Canceled = false;
    private DateTime lastTime;

    public string DlPath { get { return mDlPath; } }
    public string DlName { get { return mDlName; } }

    public HttpTask(string Url, string DlPath, string DlName = null, bool Update = false)
    {
        mUrl = Url;
        mDlPath = DlPath;
        mDlName = DlName;

        BufferRead = null;
        request = null;
        response = null;
        streamResponse = null;
        streamWriter = null;
        mDispatcher = Dispatcher.CurrentDispatcher;
    }

    // Abort the request if the timer fires.
    /*private static void TimeoutCallback(object state, bool timedOut)
    {
        if (timedOut)
        {
            HttpWebRequest request = state as HttpWebRequest;
            if (request != null)
                request.Abort();
        }
    }*/

    public bool Start()
    {
        Canceled = false;
        try
        {
            // Create a HttpWebrequest object to the desired URL. 
            request = (HttpWebRequest)WebRequest.Create(mUrl);
            //myHttpWebRequest.AllowAutoRedirect = false;

            /**
                * If you are behind a firewall and you do not have your browser proxy setup
                * you need to use the following proxy creation code.

                // Create a proxy object.
                WebProxy myProxy = new WebProxy();

                // Associate a new Uri object to the _wProxy object, using the proxy address
                // selected by the user.
                myProxy.Address = new Uri("http://myproxy");


                // Finally, initialize the Web request object proxy property with the _wProxy
                // object.
                myHttpWebRequest.Proxy=myProxy;
                ***/

            BufferRead = new byte[BUFFER_SIZE];
            mOffset = 0;

            // Start the asynchronous request.
            IAsyncResult result = (IAsyncResult)request.BeginGetResponse(new AsyncCallback(RespCallback), this);

            // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
            //ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), request, DefaultTimeout, true);          
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("\nMain Exception raised!");
            Console.WriteLine("\nMessage:{0}", e.Message);
        }
        return false;
    }

    public void Cancel()
    {
        Canceled = true;
        if (request != null)
            request.Abort();
    }

    private void Finish(int Success, int ErrCode, Exception Error = null)
    {
        // Release the HttpWebResponse resource.
        if (response != null)
        {
            response.Close();
            if (streamResponse != null)
                streamResponse.Close();
            if (streamWriter != null)
                streamWriter.Close();

        }
        response = null;
        request = null;
        streamResponse = null;
        BufferRead = null;

        if (Success == 1)
        {
            try
            {
                if (File.Exists(mDlPath + @"\" + mDlName))
                    File.Delete(mDlPath + @"\" + mDlName);
                File.Move(mDlPath + @"\" + mDlName + ".tmp", mDlPath + @"\" + mDlName);
            }
            catch
            {
                AppLog.Line("Failed to rename download {0}", mDlPath + @"\" + mDlName + ".tmp");
                mDlName += ".tmp";
            }

            try { File.SetLastWriteTime(mDlPath + @"\" + mDlName, lastTime); } catch { } // set last mod time
        }
        else if (Success == 2)
        {
            AppLog.Line("File already dowllaoded {0}", mDlPath + @"\" + mDlName);
        }
        else
        {
            try { File.Delete(mDlPath + @"\" + mDlName + ".tmp"); } catch { } // delete partial file
            AppLog.Line("Failed to download file {0}", mDlPath + @"\" + mDlName);
        }

        Finished?.Invoke(this, new FinishedEventArgs(Success > 0 ? 0 : Canceled ? -1 : ErrCode, Error));
    }

    static public string GetNextTempFile(string path, string baseName)
    {
        for (int i = 0; i < 10000; i++)
        {
            if (!File.Exists(path + @"\" + baseName + "_" + i + ".tmp"))
                return baseName + "_" + i;
        }
        return baseName;
    }

    private static void RespCallback(IAsyncResult asynchronousResult)
    {
        int Success = 0;
        int ErrCode = 0;
        Exception Error = null;
        HttpTask task = (HttpTask)asynchronousResult.AsyncState;
        try
        {
            // State of request is asynchronous.
            task.response = (HttpWebResponse)task.request.EndGetResponse(asynchronousResult);

            ErrCode = (int)task.response.StatusCode;

            Console.WriteLine("The server at {0} returned {1}", task.response.ResponseUri, task.response.StatusCode);

            string fileName = Path.GetFileName(task.response.ResponseUri.ToString());
            task.lastTime = DateTime.Now;

            Console.WriteLine("With headers:");
            foreach (string key in task.response.Headers.AllKeys)
            {
                Console.WriteLine("\t{0}:{1}", key, task.response.Headers[key]);

                if (key.Equals("Content-Length", StringComparison.CurrentCultureIgnoreCase))
                {
                    task.mLength = int.Parse(task.response.Headers[key]);
                }
                else if (key.Equals("Content-Disposition", StringComparison.CurrentCultureIgnoreCase))
                {
                    string cd = task.response.Headers[key];
                    fileName = cd.Substring(cd.IndexOf("filename=") + 9).Replace("\"", "");
                }
                else if (key.Equals("Last-Modified", StringComparison.CurrentCultureIgnoreCase))
                {
                    task.lastTime = DateTime.Parse(task.response.Headers[key]);
                }
            }

            //Console.WriteLine(task.lastTime);

            if (task.mDlName == null)
                task.mDlName = fileName;

            FileInfo testInfo = new FileInfo(task.mDlPath + @"\" + task.mDlName);
            if (testInfo.Exists && testInfo.LastWriteTime == task.lastTime && testInfo.Length == task.mLength)
            {
                task.request.Abort();
                Success = 2;
            }
            else
            {
                // prepare download filename
                if (!Directory.Exists(task.mDlPath))
                    Directory.CreateDirectory(task.mDlPath);
                if (task.mDlName.Length == 0 || task.mDlName[0] == '?')
                    task.mDlName = GetNextTempFile(task.mDlPath, "Download");

                FileInfo info = new FileInfo(task.mDlPath + @"\" + task.mDlName + ".tmp");
                if (info.Exists)
                    info.Delete();

                // Read the response into a Stream object.
                task.streamResponse = task.response.GetResponseStream();

                task.streamWriter = info.OpenWrite();

                // Begin the Reading of the contents of the HTML page and print it to the console.
                task.streamResponse.BeginRead(task.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), task);
                return;
            }
        }
        catch (WebException e)
        {
            if (e.Response != null)
            {
                string fileName = Path.GetFileName(e.Response.ResponseUri.AbsolutePath.ToString());

                if (task.mDlName == null)
                    task.mDlName = fileName;

                FileInfo testInfo = new FileInfo(task.mDlPath + @"\" + task.mDlName);
                if (testInfo.Exists)
                    Success = 2;
            }

            if(Success == 0)
            {
                ErrCode = -2;
                Error = e;
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
            }
        }
        catch (Exception e)
        {
            ErrCode = -2;
            Error = e;
            Console.WriteLine("\nRespCallback Exception raised!");
            Console.WriteLine("\nMessage:{0}", e.Message);
        }
        task.mDispatcher.Invoke(new Action(() => {
            task.Finish(Success, ErrCode, Error);
        }));
    }

    private int mOldPercent = -1;

    private static void ReadCallBack(IAsyncResult asyncResult)
    {
        int Success = 0;
        int ErrCode = 0;
        Exception Error = null;
        HttpTask task = (HttpTask)asyncResult.AsyncState;
        try
        {
            int read = task.streamResponse.EndRead(asyncResult);
            // Read the HTML page and then print it to the console.
            if (read > 0)
            {
                task.streamWriter.Write(task.BufferRead, 0, read);
                task.mOffset += read;

                int Percent = task.mLength > 0 ? (int)((Int64)100 * task.mOffset / task.mLength) : -1;
                if (Percent != task.mOldPercent)
                {
                    task.mOldPercent = Percent;
                    task.mDispatcher.Invoke(new Action(() => {
                        task.Progress?.Invoke(task, new ProgressEventArgs(Percent));
                    }));
                }

                // setup next read
                task.streamResponse.BeginRead(task.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), task);
                return;
            }
            else
            {
                // this is done on finisch
                //task.streamWriter.Close();
                //task.streamResponse.Close();
                Success = 1;
            }

        }
        catch (Exception e)
        {
            ErrCode = -3;
            Error = e;
            Console.WriteLine("\nReadCallBack Exception raised!");
            Console.WriteLine("\nMessage:{0}", e.Message);
        }
        task.mDispatcher.Invoke(new Action(() => {
            task.Finish(Success, ErrCode, Error);
        }));
    }

    public class FinishedEventArgs : EventArgs
    {
        public FinishedEventArgs(int ErrCode = 0, Exception Error = null)
        {
            this.ErrCode = ErrCode;
            this.Error = Error;
        }
        public string GetError()
        {
            if (Error != null)
                return Error.ToString();
            switch(ErrCode)
            {
                case 0: return "Ok";
                case -1: return "Canceled";
                default: return ErrCode.ToString();
            }
        }
        public bool Success { get { return ErrCode == 0; } }
        public bool Cancelled { get { return ErrCode == -1; } }

        public int ErrCode = 0;
        public Exception Error = null;
    }
    public event EventHandler<FinishedEventArgs> Finished;

    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(int Percent)
        {
            this.Percent = Percent;
        }
        public int Percent = 0;
    }
    public event EventHandler<ProgressEventArgs> Progress;
}
