using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WUApiLib;//this is required to use the Interfaces given by microsoft. 
using System.Threading;
using System.Windows.Threading;
using System.IO;
using System.Net;
using System.ComponentModel;

namespace wumgr
{
    class WuAgent
    {
        string mVersion = "0.2";

        UpdateSession mUpdateSession = null;
        IUpdateSearcher mUpdateSearcher = null;
        ISearchJob mSearchJob = null;
        UpdateDownloader mDownloader = null;
        IDownloadJob mDownloadJob = null;
        UpdateInstaller mInstaller = null;
        IInstallationJob mInstalationJob = null;

        public IUpdateHistoryEntryCollection mUpdateHistory = null;
        public UpdateCollection mPendingUpdates = null;
        public UpdateCollection mInstalledUpdates = null;
        public UpdateCollection mHiddenUpdates = null;

        public StringCollection mServiceList = new StringCollection();

        private static WuAgent mInstance = null;
        public static WuAgent GetInstance() { return mInstance; }

        public WuAgent()
        {
            mInstance = this;
            mDispatcher = Dispatcher.CurrentDispatcher;
        }

        protected Dispatcher mDispatcher = null;

        public void Init()
        {
            AppLog.Line(Program.fmt("Windows Update Manager, Version v{0} by David Xanatos", mVersion));
            AppLog.Line(Program.fmt("This Tool is Open Source under the GNU General Public License, Version 3\r\n"));

            mUpdateSession = new UpdateSession();
            mUpdateSession.ClientApplicationID = "Windows Update Manager";

            mUpdateServiceManager = new UpdateServiceManager();
            foreach (IUpdateService service in mUpdateServiceManager.Services)
            {
                if (service.Name == "Offline Sync Service")
                    mUpdateServiceManager.RemoveService(service.ServiceID);
                else
                    mServiceList.Add(service.Name);
            }

            mUpdateSearcher = mUpdateSession.CreateUpdateSearcher();

            int count = mUpdateSearcher.GetTotalHistoryCount();
            mUpdateHistory = mUpdateSearcher.QueryHistory(0, count);

            WindowsUpdateAgentInfo info = new WindowsUpdateAgentInfo();
            var currentVersion = info.GetInfo("ApiMajorVersion").ToString().Trim() + "." + info.GetInfo("ApiMinorVersion").ToString().Trim()
                                    + " (" + info.GetInfo("ProductVersionString").ToString().Trim() + ")";

            AppLog.Line(Program.fmt("Windows Update Agent Version: {0}", currentVersion));
        }

        UpdateServiceManager mUpdateServiceManager = null;
        IUpdateService mOfflineService = null;

        private bool SetOffline()
        {
            try
            {
                AppLog.Line(Program.fmt("Setting up 'Offline Sync Service'"));

                // http://go.microsoft.com/fwlink/p/?LinkID=74689
                mOfflineService = mUpdateServiceManager.AddScanPackageService("Offline Sync Service", Directory.GetCurrentDirectory() + @"\wsusscn2.cab");

                mUpdateSearcher.ServerSelection = ServerSelection.ssOthers;
                mUpdateSearcher.ServiceID = mOfflineService.ServiceID;
                mUpdateSearcher.Online = true;
                return true;
            }
            catch (Exception err)
            {
                AppLog.Line(err.Message);
                return false;
            }
        }

        private void SetOnline(string ServiceName)
        {
            mUpdateSearcher.ServerSelection = ServerSelection.ssDefault;
            mUpdateSearcher.ServiceID = "00000000-0000-0000-0000-000000000000";
            foreach (IUpdateService service in mUpdateServiceManager.Services)
            {
                if (service.Name.Equals(ServiceName, StringComparison.CurrentCultureIgnoreCase))
                {
                    mUpdateSearcher.ServerSelection = ServerSelection.ssOthers;
                    mUpdateSearcher.ServiceID = service.ServiceID;
                }

            }
            mUpdateSearcher.Online = true;
        }

        UpdateCallback mCallback = null;
        WebClient mWebClient = null;

        void wc_dlProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Progress(this, new ProgressArgs(1, e.ProgressPercentage, 0, "Downloading", 0));
        }

        void wd_Finish(object sender, AsyncCompletedEventArgs args)
        {
            if (args.Error != null || args.Cancelled)
            {
                mWebClient = null;
                if (args.Error != null)
                {
                    AppLog.Line(Program.fmt("wsusscn2.cab downloaded, failed: {0}", args.Error.ToString()));
                }
                return;
            }

            AppLog.Line(Program.fmt("wsusscn2.cab downloaded, now checking for updates"));

            SetOffline();

            SearchForUpdates();
        }

