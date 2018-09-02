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
            WebResponse response = base.GetWebResponse(request, result);
            mResponseUri = response.ResponseUri;
            mNewUri = true;
            return response;
        }
    }

    class WebDownloader
    {
        public struct Task {
            public string Url;
            public string Path;
            public string FileName;
        }
        private string mDownloadFileName = "";
        private string mDownloadPath = "";

        MyWebClient mWebClient = null;
        List<Task> mDownloads = null;
        int mCurrentTask = 0;

        public WebDownloader()
        {
            mWebClient = new MyWebClient();
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
            if (mDownloads.Count > mCurrentTask)
            {
                Task Download = mDownloads[mCurrentTask];

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
                // TODO: try catch
                if (!fi.Exists)
                    fi.Delete();

                mWebClient.DownloadFileAsync(new Uri(Download.Url), mDownloadPath);
                mWebClient.DownloadProgressChanged += wc_dlProgress;
                mWebClient.DownloadFileCompleted += wd_Finish;
            }
            else
            {
                Finished?.Invoke(this, new FinishedEventArgs());
            }
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
            
            // TODO: progress
        }

        void wd_Finish(object sender, AsyncCompletedEventArgs args)
        {
            if (args.Error != null || args.Cancelled)
            {
                mWebClient = null;
                if (args.Error != null)
                {
                    // TODO: add more info
                    AppLog.Line(Program.fmt("Download failed: {0}", args.Error.ToString()));
                }
            }
            else
            {
                Task Download = mDownloads[mCurrentTask];

                var fi = new FileInfo(Download.Path + @"\" + mDownloadFileName);
                if (fi.Exists)
                {
                    // TODO: try catch
                    fi.Delete();
                }

                var tfi = new FileInfo(mDownloadPath);
                // TODO: try catch
                tfi.MoveTo(Download.Path + @"\" + mDownloadFileName);
            }

            mCurrentTask++;
            DownloadNextFile();
        }

        public class FinishedEventArgs : EventArgs {}
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
