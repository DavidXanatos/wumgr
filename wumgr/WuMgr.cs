using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WUApiLib;//this is required to use the Interfaces given by microsoft. 
using BrightIdeasSoftware;
using System.Collections;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Runtime.InteropServices;

namespace wumgr
{
    public partial class WuMgr : Form
    {
        public const Int32 WM_SYSCOMMAND = 0x112;
        public const Int32 MF_BYPOSITION = 0x400;
        public const Int32 MF_SEPARATOR = 0x800;
        public const Int32 MYMENU_ABOUT = 1000;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);

        protected override void WndProc(ref Message msg)
        {
            if (msg.Msg == WM_SYSCOMMAND)
            {
                switch (msg.WParam.ToInt32())
                {
                    case MYMENU_ABOUT: menuAbout_Click(); return;
                }
            }
            base.WndProc(ref msg);
        }

        private void WuMgr_Load(object sender, EventArgs e)
        {
            IntPtr MenuHandle = GetSystemMenu(this.Handle, false);
            InsertMenu(MenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
            InsertMenu(MenuHandle, 6, MF_BYPOSITION, MYMENU_ABOUT, "&About");
        }

        WuAgent agent;

        void LineLogger(object sender, AppLog.LogEventArgs args)
        {
            logBox.AppendText(args.line + Environment.NewLine);
            logBox.ScrollToCaret();
        }

        private bool allowshowdisplay = true;

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        private bool mSuspendUpdate = false;

        public WuMgr()
        {
            InitializeComponent();

            notifyIcon1.ContextMenu = new System.Windows.Forms.ContextMenu();
            MenuItem menuExit = new System.Windows.Forms.MenuItem();

            menuExit.Index = 0;
            menuExit.Text = "E&xit";
            menuExit.Click += new System.EventHandler(menuExit_Click);

            notifyIcon1.ContextMenu.MenuItems.AddRange(new MenuItem[] { menuExit });

            //notifyIcon1.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            notifyIcon1.Text = Program.mName;

            if (Program.TestArg("-tray"))
            {
                allowshowdisplay = false;
                notifyIcon1.Visible = true;
            }

            this.Text = Program.fmt("{0} by David Xanatos", Program.mName);

            toolTip.SetToolTip(btnSearch, "Search");
            toolTip.SetToolTip(btnInstall, "Install");
            toolTip.SetToolTip(btnDownload, "Download");
            toolTip.SetToolTip(btnHide, "Hide");
            toolTip.SetToolTip(btnGetLink, "Get Links");
            toolTip.SetToolTip(btnUnInstall, "Uninstall");
            toolTip.SetToolTip(btnCancel, "Cancel");

            btnSearch.Image = (Image)(new Bitmap(global::wumgr.Properties.Resources.icons8_available_updates_32, new Size(25, 25)));
            btnInstall.Image = (Image)(new Bitmap(global::wumgr.Properties.Resources.icons8_software_installer_32, new Size(25, 25)));
            btnDownload.Image = (Image)(new Bitmap(global::wumgr.Properties.Resources.icons8_downloading_updates_32, new Size(25, 25)));
            btnUnInstall.Image = (Image)(new Bitmap(global::wumgr.Properties.Resources.icons8_trash_32, new Size(25, 25)));
            btnHide.Image = (Image)(new Bitmap(global::wumgr.Properties.Resources.icons8_hide_32, new Size(25, 25)));
            btnGetLink.Image = (Image)(new Bitmap(global::wumgr.Properties.Resources.icons8_link_32, new Size(25, 25)));
            btnCancel.Image = (Image)(new Bitmap(global::wumgr.Properties.Resources.icons8_cancel_32, new Size(25, 25)));

            AppLog.Logger += LineLogger;

            foreach (string line in AppLog.GetLog())
                logBox.AppendText(line + Environment.NewLine);
            logBox.ScrollToCaret();
            

            agent = WuAgent.GetInstance();
            agent.Progress += OnProgress;
            agent.Finished += OnFinished;

            if (!agent.IsActive())
            {
                if (MessageBox.Show("Windows Update Service is not available, try to start it?", Program.mName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    agent.EnableWuAuServ();
                    agent.Init();
                }
            }

            updateView.ShowGroups = true;
            updateView.ShowItemCountOnGroups = true;
            updateView.AlwaysGroupByColumn = updateView.ColumnsInDisplayOrder[1];
            updateView.Sort();
            

            mSuspendUpdate = true;
            chkDrivers.CheckState = (CheckState)GPO.GetDriverAU();
            int day, time;
            dlPolMode.SelectedIndex = GPO.GetAU(out day, out time);
            dlShDay.SelectedIndex = day; dlShTime.SelectedIndex = time;
            chkBlockMS.CheckState = (CheckState)GPO.GetBlockMS();
            chkAutoRun.Checked = Program.IsAutoStart();
            chkNoUAC.Checked = Program.IsSkipUacRun();



            chkOffline.Checked = int.Parse(GetConfig("Offline", "1")) != 0;
            chkDownload.CheckState = (CheckState)int.Parse(GetConfig("Download", "2"));
            chkManual.Checked = int.Parse(GetConfig("Manual", "0")) != 0;
            chkOld.Checked = int.Parse(GetConfig("IncludeOld", "0")) != 0;
            string source = GetConfig("Source", "Windows Update");

            string Online = Program.GetArg("-online");
            if (Online != null)
            {
                chkOffline.Checked = false;
                if (Online.Length > 0)
                    source = agent.GetServiceName(Online, true);
            }

            string Offline = Program.GetArg("-offline");
            if (Offline != null)
            {
                chkOffline.Checked = true;

                if (Offline.Equals("download", StringComparison.CurrentCultureIgnoreCase))
                    chkDownload.CheckState = CheckState.Checked;
                else if (Offline.Equals("no_download", StringComparison.CurrentCultureIgnoreCase))
                    chkDownload.CheckState = CheckState.Unchecked;
                else if (Offline.Equals("download_new", StringComparison.CurrentCultureIgnoreCase))
                    chkDownload.CheckState = CheckState.Indeterminate;
            }

            if (Program.TestArg("-manual"))
                chkManual.Checked = false;

            LoadProviders(source);

            chkMsUpd.Checked = agent.IsActive() && agent.TestService(WuAgent.MsUpdGUID);

            if (GPO.IsRespected() == 0)
            {
                dlPolMode.Enabled = false;
                //toolTip.SetToolTip(dlPolMode, "Windows 10 Pro and Home do not respect this GPO setting");
            }

            mSuspendUpdate = false;
            
            UpdateCounts();
            SwitchList(UpdateLists.UpdateHistory);

            if (Program.TestArg("-update"))
            {
                aTimer = new Timer();
                aTimer.Interval = 1000;
                // Hook up the Elapsed event for the timer. 
                aTimer.Tick += OnTimedEvent;
                aTimer.Enabled = true;
            }
        }

        private void WuMgr_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void menuExit_Click(object Sender, EventArgs e)
        {
            Application.Exit();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (allowshowdisplay)
            {
                allowshowdisplay = false;
                this.Hide();
            }
            else
            {
                allowshowdisplay = true;
                this.Show();
            }
        }

        private static void menuAbout_Click()
        {
            string About = "";
            About += Program.fmt("Author: \tDavid Xanatos\r\n");
            About += Program.fmt("Licence: \tGNU General Public License v3\r\n");
            About += Program.fmt("Version: \t{0}\r\n", Program.mVersion);
            About += "\r\n";
            About += "Icons from: https://icons8.com/";

            MessageBox.Show(About, Program.mName);
        }

        private static Timer aTimer = null;

        private void OnTimedEvent(Object source, EventArgs e)
        {
            aTimer.Stop();
            aTimer = null;
            if (!agent.IsActive())
                return;
            if (chkOffline.Checked)
                agent.SearchForUpdates((int)chkDownload.CheckState, chkOld.Checked);
            else
                agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
        }

        private void LoadProviders(string source = null)
        {
            dlSource.Items.Clear();
            for (int i = 0; i < agent.mServiceList.Count; i++)
            {
                string service = agent.mServiceList[i];
                dlSource.Items.Add(service);

                if (source != null && service.Equals(source, StringComparison.CurrentCultureIgnoreCase))
                    dlSource.SelectedIndex = i;
            }
        }

        void UpdateCounts()
        {
            if (agent.mPendingUpdates != null)
                btnWinUpd.Text = Program.fmt("Windows Update ({0})", agent.mPendingUpdates.Count);
            if (agent.mInstalledUpdates != null)
                btnInstalled.Text = Program.fmt("Installed Updates ({0})", agent.mInstalledUpdates.Count);
            if (agent.mHiddenUpdates != null)
                btnHidden.Text = Program.fmt("Hidden Updates ({0})", agent.mHiddenUpdates.Count);
            if (agent.mUpdateHistory != null)
                btnHistory.Text = Program.fmt("Update History ({0})", agent.mUpdateHistory.Count);
        }

        void LoadList()
        {
            updateView.ClearObjects();

            updateView.CheckBoxes = CurrentList != UpdateLists.UpdateHistory;

            switch (CurrentList)
            {
                case UpdateLists.PendingUpdates:    LoadList(agent.mPendingUpdates); break;
                case UpdateLists.InstaledUpdates:   LoadList(agent.mInstalledUpdates); break;
                case UpdateLists.HiddenUpdates:     LoadList(agent.mHiddenUpdates); break;
                case UpdateLists.UpdateHistory:     LoadList(agent.mUpdateHistory); break;
            }
        }

        void LoadList(List<IUpdateHistoryEntry2> List)
        {
            if (List != null)
            {
                List<Update> list = new List<Update>();
                foreach (IUpdateHistoryEntry2 update in List)
                {
                    list.Add(new Update(update));
                }
                updateView.AddObjects(list);
            }
        }

        void LoadList(UpdateCollection List)
        {
            if (List != null)
            {
                List<Update> list = new List<Update>();
                foreach (IUpdate update in List)
                {
                    list.Add(new Update(update));
                }
                updateView.AddObjects(list);
            }
        }

        public UpdateCollection GetUpdates()
        {
            UpdateCollection updates = new UpdateCollection();
            foreach (Update item in updateView.CheckedObjects)
            {
                updates.Add(item.Entry);
            }
            return updates;
        }

        enum UpdateLists {
            PendingUpdates,
            InstaledUpdates,
            HiddenUpdates,
            UpdateHistory
        };

        private UpdateLists CurrentList = UpdateLists.UpdateHistory;

        private bool suspendChange = false;

        void SwitchList(UpdateLists List)
        {
            if (suspendChange)
                return;

            suspendChange = true;
            btnWinUpd.CheckState = List == UpdateLists.PendingUpdates ? CheckState.Checked : CheckState.Unchecked;
            btnInstalled.CheckState = List == UpdateLists.InstaledUpdates ? CheckState.Checked : CheckState.Unchecked;
            btnHidden.CheckState = List == UpdateLists.HiddenUpdates ? CheckState.Checked : CheckState.Unchecked;
            btnHistory.CheckState = List == UpdateLists.UpdateHistory ? CheckState.Checked : CheckState.Unchecked;
            suspendChange = false;

            CurrentList = List;
            LoadList();

            UpdateState();
        }

        private void UpdateState()
        {
            bool busy = agent.IsBusy();
            btnCancel.Visible = busy;
            progTotal.Visible = busy;
            lblStatus.Visible = busy;

            bool enable = agent.IsActive() && !busy;
            btnSearch.Enabled = enable;
            btnDownload.Enabled = enable && (CurrentList == UpdateLists.PendingUpdates);
            btnInstall.Enabled = enable && (CurrentList == UpdateLists.PendingUpdates);
            btnUnInstall.Enabled = enable && (CurrentList == UpdateLists.InstaledUpdates);
            btnHide.Enabled = enable && (CurrentList == UpdateLists.PendingUpdates || CurrentList == UpdateLists.HiddenUpdates);
            btnGetLink.Enabled = CurrentList != UpdateLists.UpdateHistory;
        }

        private void btnWinUpd_CheckedChanged(object sender, EventArgs e)
        {
            SwitchList(UpdateLists.PendingUpdates);
        }

        private void btnInstalled_CheckedChanged(object sender, EventArgs e)
        {
            SwitchList(UpdateLists.InstaledUpdates);
        }

        private void btnHidden_CheckedChanged(object sender, EventArgs e)
        {
            SwitchList(UpdateLists.HiddenUpdates);
        }

        private void btnHistory_CheckedChanged(object sender, EventArgs e)
        {
            if (!agent.IsActive())
                return;
            agent.UpdateHistory();
            SwitchList(UpdateLists.UpdateHistory);
        }
        

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!agent.IsActive() || agent.IsBusy())
                return;
            if (chkOffline.Checked)
                agent.SearchForUpdates((int)chkDownload.CheckState, chkOld.Checked);
            else
                agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (!agent.IsActive() || agent.IsBusy())
                return;
            if (CurrentList == UpdateLists.PendingUpdates)
            {
                if (chkOffline.Checked && chkManual.Checked)
                    agent.DownloadUpdatesOffline(GetUpdates());
                else
                    agent.DownloadUpdates(GetUpdates());
            }
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (!agent.IsActive() || agent.IsBusy())
                return;
            if (CurrentList == UpdateLists.PendingUpdates)
            {
                if (chkOffline.Checked && chkManual.Checked)
                    agent.DownloadUpdatesOffline(GetUpdates(), true);
                else
                    agent.DownloadUpdates(GetUpdates(), true);
            }
        }