        public bool SearchForUpdates(String Source = "", bool IncludePotentiallySupersededUpdates = false)
        {
            if (mCallback != null)
                return false;

            mUpdateSearcher.IncludePotentiallySupersededUpdates = IncludePotentiallySupersededUpdates;

            SetOnline(Source);

            SearchForUpdates();
            return true;
        }

        public bool SearchForUpdates(int Download, bool IncludePotentiallySupersededUpdates = false)
        {
            if (mCallback != null)
                return false;

            mUpdateSearcher.IncludePotentiallySupersededUpdates = IncludePotentiallySupersededUpdates;

            if (Download != 0)
            {
                if (mCallback != null || mWebClient != null)
                    return false;

                bool isDownloaded = false;

                var fi = new FileInfo(Directory.GetCurrentDirectory() + @"\wsusscn2.cab");
                if (fi.Exists) {
                    if ((Download == 1 || fi.LastWriteTime < DateTime.Today.Subtract(new TimeSpan(1, 0, 0, 0))))
                    {
                        try
                        {
                            fi.Delete();
                        }
                        catch (Exception ex)
                        {
                            AppLog.Line(Program.fmt("failed to delete {0}", fi.FullName));
                        }
                        return false;
                    }
                    else
                    {
                        isDownloaded = true;
                        AppLog.Line(Program.fmt("up to date wsusscn2.cab is already downloaded"));
                    }
                }

                if (!isDownloaded)
                {
                    AppLog.Line(Program.fmt("downloading wsusscn2.cab"));

                    mWebClient = new WebClient();
                    mWebClient.DownloadFileAsync(new System.Uri("http://go.microsoft.com/fwlink/p/?LinkID=74689"), fi.FullName);
                    mWebClient.DownloadProgressChanged += wc_dlProgress;
                    mWebClient.DownloadFileCompleted += wd_Finish;
                    return true;
                }
            }
            
            SetOffline();
            
            SearchForUpdates();
            return true;
        }

        private void SearchForUpdates()
        {
            Progress(this, new ProgressArgs(-1, 0, 0, "Checking", 0));

            mCallback = new UpdateCallback(this);
            AppLog.Line(Program.fmt("Searching for updates"));
            //for the above search criteria refer to 
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
            mSearchJob = mUpdateSearcher.BeginSearch("(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)", mCallback, null);
        }

        public void CancelOperations()
        {
            if (mWebClient != null)
            {
                mWebClient.CancelAsync();
                mWebClient = null;
            }

            if (mCallback == null)
                return;

            if (mSearchJob != null)
            {
                try
                {
                    mUpdateSearcher.EndSearch(mSearchJob);
                }
                catch (Exception err) { }
                mSearchJob = null;
            }

            if (mOfflineService != null)
            {
                try
                {
                    mUpdateServiceManager.RemoveService(mOfflineService.ServiceID);
                }
                catch (Exception err) { }
                mOfflineService = null;
            }

            if (mDownloadJob != null)
            {
                try
                {
                    mDownloader.EndDownload(mDownloadJob);
                }
                catch (Exception err) { }
                mDownloadJob = null;
            }

            if (mInstalationJob != null)
            {
                try
                {
                    if (mCallback.Install)
                        mInstaller.EndInstall(mInstalationJob);
                    else
                        mInstaller.EndUninstall(mInstalationJob);
                }
                catch (Exception err) { }
                mInstalationJob = null;
            }

            mCallback = null;
        }

        public bool DownloadUpdates(UpdateCollection Updates, bool Install = false)
        {
            if (mCallback != null)
                return false;

            if (mDownloader == null)
                mDownloader = mUpdateSession.CreateUpdateDownloader();

            mDownloader.Updates = new UpdateCollection();
            foreach (IUpdate update in Updates)
            {
                if (update.EulaAccepted == false)
                {
                    update.AcceptEula();
                }
                mDownloader.Updates.Add(update);
            }

            if (mDownloader.Updates.Count == 0)
            {
                AppLog.Line(Program.fmt("No updates selected for download"));
                return false;
            }

            mCallback = new UpdateCallback(this);
            mCallback.Install = Install;
            AppLog.Line(Program.fmt("Downloading Updates... This may take several minutes."));
            mDownloadJob = mDownloader.BeginDownload(mCallback, mCallback, null);
            return true;
        }

