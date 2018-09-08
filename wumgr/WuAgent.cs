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
using System.Windows.Forms;
using System.ServiceProcess;

namespace wumgr
{
    class WuAgent
    {
        UpdateSession mUpdateSession = null;
        UpdateServiceManager mUpdateServiceManager = null;
        IUpdateService mOfflineService = null;
        IUpdateSearcher mUpdateSearcher = null;
        ISearchJob mSearchJob = null;
        UpdateDownloader mDownloader = null;
        IDownloadJob mDownloadJob = null;
        UpdateInstaller mInstaller = null;
        IInstallationJob mInstalationJob = null;

        public List<IUpdateHistoryEntry2> mUpdateHistory = null;
        public UpdateCollection mPendingUpdates = null;
        public UpdateCollection mInstalledUpdates = null;
        public UpdateCollection mHiddenUpdates = null;

        public StringCollection mServiceList = new StringCollection();

        private static WuAgent mInstance = null;
        public static WuAgent GetInstance() { return mInstance; }

        WebDownloader mWebDownloader = null;

        public WuAgent()
        {
            mInstance = this;
            mDispatcher = Dispatcher.CurrentDispatcher;

            mWebDownloader = new WebDownloader();
            mWebDownloader.Finished += DownloadsFinished;
            mWebDownloader.Progress += DownloadProgress;

            dlPath = Program.appPath + @"\Downloads";

            WindowsUpdateAgentInfo info = new WindowsUpdateAgentInfo();
            var currentVersion = info.GetInfo("ApiMajorVersion").ToString().Trim() + "." + info.GetInfo("ApiMinorVersion").ToString().Trim() + " (" + info.GetInfo("ProductVersionString").ToString().Trim() + ")";
            AppLog.Line(Program.fmt("Windows Update Agent Version: {0}", currentVersion));

            mUpdateSession = new UpdateSession();
            mUpdateSession.ClientApplicationID = Program.mName;
            mUpdateSession.UserLocale = 1033; // alwys show strings in englisch, we need that to know what each catergory means

            mUpdateServiceManager = new UpdateServiceManager();
        }

        protected Dispatcher mDispatcher = null;

        private string dlPath = null;

        public bool Init()
        {
            if (!LoadServices(true))
                return false;
            
            mUpdateSearcher = mUpdateSession.CreateUpdateSearcher();

            UpdateHistory();
            return true;
        }

        public void UnInit()
        {
            ClearOffline();
        }

        public bool IsActive()
        {
            return mUpdateSearcher != null;
        }

        public bool IsBusy()
        {
            if (mWebDownloader.IsBusy())
                return true;
            return mCurOperation != AgentOperation.None;
        }

        public bool LoadServices(bool cleanUp = false)
        {
            try
            {
                Console.WriteLine("Update Services:");
                mServiceList.Clear();
                foreach (IUpdateService service in mUpdateServiceManager.Services)
                {
                    if (service.Name == mMyOfflineSvc)
                    {
                        if(cleanUp)
                            mUpdateServiceManager.RemoveService(service.ServiceID);
                        continue;
                    }

                    Console.WriteLine(service.Name + ": " + service.ServiceID);
                    //AppLog.Line(service.Name + ": " + service.ServiceID);
                    mServiceList.Add(service.Name);
                }

                return true;
            }
            catch (Exception err)
            {
                if((uint)err.HResult != 0x80070422)
                    LogError(err);
                return false;
            }
        }

        private void LogError(Exception error)
        {
            uint errCode = (uint)error.HResult;
            AppLog.Line("Error: " + WinErrors.GetErrorStr(errCode));
        }

        public static string MsUpdGUID = "7971f918-a847-4430-9279-4a52d1efe18d"; // Microsoft Update
        public static string WinUpdUID = "9482f4b4-e343-43b6-b170-9a65bc822c77"; // Windows Update
        public static string WsUsUID = "3da21691-e39d-4da6-8a4b-b43877bcb1b7"; // Windows Server Update Service

        public static string DCatGUID = "8b24b027-1dee-babb-9a95-3517dfb9c552"; // DCat Flighting Prod - Windows Insider Program
        public static string WinStorGUID = "117cab2d-82b1-4b5a-a08c-4d62dbee7782 "; // Windows Store
        public static string WinStorDCat2GUID = "855e8a7c-ecb4-4ca3-b045-1dfa50104289"; // Windows Store (DCat Prod) - Insider Updates for Store Apps

