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
using System.Collections;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace wumgr
{
    public partial class WuMgr : Form
    {

        public const Int32 WM_SYSCOMMAND = 0x112;

        public const Int32 MF_BITMAP = 0x00000004;
        public const Int32 MF_CHECKED = 0x00000008;
        public const Int32 MF_DISABLED = 0x00000002;
        public const Int32 MF_ENABLED = 0x00000000;
        public const Int32 MF_GRAYED = 0x00000001;
        public const Int32 MF_MENUBARBREAK = 0x00000020;
        public const Int32 MF_MENUBREAK = 0x00000040;
        public const Int32 MF_OWNERDRAW = 0x00000100;
        public const Int32 MF_POPUP = 0x00000010;
        public const Int32 MF_SEPARATOR = 0x00000800;
        public const Int32 MF_STRING = 0x00000000;
        public const Int32 MF_UNCHECKED = 0x00000000;

        public const Int32 MF_BYPOSITION = 0x400;
        public const Int32 MF_BYCOMMAND = 0x000;
        //public const Int32 MF_REMOVE = 0x1000;

        public const Int32 MYMENU_ABOUT = 1000;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);
        [DllImport("user32.dll")]
        private static extern int AppendMenu(IntPtr hMenu, int Flags, int NewID, String Item);
        [DllImport("user32.dll")]
        static extern int GetMenuItemCount(IntPtr hMenu);
        [DllImport("user32.dll")]
        static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        protected override void WndProc(ref Message msg)
        {
            switch (msg.Msg)
            {
                case WM_SYSCOMMAND:
                    {
                        switch (msg.WParam.ToInt32())
                        {
                            case MYMENU_ABOUT: menuAbout_Click(null, null); return;
                        }
                    }
                    break;
                case Program.WM_APP:
                    if (msg.WParam == IntPtr.Zero && msg.LParam == IntPtr.Zero)
                    {
                        notifyIcon_BalloonTipClicked(null, null);
                        msg.Result = IntPtr.Zero;
                        return;
                    }
                    break;
            }
            base.WndProc(ref msg);
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


        enum AutoUpdateOptions
        {
            No = 0,
            EveryDay,
            EveryWeek,
            EveryMonth
        }

        AutoUpdateOptions AutoUpdate = AutoUpdateOptions.No;
        int IdleDelay = 0;
        DateTime LastCheck = DateTime.MaxValue;

        public WuMgr()
        {
            InitializeComponent();

            //notifyIcon1.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            notifyIcon.Text = Program.mName;

            if (Program.TestArg("-tray"))
            {
                allowshowdisplay = false;
                notifyIcon.Visible = true;
            }

            this.Text = MiscFunc.fmt("{0} by David Xanatos", Program.mName);

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
            agent.UpdatesChaged += OnUpdates;
            agent.Finished += OnFinished;

            if (!agent.IsActive())
            {
                if (MessageBox.Show("Windows Update Service is not available, try to start it?", Program.mName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    agent.EnableWuAuServ();
                    agent.Init();
                }
            }

            mSuspendUpdate = true;
            chkDrivers.CheckState = (CheckState)GPO.GetDriverAU();
            int day, time;
            dlPolMode.SelectedIndex = GPO.GetAU(out day, out time);
            dlShDay.SelectedIndex = day; dlShTime.SelectedIndex = time;
            chkBlockMS.CheckState = (CheckState)GPO.GetBlockMS();
            dlAutoCheck.SelectedIndex = MiscFunc.parseInt(GetConfig("AutoUpdate", "0"));
            chkAutoRun.Checked = Program.IsAutoStart();
            IdleDelay = MiscFunc.parseInt(GetConfig("IdleDelay", "20"));
            chkNoUAC.Checked = Program.IsSkipUacRun();


            chkOffline.Checked = MiscFunc.parseInt(GetConfig("Offline", "1")) != 0;
            chkDownload.Checked = MiscFunc.parseInt(GetConfig("Download", "1")) != 0;
            chkManual.Checked = MiscFunc.parseInt(GetConfig("Manual", "0")) != 0;
            chkOld.Checked = MiscFunc.parseInt(GetConfig("IncludeOld", "0")) != 0;
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
                    chkDownload.Checked = true;
                else if (Offline.Equals("no_download", StringComparison.CurrentCultureIgnoreCase))
                    chkDownload.Checked = false;
            }

            if (Program.TestArg("-manual"))
                chkManual.Checked = true;

            try {
                LastCheck = DateTime.Parse(GetConfig("LastCheck", ""));
                AppLog.Line(MiscFunc.fmt("Last Checked for updates: {0}", LastCheck.ToString()));
            } catch { }

            LoadProviders(source);

            chkMsUpd.Checked = agent.IsActive() && agent.TestService(WuAgent.MsUpdGUID);

            if (GPO.IsRespected() == 0)
            {
                dlPolMode.Enabled = false;
                //toolTip.SetToolTip(dlPolMode, "Windows 10 Pro and Home do not respect this GPO setting");
            }

            chkHideWU.Enabled = GPO.IsRespected() != 2;
            chkHideWU.Checked = GPO.IsUpdatePageHidden();

            mSuspendUpdate = false;

            mToolsMenu = new MenuItem();
            mToolsMenu.Text = "&Tools";

            BuildToolsMenu();

            notifyIcon.ContextMenu = new ContextMenu();

            MenuItem menuAbout = new MenuItem();
            menuAbout.Text = "&About";
            menuAbout.Click += new System.EventHandler(menuAbout_Click);

            MenuItem menuExit = new MenuItem();
            menuExit.Text = "E&xit";
            menuExit.Click += new System.EventHandler(menuExit_Click);

            notifyIcon.ContextMenu.MenuItems.AddRange(new MenuItem[] { mToolsMenu, menuAbout, new MenuItem("-"), menuExit });


            IntPtr MenuHandle = GetSystemMenu(this.Handle, false); // Note: to restore default set true
            InsertMenu(MenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
            InsertMenu(MenuHandle, 6, MF_BYPOSITION | MF_POPUP, (int)mToolsMenu.Handle, "Tools");
            InsertMenu(MenuHandle, 7, MF_BYPOSITION, MYMENU_ABOUT, "&About");


            UpdateCounts();
            SwitchList(UpdateLists.UpdateHistory);

            doUpdte = Program.TestArg("-update");

            mTimer = new Timer();
            mTimer.Interval = 1000; // once epr second
            mTimer.Tick += OnTimedEvent;
            mTimer.Enabled = true;
        }

        private static Timer mTimer = null;
        private bool doUpdte = false;
        private DateTime LastBaloon = DateTime.MinValue;

        private void OnTimedEvent(Object source, EventArgs e)
        {
            bool updateNow = false;
            if (notifyIcon.Visible)
            {
                int daysDue = GetAutoUpdateDue();
                if (daysDue != 0 && !agent.IsBusy())
                {
                    // ensure we only start a check when user is not doing anything
                    uint idleTime = MiscFunc.GetIdleTime();
                    if (IdleDelay * 60 < idleTime)
                    {
                        AppLog.Line(MiscFunc.fmt("Starting automatic search for updates."));
                        updateNow = true;
                    }
                    else if(daysDue > GetGraceDays())
                    {
                        if (LastBaloon < DateTime.Now.AddHours(-4))
                        {
                            LastBaloon = DateTime.Now;
                            notifyIcon.ShowBalloonTip(int.MaxValue, MiscFunc.fmt("Please Check For Updates"), 
                                MiscFunc.fmt("WuMgr couldn't check for updates for {0} days, please check for updates manually and resolve possible issues", daysDue), ToolTipIcon.Warning);
                        }
                    }
                }

                if (agent.mPendingUpdates.Count > 0)
                {
                    if (LastBaloon < DateTime.Now.AddHours(-4))
                    {
                        LastBaloon = DateTime.Now;
                        notifyIcon.ShowBalloonTip(int.MaxValue, MiscFunc.fmt("New Updates found"),
                            MiscFunc.fmt("WuMgr has found {0} new updates, please review the upates and install them", agent.mPendingUpdates), ToolTipIcon.Info);
                    }
                }
            }

            if ((doUpdte || updateNow) && agent.IsActive())
            {
                doUpdte = false;
                if (chkOffline.Checked)
                    agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
                else
                    agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
            }
        }

        private void WuMgr_Load(object sender, EventArgs e)
        {
            this.Width = 900;
        }

        private int GetAutoUpdateDue()
        {
            DateTime NextUpdate = DateTime.MaxValue;
            switch (AutoUpdate)
            {
                case AutoUpdateOptions.EveryDay: NextUpdate = LastCheck.AddDays(1); break;
                case AutoUpdateOptions.EveryWeek: NextUpdate = LastCheck.AddDays(7); break;
                case AutoUpdateOptions.EveryMonth: NextUpdate = LastCheck.AddMonths(1); break;
            }
            if (NextUpdate >= DateTime.Now)
                return 0;
            return (int)Math.Ceiling((DateTime.Now - NextUpdate).TotalDays);
        }

        private int GetGraceDays()
        {
            switch (AutoUpdate)
            {
                case AutoUpdateOptions.EveryMonth: return 15;
                default: return 3;
            }
        }

        private void WuMgr_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (notifyIcon.Visible && allowshowdisplay)
            {
                e.Cancel = true;
                allowshowdisplay = false;
                this.Hide();
                return;
            }

            agent.Progress -= OnProgress;
            agent.UpdatesChaged -= OnUpdates;
            agent.Finished -= OnFinished;
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
                btnWinUpd.Text = MiscFunc.fmt("Windows Update ({0})", agent.mPendingUpdates.Count);
            if (agent.mInstalledUpdates != null)
                btnInstalled.Text = MiscFunc.fmt("Installed Updates ({0})", agent.mInstalledUpdates.Count);
            if (agent.mHiddenUpdates != null)
                btnHidden.Text = MiscFunc.fmt("Hidden Updates ({0})", agent.mHiddenUpdates.Count);
            if (agent.mUpdateHistory != null)
                btnHistory.Text = MiscFunc.fmt("Update History ({0})", agent.mUpdateHistory.Count);
        }

        void LoadList()
        {
            updateView.CheckBoxes = CurrentList != UpdateLists.UpdateHistory;
            updateView.ForeColor = updateView.CheckBoxes && !agent.IsValid() ? Color.Gray : Color.Black;

            switch (CurrentList)
            {
                case UpdateLists.PendingUpdates:    LoadList(agent.mPendingUpdates); break;
                case UpdateLists.InstaledUpdates:   LoadList(agent.mInstalledUpdates); break;
                case UpdateLists.HiddenUpdates:     LoadList(agent.mHiddenUpdates); break;
                case UpdateLists.UpdateHistory:     LoadList(agent.mUpdateHistory); break;
            }
        }

        void LoadList(List<MsUpdate> List)
        {
            updateView.Items.Clear();
            ListViewItem[] items = new ListViewItem[List.Count];
            for (int i = 0; i < List.Count; i++)
            {
                MsUpdate Update = List[i];
                string State = "";
                switch (Update.State)
                {
                    case MsUpdate.UpdateState.History:
                        switch ((OperationResultCode)Update.ResultCode)
                        {
                            case OperationResultCode.orcNotStarted: State = "Not Started"; break;
                            case OperationResultCode.orcInProgress: State = "In Progress"; break;
                            case OperationResultCode.orcSucceeded: State = "Succeeded"; break;
                            case OperationResultCode.orcSucceededWithErrors: State = "Succeeded with Errors"; break;
                            case OperationResultCode.orcFailed: State = "Failed"; break;
                            case OperationResultCode.orcAborted: State = "Aborted"; break;
                        }
                        State += " (0x" + String.Format("{0:X8}", Update.HResult) + ")";
                        break;

                    default:
                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Beta) != 0)
                            State = "Beta ";

                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Installed) != 0)
                        {
                            State += "Installed";
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Uninstallable) != 0)
                                State += " Removable";
                        }
                        else if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Hidden) != 0)
                        {
                            State += "Hidden";
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
                                State += " Downloaded";
                        }
                        else
                        {
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
                                State += "Downloaded";
                            else
                                State += "Pending";
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.AutoSelect) != 0)
                                State += " (!)";
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Mandatory) != 0)
                                State += " Manatory";
                        }

                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Exclusive) != 0)
                            State += ", Exclusive";

                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Reboot) != 0)
                            State += ", Needs Reboot";
                        break;
                }

                items[i] = new ListViewItem(new string[] {
                    Update.Title,
                    Update.Category,
                    Update.KB,
                    FileOps.FormatSize(Update.Size),
                    Update.Date.ToString("dd.MM.yyyy"),
                    State});

                items[i].Tag = Update;

                ListViewGroup lvg = updateView.Groups[Update.Category];
                if (lvg == null)
                    lvg = updateView.Groups.Add(Update.Category, Update.Category);
                items[i].Group = lvg;
            }
            updateView.Items.AddRange(items);

            updateView.SetGroupState(ListViewGroupState.Collapsible);
        }

        public List<MsUpdate> GetUpdates()
        {
            List<MsUpdate> updates = new List<MsUpdate>();
            foreach (ListViewItem item in updateView.CheckedItems)
                updates.Add((MsUpdate)item.Tag);
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

            lblSupport.Visible = false;
        }

        private void UpdateState()
        {
            bool busy = agent.IsBusy();
            btnCancel.Visible = busy;
            progTotal.Visible = busy;
            lblStatus.Visible = busy;

            bool isValid = agent.IsValid();
            bool isValid2 = isValid || chkManual.Checked;

            bool enable = agent.IsActive() && !busy;
            btnSearch.Enabled = enable;
            btnDownload.Enabled = enable && isValid2 && (CurrentList == UpdateLists.PendingUpdates);
            btnInstall.Enabled = enable && isValid2 && (CurrentList == UpdateLists.PendingUpdates);
            btnUnInstall.Enabled = enable && isValid2 && (CurrentList == UpdateLists.InstaledUpdates);
            btnHide.Enabled = enable && isValid && (CurrentList == UpdateLists.PendingUpdates || CurrentList == UpdateLists.HiddenUpdates);
            btnGetLink.Enabled = CurrentList != UpdateLists.UpdateHistory;
        }

        private MenuItem mToolsMenu = null;
        private MenuItem wuauMenu = null;

        private void BuildToolsMenu()
        {
            wuauMenu = new MenuItem();
            wuauMenu.Text = "Windows Update Service";
            wuauMenu.Checked = agent.TestWuAuServ();
            wuauMenu.Click += new System.EventHandler(menuWuAu_Click);
            mToolsMenu.MenuItems.Add(wuauMenu);
            mToolsMenu.MenuItems.Add(new MenuItem("-"));

            if (Directory.Exists(Program.appPath + @"\Tools"))
            {
                foreach (string subDir in Directory.GetDirectories(Program.appPath + @"\Tools"))
                {
                    string Name = Path.GetFileName(subDir);
                    string INIPath = subDir + @"\" + Name + ".ini";

                    MenuItem toolMenu = new MenuItem();
                    toolMenu.Text = Program.IniReadValue("Root", "Name", Name, INIPath);

                    string Exec = Program.IniReadValue("Root", "Exec", "", INIPath);
                    bool Silent = MiscFunc.parseInt(Program.IniReadValue("Root", "Silent", "0", INIPath)) != 0;
                    if (Exec.Length > 0)
                        toolMenu.Click += delegate (object sender, EventArgs e) { menuExec_Click(sender, e, Exec, subDir, Silent); };
                    else
                    {
                        int count = MiscFunc.parseInt(Program.IniReadValue("Root", "Entries", "", INIPath), 99);
                        for (int i = 1; i <= count; i++)
                        {
                            string name = Program.IniReadValue("Entry" + i.ToString(), "Name", "", INIPath);
                            if (name.Length == 0)
                            {
                                if (count != 99)
                                    continue;
                                break;
                            }

                            MenuItem subMenu = new MenuItem();
                            subMenu.Text = name;

                            string exec = Program.IniReadValue("Entry" + i.ToString(), "Exec", "", INIPath);
                            bool silent = MiscFunc.parseInt(Program.IniReadValue("Entry" + i.ToString(), "Silent", "0", INIPath)) != 0;
                            subMenu.Click += delegate (object sender, EventArgs e) { menuExec_Click(sender, e, exec, subDir, silent); };

                            toolMenu.MenuItems.Add(subMenu);
                        }
                    }

                    mToolsMenu.MenuItems.Add(toolMenu);
                }

                mToolsMenu.MenuItems.Add(new MenuItem("-"));
            }

            MenuItem refreshMenu = new MenuItem();
            refreshMenu.Text = "&Refresh";
            refreshMenu.Click += new System.EventHandler(menuRefresh_Click);
            mToolsMenu.MenuItems.Add(refreshMenu);
        }

        private void menuExec_Click(object Sender, EventArgs e, string exec, string dir, bool silent = false)
        {
            ProcessStartInfo startInfo = Program.PrepExec(exec, silent);
            startInfo.WorkingDirectory = dir;
            if(!Program.DoExec(startInfo))
                MessageBox.Show(MiscFunc.fmt("Filed to start tool"), Program.mName);
        }

        private void menuExit_Click(object Sender, EventArgs e)
        {
            Application.Exit();
        }

        private void menuAbout_Click(object Sender, EventArgs e)
        {
            string About = "";
            About += MiscFunc.fmt("Author: \tDavid Xanatos\r\n");
            About += MiscFunc.fmt("Licence: \tGNU General Public License v3\r\n");
            About += MiscFunc.fmt("Version: \t{0}\r\n", Program.mVersion);
            About += "\r\n";
            About += "Icons from: https://icons8.com/";
            MessageBox.Show(About, Program.mName);
        }

        private void menuWuAu_Click(object Sender, EventArgs e)
        {
            wuauMenu.Checked = !wuauMenu.Checked;
            if (wuauMenu.Checked)
            {
                agent.EnableWuAuServ(true);
                agent.Init();
            }
            else
            {
                agent.UnInit();
                agent.EnableWuAuServ(false);
            }
            UpdateState();
        }

        private void menuRefresh_Click(object Sender, EventArgs e)
        {
            IntPtr MenuHandle = GetSystemMenu(this.Handle, false); // Note: to restore default set true
            RemoveMenu(MenuHandle, 6, MF_BYPOSITION);
            mToolsMenu.MenuItems.Clear();
            BuildToolsMenu();
            InsertMenu(MenuHandle, 6, MF_BYPOSITION | MF_POPUP, (int)mToolsMenu.Handle, "Tools");
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
            if (agent.IsActive())
                agent.UpdateHistory();
            SwitchList(UpdateLists.UpdateHistory);
        }
        
        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!agent.IsActive() || agent.IsBusy())
                return;
            if (chkOffline.Checked)
                agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
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
            {
                if (chkOffline.Checked && chkManual.Checked)
                    agent.UnInstallUpdatesOffline(GetUpdates());
                else
                    agent.UnInstallUpdates(GetUpdates());
            }
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
        }

        private void btnGetLink_Click(object sender, EventArgs e)
        {
            foreach (MsUpdate Update in GetUpdates())
            {
                AppLog.Line(Update.Title);
                foreach (string url in Update.Downloads)
                    AppLog.Line(url);
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

        void OnUpdates(object sender, WuAgent.UpdatesArgs args)
        {
            UpdateCounts();
            if (args.Found) // if (agent.CurOperation() == WuAgent.AgentOperation.CheckingUpdates)
            {
                SetConfig("LastCheck", DateTime.Now.ToString());
                SwitchList(UpdateLists.PendingUpdates);
            }
            else
            {
                LoadList();

                if (MiscFunc.parseInt(Program.IniReadValue("Options", "Refresh", "0")) == 1 && (agent.CurOperation() == WuAgent.AgentOperation.InstallingUpdates || agent.CurOperation() == WuAgent.AgentOperation.RemoveingUpdtes))
                    doUpdte = true;
            }
        }

        void OnFinished(object sender, WuAgent.FinishedArgs args)
        {
            UpdateState();
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

        private void chkDownload_CheckedChanged(object sender, EventArgs e)
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
        }

        private void chkAutoRun_CheckedChanged(object sender, EventArgs e)
        {
            notifyIcon.Visible = dlAutoCheck.Enabled = chkAutoRun.Checked;
            AutoUpdate = chkAutoRun.Checked ? (AutoUpdateOptions)dlAutoCheck.SelectedIndex : AutoUpdateOptions.No;
            if (mSuspendUpdate)
                return;
            Program.AutoStart(chkAutoRun.Checked);
        }

        private void dlAutoCheck_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            SetConfig("AutoUpdate", dlAutoCheck.SelectedIndex.ToString());
            AutoUpdate = (AutoUpdateOptions)dlAutoCheck.SelectedIndex;
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
            UpdateState();
            SetConfig("Manual", chkManual.Checked ? "1" : "0");
        }
        
        private void chkHideWU_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.HideUpdatePage(chkHideWU.Checked);
        }

        private void updateView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(updateView.SelectedItems.Count == 1)
            {
                MsUpdate Update = (MsUpdate)updateView.SelectedItems[0].Tag;
                lblSupport.Links[0].LinkData = Update.SupportUrl;
                lblSupport.Links[0].Visited = false;
                lblSupport.Visible = true;
                toolTip.SetToolTip(lblSupport, Update.SupportUrl);
            }
            else
                lblSupport.Visible = false;
        }

        private void lblSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string target = e.Link.LinkData as string;
            System.Diagnostics.Process.Start(target);
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

        [DllImport("User32.dll")]
        public static extern Int32 SetForegroundWindow(int hWnd);

        private void notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (!allowshowdisplay)
            {
                allowshowdisplay = true;
                this.Show();
            }
            if(this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;   
            SetForegroundWindow(this.Handle.ToInt32());
        }

        private void updateView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            updateView.ListViewItemSorter = new ListViewItemComparer(e.Column);
            updateView.Sort();
        }

        // Implements the manual sorting of items by columns.
        class ListViewItemComparer : IComparer
        {
            private int col;
            public ListViewItemComparer()
            {
                col = 0;
            }
            public ListViewItemComparer(int column)
            {
                col = column;
            }
            public int Compare(object x, object y)
            {
                return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
            }
        }
    }
}