        private bool InstallUpdates(UpdateCollection Updates)
        {
            if (mCallback != null)
                return false;

            if (mInstaller == null)
                mInstaller = mUpdateSession.CreateUpdateInstaller() as UpdateInstaller;

            mInstaller.Updates = Updates;

            if (mDownloader.Updates.Count == 0)
            {
                AppLog.Line(Program.fmt("No updates selected or downloaded for instalation"));
                return false;
            }

            mCallback = new UpdateCallback(this);
            AppLog.Line(Program.fmt("Installing Updates... This may take several minutes."));
            mCallback.Install = true;
            mInstalationJob = mInstaller.BeginInstall(mCallback, mCallback, null);
            return true;
        }

        public bool UnInstallUpdates(UpdateCollection Updates)
        {
            if (mInstalationJob != null)
                return false;

            if (mInstaller == null)
                mInstaller = mUpdateSession.CreateUpdateInstaller() as UpdateInstaller;

            mInstaller.Updates = new UpdateCollection();
            foreach (IUpdate update in Updates)
            {
                if (!update.IsUninstallable)
                {
                    AppLog.Line(Program.fmt("Update can not be uninstalled: {0}", update.Title));
                    continue;
                }
                mInstaller.Updates.Add(update);
            }
            if (mDownloader.Updates.Count == 0)
            {
                AppLog.Line(Program.fmt("No updates selected or eligible for uninstallation"));
                return false;
            }

            mCallback = new UpdateCallback(this);
            AppLog.Line(Program.fmt("Removing Updates... This may take several minutes."));
            mCallback.Install = false;
            mInstalationJob = mInstaller.BeginUninstall(mCallback, mCallback, null);
            return true;
        }

        public void RemoveFrom(UpdateCollection Updates, IUpdate Update)
        {
            for (int i = 0; i < Updates.Count; i++)
            {
                if (Updates[i] == Update)
                {
                    Updates.RemoveAt(i);
                    break;
                }
            }
        }

        public void HideUpdates(UpdateCollection Updates, bool Hide)
        {
            foreach (IUpdate update in Updates)
            {
                try
                {
                    update.IsHidden = Hide;
                    if (Hide)
                    {
                        mHiddenUpdates.Add(update);
                        RemoveFrom(mPendingUpdates, update);
                    }
                    else
                    {
                        mPendingUpdates.Add(update);
                        RemoveFrom(mHiddenUpdates, update);
                    }
                }
                catch (Exception err)
                {
                    // Hide update may throw an exception, if the user has hidden the update manually while the search was in progress.
                }
            }
        }

        public class FoundUpdatesArgs : EventArgs
        {
            //public ISearchResult result { get; set; }
        }

        public event EventHandler<FoundUpdatesArgs> Found;

        protected void OnUpdatesFound(ISearchJob searchJob)
        {
            if (searchJob != mSearchJob)
                return;
            mSearchJob = null;
            mCallback = null;

            ISearchResult SearchResults = null;
            try
            {
                SearchResults = mUpdateSearcher.EndSearch(searchJob);

                if (mOfflineService != null)
                {
                    mUpdateServiceManager.RemoveService(mOfflineService.ServiceID);
                    mOfflineService = null;
                }
            }
            catch (Exception err)
            {
                AppLog.Line(Program.fmt("Search for updats failed, Error: {0}", err.Message));
                return;
            }

            mPendingUpdates = new UpdateCollection();
            mInstalledUpdates = new UpdateCollection();
            mHiddenUpdates = new UpdateCollection();

            foreach (IUpdate update in SearchResults.Updates)
            {
                if (safe_IsHidden(update))
                    mHiddenUpdates.Add(update);
                else if (update.IsInstalled)
                    mInstalledUpdates.Add(update);
                else
                    mPendingUpdates.Add(update);         

                Console.WriteLine("\r\n");
            }

            AppLog.Line(Program.fmt("Found {0} pending updates.", mPendingUpdates.Count));

            Progress(this, new ProgressArgs(0, 0, 0, "", 0));

            Found(this, new FoundUpdatesArgs());
        }

        protected void OnUpdatesDownloaded(IDownloadJob downloadJob, bool Install)
        {
            if (downloadJob != mDownloadJob)
                return;
            mDownloadJob = null;
            mCallback = null;

            IDownloadResult DownloadResults = null;
            try
            {
                DownloadResults = mDownloader.EndDownload(downloadJob);
            }
            catch (Exception err)
            {
                AppLog.Line(Program.fmt("Downloading updates failed, Error: {0}", err.Message));
                OnFinished(false);
                return;
            }

            AppLog.Line(Program.fmt("Updates downloaded to %windir%\\SoftwareDistribution\\Download"));

            if (Install)
                InstallUpdates(downloadJob.Updates);
            else
                OnFinished(true);
        }