        public void EnableService(string GUID, bool enable = true)
        {
            if (enable)
                AddService(GUID);
            else
                RemoveService(GUID);
            LoadServices();
        }

        private void AddService(string ID)
        {
            mUpdateServiceManager.AddService2(ID, (int)(tagAddServiceFlag.asfAllowOnlineRegistration | tagAddServiceFlag.asfAllowPendingRegistration | tagAddServiceFlag.asfRegisterServiceWithAU), "");
        }

        private void RemoveService(string ID)
        {
            mUpdateServiceManager.RemoveService(ID);
        }

        public bool TestService(string ID)
        {
            foreach (IUpdateService service in mUpdateServiceManager.Services)
            {
                if (service.ServiceID.Equals(ID))
                    return true;
            }
            return false;
        }

        public string GetServiceName(string ID, bool bAdd = false)
        {
            foreach (IUpdateService service in mUpdateServiceManager.Services)
            {
                if (service.ServiceID.Equals(ID))
                    return service.Name;
            }
            if (bAdd == false)
                return null;
            AddService(ID);
            LoadServices();
            return GetServiceName(ID, false);
        }

        public void UpdateHistory()
        {
            int count = mUpdateSearcher.GetTotalHistoryCount();
            mUpdateHistory = new List<IUpdateHistoryEntry2>();
            if (count == 0)
                return;
            foreach (IUpdateHistoryEntry2 update in mUpdateSearcher.QueryHistory(0, count))
            {
                if (update.Title == null)
                    continue;
                mUpdateHistory.Add(update);
            }
        }

        public string mMyOfflineSvc = "Offline Sync Service";

        private bool SetupOffline()
        {
            try
            {
                if (mOfflineService == null)
                {
                    AppLog.Line(Program.fmt("Setting up 'Offline Sync Service'"));

                    // http://go.microsoft.com/fwlink/p/?LinkID=74689
                    mOfflineService = mUpdateServiceManager.AddScanPackageService(mMyOfflineSvc, dlPath + @"\wsusscn2.cab");
                }

                mUpdateSearcher.ServerSelection = ServerSelection.ssOthers;
                mUpdateSearcher.ServiceID = mOfflineService.ServiceID;
                //mUpdateSearcher.Online = false;
                return true;
            }
            catch (Exception err)
            {
                AppLog.Line(err.Message);
                return false;
            }
        }

        private void ClearOffline()
        {
            if (mOfflineService != null)
            {
                mUpdateServiceManager.RemoveService(mOfflineService.ServiceID);
                mOfflineService = null;
            }
        }

        private void SetOnline(string ServiceName)
        {
            foreach (IUpdateService service in mUpdateServiceManager.Services)
            {
                if (service.Name.Equals(ServiceName, StringComparison.CurrentCultureIgnoreCase))
                {
                    mUpdateSearcher.ServerSelection = ServerSelection.ssDefault;
                    mUpdateSearcher.ServiceID = service.ServiceID;
                    //mUpdateSearcher.Online = true;
                }
            }
        }

        UpdateCallback mCallback = null;

        public enum AgentOperation
        {
            None = 0,
            CheckingUpdates,
            PreparingCheck,
            DownloadingUpdates,
            InstallingUpdates,
            PreparingUpdates,
            RemoveingUpdtes
        };

        private AgentOperation mCurOperation = AgentOperation.None;

        public AgentOperation CurOperation() { return mCurOperation; }

        public bool SearchForUpdates(String Source = "", bool IncludePotentiallySupersededUpdates = false)
        {
            if (IsBusy())
                return false;

            mUpdateSearcher.IncludePotentiallySupersededUpdates = IncludePotentiallySupersededUpdates;

            SetOnline(Source);

            SearchForUpdates();
            return true;
        }

