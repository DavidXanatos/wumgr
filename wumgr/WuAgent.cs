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
using System.Collections.Specialized;

namespace wumgr
{
    class WuAgent
    {
        UpdateSession mUpdateSession = null;
        UpdateServiceManager mUpdateServiceManager = null;
        IUpdateService mOfflineService = null;
        IUpdateSearcher mUpdateSearcher = null;
        ISearchJob mSearchJob = null;
        WUApiLib.UpdateDownloader mDownloader = null;
        IDownloadJob mDownloadJob = null;
        IUpdateInstaller mInstaller = null;
        IInstallationJob mInstalationJob = null;

        public List<MsUpdate> mUpdateHistory = new List<MsUpdate>();
        public List<MsUpdate> mPendingUpdates = new List<MsUpdate>();
        public List<MsUpdate> mInstalledUpdates = new List<MsUpdate>();
        public List<MsUpdate> mHiddenUpdates = new List<MsUpdate>();

        public System.Collections.Specialized.StringCollection mServiceList = new System.Collections.Specialized.StringCollection();

        private static WuAgent mInstance = null;
        public static WuAgent GetInstance() { return mInstance; }

        UpdateDownloader mUpdateDownloader = null;
        UpdateInstaller mUpdateInstaller = null;

        public WuAgent()
        {
            mInstance = this;
            mDispatcher = Dispatcher.CurrentDispatcher;

            mUpdateDownloader = new UpdateDownloader();
            mUpdateDownloader.Finished += DownloadsFinished;
            mUpdateDownloader.Progress += DownloadProgress;


            mUpdateInstaller = new UpdateInstaller();
            mUpdateInstaller.Finished += InstallFinished;
            mUpdateInstaller.Progress += InstallProgress;

            dlPath = Program.appPath + @"\Downloads";

            WindowsUpdateAgentInfo info = new WindowsUpdateAgentInfo();
            var currentVersion = info.GetInfo("ApiMajorVersion").ToString().Trim() + "." + info.GetInfo("ApiMinorVersion").ToString().Trim() + " (" + info.GetInfo("ProductVersionString").ToString().Trim() + ")";
            AppLog.Line(MiscFunc.fmt("Windows Update Agent Version: {0}", currentVersion));

            mUpdateSession = new UpdateSession();
            mUpdateSession.ClientApplicationID = Program.mName;
            //mUpdateSession.UserLocale = 1033; // alwys show strings in englisch

            mUpdateServiceManager = new UpdateServiceManager();

            if(MiscFunc.parseInt(Program.IniReadValue("Options", "LoadLists", "0")) != 0)
                LoadUpdates();
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

            mUpdateSearcher = null;
        }

        public bool IsActive()
        {
            return mUpdateSearcher != null;
        }

        public bool IsBusy()
        {
            if (mUpdateDownloader.IsBusy())
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
            AppLog.Line(MiscFunc.fmt("Error 0x{0}: {1}", errCode.ToString("X").PadLeft(8,'0'), UpdateErrors.GetErrorStr(errCode)));
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
            mUpdateHistory.Clear();
            int count = mUpdateSearcher.GetTotalHistoryCount();
            if (count == 0) // sanity check
                return;
            foreach (IUpdateHistoryEntry2 update in mUpdateSearcher.QueryHistory(0, count))
            {
                if (update.Title == null) // sanity check
                    continue;
                mUpdateHistory.Add(new MsUpdate(update));
            }
        }

        public string mMyOfflineSvc = "Offline Sync Service";

