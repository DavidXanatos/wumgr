using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace wumgr
{
    class MyWebClient : WebClient
    {
        Uri mResponseUri;

        public Uri ResponseUri
        {
            get { return mResponseUri; }
        }

        private bool mNewUri = false;
        public bool HasNewUri()
        {
            if (mNewUri)
            {
                mNewUri = false;
                return true;
            }
            return false;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            LastException = null;
            try
            {
                WebResponse response = base.GetWebResponse(request, result);
                mResponseUri = response.ResponseUri;
                mNewUri = true;
                return response;
            }
            catch(Exception err) // note: thats a realy uggly workaround
            {
                LastException = err;
                return null;
            }
        }

        public Exception LastException = null;
    }

    class WebDownloader
    {
        public struct Task {
            public string Url;
            public string Path;
            public string FileName;
            public bool Failed;
        }

        private MyWebClient mWebClient = null;
        private List<Task> mDownloads = null;
        private int mCurrentTask = 0;
        private string mDownloadFileName = "";
        private string mDownloadPath = "";

        public WebDownloader()
        {
            mWebClient = new MyWebClient();
            mWebClient.DownloadProgressChanged += wc_dlProgress;
            mWebClient.DownloadFileCompleted += wd_Finish;
        }

        public bool Download(List<Task> Downloads)
        {
            if (mDownloads != null)
                return false;

            mDownloads = Downloads;
            mCurrentTask = 0;

            DownloadNextFile();
            return true;
        }

        public bool IsBusy()
        {
            return (mDownloads != null);
        }

        public void CancelOperations()
        {
            mWebClient.CancelAsync();
        }

        static public string GetNextTempFile(string path, string baseName)
        {
            for (int i = 0; i < 10000; i++)
            {
                var fi = new FileInfo(path + @"\" + baseName + "_" + i + ".tmp");
                if (!fi.Exists)
                    return baseName + "_" + i;
            }
            return baseName;
        }

        private void DownloadNextFile()
        {
            while (mCurrentTask != -1 && mDownloads.Count > mCurrentTask)
            {
                Task Download = mDownloads[mCurrentTask];
                try
                {
                    if (!Directory.Exists(Download.Path))
                        Directory.CreateDirectory(Download.Path);

                    if (Download.FileName == null)
                    {
                        mDownloadFileName = Path.GetFileName(Download.Url);
                        if (mDownloadFileName.Length == 0 || mDownloadFileName[0] == '?')
                            mDownloadFileName = GetNextTempFile(Download.Path, "Download");
                    }
                    else
                        mDownloadFileName = Download.FileName;

                    mDownloadPath = Download.Path + @"\" + mDownloadFileName + ".tmp";

                    var fi = new FileInfo(mDownloadPath);

                    if (fi.Exists)
                        fi.Delete();
                }
                catch
                {
                    mCurrentTask++;
                    Download.Failed = true;
                    mDownloads[mCurrentTask] = Download;
                    continue;
                }

                mWebClient.DownloadFileAsync(new Uri(Download.Url), mDownloadPath);
                return;
            }

            FinishedEventArgs args = new FinishedEventArgs();
            args.Downloads = mDownloads;
            mDownloads = null;
            Finished?.Invoke(this, args);
        }

        void wc_dlProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            //string cd = mWebClient.ResponseHeaders["Content-Disposition"];
            //string fileName = cd != null ? cd.Substring(cd.IndexOf("filename=") + 9).Replace("\"", "") : "";
            Task Download = mDownloads[mCurrentTask];
            if (Download.FileName == null && mWebClient.HasNewUri())
            {
                if (mWebClient.ResponseUri != null)
                {
                    string FileName = Path.GetFileName(mWebClient.ResponseUri.ToString());
                    if (FileName.Length > 0 && FileName[0] != '?')
                        mDownloadFileName = FileName;
                }
            }

            Progress?.Invoke(this, new ProgressEventArgs(mDownloads.Count, mDownloads.Count == 0 ? 0 : (100 * mCurrentTask + e.ProgressPercentage) / mDownloads.Count, mCurrentTask, e.ProgressPercentage));
        }

        void wd_Finish(object sender, AsyncCompletedEventArgs args)
        {
            Task Download = mDownloads[mCurrentTask];
            if (args.Error != null || args.Cancelled)
            {
                var fi = new FileInfo(Download.Path + @"\" + mDownloadFileName);
                try { fi.Delete(); } catch { } // delete partial file
                if (!args.Cancelled)
                {
                    AppLog.Line(Program.fmt("Download failed: {0}", (mWebClient.LastException != null ? mWebClient.LastException : args.Error).ToString()));
                    Download.Failed = true;
                }
            }
            else
            {
                var fi = new FileInfo(Download.Path + @"\" + mDownloadFileName);
                try
                {
                    if (fi.Exists)
                        fi.Delete();
                    var tfi = new FileInfo(mDownloadPath);
                    tfi.MoveTo(Download.Path + @"\" + mDownloadFileName);

                    Download.FileName = mDownloadFileName;
                }
                catch
                {
                    AppLog.Line(Program.fmt("Failed to rename download {0} to {1}", mDownloadPath, mDownloadFileName));

                    Download.FileName = mDownloadFileName + ".tmp";
                }
            }
            mDownloads[mCurrentTask] = Download;

            if(args.Cancelled)
                mCurrentTask = -1;
            else
                mCurrentTask++;
            DownloadNextFile();
        }

        public class FinishedEventArgs : EventArgs
        {
            public List<Task> Downloads;
            public bool Success
            {
                get {
                    foreach (Task task in Downloads)
                    {
                        if (task.Failed)
                            return false;
                    }
                    return true;
                }
            }
        }
        public event EventHandler<FinishedEventArgs> Finished;

        public class ProgressEventArgs : EventArgs
        {
            public ProgressEventArgs(int TotalFiles, int TotalPercent, int CurrentIndex, int CurrentPercent)
            {
                this.TotalFiles = TotalFiles;
                this.TotalPercent = TotalPercent;
                this.CurrentIndex = CurrentIndex;
                this.CurrentPercent = CurrentPercent;
            }

            public int TotalFiles = 0;
            public int TotalPercent = 0;
            public int CurrentIndex = 0;
            public int CurrentPercent = 0;
        }
        public event EventHandler<ProgressEventArgs> Progress;
    }
}