        public bool SearchForUpdates(int Download, bool IncludePotentiallySupersededUpdates = false)
        {
            if (IsBusy())
                return false;

            mUpdateSearcher.IncludePotentiallySupersededUpdates = IncludePotentiallySupersededUpdates;

            mCurOperation = AgentOperation.PreparingCheck;

            if (Download != 0)
            {
                bool isDownloaded = false;

                if (!Directory.Exists(dlPath))
                    Directory.CreateDirectory(dlPath);

                var fi = new FileInfo(dlPath + @"\wsusscn2.cab");
                if (fi.Exists) {
                    if (!(Download == 1 || fi.LastWriteTime < DateTime.Today.Subtract(new TimeSpan(1, 0, 0, 0))))
                    {
                        isDownloaded = true;
                        AppLog.Line(Program.fmt("up to date wsusscn2.cab is already downloaded"));
                    }
                }

                if (!isDownloaded)
                {
                    OnProgress(-1, 0, 0, 0);

                    AppLog.Line(Program.fmt("downloading wsusscn2.cab"));

                    List<WebDownloader.Task> downloads = new List<WebDownloader.Task>();
                    WebDownloader.Task download = new WebDownloader.Task();
                    download.Url = "http://go.microsoft.com/fwlink/p/?LinkID=74689";
                    download.Path = dlPath;
                    download.FileName = "wsusscn2.cab";
                    downloads.Add(download); 
                    if(!mWebDownloader.Download(downloads))
                    {
                        mCurOperation = AgentOperation.None;
                        return false;
                    }
                    return true;
                }
            }

            SetupOffline();
            
            SearchForUpdates();
            return true;
        }

        private void SearchForUpdates()
        {
            mCurOperation = AgentOperation.CheckingUpdates;

            OnProgress(-1, 0, 0, 0);

            mCallback = new UpdateCallback(this);

            AppLog.Line(Program.fmt("Searching for updates"));
            //for the above search criteria refer to 
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
            mSearchJob = mUpdateSearcher.BeginSearch("(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)", mCallback, null);
        }

        public void CancelOperations()
        {
            mWebDownloader.CancelOperations();

            if (mSearchJob != null)
            {
                try
                {
                    mUpdateSearcher.EndSearch(mSearchJob);
                }
                catch { }
                mSearchJob = null;
            }

            if (mDownloadJob != null)
            {
                try
                {
                    mDownloader.EndDownload(mDownloadJob);
                }
                catch { }
                mDownloadJob = null;
            }

            if (mInstalationJob != null)
            {
                try
                {
                    if (mCurOperation == AgentOperation.InstallingUpdates)
                        mInstaller.EndInstall(mInstalationJob);
                    else if (mCurOperation == AgentOperation.RemoveingUpdtes)
                        mInstaller.EndUninstall(mInstalationJob);
                }
                catch { }
                mInstalationJob = null;
            }

            mCurOperation = AgentOperation.None;

            mCallback = null;
        }

        public bool DownloadUpdatesOffline(UpdateCollection Updates, bool Install = false)
        {
            if (IsBusy())
                return false;

            mCurOperation = Install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;

            OnProgress(-1, 0, 0, 0);

            List<WebDownloader.Task> downloads = new List<WebDownloader.Task>();
            foreach (IUpdate update in Updates)
            {
                string KB = update.KBArticleIDs.Count > 0 ? "KB" + update.KBArticleIDs[0] : "KBUnknown";
                int Counter = 0;
                foreach (IUpdate bundle in update.BundledUpdates)
                {
                    foreach (IUpdateDownloadContent udc in bundle.DownloadContents)
                    {
                        if (String.IsNullOrEmpty(udc.DownloadUrl))
                            continue;
                        Counter++;
                        WebDownloader.Task download = new WebDownloader.Task();
                        download.Url = udc.DownloadUrl;
                        download.Path = dlPath + @"\" + KB;
                        downloads.Add(download);
                    }
                }
                if (Counter == 0)
                    AppLog.Line(Program.fmt("Error: No Download Url's found for update {0}", update.Title));
            }
            /*WebDownloader.Task download = new WebDownloader.Task();
            //download.Url = "http://download.windowsupdate.com/d/msdownload/update/software/secu/2018/08/windows10.0-kb4343902-x64_346f95cde08164057941d1182e28cf35ff6dfca7.cab";
            download.Url = "http://go.microsoft.com/fwlink/p/?LinkID=74689";
            download.Path = dlPath + @"\" + "KB1234567890";
            downloads.Add(download);*/
            return mWebDownloader.Download(downloads);
        }

        void DownloadsFinished(object sender, WebDownloader.FinishedEventArgs args)
        {
            if (mCurOperation == AgentOperation.CheckingUpdates)
            {
                AppLog.Line(Program.fmt("wsusscn2.cab downloaded"));

                ClearOffline();
                SetupOffline();

                SearchForUpdates();
            }
            else if (mCurOperation == AgentOperation.InstallingUpdates)
            {
                // TODO:
                MessageBox.Show("\"Manual\" update instalation is not yet implemented.\r\nYou have to install the downloaded updates manually");
                AppLog.Line(Program.fmt("Updates downloaded to {0}", dlPath));
                OnFinished(args.Success);
            }
            else
            {
                AppLog.Line(Program.fmt("Updates downloaded to {0}", dlPath));
                OnFinished(args.Success);
            }
        }

