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
        GPO.Respect mGPORespect = GPO.Respect.Unknown;
        float mWinVersion = 0.0f;


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

            if(!MiscFunc.IsRunningAsUwp())
                this.Text = MiscFunc.fmt("{0} v{1} by David Xanatos", Program.mName, Program.mVersion);

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

            mGPORespect = GPO.GetRespect();
            mWinVersion = GPO.GetWinVersion();

            if (mWinVersion < 10) // 8.1 or below
                chkHideWU.Enabled = false;
            chkHideWU.Checked = GPO.IsUpdatePageHidden();

            if (mGPORespect == GPO.Respect.Partial || mGPORespect == GPO.Respect.None)
                radSchedule.Enabled = radDownload.Enabled = radNotify.Enabled = false;
            else if (mGPORespect == GPO.Respect.Unknown)
                AppLog.Line("Unrecognized Windows Edition, respect for GPO settings is unknown.");

            if (mGPORespect == GPO.Respect.None)
                chkBlockMS.Enabled = false;
            chkBlockMS.CheckState = (CheckState)GPO.GetBlockMS();

            int day, time;
            switch (GPO.GetAU(out day, out time))
            {
                case GPO.AUOptions.Default: radDefault.Checked = true; break;
                case GPO.AUOptions.Disabled: radDisable.Checked = true; break;
                case GPO.AUOptions.Notification: radNotify.Checked = true; break;
                case GPO.AUOptions.Download: radDownload.Checked = true; break;
                case GPO.AUOptions.Scheduled: radSchedule.Checked = true; break;
            }
            dlShDay.SelectedIndex = day; dlShTime.SelectedIndex = time;

            if (mWinVersion >= 10) // 10 or abive
                chkDisableAU.Checked = GPO.GetDisableAU();

            if (mWinVersion < 6.2) // win 7 or below
                chkStore.Enabled = false;
            chkStore.Checked = GPO.GetStoreAU();

            dlAutoCheck.SelectedIndex = MiscFunc.parseInt(GetConfig("AutoUpdate", "0"));
            chkAutoRun.Checked = Program.IsAutoStart();
            if (MiscFunc.IsRunningAsUwp() && chkAutoRun.CheckState == CheckState.Checked)
                chkAutoRun.Enabled = false;
            IdleDelay = MiscFunc.parseInt(GetConfig("IdleDelay", "20"));
            chkNoUAC.Checked = Program.IsSkipUacRun();
            chkNoUAC.Enabled = MiscFunc.IsAdministrator();
            chkNoUAC.Visible = chkNoUAC.Enabled || chkNoUAC.Checked || !MiscFunc.IsRunningAsUwp();


            chkOffline.Checked = MiscFunc.parseInt(GetConfig("Offline", "0")) != 0;
            chkDownload.Checked = MiscFunc.parseInt(GetConfig("Download", "1")) != 0;
            chkManual.Checked = MiscFunc.parseInt(GetConfig("Manual", "0")) != 0;
            if (!MiscFunc.IsAdministrator())
            {
                if (MiscFunc.IsRunningAsUwp())
                {
                    chkOffline.Enabled = false;
                    chkOffline.Checked = false;

                    chkManual.Enabled = false;
                    chkManual.Checked = true;
                }
                chkMsUpd.Enabled = false;
            }
            chkMsUpd.Checked = agent.IsActive() && agent.TestService(WuAgent.MsUpdGUID);

            // Note: when running in the UWP sandbox we cant write the real registry even as admins
            if (!MiscFunc.IsAdministrator() || MiscFunc.IsRunningAsUwp())
            {
                foreach (Control ctl in tabGPO.Controls)
                    ctl.Enabled = false;
            }

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
                AppLog.Line("Last Checked for updates: {0}", LastCheck.ToString());
            } catch { }

            LoadProviders(source);

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

            Program.ipc.PipeMessage += new PipeIPC.DelegateMessage(PipesMessageHandler);
            Program.ipc.Listen();
        }

        private void PipesMessageHandler(PipeIPC.PipeServer pipe, string data)
        {
            if (data.Equals("show", StringComparison.CurrentCultureIgnoreCase))
            {
                notifyIcon_BalloonTipClicked(null, null);
                pipe.Send("ok");
            }
            else
            {
                pipe.Send("unknown");
            }
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
                        AppLog.Line("Starting automatic search for updates.");
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
                    Update.Date.ToString("dd.MM.yyyy"),
                    FileOps.FormatSize(Update.Size),
                    State});

                items[i].Tag = Update;

                ListViewGroup lvg = updateView.Groups[Update.Category];
                if (lvg == null)
                {
                    lvg = updateView.Groups.Add(Update.Category, Update.Category);
                    ListViewExtended.setGrpState(lvg, ListViewGroupState.Collapsible);
                }
                items[i].Group = lvg;
            }
            updateView.Items.AddRange(items);

            // Note: this has caused issues in the past
            //updateView.SetGroupState(ListViewGroupState.Collapsible);
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

            bool admin = MiscFunc.IsAdministrator() || !MiscFunc.IsRunningAsUwp();

            bool enable = agent.IsActive() && !busy;
            btnSearch.Enabled = enable;
            btnDownload.Enabled = enable && isValid2 && (CurrentList == UpdateLists.PendingUpdates);
            btnInstall.Enabled = admin && enable && isValid2 && (CurrentList == UpdateLists.PendingUpdates);
            btnUnInstall.Enabled = admin && enable && isValid2 && (CurrentList == UpdateLists.InstaledUpdates);
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

            if (Directory.Exists(Program.GetToolsPath()))
            {
                foreach (string subDir in Directory.GetDirectories(Program.GetToolsPath()))
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
            WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
            if (chkOffline.Checked)
                ret = agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
            else
                ret = agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
            ShowResult(WuAgent.AgentOperation.CheckingUpdates, ret);
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (!chkManual.Checked && !MiscFunc.IsAdministrator())
            {
                MessageBox.Show(MiscFunc.fmt("Administrator privilegs are required in order to download updates using windows update services. Use 'Manual' download instead."), Program.mName);
                return;
            }

            if (!agent.IsActive() || agent.IsBusy())
                return;
            WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
            if (chkManual.Checked)
                ret = agent.DownloadUpdatesManually(GetUpdates());
            else
                ret = agent.DownloadUpdates(GetUpdates());
            ShowResult(WuAgent.AgentOperation.DownloadingUpdates, ret);
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (!MiscFunc.IsAdministrator())
            {
                MessageBox.Show(MiscFunc.fmt("Administrator privilegs are required in order to install updates."), Program.mName);
                return;
            }

            if (!agent.IsActive() || agent.IsBusy())
                return;
            WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
            if (chkManual.Checked)
                ret = agent.DownloadUpdatesManually(GetUpdates(), true);
            else
                ret = agent.DownloadUpdates(GetUpdates(), true);
            ShowResult(WuAgent.AgentOperation.InstallingUpdates, ret);
        }

        private void btnUnInstall_Click(object sender, EventArgs e)
        {
            if (!MiscFunc.IsAdministrator())
            {
                MessageBox.Show(MiscFunc.fmt("Administrator privilegs are required in order to remove updates."), Program.mName);
                return;
            }

            if (!agent.IsActive() || agent.IsBusy())
                return;
            WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
            if (chkManual.Checked)
                ret = agent.UnInstallUpdatesOffline(GetUpdates());
            else
                ret = agent.UnInstallUpdates(GetUpdates());
            ShowResult(WuAgent.AgentOperation.RemoveingUpdates, ret);
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
            string Links = "";
            foreach (MsUpdate Update in GetUpdates())
            {
                Links += Update.Title + "\r\n";
                foreach (string url in Update.Downloads)
                    Links += url + "\r\n";
                Links += "\r\n";
            }

            if (Links.Length != 0)
            {
                Clipboard.SetText(Links);
                AppLog.Line("Update Download Links copyed to clipboard");
            }
            else
                AppLog.Line("No updates sellected");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            agent.CancelOperations();
        }

        string GetOpStr(WuAgent.AgentOperation op)
        {
            switch (op)
            {
                case WuAgent.AgentOperation.CheckingUpdates: return "Checking for Updates";
                case WuAgent.AgentOperation.PreparingCheck: return "Preparing Check"; 
                case WuAgent.AgentOperation.PreparingUpdates:
                case WuAgent.AgentOperation.DownloadingUpdates: return "Downloading Updates"; 
                case WuAgent.AgentOperation.InstallingUpdates: return "Installing Updates"; 
                case WuAgent.AgentOperation.RemoveingUpdates: return "Removing Updates"; 
                case WuAgent.AgentOperation.CancelingOperation: return "Canceling Operation"; 
            }
            return "Unknown Operation";
        }

        void OnProgress(object sender, WuAgent.ProgressArgs args)
        {
            string Status = GetOpStr(agent.CurOperation());

            if (args.TotalCount == -1)
            {
                progTotal.Style = ProgressBarStyle.Marquee;
                progTotal.MarqueeAnimationSpeed = 30;
                Status += "...";
            }
            else
            {
                progTotal.Style = ProgressBarStyle.Continuous;
                progTotal.MarqueeAnimationSpeed = 0;

                if(args.TotalPercent >= 0 && args.TotalPercent <= 100)
                    progTotal.Value = args.TotalPercent;

                if(args.TotalCount > 1)
                    Status += " " + args.CurrentIndex + "/" + args.TotalCount + " ";

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

                if (MiscFunc.parseInt(Program.IniReadValue("Options", "Refresh", "0")) == 1 && (agent.CurOperation() == WuAgent.AgentOperation.InstallingUpdates || agent.CurOperation() == WuAgent.AgentOperation.RemoveingUpdates))
                    doUpdte = true;
            }
        }

        void OnFinished(object sender, WuAgent.FinishedArgs args)
        {
            UpdateState();
            lblStatus.Text = "";
            toolTip.SetToolTip(lblStatus, "");

            ShowResult(args.Op, args.Ret, args.RebootNeeded);
        }

        private void ShowResult(WuAgent.AgentOperation op, WuAgent.RetCodes ret, bool reboot = false)
        {
            if (op == WuAgent.AgentOperation.DownloadingUpdates && chkManual.Checked)
            {
                if (ret == WuAgent.RetCodes.Success)
                {
                    MessageBox.Show(MiscFunc.fmt("Updates downloaded to {0}, \r\nready to be installed by the user.", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else if (ret == WuAgent.RetCodes.DownloadFailed)
                {
                    MessageBox.Show(MiscFunc.fmt("Updates downloaded to {0}, \r\nsome updates failed to download.", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            if (op == WuAgent.AgentOperation.InstallingUpdates && reboot)
            {
                if (ret == WuAgent.RetCodes.Success)
                {
                    MessageBox.Show(MiscFunc.fmt("Updates successfully installed, howeever a reboot is required.", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else if (ret == WuAgent.RetCodes.DownloadFailed)
                {
                    MessageBox.Show(MiscFunc.fmt("Instalation of some Updates has failed, also a reboot is required.", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            string status = "";
            switch (ret)
            {
                case WuAgent.RetCodes.Success:
                case WuAgent.RetCodes.Abborted:
                case WuAgent.RetCodes.InProgress: return;
                case WuAgent.RetCodes.AccessError: status = MiscFunc.fmt("Required privilegs are not available"); break;
                case WuAgent.RetCodes.Busy: status = MiscFunc.fmt("Anotehr operation is already in progress"); break;
                case WuAgent.RetCodes.DownloadFailed: status = MiscFunc.fmt("Download failed"); break;
                case WuAgent.RetCodes.InstallFailed: status = MiscFunc.fmt("Instalation failed"); break;
                case WuAgent.RetCodes.NoUpdated: status = MiscFunc.fmt("No selected updates or no updates eligible for the operation"); break;
                case WuAgent.RetCodes.InternalError: status = MiscFunc.fmt("Inernal error"); break;
                case WuAgent.RetCodes.FileNotFound: status = MiscFunc.fmt("Required file(s) could not be found"); break;
            }

            string action = GetOpStr(op);

            MessageBox.Show(MiscFunc.fmt("{0} failed: {1}.", action, status), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void dlSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetConfig("Source", dlSource.Text);
        }

        private void chkOffline_CheckedChanged(object sender, EventArgs e)
        {
            dlSource.Enabled = !chkOffline.Checked;
            chkDownload.Enabled = chkOffline.Checked;

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

        private void dlShDay_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.ConfigAU(GPO.AUOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
        }

        private void dlShTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.ConfigAU(GPO.AUOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
        }

        private void radGPO_CheckedChanged(object sender, EventArgs e)
        {
            dlShDay.Enabled = dlShTime.Enabled = radSchedule.Checked;

            if (radDisable.Checked)
            {
                switch (mGPORespect)
                {
                    case GPO.Respect.Partial:
                        if (chkBlockMS.Checked == true)
                        {
                            chkDisableAU.Enabled = true;
                            break;
                        }
                        goto case GPO.Respect.None;
                    case GPO.Respect.None:
                        chkDisableAU.Enabled = false;
                        chkDisableAU.Checked = true;
                        break;
                    case GPO.Respect.Full: // we can do whatever we want
                        chkDisableAU.Enabled = mWinVersion >= 10;
                        break;
                }
            }
            else
                chkDisableAU.Enabled = false;

            if (mSuspendUpdate)
                return;

            if (radDisable.Checked)
            {
                if(chkDisableAU.Checked)
                    GPO.DisableAU(true);

                GPO.ConfigAU(GPO.AUOptions.Disabled);
            }
            else
            {
                chkDisableAU.Checked = false; // Note: this triggers chkDisableAU_CheckedChanged

                if (radNotify.Checked)
                    GPO.ConfigAU(GPO.AUOptions.Notification);
                else if (radDownload.Checked)
                    GPO.ConfigAU(GPO.AUOptions.Download);
                else if (radSchedule.Checked)
                    GPO.ConfigAU(GPO.AUOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
                else //if (radDefault.Checked)
                    GPO.ConfigAU(GPO.AUOptions.Default);
            }
        }

        private void chkBlockMS_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;

            if (radDisable.Checked && mGPORespect == GPO.Respect.Partial)
            {
                if (chkBlockMS.Checked)
                {
                    chkDisableAU.Enabled = true;
                }
                else
                {
                    if (!chkDisableAU.Checked)
                    {
                        switch (MessageBox.Show("Your version of Windows does not respect the standard GPO's, to keep autoamtic windows update disabled update facilitation services must be disabled.", Program.mName, MessageBoxButtons.YesNoCancel))
                        {
                            case DialogResult.Yes:
                                chkDisableAU.Checked = true; // Note: this triggers chkDisableAU_CheckedChanged
                                break;
                            case DialogResult.No:
                                radDefault.Checked = true;
                                break;
                            case DialogResult.Cancel:
                                mSuspendUpdate = true;
                                chkBlockMS.Checked = true;
                                mSuspendUpdate = false;
                                return;
                        }
                    }
                    chkDisableAU.Enabled = false;
                }
            }

            GPO.BlockMS(chkBlockMS.Checked);
        }

        private void chkDisableAU_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDisableAU.Checked)
            {
                chkHideWU.Checked = true;
                chkHideWU.Enabled = false;
            }
            else
                chkHideWU.Enabled = true;

            if (mSuspendUpdate)
                return;
            GPO.DisableAU(chkDisableAU.Checked);
        }

        private void chkAutoRun_CheckedChanged(object sender, EventArgs e)
        {
            notifyIcon.Visible = dlAutoCheck.Enabled = chkAutoRun.Checked;
            AutoUpdate = chkAutoRun.Checked ? (AutoUpdateOptions)dlAutoCheck.SelectedIndex : AutoUpdateOptions.No;
            if (mSuspendUpdate)
                return;
            if (chkAutoRun.CheckState == CheckState.Indeterminate)
                return;
            if (MiscFunc.IsRunningAsUwp())
            {
                if (chkAutoRun.CheckState == CheckState.Checked)
                {
                    mSuspendUpdate = true;
                    chkAutoRun.CheckState = CheckState.Indeterminate;
                    mSuspendUpdate = false;
                }
                return;
            }
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

        private void chkStore_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;
            GPO.SetStoreAU(chkStore.Checked);
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
