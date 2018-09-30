using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace wumgr
{
    class UpdateInstaller
    {
        List<MsUpdate> mUpdates = null;
        private MultiValueDictionary<string, string> mAllFiles = null;
        private int mCurrentTask = 0;
        private Thread mThread = null;
        private Dispatcher mDispatcher;

        public UpdateInstaller()
        {
            mDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool Install(List<MsUpdate> Updates, MultiValueDictionary<string, string> AllFiles)
        {
            /*mUpdates = Updates;
            mAllFiles = AllFiles;
            mCurrentTask = 0;

            InstallNextFile();
            return true;*/
            AppLog.Line("\"Manual\" update instalation is not yet implemented.");
            return false;
        }

        public bool UnInstall(List<MsUpdate> Updates)
        {
            AppLog.Line("\"Manual\" update deinstalation is not yet implemented.");
            return false;
        }

        public bool IsBusy()
        {
            return mUpdates != null;
        }

        public void CancelOperations()
        {
            // TODO
        }

        private void InstallNextFile()
        {
            while (mCurrentTask != -1 && mAllFiles.GetCount() > mCurrentTask)
            {
                string File = mAllFiles.GetAt(mCurrentTask);

                mThread = new Thread(new ParameterizedThreadStart(Run));
                mThread.Start(File);
            }

            FinishedEventArgs args = new FinishedEventArgs();
            //args.AllFiles = mAllFiles;
            mAllFiles = null;
            args.Updates = mUpdates;
            mUpdates = null;
            Finished?.Invoke(this, args);
        }

        public void Run(object parameters)
        {
            string File = (string)parameters;

            string ext = Path.GetExtension(File);
            if (ext.Equals(".exe", StringComparison.CurrentCultureIgnoreCase))
                ;
            else if (ext.Equals(".msi", StringComparison.CurrentCultureIgnoreCase))
                ;
            else if (ext.Equals(".msu", StringComparison.CurrentCultureIgnoreCase))
                ;
            else if (ext.Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
                ;
            else if (ext.Equals(".cab", StringComparison.CurrentCultureIgnoreCase))
                ;
            else
                AppLog.Line("Unknown Update format: {0}", ext);
            // TODO do install

            mDispatcher.BeginInvoke(new Action(() => {
                Finish();
            }));
        }

        private void Finish()
        {
            mThread.Join();
            mThread = null;

            mCurrentTask++;

            int Percent = 0; // TODO if possible get progress of individual updates
            Progress?.Invoke(this, new ProgressEventArgs(mAllFiles.Count, mAllFiles.Count == 0 ? 0 : (100 * mCurrentTask + Percent) / mAllFiles.Count, mCurrentTask, Percent));

            InstallNextFile();
        }

        public class FinishedEventArgs : EventArgs
        {
            public List<MsUpdate> Updates;
            public bool Success = true;
            public bool Reboot = false;
            //public MultiValueDictionary<string, string> AllFiles;
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