        void DownloadProgress(object sender, WebDownloader.ProgressEventArgs args)
        {
            OnProgress(args.TotalFiles, args.TotalPercent, args.CurrentIndex, args.CurrentPercent);
        }

        public bool DownloadUpdates(UpdateCollection Updates, bool Install = false)
        {
            if (IsBusy())
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

            mCurOperation = Install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;

            OnProgress(-1, 0, 0, 0);

            mCallback = new UpdateCallback(this);

            AppLog.Line(Program.fmt("Downloading Updates... This may take several minutes."));
            mDownloadJob = mDownloader.BeginDownload(mCallback, mCallback, null);
            return true;
        }

        private bool InstallUpdates(UpdateCollection Updates)
        {
            if (IsBusy())
                return false;

            if (mInstaller == null)
                mInstaller = mUpdateSession.CreateUpdateInstaller() as UpdateInstaller;

            mInstaller.Updates = Updates;

            if (mDownloader.Updates.Count == 0)
            {
                AppLog.Line(Program.fmt("No updates selected or downloaded for instalation"));
                return false;
            }

            mCurOperation = AgentOperation.InstallingUpdates;

            OnProgress(-1, 0, 0, 0);

            mCallback = new UpdateCallback(this);

            AppLog.Line(Program.fmt("Installing Updates... This may take several minutes."));
            mInstalationJob = mInstaller.BeginInstall(mCallback, mCallback, null);
            return true;
        }

