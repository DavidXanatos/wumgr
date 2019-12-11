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
using System.Globalization;

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

        float mSearchBoxHeight = 0.0f;
        string mSearchFilter = null;
        bool bUpdateList = false;

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
                this.Text = string.Format("{0} v{1} by David Xanatos", Program.mName, Program.mVersion);

            Localize();

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
                if (MessageBox.Show(Translate.fmt("msg_wuau"), Program.mName, MessageBoxButtons.YesNo) == DialogResult.Yes)
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
            try{
                dlShDay.SelectedIndex = day; dlShTime.SelectedIndex = time;
            }catch{ }

            if (mWinVersion >= 10) // 10 or abive
                chkDisableAU.Checked = GPO.GetDisableAU();

            if (mWinVersion < 6.2) // win 7 or below
                chkStore.Enabled = false;
            chkStore.Checked = GPO.GetStoreAU();

            try{
                dlAutoCheck.SelectedIndex = MiscFunc.parseInt(GetConfig("AutoUpdate", "0"));
            }catch{ }
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
                foreach (Control ctl in tabAU.Controls)
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
                AppLog.Line("Last Checked for updates: {0}", LastCheck.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern));
            } catch {
                LastCheck = DateTime.Now;
            }

            LoadProviders(source);

            mSearchBoxHeight = this.panelList.RowStyles[2].Height;
            this.panelList.RowStyles[2].Height = 0;

            chkGrupe.Checked = MiscFunc.parseInt(GetConfig("GroupUpdates", "1")) != 0;
            updateView.ShowGroups = chkGrupe.Checked;

            mSuspendUpdate = false;


            if (Program.TestArg("-provisioned"))
                tabs.Enabled = false;


            mToolsMenu = new MenuItem();
            mToolsMenu.Text = Translate.fmt("menu_tools");

            BuildToolsMenu();

            notifyIcon.ContextMenu = new ContextMenu();

            MenuItem menuAbout = new MenuItem();
            menuAbout.Text = Translate.fmt("menu_about"); 
            menuAbout.Click += new System.EventHandler(menuAbout_Click);

            MenuItem menuExit = new MenuItem();
            menuExit.Text = Translate.fmt("menu_exit"); 
            menuExit.Click += new System.EventHandler(menuExit_Click);

            notifyIcon.ContextMenu.MenuItems.AddRange(new MenuItem[] { mToolsMenu, menuAbout, new MenuItem("-"), menuExit });


            IntPtr MenuHandle = GetSystemMenu(this.Handle, false); // Note: to restore default set true
            InsertMenu(MenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
            InsertMenu(MenuHandle, 6, MF_BYPOSITION | MF_POPUP, (int)mToolsMenu.Handle, mToolsMenu.Text);
            InsertMenu(MenuHandle, 7, MF_BYPOSITION, MYMENU_ABOUT, menuAbout.Text);


            UpdateCounts();
            SwitchList(UpdateLists.UpdateHistory);

            doUpdte = Program.TestArg("-update");

            mTimer = new Timer();
            mTimer.Interval = 250; // 4 times per second
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
                            notifyIcon.ShowBalloonTip(int.MaxValue, Translate.fmt("cap_chk_upd"), Translate.fmt("msg_chk_upd", Program.mName, daysDue), ToolTipIcon.Warning);
                        }
                    }
                }

                if (agent.mPendingUpdates.Count > 0)
                {
                    if (LastBaloon < DateTime.Now.AddHours(-4))
                    {
                        LastBaloon = DateTime.Now;
                        notifyIcon.ShowBalloonTip(int.MaxValue, Translate.fmt("cap_new_upd"), Translate.fmt("msg_new_upd", Program.mName, agent.mPendingUpdates.Count), ToolTipIcon.Info);
                    }
                }
            }

            if ((doUpdte || (updateNow && !ResultShown)) && agent.IsActive())
            {
                doUpdte = false;
                if (chkOffline.Checked)
                    agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
                else
                    agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
            }

            if (bUpdateList)
            {
                bUpdateList = false;
                LoadList();
            }

            if (checkChecks)
                UpdateState();
        }

        private void WuMgr_Load(object sender, EventArgs e)
        {
            this.Width = 900;
        }

        private int GetAutoUpdateDue()
        {
            try
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
            catch
            {
                LastCheck = DateTime.Now;
                return 0;
            }
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
            btnWinUpd.Text = Translate.fmt("lbl_fnd_upd", agent.mPendingUpdates.Count);
            btnInstalled.Text = Translate.fmt("lbl_inst_upd", agent.mInstalledUpdates.Count);
            btnHidden.Text = Translate.fmt("lbl_block_upd", agent.mHiddenUpdates.Count);
            btnHistory.Text = Translate.fmt("lbl_old_upd", agent.mUpdateHistory.Count);
        }

        void LoadList()
        {
            ignoreChecks = true;
            updateView.CheckBoxes = CurrentList != UpdateLists.UpdateHistory;
            ignoreChecks = false;
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
            string INIPath = Program.wrkPath + @"\Updates.ini";

            updateView.Items.Clear();
            List<ListViewItem> items = new List<ListViewItem>();
            for (int i = 0; i < List.Count; i++)
            {
                MsUpdate Update = List[i];
                string State = "";
                switch (Update.State)
                {
                    case MsUpdate.UpdateState.History:
                        switch ((OperationResultCode)Update.ResultCode)
                        {
                            case OperationResultCode.orcNotStarted: State = Translate.fmt("stat_not_start"); break;
                            case OperationResultCode.orcInProgress: State = Translate.fmt("stat_in_prog"); break;
                            case OperationResultCode.orcSucceeded: State = Translate.fmt("stat_success"); break;
                            case OperationResultCode.orcSucceededWithErrors: State = Translate.fmt("stat_success_2"); break;
                            case OperationResultCode.orcFailed: State = Translate.fmt("stat_failed"); break;
                            case OperationResultCode.orcAborted: State = Translate.fmt("stat_abbort"); break;
                        }
                        State += " (0x" + String.Format("{0:X8}", Update.HResult) + ")";
                        break;

                    default:
                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Beta) != 0)
                            State = Translate.fmt("stat_beta" + " ");

                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Installed) != 0)
                        {
                            State += Translate.fmt("stat_install");
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Uninstallable) != 0)
                                State += " " + Translate.fmt("stat_rem");
                        }
                        else if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Hidden) != 0)
                        {
                            State += Translate.fmt("stat_block"); 
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
                                State += " " + Translate.fmt("stat_dl");
                        }
                        else
                        {
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
                                State += Translate.fmt("stat_dl");
                            else
                                State += Translate.fmt("stat_pending");
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.AutoSelect) != 0)
                                State += " " + Translate.fmt("stat_sel");
                            if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Mandatory) != 0)
                                State += " " + Translate.fmt("stat_mand");
                        }

                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Exclusive) != 0)
                            State += ", " + Translate.fmt("stat_excl");

                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Reboot) != 0)
                            State += ", " + Translate.fmt("stat_reboot"); 
                        break;
                }


                string[] strings = new string[] {
                    Update.Title,
                    Update.Category,
                    CurrentList == UpdateLists.UpdateHistory ? Update.ApplicationID : Update.KB,
                    Update.Date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern),
                    FileOps.FormatSize(Update.Size),
                    State};

                if (mSearchFilter != null)
                {
                    bool match = false;
                    foreach (string str in strings)
                    {
                        if (str.IndexOf(mSearchFilter, StringComparison.CurrentCultureIgnoreCase) != -1)
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                        continue;
                }

                ListViewItem item = new ListViewItem(strings);
                item.SubItems[3].Tag = Update.Date;
                item.SubItems[4].Tag = Update.Size;


                item.Tag = Update;

                if (CurrentList == UpdateLists.PendingUpdates)
                {
                    if (MiscFunc.parseInt(Program.IniReadValue(Update.KB, "BlackList", "0", INIPath)) != 0)
                        item.Font = new Font(item.Font.FontFamily, item.Font.Size, FontStyle.Strikeout);
                    else if (MiscFunc.parseInt(Program.IniReadValue(Update.KB, "Select", "0", INIPath)) != 0)
                        item.Checked = true;
                }
                else if (CurrentList == UpdateLists.InstaledUpdates)
                {
                    if (MiscFunc.parseInt(Program.IniReadValue(Update.KB, "Remove", "0", INIPath)) != 0)
                        item.Checked = true;
                }

                string colorStr = Program.IniReadValue(Update.KB, "Color", "", INIPath);
                if (colorStr.Length > 0)
                {
                    Color? color = MiscFunc.parseColor(colorStr);
                    if (color != null)
                        item.BackColor = (Color)color;
                }

                ListViewGroup lvg = updateView.Groups[Update.Category];
                if (lvg == null)
                {
                    lvg = updateView.Groups.Add(Update.Category, Update.Category);
                    ListViewExtended.setGrpState(lvg, ListViewGroupState.Collapsible);
                }
                item.Group = lvg;
                items.Add(item);
            }
            updateView.Items.AddRange(items.ToArray());

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

            updateView.Columns[2].Text = (CurrentList == UpdateLists.UpdateHistory) ? Translate.fmt("col_app_id") : Translate.fmt("col_kb");

            LoadList();

            UpdateState();

            lblSupport.Visible = false;
        }

        private void UpdateState()
        {
            checkChecks = false;

            bool isChecked = updateView.CheckedItems.Count > 0;

            bool busy = agent.IsBusy();
            btnCancel.Visible = busy;
            progTotal.Visible = busy;
            lblStatus.Visible = busy;

            bool isValid = agent.IsValid();
            bool isValid2 = isValid || chkManual.Checked;

            bool admin = MiscFunc.IsAdministrator() || !MiscFunc.IsRunningAsUwp();

            bool enable = agent.IsActive() && !busy;
            btnSearch.Enabled = enable;
            btnDownload.Enabled = isChecked && enable && isValid2 && (CurrentList == UpdateLists.PendingUpdates);
            btnInstall.Enabled = isChecked && admin && enable && isValid2 && (CurrentList == UpdateLists.PendingUpdates);
            btnUnInstall.Enabled = isChecked && admin && enable && (CurrentList == UpdateLists.InstaledUpdates);
            btnHide.Enabled = isChecked && enable && isValid && (CurrentList == UpdateLists.PendingUpdates || CurrentList == UpdateLists.HiddenUpdates);
            btnGetLink.Enabled = isChecked && CurrentList != UpdateLists.UpdateHistory;
        }

        private MenuItem mToolsMenu = null;
        private MenuItem wuauMenu = null;

        private void BuildToolsMenu()
        {
            wuauMenu = new MenuItem();
            wuauMenu.Text = Translate.fmt("menu_wuau");
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
            refreshMenu.Text = Translate.fmt("menu_refresh");
            refreshMenu.Click += new System.EventHandler(menuRefresh_Click);
            mToolsMenu.MenuItems.Add(refreshMenu);
        }

        private void menuExec_Click(object Sender, EventArgs e, string exec, string dir, bool silent = false)
        {
            ProcessStartInfo startInfo = Program.PrepExec(exec, silent);
            startInfo.WorkingDirectory = dir;
            if(!Program.DoExec(startInfo))
                MessageBox.Show(Translate.fmt("msg_tool_err"), Program.mName);
        }

        private void menuExit_Click(object Sender, EventArgs e)
        {
            Application.Exit();
        }

        private void menuAbout_Click(object Sender, EventArgs e)
        {
            string About = "";
            About += "Author: \tDavid Xanatos\r\n";
            About += "Licence: \tGNU General Public License v3\r\n";
            About += string.Format("Version: \t{0}\r\n", Program.mVersion);
            About += "\r\n";
            About += "Source: \thttps://github.com/DavidXanatos/wumgr\r\n";
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
            InsertMenu(MenuHandle, 6, MF_BYPOSITION | MF_POPUP, (int)mToolsMenu.Handle, Translate.fmt("menu_tools"));
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
                MessageBox.Show(Translate.fmt("msg_admin_dl"), Program.mName);
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
                MessageBox.Show(Translate.fmt("msg_admin_inst"), Program.mName);
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
                MessageBox.Show(Translate.fmt("msg_admin_rem"), Program.mName);
                return;
            }

            if (!agent.IsActive() || agent.IsBusy())
                return;
            WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
            ret = agent.UnInstallUpdatesManually(GetUpdates());
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
                AppLog.Line("No updates selected");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            agent.CancelOperations();
        }

        string GetOpStr(WuAgent.AgentOperation op)
        {
            switch (op)
            {
                case WuAgent.AgentOperation.CheckingUpdates: return Translate.fmt("op_check");
                case WuAgent.AgentOperation.PreparingCheck: return Translate.fmt("op_prep"); 
                case WuAgent.AgentOperation.PreparingUpdates:
                case WuAgent.AgentOperation.DownloadingUpdates: return Translate.fmt("op_dl"); 
                case WuAgent.AgentOperation.InstallingUpdates: return Translate.fmt("op_inst"); 
                case WuAgent.AgentOperation.RemoveingUpdates: return Translate.fmt("op_rem"); 
                case WuAgent.AgentOperation.CancelingOperation: return Translate.fmt("op_cancel"); 
            }
            return Translate.fmt("op_unk");
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
                LastCheck = DateTime.Now;
                SetConfig("LastCheck", LastCheck.ToString());
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

        bool ResultShown = false;

        private void ShowResult(WuAgent.AgentOperation op, WuAgent.RetCodes ret, bool reboot = false)
        {
            if (op == WuAgent.AgentOperation.DownloadingUpdates && chkManual.Checked)
            {
                if (ret == WuAgent.RetCodes.Success)
                {
                    MessageBox.Show(Translate.fmt("msg_dl_done", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else if (ret == WuAgent.RetCodes.DownloadFailed)
                {
                    MessageBox.Show(Translate.fmt("msg_dl_err", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            if (op == WuAgent.AgentOperation.InstallingUpdates && reboot)
            {
                if (ret == WuAgent.RetCodes.Success)
                {
                    MessageBox.Show(Translate.fmt("msg_inst_done", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else if (ret == WuAgent.RetCodes.DownloadFailed)
                {
                    MessageBox.Show(Translate.fmt("msg_inst_err", agent.dlPath), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }

            string status = "";
            switch (ret)
            {
                case WuAgent.RetCodes.Success:
                case WuAgent.RetCodes.Abborted:
                case WuAgent.RetCodes.InProgress: return;
                case WuAgent.RetCodes.AccessError: status = Translate.fmt("err_admin"); break;
                case WuAgent.RetCodes.Busy: status = Translate.fmt("err_busy"); break;
                case WuAgent.RetCodes.DownloadFailed: status = Translate.fmt("err_dl"); break;
                case WuAgent.RetCodes.InstallFailed: status = Translate.fmt("err_inst"); break;
                case WuAgent.RetCodes.NoUpdated: status = Translate.fmt("err_no_sel"); break;
                case WuAgent.RetCodes.InternalError: status = Translate.fmt("err_int"); break;
                case WuAgent.RetCodes.FileNotFound: status = Translate.fmt("err_file"); break;
            }

            string action = GetOpStr(op);

            ResultShown = true;
            MessageBox.Show(Translate.fmt("msg_err", action, status), Program.mName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            ResultShown = false;
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
                if (chkDisableAU.Checked)
                {
                    bool test = GPO.GetDisableAU();
                    GPO.DisableAU(true);
                    if(!test)
                        MessageBox.Show(Translate.fmt("msg_disable_au"));
                }

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
                        switch (MessageBox.Show(Translate.fmt("msg_gpo"), Program.mName, MessageBoxButtons.YesNoCancel))
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
            {
                //chkHideWU.Checked = false;
                chkHideWU.Enabled = true;
            }

            if (mSuspendUpdate)
                return;
            bool test = GPO.GetDisableAU();
            GPO.DisableAU(chkDisableAU.Checked);
            if(test != chkDisableAU.Checked)
                MessageBox.Show(Translate.fmt("msg_disable_au"));
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
            lblSupport.Visible = false;
            if (updateView.SelectedItems.Count == 1)
            {
                MsUpdate Update = (MsUpdate)updateView.SelectedItems[0].Tag;
                if (Update.KB != null && Update.KB.Length > 2)
                {
                    lblSupport.Links[0].LinkData = "https://support.microsoft.com/help/" + Update.KB.Substring(2);
                    lblSupport.Links[0].Visited = false;
                    lblSupport.Visible = true;
                    toolTip.SetToolTip(lblSupport, lblSupport.Links[0].LinkData.ToString());
                }
            }
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
            if(updateView.ListViewItemSorter == null)
                updateView.ListViewItemSorter = new ListViewItemComparer();
            ((ListViewItemComparer)updateView.ListViewItemSorter).Update(e.Column);
            updateView.Sort();
        }

        // Implements the manual sorting of items by columns.
        class ListViewItemComparer : IComparer
        {
            private int col;
            private int inv;
            public ListViewItemComparer()
            {
                col = 0;
                inv = 1;
            }
            public void Update(int column)
            {
                if (col == column)
                    inv = -inv;
                else
                    inv = 1;
                col = column;
            }

            public int Compare(object x, object y)
            {
                if (col == 3) // date
                    return ((DateTime)((ListViewItem)y).SubItems[col].Tag).CompareTo(((DateTime)((ListViewItem)x).SubItems[col].Tag)) * inv;
                if (col == 4) // size
                    return ((decimal)((ListViewItem)y).SubItems[col].Tag).CompareTo(((decimal)((ListViewItem)x).SubItems[col].Tag)) * inv;
                return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text) * inv;
            }
        }


        private void Localize()
        {

            btnWinUpd.Text = Translate.fmt("lbl_fnd_upd", 0);
            btnInstalled.Text = Translate.fmt("lbl_inst_upd", 0);
            btnHidden.Text = Translate.fmt("lbl_block_upd", 0);
            btnHistory.Text = Translate.fmt("lbl_old_upd", 0);

            toolTip.SetToolTip(btnSearch, Translate.fmt("tip_search"));
            toolTip.SetToolTip(btnInstall, Translate.fmt("tip_inst"));
            toolTip.SetToolTip(btnDownload, Translate.fmt("tip_dl"));
            toolTip.SetToolTip(btnHide, Translate.fmt("tip_hide"));
            toolTip.SetToolTip(btnGetLink, Translate.fmt("tip_lnk"));
            toolTip.SetToolTip(btnUnInstall, Translate.fmt("tip_rem"));
            toolTip.SetToolTip(btnCancel, Translate.fmt("tip_cancel"));

            updateView.Columns[0].Text = Translate.fmt("col_title");
            updateView.Columns[1].Text = Translate.fmt("col_cat");
            updateView.Columns[2].Text = Translate.fmt("col_kb");
            updateView.Columns[3].Text = Translate.fmt("col_date");
            updateView.Columns[4].Text = Translate.fmt("col_site");
            updateView.Columns[5].Text = Translate.fmt("col_stat");

            chkGrupe.Text = Translate.fmt("lbl_group");
            chkAll.Text = Translate.fmt("lbl_all");

            lblSupport.Text = Translate.fmt("lbl_support");
            lblPatreon.Text = Translate.fmt("lbl_patreon");
            //string cc = "";
            //toolTip.SetToolTip(lblPatreon, cc);

            lblSearch.Text = Translate.fmt("lbl_search");

            tabOptions.Text = Translate.fmt("lbl_opt");

            chkOffline.Text = Translate.fmt("lbl_off");
            chkDownload.Text = Translate.fmt("lbl_dl");
            chkManual.Text = Translate.fmt("lbl_man");
            chkOld.Text = Translate.fmt("lbl_old");
            chkMsUpd.Text = Translate.fmt("lbl_ms");

            gbStartup.Text = Translate.fmt("lbl_start");
            chkAutoRun.Text = Translate.fmt("lbl_auto");
            dlAutoCheck.Items.Clear();
            dlAutoCheck.Items.Add(Translate.fmt("lbl_ac_no"));
            dlAutoCheck.Items.Add(Translate.fmt("lbl_ac_day"));
            dlAutoCheck.Items.Add(Translate.fmt("lbl_ac_week"));
            dlAutoCheck.Items.Add(Translate.fmt("lbl_ac_month"));
            chkNoUAC.Text = Translate.fmt("lbl_uac");


            tabAU.Text = Translate.fmt("lbl_au");

            chkBlockMS.Text = Translate.fmt("lbl_block_ms");
            radDisable.Text = Translate.fmt("lbl_au_off");
            chkDisableAU.Text = Translate.fmt("lbl_au_dissable");
            radNotify.Text = Translate.fmt("lbl_au_notify");
            radDownload.Text = Translate.fmt("lbl_au_dl");
            radSchedule.Text = Translate.fmt("lbl_au_time");
            radDefault.Text = Translate.fmt("lbl_au_def");
            chkHideWU.Text = Translate.fmt("lbl_hide");
            chkStore.Text = Translate.fmt("lbl_store");
            chkDrivers.Text = Translate.fmt("lbl_drv");

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                this.panelList.RowStyles[2].Height = mSearchBoxHeight;
                this.txtFilter.SelectAll();
                this.txtFilter.Focus();
                return true;
            }

            if (keyData == (Keys.Control | Keys.C))
            {
                string Info = "";
                foreach (ListViewItem item in updateView.SelectedItems)
                {
                    if(Info.Length != 0)
                        Info += "\r\n";
                    Info += item.Text;
                    for(int i=1; i < item.SubItems.Count; i++)
                        Info += "; " + item.SubItems[i].Text;
                }

                if (Info.Length != 0)
                    Clipboard.SetText(Info);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void btnSearchOff_Click(object sender, EventArgs e)
        {
            this.panelList.RowStyles[2].Height = 0;
            mSearchFilter = null;
            LoadList();
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            mSearchFilter = txtFilter.Text;
            bUpdateList = true;
        }

        private void chkGrupe_CheckedChanged(object sender, EventArgs e)
        {
            if (mSuspendUpdate)
                return;

            updateView.ShowGroups = chkGrupe.Checked;
            SetConfig("GroupUpdates", chkGrupe.Checked ? "1" : "0");
        }

        bool checkChecks = false;
        bool ignoreChecks = false;

        private void chkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (ignoreChecks)
                return;

            ignoreChecks = true;

            foreach (ListViewItem item in updateView.Items)
                item.Checked = chkAll.Checked;

            ignoreChecks = false;

            checkChecks = true;
        }

        private void updateView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (ignoreChecks)
                return;

            ignoreChecks = true;

            if (updateView.CheckedItems.Count == 0)
                chkAll.CheckState = CheckState.Unchecked;
            else if (updateView.CheckedItems.Count == updateView.Items.Count)
                chkAll.CheckState = CheckState.Checked;
            else
                chkAll.CheckState = CheckState.Indeterminate;

            ignoreChecks = false;

            checkChecks = true;
        }

        private void lblPatreon_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.patreon.com/DavidXanatos");
        }
    }
}