        private void btnUnInstall_Click(object sender, EventArgs e)
        {
            if (!agent.IsActive() || agent.IsBusy())
                return;
            if (CurrentList == UpdateLists.InstaledUpdates)
                agent.UnInstallUpdates(GetUpdates());
        }

        private void btnHide_Click(object sender, EventArgs e)
        {
            if (!agent.IsActive() || agent.IsBusy())
                return;
            switch (CurrentList)
            {
                case UpdateLists.PendingUpdates: agent.HideUpdates(GetUpdates(), true); break;
                case UpdateLists.HiddenUpdates: agent.HideUpdates(GetUpdates(), false); break;
            }
            UpdateCounts();
            LoadList();
        }

        private void btnGetLink_Click(object sender, EventArgs e)
        {
            foreach (IUpdate update in GetUpdates())
            {
                AppLog.Line(update.Title);
                foreach (IUpdate bundle in update.BundledUpdates)
                {
                    foreach (IUpdateDownloadContent udc in bundle.DownloadContents)
                    {
                        if (String.IsNullOrEmpty(udc.DownloadUrl))
                            continue;

                        AppLog.Line(udc.DownloadUrl);
                    }
                }
                AppLog.Line("");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            agent.CancelOperations();
        }

        void OnProgress(object sender, WuAgent.ProgressArgs args)
        {
            string Status = "";

            switch(agent.CurOperation())
            {
                case WuAgent.AgentOperation.CheckingUpdates:    Status = "Checking for Updates"; break;
                case WuAgent.AgentOperation.PreparingCheck:     Status = "Preparing Check"; break;
                case WuAgent.AgentOperation.PreparingUpdates:
                case WuAgent.AgentOperation.DownloadingUpdates: Status = "Downloading Updates"; break;
                case WuAgent.AgentOperation.InstallingUpdates:  Status = "Installing Updates"; break;
                case WuAgent.AgentOperation.RemoveingUpdtes:    Status = "Removing Updates"; break;
            }

            if (args.TotalUpdates == -1)
            {
                progTotal.Style = ProgressBarStyle.Marquee;
                progTotal.MarqueeAnimationSpeed = 30;
                Status += "...";
            }
            else
            {
                progTotal.Style = ProgressBarStyle.Continuous;
                progTotal.MarqueeAnimationSpeed = 0;

                progTotal.Value = args.TotalPercent;

                if(args.TotalUpdates > 1)
                    Status += " " + args.CurrentIndex + "/" + args.TotalUpdates + " ";

                //if (args.UpdatePercent != 0)
                //    Status += " " + args.UpdatePercent + "%";
            }
            lblStatus.Text = Status;
            toolTip.SetToolTip(lblStatus, args.Info);

            UpdateState();
        }

        void OnFinished(object sender, WuAgent.FinishedArgs args)
        {
            UpdateCounts();
            UpdateState();
            if (args.FoundUpdates)
                SwitchList(UpdateLists.PendingUpdates);
            lblStatus.Text = "";
            toolTip.SetToolTip(lblStatus, "");
        }

        private void dlSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetConfig("Source", dlSource.Text);
        }