        public bool UnInstallUpdates(UpdateCollection Updates)
        {
            if (IsBusy())
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

            mCurOperation = AgentOperation.RemoveingUpdtes;

            OnProgress(-1, 0, 0, 0);

            mCallback = new UpdateCallback(this);

            AppLog.Line(Program.fmt("Removing Updates... This may take several minutes."));
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
                catch { } // Hide update may throw an exception, if the user has hidden the update manually while the search was in progress.
            }
        }

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
            }
            catch (Exception err)
            {
                AppLog.Line(Program.fmt("Search for updats failed, Error: {0}", WinErrors.GetErrorStr((uint)err.HResult)));
                OnFinished(false);
                return;
            }

            mPendingUpdates = new UpdateCollection();
            mInstalledUpdates = new UpdateCollection();
            mHiddenUpdates = new UpdateCollection();

            foreach (IUpdate update in SearchResults.Updates)
            {
                if (update.IsHidden)
                    mHiddenUpdates.Add(update);
                else if (update.IsInstalled)
                    mInstalledUpdates.Add(update);
                else
                    mPendingUpdates.Add(update);

                Console.WriteLine(update.Title);
                foreach (IUpdate bundle in update.BundledUpdates)
                {
                    foreach (IUpdateDownloadContent udc in bundle.DownloadContents)
                    {
                        if (String.IsNullOrEmpty(udc.DownloadUrl))
                            continue;

                        Console.WriteLine(udc.DownloadUrl);
                    }
                }
                Console.WriteLine("");
            }

            AppLog.Line(Program.fmt("Found {0} pending updates.", mPendingUpdates.Count));

            OnFinished(SearchResults.ResultCode == OperationResultCode.orcSucceeded, false, true);
        }

        protected void OnUpdatesDownloaded(IDownloadJob downloadJob)
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
                AppLog.Line(Program.fmt("Downloading updates failed, Error: {0}", WinErrors.GetErrorStr((uint)err.HResult)));
                OnFinished(false);
                return;
            }

            if (mCurOperation == AgentOperation.PreparingUpdates)
                InstallUpdates(downloadJob.Updates);
            else
            {
                AppLog.Line(Program.fmt("Updates downloaded to %windir%\\SoftwareDistribution\\Download"));
                OnFinished(DownloadResults.ResultCode == OperationResultCode.orcSucceeded);
            }
        }

        protected void OnInstalationCompleted(IInstallationJob installationJob)
        {
            if (installationJob != mInstalationJob)
                return;
            mInstalationJob = null;
            mCallback = null;

            IInstallationResult InstallationResults = null;
            try
            {
                if (mCurOperation == AgentOperation.InstallingUpdates)
                    InstallationResults = mInstaller.EndInstall(installationJob);
                else if (mCurOperation == AgentOperation.RemoveingUpdtes)
                    InstallationResults = mInstaller.EndUninstall(installationJob);
                
            }
            catch (Exception err)
            {
                AppLog.Line(Program.fmt("(Un)Installing updates failed, Error: {0}", WinErrors.GetErrorStr((uint)err.HResult)));
                OnFinished(false);
                return;
            }

            if (InstallationResults.ResultCode == OperationResultCode.orcSucceeded)
            {
                AppLog.Line(Program.fmt("Updates (Un)Installed succesfully"));

                if (InstallationResults.RebootRequired == true)
                    AppLog.Line(Program.fmt("Reboot is required for one of more updates"));
            }
            else
                AppLog.Line(Program.fmt("Updates failed to (Un)Install"));

            OnFinished(InstallationResults.ResultCode == OperationResultCode.orcSucceeded, InstallationResults.RebootRequired);
        }


        public void EnableWuAuServ(bool enable = true)
        {
            ServiceController svc = new ServiceController("wuauserv");
            try
            {
                if (enable)
                {
                    if (svc.Status != ServiceControllerStatus.Running)
                    {
                        ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Manual);
                        svc.Start();
                    }
                }
                else
                {
                    if (svc.Status == ServiceControllerStatus.Running)
                        svc.Stop();
                    ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Disabled);
                }
            }
            catch (Exception err)
            {
                AppLog.Line("Error: " + err.Message);
            }
            svc.Close();
        }

        public class ProgressArgs : EventArgs
        {
            public ProgressArgs(int TotalUpdates, int TotalPercent, int CurrentIndex, int UpdatePercent, String Info)
            {
                this.TotalUpdates = TotalUpdates;
                this.TotalPercent = TotalPercent;
                this.CurrentIndex = CurrentIndex;
                this.UpdatePercent = UpdatePercent;
                this.Info = Info;
            }

            public int TotalUpdates = 0;
            public int TotalPercent = 0;
            public int CurrentIndex = 0;
            public int UpdatePercent = 0;
            public String Info = "";
        }

        public event EventHandler<ProgressArgs> Progress;

        protected void OnProgress(int TotalUpdates, int TotalPercent, int CurrentIndex, int UpdatePercent, String Info = "")
        {
            Progress?.Invoke(this, new ProgressArgs(TotalUpdates, TotalPercent, CurrentIndex, UpdatePercent, Info));
        }

        public class FinishedArgs : EventArgs
        {
            public FinishedArgs(bool success, bool needReboot = false, bool foundUpdates = false)
            {
                Success = success;
                RebootNeeded = needReboot;
                FoundUpdates = foundUpdates;
            }

            public bool Success = false;
            public bool FoundUpdates = false;
            public bool RebootNeeded = false;
        }
        public event EventHandler<FinishedArgs> Finished;

        protected void OnFinished(bool success, bool needReboot = false, bool foundUpdates = false)
        {
            mCurOperation = AgentOperation.None;

            Finished?.Invoke(this, new FinishedArgs(success, needReboot, foundUpdates));
        }

        class UpdateCallback : ISearchCompletedCallback, IDownloadProgressChangedCallback, IDownloadCompletedCallback, IInstallationProgressChangedCallback, IInstallationCompletedCallback
        {
            private WuAgent agent;

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
                        callbackArgs.Progress.CurrentUpdatePercentComplete, downloadJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title);
                }));
            }

            // Implementation of IDownloadCompletedCallback interface...
            public void Invoke(IDownloadJob downloadJob, IDownloadCompletedCallbackArgs callbackArgs)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnUpdatesDownloaded(downloadJob);
                }));
            }

            // Implementation of IInstallationProgressChangedCallback interface...
            public void Invoke(IInstallationJob installationJob, IInstallationProgressChangedCallbackArgs callbackArgs)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnProgress(installationJob.Updates.Count, callbackArgs.Progress.PercentComplete, callbackArgs.Progress.CurrentUpdateIndex + 1,
                        callbackArgs.Progress.CurrentUpdatePercentComplete, installationJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title);
                }));
            }

            // Implementation of IInstallationCompletedCallback interface...
            public void Invoke(IInstallationJob installationJob, IInstallationCompletedCallbackArgs callbackArgs)
            {
                // !!! warning this function is invoced from a different thread !!!            
                agent.mDispatcher.Invoke(new Action(() => {
                    agent.OnInstalationCompleted(installationJob);
                }));
            }
        }
    }
}