        private bool SetupOffline()
        {
            try
            {
                if (mOfflineService == null)
                {
                    AppLog.Line(MiscFunc.fmt("Setting up 'Offline Sync Service'"));

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

        private bool mIsValid = false;
        public bool IsValid() { return mIsValid; }

        private void ClearOffline()
        {
            if (mOfflineService != null)
            {
                // note: if we keep references to updates reffering to an removed service we may got a crash
                foreach (MsUpdate Update in mUpdateHistory)
                    Update.Invalidate();
                foreach (MsUpdate Update in mPendingUpdates)
                    Update.Invalidate();
                foreach (MsUpdate Update in mInstalledUpdates)
                    Update.Invalidate();
                foreach (MsUpdate Update in mHiddenUpdates)
                    Update.Invalidate();
                mIsValid = false;

                OnUpdatesChanged();

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
            if (mCallback != null)
                return false;

            mUpdateSearcher.IncludePotentiallySupersededUpdates = IncludePotentiallySupersededUpdates;

            SetOnline(Source);

            SearchForUpdates();
            return true;
        }

        public bool SearchForUpdates(bool Download, bool IncludePotentiallySupersededUpdates = false)
        {
            if (IsBusy()) // if (mCallback != null)
                return false;

            mUpdateSearcher.IncludePotentiallySupersededUpdates = IncludePotentiallySupersededUpdates;

            mCurOperation = AgentOperation.PreparingCheck;

            if (Download)
            {
                OnProgress(-1, 0, 0, 0);

                AppLog.Line(MiscFunc.fmt("downloading wsusscn2.cab"));

                List<UpdateDownloader.Task> downloads = new List<UpdateDownloader.Task>();
                UpdateDownloader.Task download = new UpdateDownloader.Task();
                download.Url = Program.IniReadValue("Options", "OfflineCab", "http://go.microsoft.com/fwlink/p/?LinkID=74689");
                download.Path = dlPath;
                download.FileName = "wsusscn2.cab";
                downloads.Add(download); 
                if(!mUpdateDownloader.Download(downloads))
                {
                    mCurOperation = AgentOperation.None;
                    return false;
                }
                return true;
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

            AppLog.Line(MiscFunc.fmt("Searching for updates"));
            //for the above search criteria refer to 
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
            mSearchJob = mUpdateSearcher.BeginSearch("(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)", mCallback, null);
        }

        public IUpdate FindUpdate(string UUID)
        {
            if (mUpdateSearcher == null)
                return null;
            // Note: this is sloooow!
            ISearchResult result = mUpdateSearcher.Search("UpdateID = '" + UUID + "'");
            if (result.Updates.Count > 0)
                return result.Updates[0];
            return null;
        }

        public void CancelOperations()
        {
            mUpdateDownloader.CancelOperations();
            mUpdateInstaller.CancelOperations();

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

        public bool DownloadUpdatesOffline(List<MsUpdate> Updates, bool Install = false)
        {
            if (mUpdateDownloader.IsBusy())
                return false;

            mCurOperation = Install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;
            
            List<UpdateDownloader.Task> downloads = new List<UpdateDownloader.Task>();
            foreach (MsUpdate Update in Updates)
            {
                if (Update.Downloads.Count == 0)
                {
                    AppLog.Line(MiscFunc.fmt("Error: No Download Url's found for update {0}", Update.Title));
                    continue;
                }

                foreach (string url in Update.Downloads)
                {
                    UpdateDownloader.Task download = new UpdateDownloader.Task();
                    download.Url = url;
                    download.Path = dlPath + @"\" + Update.KB;
                    download.KB = Update.KB;
                    downloads.Add(download);
                }
            }

            if (mUpdateDownloader.Download(downloads, Updates))
            {
                OnProgress(-1, 0, 0, 0);
                return true;
            }
            return false;
        }

        private bool InstallUpdatesOffline(List<MsUpdate> Updates, MultiValueDictionary<string, string> AllFiles)
        {
            if (mUpdateInstaller.IsBusy())
                return false;

            mCurOperation = AgentOperation.InstallingUpdates;

            if (mUpdateInstaller.Install(Updates, AllFiles))
            {
                OnProgress(-1, 0, 0, 0);
                return true;
            }
            return false;
        }


        public bool UnInstallUpdatesOffline(List<MsUpdate> Updates)
        {
            if (mUpdateInstaller.IsBusy())
                return false;

            mCurOperation = AgentOperation.RemoveingUpdtes;

            if (mUpdateInstaller.UnInstall(Updates))
            {
                OnProgress(-1, 0, 0, 0);
                return true;
            }
            return false;
        }

        void DownloadsFinished(object sender, UpdateDownloader.FinishedEventArgs args)
        {
            if (mCurOperation == AgentOperation.PreparingCheck)
            {
                AppLog.Line(MiscFunc.fmt("wsusscn2.cab downloaded"));

                ClearOffline();
                SetupOffline();

                SearchForUpdates();
            }
            else
            {
                MultiValueDictionary<string, string> AllFiles = new MultiValueDictionary<string, string>();
                foreach (UpdateDownloader.Task task in args.Downloads)
                    AllFiles.Add(task.KB, task.Path + @"\" + task.FileName);

                // TODO
                /*string INIPath = dlPath + @"\updates.ini";
                foreach (string KB in AllFiles.Keys)
                {
                    string Files = "";
                    foreach (string FileName in AllFiles.GetValues(KB))
                    {
                        if (Files.Length > 0)
                            Files += "|";
                        Files += FileName;
                    }
                    Program.IniWriteValue(KB, "Files", Files, INIPath);
                }*/

                if (mCurOperation != AgentOperation.PreparingUpdates)
                    AppLog.Line(MiscFunc.fmt("Updates downloaded to {0}", dlPath));
                else if (InstallUpdatesOffline(args.Updates, AllFiles))
                    return;

                OnFinished(args.Success);
            }
        }

        void DownloadProgress(object sender, UpdateDownloader.ProgressEventArgs args)
        {
            OnProgress(args.TotalFiles, args.TotalPercent, args.CurrentIndex, args.CurrentPercent);
        }

        void InstallFinished(object sender, UpdateInstaller.FinishedEventArgs args)
        {
            if (args.Success)
            {
                AppLog.Line(MiscFunc.fmt("Updates (Un)Installed succesfully"));

                foreach (MsUpdate Update in args.Updates)
                {
                    if (mCurOperation == AgentOperation.InstallingUpdates)
                    {
                        if (RemoveFrom(mPendingUpdates, Update))
                        {
                            Update.Attributes |= (int)MsUpdate.UpdateAttr.Installed;
                            mInstalledUpdates.Add(Update);
                        }
                    }
                    else if (mCurOperation == AgentOperation.RemoveingUpdtes)
                    {
                        if (RemoveFrom(mInstalledUpdates, Update))
                        {
                            Update.Attributes &= ~(int)MsUpdate.UpdateAttr.Installed;
                            mPendingUpdates.Add(Update);
                        }
                    }
                }

                if (args.Reboot)
                    AppLog.Line(MiscFunc.fmt("Reboot is required for one of more updates"));
            }
            else
                AppLog.Line(MiscFunc.fmt("Updates failed to (Un)Install"));

            OnUpdatesChanged();

            OnFinished(args.Success, args.Reboot);
        }

        void InstallProgress(object sender, UpdateInstaller.ProgressEventArgs args)
        {
            OnProgress(args.TotalFiles, args.TotalPercent, args.CurrentIndex, args.CurrentPercent);
        }

        public bool DownloadUpdates(List<MsUpdate> Updates, bool Install = false)
        {
            if (mCallback != null)
                return false;

            if (mDownloader == null)
                mDownloader = mUpdateSession.CreateUpdateDownloader();

            mDownloader.Updates = new UpdateCollection();
            foreach (MsUpdate Update in Updates)
            {
                IUpdate update = Update.GetUpdate();
                if (update == null)
                    continue;

                if (update.EulaAccepted == false)
                {
                    update.AcceptEula();
                }
                mDownloader.Updates.Add(update);
            }

            if (mDownloader.Updates.Count == 0)
            {
                AppLog.Line(MiscFunc.fmt("No updates selected for download"));
                return false;
            }

            mCurOperation = Install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;

            OnProgress(-1, 0, 0, 0);

            mCallback = new UpdateCallback(this);

            AppLog.Line(MiscFunc.fmt("Downloading Updates... This may take several minutes."));
            mDownloadJob = mDownloader.BeginDownload(mCallback, mCallback, Updates);
            return true;
        }

        private bool InstallUpdates(List<MsUpdate> Updates)
        {
            if (mCallback != null)
                return false;

            if (mInstaller == null)
                mInstaller = mUpdateSession.CreateUpdateInstaller() as IUpdateInstaller;

            mInstaller.Updates = new UpdateCollection();
            foreach (MsUpdate Update in Updates)
            {
                IUpdate update = Update.GetUpdate();
                if (update == null)
                    continue;

                mInstaller.Updates.Add(update);
            }

            if (mInstaller.Updates.Count == 0)
            {
                AppLog.Line(MiscFunc.fmt("No updates selected for instalation"));
                return false;
            }

            mCurOperation = AgentOperation.InstallingUpdates;

            OnProgress(-1, 0, 0, 0);

            mCallback = new UpdateCallback(this);

            AppLog.Line(MiscFunc.fmt("Installing Updates... This may take several minutes."));
            mInstalationJob = mInstaller.BeginInstall(mCallback, mCallback, Updates);
            return true;
        }

        public bool UnInstallUpdates(List<MsUpdate> Updates)
        {
            if (mCallback != null)
                return false;

            if (mInstaller == null)
                mInstaller = mUpdateSession.CreateUpdateInstaller() as IUpdateInstaller;

            mInstaller.Updates = new UpdateCollection();
            foreach (MsUpdate Update in Updates)
            {
                IUpdate update = Update.GetUpdate();
                if (update == null)
                    continue;

                if (!update.IsUninstallable)
                {
                    AppLog.Line(MiscFunc.fmt("Update can not be uninstalled: {0}", update.Title));
                    continue;
                }
                mInstaller.Updates.Add(update);
            }
            if (mDownloader.Updates.Count == 0)
            {
                AppLog.Line(MiscFunc.fmt("No updates selected or eligible for uninstallation"));
                return false;
            }

            mCurOperation = AgentOperation.RemoveingUpdtes;

            OnProgress(-1, 0, 0, 0);

            mCallback = new UpdateCallback(this);

            AppLog.Line(MiscFunc.fmt("Removing Updates... This may take several minutes."));
            mInstalationJob = mInstaller.BeginUninstall(mCallback, mCallback, Updates);
            return true;
        }

        public bool RemoveFrom(List<MsUpdate> Updates, MsUpdate Update)
        {
            for (int i = 0; i < Updates.Count; i++)
            {
                if (Updates[i] == Update)
                {
                    Updates.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void HideUpdates(List<MsUpdate> Updates, bool Hide)
        {
            foreach (MsUpdate Update in Updates)
            {
                try
                {
                    IUpdate update = Update.GetUpdate();
                    if (update == null)
                        continue;
                    update.IsHidden = Hide;

                    if (Hide)
                    {
                        Update.Attributes |= (int)MsUpdate.UpdateAttr.Hidden;
                        mHiddenUpdates.Add(Update);
                        RemoveFrom(mPendingUpdates, Update);
                    }
                    else
                    {
                        Update.Attributes &= ~(int)MsUpdate.UpdateAttr.Hidden;
                        mPendingUpdates.Add(Update);
                        RemoveFrom(mHiddenUpdates, Update);
                    }

                    OnUpdatesChanged();
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
                AppLog.Line(MiscFunc.fmt("Search for updats failed"));
                LogError(err);
                OnFinished(false);
                return;
            }

            mPendingUpdates.Clear();
            mInstalledUpdates.Clear();
            mHiddenUpdates.Clear();
            mIsValid = true;

            foreach (IUpdate update in SearchResults.Updates)
            {
                if (update.IsHidden)
                    mHiddenUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Hidden));
                else if (update.IsInstalled)
                    mInstalledUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Installed));
                else
                    mPendingUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Pending));
                Console.WriteLine(update.Title);
            }

            AppLog.Line(MiscFunc.fmt("Found {0} pending updates.", mPendingUpdates.Count));

            OnUpdatesChanged(true);

            OnFinished(SearchResults.ResultCode == OperationResultCode.orcSucceeded, false);
        }

        protected void OnUpdatesDownloaded(IDownloadJob downloadJob, List<MsUpdate> Updates)
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
                AppLog.Line(MiscFunc.fmt("Downloading updates failed"));
                LogError(err);
                OnFinished(false);
                return;
            }

            OnUpdatesChanged();

            if (mCurOperation == AgentOperation.PreparingUpdates)
                InstallUpdates(Updates);
            else
            {
                AppLog.Line(MiscFunc.fmt("Updates downloaded to %windir%\\SoftwareDistribution\\Download"));
                OnFinished(DownloadResults.ResultCode == OperationResultCode.orcSucceeded);
            }
        }

        protected void OnInstalationCompleted(IInstallationJob installationJob, List<MsUpdate> Updates)
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
                AppLog.Line(MiscFunc.fmt("(Un)Installing updates failed"));
                LogError(err);
                OnFinished(false);
                return;
            }

            if (InstallationResults.ResultCode == OperationResultCode.orcSucceeded)
            {
                AppLog.Line(MiscFunc.fmt("Updates (Un)Installed succesfully"));

                foreach (MsUpdate Update in Updates)
                {
                    if (mCurOperation == AgentOperation.InstallingUpdates)
                    {
                        if (RemoveFrom(mPendingUpdates, Update))
                        {
                            Update.Attributes |= (int)MsUpdate.UpdateAttr.Installed;
                            mInstalledUpdates.Add(Update);
                        }
                    }
                    else if (mCurOperation == AgentOperation.RemoveingUpdtes)
                    {
                        if (RemoveFrom(mInstalledUpdates, Update))
                        {
                            Update.Attributes &= ~(int)MsUpdate.UpdateAttr.Installed;
                            mPendingUpdates.Add(Update);
                        }
                    }
                }

                if (InstallationResults.RebootRequired == true)
                    AppLog.Line(MiscFunc.fmt("Reboot is required for one of more updates"));
            }
            else
                AppLog.Line(MiscFunc.fmt("Updates failed to (Un)Install"));

            OnUpdatesChanged();

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

        public bool TestWuAuServ()
        {
            ServiceController svc = new ServiceController("wuauserv");
            bool ret = svc.Status == ServiceControllerStatus.Running;
            svc.Close();
            return ret;
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
            public FinishedArgs(bool success, bool needReboot = false)
            {
                Success = success;
                RebootNeeded = needReboot;
            }

            public bool Success = false;
            public bool RebootNeeded = false;
        }
        public event EventHandler<FinishedArgs> Finished;

        protected void OnFinished(bool success, bool needReboot = false)
        {
            mCurOperation = AgentOperation.None;

            Finished?.Invoke(this, new FinishedArgs(success, needReboot));
        }

        public class UpdatesArgs : EventArgs
        {
            public UpdatesArgs(bool found)
            {
                Found = found;
            }
            public bool Found = false;
        }
        public event EventHandler<UpdatesArgs> UpdatesChaged;

        protected void OnUpdatesChanged(bool found = false)
        {
            string INIPath = Program.appPath + @"\updates.ini";
            FileOps.DeleteFile(INIPath);

            StoreUpdates(mUpdateHistory);
            StoreUpdates(mPendingUpdates);
            StoreUpdates(mInstalledUpdates);
            StoreUpdates(mHiddenUpdates);
            
            UpdatesChaged?.Invoke(this, new UpdatesArgs(found));
        }

        private void StoreUpdates(List<MsUpdate> Updates)
        {
            string INIPath = Program.appPath + @"\updates.ini";
            foreach (MsUpdate Update in Updates)
            {
                Program.IniWriteValue(Update.KB, "UUID", Update.UUID, INIPath);

                Program.IniWriteValue(Update.KB, "Title", Update.Title, INIPath);
                Program.IniWriteValue(Update.KB, "Info", Update.Description, INIPath);
                Program.IniWriteValue(Update.KB, "Category", Update.Category, INIPath);

                Program.IniWriteValue(Update.KB, "Date", Update.Date.ToString("dd.MM.yyyy"), INIPath);
                Program.IniWriteValue(Update.KB, "Size", Update.Size.ToString(), INIPath);

                Program.IniWriteValue(Update.KB, "SupportUrl", Update.SupportUrl, INIPath);

                Program.IniWriteValue(Update.KB, "Downloads", string.Join("|",Update.Downloads.Cast<string>().ToArray<string>()), INIPath);

                Program.IniWriteValue(Update.KB, "State", ((int)Update.State).ToString(), INIPath);
                Program.IniWriteValue(Update.KB, "Attributes", Update.Attributes.ToString(), INIPath);
                Program.IniWriteValue(Update.KB, "ResultCode", Update.ResultCode.ToString(), INIPath);
                Program.IniWriteValue(Update.KB, "HResult", Update.HResult.ToString(), INIPath);
            }
        }

        private void LoadUpdates()
        {
            string INIPath = Program.appPath + @"\updates.ini";
            foreach (string KB in Program.IniEnumSections(INIPath))
            {
                if (KB.Length == 0)
                    continue;

                MsUpdate Update = new MsUpdate();
                Update.KB = KB;
                Update.UUID = Program.IniReadValue(Update.KB, "UUID", "", INIPath);

                Update.Title = Program.IniReadValue(Update.KB, "Title", "", INIPath);
                Update.Description = Program.IniReadValue(Update.KB, "Info", "", INIPath);
                Update.Category = Program.IniReadValue(Update.KB, "Category", "", INIPath);

                try { Update.Date = DateTime.Parse(Program.IniReadValue(Update.KB, "Date", "", INIPath)); } catch { }
                Update.Size = (decimal)MiscFunc.parseInt(Program.IniReadValue(Update.KB, "Size", "0", INIPath));

                Update.SupportUrl = Program.IniReadValue(Update.KB, "SupportUrl", "", INIPath);
                Update.Downloads.AddRange(Program.IniReadValue(Update.KB, "Downloads", "", INIPath).Split('|'));

                Update.State = (MsUpdate.UpdateState)MiscFunc.parseInt(Program.IniReadValue(Update.KB, "State", "0", INIPath));
                Update.Attributes = MiscFunc.parseInt(Program.IniReadValue(Update.KB, "Attributes", "0", INIPath));
                Update.ResultCode = MiscFunc.parseInt(Program.IniReadValue(Update.KB, "ResultCode", "0", INIPath));
                Update.HResult = MiscFunc.parseInt(Program.IniReadValue(Update.KB, "HResult", "0", INIPath));

                switch (Update.State)
                {
                    case MsUpdate.UpdateState.Pending: mPendingUpdates.Add(Update); break;
                    case MsUpdate.UpdateState.Installed: mInstalledUpdates.Add(Update); break;
                    case MsUpdate.UpdateState.Hidden: mHiddenUpdates.Add(Update); break;
                    case MsUpdate.UpdateState.History: mUpdateHistory.Add(Update); break;
                }
            }
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
                    agent.OnUpdatesDownloaded(downloadJob, downloadJob.AsyncState);
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
                    agent.OnInstalationCompleted(installationJob, installationJob.AsyncState);
                }));
            }
        }
    }
}
