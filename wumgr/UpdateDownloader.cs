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
        private HttpTask mCurTask = null;

        public UpdateDownloader()
        {
        }

        public bool Download(List<Task> Downloads, List<MsUpdate> Updates = null)
        {
            if (mDownloads != null)
                return false;

            mDownloads = Downloads;
            mCurrentTask = 0;
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
            while (mCurrentTask != -1 && mDownloads.Count > mCurrentTask)
            {
                Task Download = mDownloads[mCurrentTask];
                mCurTask = new HttpTask(Download.Url, Download.Path, Download.FileName, true); // todo update flag
                mCurTask.Progress += OnProgress;
                mCurTask.Finished += OnFinished;
                if (mCurTask.Start())
                    return;
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
            Progress?.Invoke(this, new ProgressEventArgs(mDownloads.Count, mDownloads.Count == 0 ? 0 : (100 * mCurrentTask + args.Percent) / mDownloads.Count, mCurrentTask, args.Percent));
        }

        void OnFinished(object sender, HttpTask.FinishedEventArgs args)
        {
            if (!args.Cancelled)
            {
                Task Download = mDownloads[mCurrentTask];
                if (args.ErrCode != 0)
                {
                    AppLog.Line(MiscFunc.fmt("Download failed: {0}", args.GetError()));
                    if (File.Exists(mCurTask.DlPath + @"\" + mCurTask.DlName))
                        AppLog.Line(MiscFunc.fmt("An older version is present and will be used."));
                    else
                        Download.Failed = true;
                }
                
                Download.FileName = mCurTask.DlName;
                mDownloads[mCurrentTask] = Download;
                mCurTask = null;

                mCurrentTask++;
            }
            else
                mCurrentTask = -1;
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