        protected void OnInstalationCompleted(IInstallationJob installationJob, bool Install)
        {
            if (installationJob != mInstalationJob)
                return;
            mInstalationJob = null;
            mCallback = null;

            IInstallationResult InstallationResults = null;
            try
            {
                if (Install)
                    InstallationResults = mInstaller.EndInstall(installationJob);
                else
                    InstallationResults = mInstaller.EndUninstall(installationJob);
                
            }
            catch (Exception err)
            {
                AppLog.Line(Program.fmt("(Un)Installing updates failed, Error: {0}", err.Message));
                OnFinished(false);
                return;
            }

            if (InstallationResults.ResultCode == OperationResultCode.orcSucceeded)
            {
                AppLog.Line(Program.fmt("Updates (Un)Installed succesfully"));

                if (InstallationResults.RebootRequired == true)
                {
                    AppLog.Line(Program.fmt("Reboot is required for one of more updates."));
                }
            }
            else
            {
                AppLog.Line(Program.fmt("Updates failed to (Un)Install do it manually"));
            }

            OnFinished(InstallationResults.ResultCode == OperationResultCode.orcSucceeded, InstallationResults.RebootRequired);
        }

        public class ProgressArgs : EventArgs
        {
            public ProgressArgs(int TotalUpdates, int TotalPercent, int CurrentIndex, String Title, int UpdatePercent)
            {
                this.TotalUpdates = TotalUpdates;
                this.TotalPercent = TotalPercent;
                this.CurrentIndex = CurrentIndex;
                this.Title = Title;
                this.UpdatePercent = UpdatePercent;
            }

            public ProgressArgs(bool success, bool needReboot)
            {
                if (success)
                    Finished = true;
                else
                    Failed = true;
                RebootNeeded = needReboot;
            }

            public int TotalUpdates = 0;
            public int TotalPercent = 0;
            public int CurrentIndex = 0;
            public String Title = "";
            public int UpdatePercent = 0;
            public bool Finished = false;
            public bool Failed = false;
            public bool RebootNeeded = false;
        }

        public event EventHandler<ProgressArgs> Progress;

        protected void OnProgress(int TotalUpdates, int TotalPercent, int CurrentIndex, String Title, int UpdatePercent)
        {
            Progress(this, new ProgressArgs(TotalUpdates, TotalPercent, CurrentIndex, Title, UpdatePercent));
        }

        protected void OnFinished(bool success, bool needReboot = false)
        {
            Progress(this, new ProgressArgs(success, needReboot));
        }

        class UpdateCallback : ISearchCompletedCallback, IDownloadProgressChangedCallback, IDownloadCompletedCallback, IInstallationProgressChangedCallback, IInstallationCompletedCallback
        {
            private WuAgent agent;
            public bool Install = false;

            public UpdateCallback(WuAgent agent)
            {
                this.agent = agent;
            }

            // Implementation of ISearchCompletedCallback interface...
            public void Invoke(ISearchJob searchJob, ISearchCompletedCallbackArgs e)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnUpdatesFound(searchJob);
                }));
            }

            // Implementation of IDownloadProgressChangedCallback interface...
            public void Invoke(IDownloadJob downloadJob, IDownloadProgressChangedCallbackArgs callbackArgs)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnProgress(downloadJob.Updates.Count, callbackArgs.Progress.PercentComplete, callbackArgs.Progress.CurrentUpdateIndex + 1,
                        downloadJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title, callbackArgs.Progress.CurrentUpdatePercentComplete);
                }));
            }

            // Implementation of IDownloadCompletedCallback interface...
            public void Invoke(IDownloadJob downloadJob, IDownloadCompletedCallbackArgs callbackArgs)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnUpdatesDownloaded(downloadJob, Install);
                }));
            }

            // Implementation of IInstallationProgressChangedCallback interface...
            public void Invoke(IInstallationJob installationJob, IInstallationProgressChangedCallbackArgs callbackArgs)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnProgress(installationJob.Updates.Count, callbackArgs.Progress.PercentComplete, callbackArgs.Progress.CurrentUpdateIndex + 1,
                        installationJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title, callbackArgs.Progress.CurrentUpdatePercentComplete);
                }));
            }

            // Implementation of IInstallationCompletedCallback interface...
            public void Invoke(IInstallationJob installationJob, IInstallationCompletedCallbackArgs callbackArgs)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnInstalationCompleted(installationJob, Install);
                }));
            }
        }

        static public bool safe_IsHidden(IUpdate update)
        {
            try
            {
                return update.IsHidden;
            }
            catch (Exception err)
            {
                return false;
            }
        }

        static public bool safe_IsDownloaded(IUpdate update)
        {
            try
            {
                return update.IsDownloaded;
            }
            catch (Exception err)
            {
                return false;
            }
        }
    }
}
