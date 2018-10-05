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

namespace wumgr
{
    class UpdateDownloader
    {
        public struct Task {
            public string Url;
            public string Path;
            public string FileName;
            public bool Failed;
            public string KB;
        }

        private List<Task> mDownloads = null;
        private List<MsUpdate> mUpdates = null;
        private int mCurrentTask = 0;
        private bool Canceled = false;
        private HttpTask mCurTask = null;
        private string mInfo = "";

        public UpdateDownloader()
        {
        }

        public bool Download(List<Task> Downloads, List<MsUpdate> Updates = null)
        {
            if (mDownloads != null)
                return false;

            Canceled = false;
            mDownloads = Downloads;
            mCurrentTask = 0;
            mInfo = "";
            mUpdates = Updates;

            DownloadNextFile();
            return true;
        }

        public bool IsBusy()
        {
            return (mDownloads != null);
        }

        public void CancelOperations()
        {
            if(mCurTask != null)
                mCurTask.Cancel();
        }

        private void DownloadNextFile()
        {
            while (!Canceled && mDownloads.Count > mCurrentTask)
            {
                Task Download = mDownloads[mCurrentTask];

                if (mUpdates != null)
                {
                    foreach (MsUpdate update in mUpdates)
                    {
                        if (update.KB.Equals(Download.KB))
                        {
                            mInfo = update.Title;
                            break;
                        }
                    }
                }

                mCurTask = new HttpTask(Download.Url, Download.Path, Download.FileName, true); // todo update flag
                mCurTask.Progress += OnProgress;
                mCurTask.Finished += OnFinished;
                if (mCurTask.Start())
                    return;
                // Failedto start this task lets try an otehr one
                mCurrentTask++;
            }

            FinishedEventArgs args = new FinishedEventArgs();
            args.Downloads = mDownloads;
            mDownloads = null;
            args.Updates = mUpdates;
            mUpdates = null;
            Finished?.Invoke(this, args);
        }

        void OnProgress(object sender, HttpTask.ProgressEventArgs args)
        {
            Progress?.Invoke(this, new WuAgent.ProgressArgs(mDownloads.Count, mDownloads.Count == 0 ? 0 : (100 * mCurrentTask + args.Percent) / mDownloads.Count, mCurrentTask + 1, args.Percent, mInfo));
        }

        void OnFinished(object sender, HttpTask.FinishedEventArgs args)
        {
            if (!args.Cancelled)
            {
                Task Download = mDownloads[mCurrentTask];
                if (!args.Success)
                {
                    AppLog.Line("Download failed: {0}", args.GetError());
                    if (mCurTask.DlName != null && File.Exists(mCurTask.DlPath + @"\" + mCurTask.DlName))
                        AppLog.Line("An older version is present and will be used.");
                    else
                        Download.Failed = true;
                }

                Download.FileName = mCurTask.DlName;
                mDownloads[mCurrentTask] = Download;
                mCurTask = null;

                mCurrentTask++;
            }
            else
                Canceled = true;
            DownloadNextFile();
        }

        public class FinishedEventArgs : EventArgs
        {
            public List<Task> Downloads;
            public List<MsUpdate> Updates;
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

        public event EventHandler<WuAgent.ProgressArgs> Progress;
    }
}