        private void chkOffline_CheckedChanged(object sender, EventArgs e)
        {
            dlSource.Enabled = !chkOffline.Checked;
            chkDownload.Enabled = chkOffline.Checked;
            chkManual.Enabled = chkOffline.Checked;

            SetConfig("Offline", chkOffline.Checked ? "1" : "0");
        }

        private void chkDownload_CheckStateChanged(object sender, EventArgs e)
        {
            SetConfig("Download", chkDownload.Checked ? "1" : "0");
        }

        private void chkOld_CheckedChanged(object sender, EventArgs e)
        {
            SetConfig("IncludeOld", chkOld.Checked ? "1" : "0");
        }

        private void chkDrivers_CheckStateChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.ConfigDriverAU((int)chkDrivers.CheckState);
        }

        private void dlPolMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            dlShDay.Enabled = dlShTime.Enabled = dlPolMode.SelectedIndex == 4;

            if (mSuspendUpdate)
                return;
            GPO.ConfigAU(dlPolMode.SelectedIndex, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
            CheckAndHideUpdatePage();
        }

        private void dlShDay_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.ConfigAU(4, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
        }

        private void dlShTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.ConfigAU(4, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
        }

        private void chkBlockMS_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.BlockMS(chkBlockMS.Checked);
            CheckAndHideUpdatePage();
        }

        private void chkAutoRun_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            Program.AutoStart(chkAutoRun.Checked);
        }

        private void chkNoUAC_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            Program.SkipUacEnable(chkNoUAC.Checked);
        }

        private void chkMsUpd_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            string source = dlSource.Text;
            agent.EnableService(WuAgent.MsUpdGUID, chkMsUpd.Checked);
            LoadProviders(source);
        }

        private void chkManual_CheckedChanged(object sender, EventArgs e)
        {
            SetConfig("Manual", chkManual.Checked ? "1" : "0");
        }

        private void CheckAndHideUpdatePage()
        {
            GPO.HideUpdatePage(chkBlockMS.Checked || dlPolMode.SelectedIndex == 1);
        }

        public string GetConfig(string name, string def = "")
        {
            return Program.IniReadValue("Options", name, def);
            //var subKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Xanatos\Windows Update Manager", true);
            //return subKey.GetValue(name, def).ToString();
        }

        public void SetConfig(string name, string value)
        {
            if (mSuspendUpdate)
                return;
            Program.IniWriteValue("Options", name, value.ToString());
            //var subKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Xanatos\Windows Update Manager", true);
            //subKey.SetValue(name, value);
        }
    }


    class Update : INotifyPropertyChanged
    {
        public bool IsActive = true;

        public Update(IUpdate update)
        {
            Entry = update;

            Title = update.Title;
            Category = GetCategory(update.Categories);
            Description = update.Description;
            Size = GetSizeStr(update);
            Date = update.LastDeploymentChangeTime.ToString("dd.MM.yyyy");
            KB = GetKB(update);

            if (update.IsBeta)
                State = "Beta ";

            if (update.IsInstalled)
            {
                State += "Installed";
                if (update.IsUninstallable)
                    State += " Removable";
            }
            else if (update.IsHidden)
            {
                State += "Hidden";
                if (update.IsDownloaded)
                    State += " Downloaded";
            }
            else
            {
                if (update.IsDownloaded)
                    State += "Downloaded";
                else
                    State += "Pending";
                if (update.AutoSelectOnWebSites) //update.DeploymentAction
                    State += " (!)";
                if (update.IsMandatory)
                    State += " Manatory";
            }
        }

        public Update(IUpdateHistoryEntry2 update)
        {
            Title = update.Title;
            Category = GetCategory(update.Categories);
            Description = update.Description;
            Date = update.Date.ToString("dd.MM.yyyy");
            switch (update.ResultCode)
            {
                case OperationResultCode.orcNotStarted: State = "Not Started"; break;
                case OperationResultCode.orcInProgress: State = "In Progress"; break;
                case OperationResultCode.orcSucceeded: State = "Succeeded"; break;
                case OperationResultCode.orcSucceededWithErrors: State = "Succeeded with Errors"; break;
                case OperationResultCode.orcFailed: State = "Failed"; break;
                case OperationResultCode.orcAborted: State = "Aborted"; break;
            }
            State += " (0x" + String.Format("{0:X8}", update.HResult) + ")";
        }

        string GetCategory(ICategoryCollection cats)
        {
            string category = "";
            foreach (ICategory cat in cats)
            {
                if (category.Length > 0)
                    category += "; ";
                category += cat.Name;
            }
            return category;
            //return update.Categories.Count > 0 ? update.Categories[0].Name : "Unknown";
        }

        string GetKB(IUpdate update)
        {
             return update.KBArticleIDs.Count > 0 ? "KB" + update.KBArticleIDs[0] : "KBUnknown";
        }

        string GetSizeStr(IUpdate update)
        {
            decimal size = update.MaxDownloadSize;

            if (size > 1024 * 1024 * 1024)
                return (size / (1024 * 1024 * 1024)).ToString("F") + " Gb";
            if (size > 1024 * 1024)
                return (size / (1024 * 1024)).ToString("F") + " Mb";
            if (size > 1024 * 1024)
                return (size / (1024)).ToString("F") + " Kb";
            return ((Int64)size).ToString() + " b";
        }

        public String Title = "";
        public String Category = "";
        public String KB = "";
        public String Date = "";
        public String Size = "";
        public String Description = "";
        public String State = "";

        public IUpdate Entry = null;

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
