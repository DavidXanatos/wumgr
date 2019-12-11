namespace wumgr
{
    partial class WuMgr
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WuMgr));
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.chkAutoRun = new System.Windows.Forms.CheckBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.panelList = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.lblSupport = new System.Windows.Forms.LinkLabel();
            this.chkGrupe = new System.Windows.Forms.CheckBox();
            this.chkAll = new System.Windows.Forms.CheckBox();
            this.lblPatreon = new System.Windows.Forms.LinkLabel();
            this.updateView = new wumgr.ListViewExtended();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.logBox = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.btnSearchOff = new System.Windows.Forms.Button();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnDownload = new System.Windows.Forms.Button();
            this.btnInstall = new System.Windows.Forms.Button();
            this.btnUnInstall = new System.Windows.Forms.Button();
            this.btnHide = new System.Windows.Forms.Button();
            this.btnGetLink = new System.Windows.Forms.Button();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.progTotal = new System.Windows.Forms.ProgressBar();
            this.btnHistory = new System.Windows.Forms.CheckBox();
            this.btnHidden = new System.Windows.Forms.CheckBox();
            this.btnInstalled = new System.Windows.Forms.CheckBox();
            this.btnWinUpd = new System.Windows.Forms.CheckBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabOptions = new System.Windows.Forms.TabPage();
            this.gbStartup = new System.Windows.Forms.GroupBox();
            this.chkNoUAC = new System.Windows.Forms.CheckBox();
            this.dlAutoCheck = new System.Windows.Forms.ComboBox();
            this.dlSource = new System.Windows.Forms.ComboBox();
            this.chkOffline = new System.Windows.Forms.CheckBox();
            this.chkMsUpd = new System.Windows.Forms.CheckBox();
            this.chkOld = new System.Windows.Forms.CheckBox();
            this.chkManual = new System.Windows.Forms.CheckBox();
            this.chkDownload = new System.Windows.Forms.CheckBox();
            this.tabAU = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.chkDrivers = new System.Windows.Forms.CheckBox();
            this.chkStore = new System.Windows.Forms.CheckBox();
            this.chkHideWU = new System.Windows.Forms.CheckBox();
            this.chkDisableAU = new System.Windows.Forms.CheckBox();
            this.radDefault = new System.Windows.Forms.RadioButton();
            this.radSchedule = new System.Windows.Forms.RadioButton();
            this.radDownload = new System.Windows.Forms.RadioButton();
            this.chkBlockMS = new System.Windows.Forms.CheckBox();
            this.radNotify = new System.Windows.Forms.RadioButton();
            this.radDisable = new System.Windows.Forms.RadioButton();
            this.dlShDay = new System.Windows.Forms.ComboBox();
            this.dlShTime = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panelList.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tabs.SuspendLayout();
            this.tabOptions.SuspendLayout();
            this.gbStartup.SuspendLayout();
            this.tabAU.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkAutoRun
            // 
            this.chkAutoRun.AutoSize = true;
            this.chkAutoRun.Location = new System.Drawing.Point(3, 16);
            this.chkAutoRun.Name = "chkAutoRun";
            this.chkAutoRun.Size = new System.Drawing.Size(117, 17);
            this.chkAutoRun.TabIndex = 0;
            this.chkAutoRun.Text = "Run in background";
            this.chkAutoRun.ThreeState = true;
            this.toolTip.SetToolTip(this.chkAutoRun, "Auto Start with Windows");
            this.chkAutoRun.UseVisualStyleBackColor = true;
            this.chkAutoRun.CheckedChanged += new System.EventHandler(this.chkAutoRun_CheckedChanged);
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "notifyIcon1";
            this.notifyIcon.BalloonTipClicked += new System.EventHandler(this.notifyIcon_BalloonTipClicked);
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // panelList
            // 
            this.panelList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelList.ColumnCount = 1;
            this.panelList.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelList.Controls.Add(this.tableLayoutPanel7, 0, 0);
            this.panelList.Controls.Add(this.updateView, 0, 1);
            this.panelList.Controls.Add(this.logBox, 0, 3);
            this.panelList.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.panelList.Location = new System.Drawing.Point(188, 0);
            this.panelList.Margin = new System.Windows.Forms.Padding(0);
            this.panelList.Name = "panelList";
            this.panelList.RowCount = 4;
            this.panelList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.panelList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.panelList.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.panelList.Size = new System.Drawing.Size(495, 443);
            this.panelList.TabIndex = 1;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel7.ColumnCount = 4;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel7.Controls.Add(this.lblSupport, 3, 0);
            this.tableLayoutPanel7.Controls.Add(this.chkGrupe, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.chkAll, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.lblPatreon, 2, 0);
            this.tableLayoutPanel7.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel7.Size = new System.Drawing.Size(495, 20);
            this.tableLayoutPanel7.TabIndex = 5;
            // 
            // lblSupport
            // 
            this.lblSupport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSupport.AutoSize = true;
            this.lblSupport.Location = new System.Drawing.Point(423, 5);
            this.lblSupport.Name = "lblSupport";
            this.lblSupport.Size = new System.Drawing.Size(69, 13);
            this.lblSupport.TabIndex = 0;
            this.lblSupport.TabStop = true;
            this.lblSupport.Text = "Support URL";
            this.lblSupport.Visible = false;
            this.lblSupport.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblSupport_LinkClicked);
            // 
            // chkGrupe
            // 
            this.chkGrupe.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkGrupe.AutoSize = true;
            this.chkGrupe.Location = new System.Drawing.Point(79, 3);
            this.chkGrupe.Name = "chkGrupe";
            this.chkGrupe.Size = new System.Drawing.Size(98, 17);
            this.chkGrupe.TabIndex = 1;
            this.chkGrupe.Text = "Group Updates";
            this.chkGrupe.UseVisualStyleBackColor = true;
            this.chkGrupe.CheckedChanged += new System.EventHandler(this.chkGrupe_CheckedChanged);
            // 
            // chkAll
            // 
            this.chkAll.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAll.AutoSize = true;
            this.chkAll.Location = new System.Drawing.Point(3, 3);
            this.chkAll.Name = "chkAll";
            this.chkAll.Size = new System.Drawing.Size(70, 17);
            this.chkAll.TabIndex = 2;
            this.chkAll.Text = "Select All";
            this.chkAll.UseVisualStyleBackColor = true;
            this.chkAll.CheckedChanged += new System.EventHandler(this.chkAll_CheckedChanged);
            // 
            // lblPatreon
            // 
            this.lblPatreon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPatreon.AutoSize = true;
            this.lblPatreon.Location = new System.Drawing.Point(183, 5);
            this.lblPatreon.Name = "lblPatreon";
            this.lblPatreon.Size = new System.Drawing.Size(234, 13);
            this.lblPatreon.TabIndex = 0;
            this.lblPatreon.TabStop = true;
            this.lblPatreon.Text = "Support WuMgr on Patreon";
            this.lblPatreon.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblPatreon_LinkClicked);
            // 
            // updateView
            // 
            this.updateView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.updateView.CheckBoxes = true;
            this.updateView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.updateView.HideSelection = false;
            this.updateView.Location = new System.Drawing.Point(3, 23);
            this.updateView.Name = "updateView";
            this.updateView.ShowItemToolTips = true;
            this.updateView.Size = new System.Drawing.Size(489, 292);
            this.updateView.TabIndex = 2;
            this.updateView.UseCompatibleStateImageBehavior = false;
            this.updateView.View = System.Windows.Forms.View.Details;
            this.updateView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.updateView_ColumnClick);
            this.updateView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.updateView_ItemChecked);
            this.updateView.SelectedIndexChanged += new System.EventHandler(this.updateView_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Title";
            this.columnHeader1.Width = 260;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Category";
            this.columnHeader2.Width = 100;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "KB Article";
            this.columnHeader3.Width = 80;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Date";
            this.columnHeader4.Width = 70;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Size";
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "State";
            this.columnHeader6.Width = 80;
            // 
            // logBox
            // 
            this.logBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logBox.Location = new System.Drawing.Point(3, 346);
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(489, 94);
            this.logBox.TabIndex = 4;
            this.logBox.Text = "";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel3.Controls.Add(this.btnSearchOff, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.txtFilter, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.lblSearch, 0, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 318);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(495, 25);
            this.tableLayoutPanel3.TabIndex = 6;
            // 
            // btnSearchOff
            // 
            this.btnSearchOff.Location = new System.Drawing.Point(473, 3);
            this.btnSearchOff.Name = "btnSearchOff";
            this.btnSearchOff.Size = new System.Drawing.Size(19, 19);
            this.btnSearchOff.TabIndex = 0;
            this.btnSearchOff.Text = "X";
            this.btnSearchOff.UseVisualStyleBackColor = true;
            this.btnSearchOff.Click += new System.EventHandler(this.btnSearchOff_Click);
            // 
            // txtFilter
            // 
            this.txtFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilter.Location = new System.Drawing.Point(103, 3);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(364, 20);
            this.txtFilter.TabIndex = 1;
            this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);
            // 
            // lblSearch
            // 
            this.lblSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(3, 6);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(94, 13);
            this.lblSearch.TabIndex = 2;
            this.lblSearch.Text = "Search Filter:";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel4, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tabs, 0, 1);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(188, 443);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Controls.Add(this.flowLayoutPanel1, 0, 4);
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel5, 0, 5);
            this.tableLayoutPanel4.Controls.Add(this.btnHistory, 0, 3);
            this.tableLayoutPanel4.Controls.Add(this.btnHidden, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.btnInstalled, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.btnWinUpd, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.lblStatus, 0, 6);
            this.tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 7;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 37F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(186, 210);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnSearch);
            this.flowLayoutPanel1.Controls.Add(this.btnDownload);
            this.flowLayoutPanel1.Controls.Add(this.btnInstall);
            this.flowLayoutPanel1.Controls.Add(this.btnUnInstall);
            this.flowLayoutPanel1.Controls.Add(this.btnHide);
            this.flowLayoutPanel1.Controls.Add(this.btnGetLink);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 123);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(180, 29);
            this.flowLayoutPanel1.TabIndex = 4;
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(0, 0);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(0);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(30, 30);
            this.btnSearch.TabIndex = 0;
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // btnDownload
            // 
            this.btnDownload.Location = new System.Drawing.Point(30, 0);
            this.btnDownload.Margin = new System.Windows.Forms.Padding(0);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(30, 30);
            this.btnDownload.TabIndex = 1;
            this.btnDownload.UseVisualStyleBackColor = true;
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
            // 
            // btnInstall
            // 
            this.btnInstall.Location = new System.Drawing.Point(60, 0);
            this.btnInstall.Margin = new System.Windows.Forms.Padding(0);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(30, 30);
            this.btnInstall.TabIndex = 2;
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            // 
            // btnUnInstall
            // 
            this.btnUnInstall.Location = new System.Drawing.Point(90, 0);
            this.btnUnInstall.Margin = new System.Windows.Forms.Padding(0);
            this.btnUnInstall.Name = "btnUnInstall";
            this.btnUnInstall.Size = new System.Drawing.Size(30, 30);
            this.btnUnInstall.TabIndex = 3;
            this.btnUnInstall.UseVisualStyleBackColor = true;
            this.btnUnInstall.Click += new System.EventHandler(this.btnUnInstall_Click);
            // 
            // btnHide
            // 
            this.btnHide.Location = new System.Drawing.Point(120, 0);
            this.btnHide.Margin = new System.Windows.Forms.Padding(0);
            this.btnHide.Name = "btnHide";
            this.btnHide.Size = new System.Drawing.Size(30, 30);
            this.btnHide.TabIndex = 4;
            this.btnHide.UseVisualStyleBackColor = true;
            this.btnHide.Click += new System.EventHandler(this.btnHide_Click);
            // 
            // btnGetLink
            // 
            this.btnGetLink.Location = new System.Drawing.Point(150, 0);
            this.btnGetLink.Margin = new System.Windows.Forms.Padding(0);
            this.btnGetLink.Name = "btnGetLink";
            this.btnGetLink.Size = new System.Drawing.Size(30, 30);
            this.btnGetLink.TabIndex = 5;
            this.btnGetLink.UseVisualStyleBackColor = true;
            this.btnGetLink.Click += new System.EventHandler(this.btnGetLink_Click);
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel5.ColumnCount = 2;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanel5.Controls.Add(this.btnCancel, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.progTotal, 0, 0);
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 160);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(180, 28);
            this.tableLayoutPanel5.TabIndex = 5;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(151, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(29, 29);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // progTotal
            // 
            this.progTotal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progTotal.Location = new System.Drawing.Point(3, 3);
            this.progTotal.Name = "progTotal";
            this.progTotal.Size = new System.Drawing.Size(145, 23);
            this.progTotal.TabIndex = 1;
            // 
            // btnHistory
            // 
            this.btnHistory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnHistory.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnHistory.AutoSize = true;
            this.btnHistory.Location = new System.Drawing.Point(3, 93);
            this.btnHistory.Name = "btnHistory";
            this.btnHistory.Size = new System.Drawing.Size(180, 23);
            this.btnHistory.TabIndex = 6;
            this.btnHistory.Text = "Update History";
            this.btnHistory.UseVisualStyleBackColor = true;
            this.btnHistory.CheckedChanged += new System.EventHandler(this.btnHistory_CheckedChanged);
            // 
            // btnHidden
            // 
            this.btnHidden.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnHidden.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnHidden.AutoSize = true;
            this.btnHidden.Location = new System.Drawing.Point(3, 63);
            this.btnHidden.Name = "btnHidden";
            this.btnHidden.Size = new System.Drawing.Size(180, 23);
            this.btnHidden.TabIndex = 7;
            this.btnHidden.Text = "Hidden Updates";
            this.btnHidden.UseVisualStyleBackColor = true;
            this.btnHidden.CheckedChanged += new System.EventHandler(this.btnHidden_CheckedChanged);
            // 
            // btnInstalled
            // 
            this.btnInstalled.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInstalled.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnInstalled.AutoSize = true;
            this.btnInstalled.Location = new System.Drawing.Point(3, 33);
            this.btnInstalled.Name = "btnInstalled";
            this.btnInstalled.Size = new System.Drawing.Size(180, 23);
            this.btnInstalled.TabIndex = 8;
            this.btnInstalled.Text = "Installed Updates";
            this.btnInstalled.UseVisualStyleBackColor = true;
            this.btnInstalled.CheckedChanged += new System.EventHandler(this.btnInstalled_CheckedChanged);
            // 
            // btnWinUpd
            // 
            this.btnWinUpd.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnWinUpd.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnWinUpd.AutoSize = true;
            this.btnWinUpd.Location = new System.Drawing.Point(3, 3);
            this.btnWinUpd.Name = "btnWinUpd";
            this.btnWinUpd.Size = new System.Drawing.Size(180, 23);
            this.btnWinUpd.TabIndex = 0;
            this.btnWinUpd.Text = "Windows Updates";
            this.btnWinUpd.UseVisualStyleBackColor = true;
            this.btnWinUpd.CheckedChanged += new System.EventHandler(this.btnWinUpd_CheckedChanged);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(3, 194);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(180, 13);
            this.lblStatus.TabIndex = 9;
            // 
            // tabs
            // 
            this.tabs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabs.Controls.Add(this.tabOptions);
            this.tabs.Controls.Add(this.tabAU);
            this.tabs.Location = new System.Drawing.Point(3, 213);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(182, 227);
            this.tabs.TabIndex = 1;
            // 
            // tabOptions
            // 
            this.tabOptions.Controls.Add(this.gbStartup);
            this.tabOptions.Controls.Add(this.dlSource);
            this.tabOptions.Controls.Add(this.chkOffline);
            this.tabOptions.Controls.Add(this.chkMsUpd);
            this.tabOptions.Controls.Add(this.chkOld);
            this.tabOptions.Controls.Add(this.chkManual);
            this.tabOptions.Controls.Add(this.chkDownload);
            this.tabOptions.Location = new System.Drawing.Point(4, 22);
            this.tabOptions.Name = "tabOptions";
            this.tabOptions.Padding = new System.Windows.Forms.Padding(3);
            this.tabOptions.Size = new System.Drawing.Size(174, 201);
            this.tabOptions.TabIndex = 0;
            this.tabOptions.Text = "Options";
            this.tabOptions.UseVisualStyleBackColor = true;
            // 
            // gbStartup
            // 
            this.gbStartup.Controls.Add(this.chkAutoRun);
            this.gbStartup.Controls.Add(this.chkNoUAC);
            this.gbStartup.Controls.Add(this.dlAutoCheck);
            this.gbStartup.Location = new System.Drawing.Point(1, 125);
            this.gbStartup.Name = "gbStartup";
            this.gbStartup.Size = new System.Drawing.Size(170, 75);
            this.gbStartup.TabIndex = 8;
            this.gbStartup.TabStop = false;
            this.gbStartup.Text = "Startup";
            // 
            // chkNoUAC
            // 
            this.chkNoUAC.AutoSize = true;
            this.chkNoUAC.Location = new System.Drawing.Point(3, 56);
            this.chkNoUAC.Name = "chkNoUAC";
            this.chkNoUAC.Size = new System.Drawing.Size(154, 17);
            this.chkNoUAC.TabIndex = 1;
            this.chkNoUAC.Text = "Always run as Administrator";
            this.chkNoUAC.UseVisualStyleBackColor = true;
            this.chkNoUAC.CheckedChanged += new System.EventHandler(this.chkNoUAC_CheckedChanged);
            // 
            // dlAutoCheck
            // 
            this.dlAutoCheck.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dlAutoCheck.Enabled = false;
            this.dlAutoCheck.FormattingEnabled = true;
            this.dlAutoCheck.Items.AddRange(new object[] {
            "No auto search for updates",
            "Search updates every day",
            "Search updates once a week",
            "Search updates every month"});
            this.dlAutoCheck.Location = new System.Drawing.Point(3, 33);
            this.dlAutoCheck.Name = "dlAutoCheck";
            this.dlAutoCheck.Size = new System.Drawing.Size(163, 21);
            this.dlAutoCheck.TabIndex = 2;
            this.dlAutoCheck.SelectedIndexChanged += new System.EventHandler(this.dlAutoCheck_SelectedIndexChanged);
            // 
            // dlSource
            // 
            this.dlSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dlSource.Enabled = false;
            this.dlSource.FormattingEnabled = true;
            this.dlSource.Location = new System.Drawing.Point(4, 5);
            this.dlSource.Name = "dlSource";
            this.dlSource.Size = new System.Drawing.Size(164, 21);
            this.dlSource.TabIndex = 0;
            this.dlSource.SelectedIndexChanged += new System.EventHandler(this.dlSource_SelectedIndexChanged);
            // 
            // chkOffline
            // 
            this.chkOffline.AutoSize = true;
            this.chkOffline.Checked = true;
            this.chkOffline.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOffline.Location = new System.Drawing.Point(4, 29);
            this.chkOffline.Name = "chkOffline";
            this.chkOffline.Size = new System.Drawing.Size(86, 17);
            this.chkOffline.TabIndex = 1;
            this.chkOffline.Text = "Offline Mode";
            this.chkOffline.UseVisualStyleBackColor = true;
            this.chkOffline.CheckedChanged += new System.EventHandler(this.chkOffline_CheckedChanged);
            // 
            // chkMsUpd
            // 
            this.chkMsUpd.AutoSize = true;
            this.chkMsUpd.Location = new System.Drawing.Point(4, 93);
            this.chkMsUpd.Name = "chkMsUpd";
            this.chkMsUpd.Size = new System.Drawing.Size(149, 17);
            this.chkMsUpd.TabIndex = 0;
            this.chkMsUpd.Text = "Register Microsoft Update";
            this.chkMsUpd.UseVisualStyleBackColor = true;
            this.chkMsUpd.CheckedChanged += new System.EventHandler(this.chkMsUpd_CheckedChanged);
            // 
            // chkOld
            // 
            this.chkOld.AutoSize = true;
            this.chkOld.Location = new System.Drawing.Point(4, 77);
            this.chkOld.Name = "chkOld";
            this.chkOld.Size = new System.Drawing.Size(119, 17);
            this.chkOld.TabIndex = 2;
            this.chkOld.Text = "Include superseded";
            this.chkOld.UseVisualStyleBackColor = true;
            this.chkOld.CheckedChanged += new System.EventHandler(this.chkOld_CheckedChanged);
            // 
            // chkManual
            // 
            this.chkManual.AutoSize = true;
            this.chkManual.Location = new System.Drawing.Point(4, 61);
            this.chkManual.Name = "chkManual";
            this.chkManual.Size = new System.Drawing.Size(148, 17);
            this.chkManual.TabIndex = 0;
            this.chkManual.Text = "\'Manual\' Download/Install";
            this.chkManual.UseVisualStyleBackColor = true;
            this.chkManual.CheckedChanged += new System.EventHandler(this.chkManual_CheckedChanged);
            // 
            // chkDownload
            // 
            this.chkDownload.AutoSize = true;
            this.chkDownload.Checked = true;
            this.chkDownload.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDownload.Location = new System.Drawing.Point(4, 45);
            this.chkDownload.Name = "chkDownload";
            this.chkDownload.Size = new System.Drawing.Size(145, 17);
            this.chkDownload.TabIndex = 3;
            this.chkDownload.Text = "Download wsusscn2.cab";
            this.chkDownload.UseVisualStyleBackColor = true;
            this.chkDownload.CheckedChanged += new System.EventHandler(this.chkDownload_CheckedChanged);
            // 
            // tabAU
            // 
            this.tabAU.Controls.Add(this.label1);
            this.tabAU.Controls.Add(this.chkDrivers);
            this.tabAU.Controls.Add(this.chkStore);
            this.tabAU.Controls.Add(this.chkHideWU);
            this.tabAU.Controls.Add(this.chkDisableAU);
            this.tabAU.Controls.Add(this.radDefault);
            this.tabAU.Controls.Add(this.radSchedule);
            this.tabAU.Controls.Add(this.radDownload);
            this.tabAU.Controls.Add(this.chkBlockMS);
            this.tabAU.Controls.Add(this.radNotify);
            this.tabAU.Controls.Add(this.radDisable);
            this.tabAU.Controls.Add(this.dlShDay);
            this.tabAU.Controls.Add(this.dlShTime);
            this.tabAU.Location = new System.Drawing.Point(4, 22);
            this.tabAU.Name = "tabAU";
            this.tabAU.Padding = new System.Windows.Forms.Padding(3);
            this.tabAU.Size = new System.Drawing.Size(174, 201);
            this.tabAU.TabIndex = 1;
            this.tabAU.Text = "Auto Update";
            this.tabAU.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label1.Location = new System.Drawing.Point(0, 149);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(174, 2);
            this.label1.TabIndex = 22;
            // 
            // chkDrivers
            // 
            this.chkDrivers.AutoSize = true;
            this.chkDrivers.Location = new System.Drawing.Point(4, 186);
            this.chkDrivers.Name = "chkDrivers";
            this.chkDrivers.Size = new System.Drawing.Size(97, 17);
            this.chkDrivers.TabIndex = 7;
            this.chkDrivers.Text = "Include Drivers";
            this.chkDrivers.ThreeState = true;
            this.chkDrivers.UseVisualStyleBackColor = true;
            this.chkDrivers.CheckStateChanged += new System.EventHandler(this.chkDrivers_CheckStateChanged);
            // 
            // chkStore
            // 
            this.chkStore.AutoSize = true;
            this.chkStore.Location = new System.Drawing.Point(4, 170);
            this.chkStore.Name = "chkStore";
            this.chkStore.Size = new System.Drawing.Size(152, 17);
            this.chkStore.TabIndex = 21;
            this.chkStore.Text = "Disable Store Auto Update";
            this.chkStore.UseVisualStyleBackColor = true;
            this.chkStore.CheckedChanged += new System.EventHandler(this.chkStore_CheckedChanged);
            // 
            // chkHideWU
            // 
            this.chkHideWU.AutoSize = true;
            this.chkHideWU.Location = new System.Drawing.Point(4, 154);
            this.chkHideWU.Name = "chkHideWU";
            this.chkHideWU.Size = new System.Drawing.Size(139, 17);
            this.chkHideWU.TabIndex = 1;
            this.chkHideWU.Text = "Hide WU Settings Page";
            this.chkHideWU.UseVisualStyleBackColor = true;
            this.chkHideWU.CheckedChanged += new System.EventHandler(this.chkHideWU_CheckedChanged);
            // 
            // chkDisableAU
            // 
            this.chkDisableAU.Location = new System.Drawing.Point(16, 37);
            this.chkDisableAU.Name = "chkDisableAU";
            this.chkDisableAU.Size = new System.Drawing.Size(155, 21);
            this.chkDisableAU.TabIndex = 20;
            this.chkDisableAU.Text = "Disable Update Facilitators";
            this.chkDisableAU.UseVisualStyleBackColor = true;
            this.chkDisableAU.CheckedChanged += new System.EventHandler(this.chkDisableAU_CheckedChanged);
            // 
            // radDefault
            // 
            this.radDefault.AutoSize = true;
            this.radDefault.Location = new System.Drawing.Point(4, 130);
            this.radDefault.Name = "radDefault";
            this.radDefault.Size = new System.Drawing.Size(151, 17);
            this.radDefault.TabIndex = 19;
            this.radDefault.TabStop = true;
            this.radDefault.Text = "Automatic Update (default)";
            this.radDefault.UseVisualStyleBackColor = true;
            this.radDefault.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
            // 
            // radSchedule
            // 
            this.radSchedule.AutoSize = true;
            this.radSchedule.Location = new System.Drawing.Point(4, 91);
            this.radSchedule.Name = "radSchedule";
            this.radSchedule.Size = new System.Drawing.Size(132, 17);
            this.radSchedule.TabIndex = 18;
            this.radSchedule.TabStop = true;
            this.radSchedule.Text = "Scheduled & Installation";
            this.radSchedule.UseVisualStyleBackColor = true;
            this.radSchedule.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
            // 
            // radDownload
            // 
            this.radDownload.AutoSize = true;
            this.radDownload.Location = new System.Drawing.Point(4, 73);
            this.radDownload.Name = "radDownload";
            this.radDownload.Size = new System.Drawing.Size(97, 17);
            this.radDownload.TabIndex = 17;
            this.radDownload.TabStop = true;
            this.radDownload.Text = "Download Only";
            this.radDownload.UseVisualStyleBackColor = true;
            this.radDownload.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
            // 
            // chkBlockMS
            // 
            this.chkBlockMS.AutoSize = true;
            this.chkBlockMS.Location = new System.Drawing.Point(4, 4);
            this.chkBlockMS.Name = "chkBlockMS";
            this.chkBlockMS.Size = new System.Drawing.Size(164, 17);
            this.chkBlockMS.TabIndex = 4;
            this.chkBlockMS.Text = "Block Access to WU Servers";
            this.chkBlockMS.UseVisualStyleBackColor = true;
            this.chkBlockMS.CheckedChanged += new System.EventHandler(this.chkBlockMS_CheckedChanged);
            // 
            // radNotify
            // 
            this.radNotify.AutoSize = true;
            this.radNotify.Location = new System.Drawing.Point(4, 55);
            this.radNotify.Name = "radNotify";
            this.radNotify.Size = new System.Drawing.Size(102, 17);
            this.radNotify.TabIndex = 16;
            this.radNotify.TabStop = true;
            this.radNotify.Text = "Notification Only";
            this.radNotify.UseVisualStyleBackColor = true;
            this.radNotify.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
            // 
            // radDisable
            // 
            this.radDisable.AutoSize = true;
            this.radDisable.Location = new System.Drawing.Point(4, 22);
            this.radDisable.Name = "radDisable";
            this.radDisable.Size = new System.Drawing.Size(148, 17);
            this.radDisable.TabIndex = 15;
            this.radDisable.TabStop = true;
            this.radDisable.Text = "Disable Automatic Update";
            this.radDisable.UseVisualStyleBackColor = true;
            this.radDisable.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
            // 
            // dlShDay
            // 
            this.dlShDay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dlShDay.Enabled = false;
            this.dlShDay.FormattingEnabled = true;
            this.dlShDay.Items.AddRange(new object[] {
            "Daily",
            "Sunday",
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday"});
            this.dlShDay.Location = new System.Drawing.Point(18, 108);
            this.dlShDay.Name = "dlShDay";
            this.dlShDay.Size = new System.Drawing.Size(90, 21);
            this.dlShDay.TabIndex = 5;
            this.dlShDay.SelectedIndexChanged += new System.EventHandler(this.dlShDay_SelectedIndexChanged);
            // 
            // dlShTime
            // 
            this.dlShTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dlShTime.Enabled = false;
            this.dlShTime.FormattingEnabled = true;
            this.dlShTime.Items.AddRange(new object[] {
            "00:00",
            "01:00",
            "02:00",
            "03:00",
            "04:00",
            "05:00",
            "06:00",
            "07:00",
            "08:00",
            "09:00",
            "10:00",
            "11:00",
            "12:00",
            "13:00",
            "14:00",
            "15:00",
            "16:00",
            "17:00",
            "18:00",
            "19:00",
            "20:00",
            "21:00",
            "22:00",
            "23:00"});
            this.dlShTime.Location = new System.Drawing.Point(114, 108);
            this.dlShTime.Name = "dlShTime";
            this.dlShTime.Size = new System.Drawing.Size(55, 21);
            this.dlShTime.TabIndex = 6;
            this.dlShTime.SelectedIndexChanged += new System.EventHandler(this.dlShTime_SelectedIndexChanged);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panelList, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(1, 2);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(683, 443);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // WuMgr
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(685, 447);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(700, 485);
            this.Name = "WuMgr";
            this.Text = "Update Manager for Windows";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WuMgr_FormClosing);
            this.Load += new System.EventHandler(this.WuMgr_Load);
            this.panelList.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tabs.ResumeLayout(false);
            this.tabOptions.ResumeLayout(false);
            this.tabOptions.PerformLayout();
            this.gbStartup.ResumeLayout(false);
            this.gbStartup.PerformLayout();
            this.tabAU.ResumeLayout(false);
            this.tabAU.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.TableLayoutPanel panelList;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.Button btnUnInstall;
        private System.Windows.Forms.Button btnHide;
        private System.Windows.Forms.Button btnGetLink;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ProgressBar progTotal;
        private System.Windows.Forms.CheckBox btnHistory;
        private System.Windows.Forms.CheckBox btnHidden;
        private System.Windows.Forms.CheckBox btnInstalled;
        private System.Windows.Forms.CheckBox btnWinUpd;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.CheckBox chkBlockMS;
        private System.Windows.Forms.CheckBox chkDrivers;
        private System.Windows.Forms.ComboBox dlShTime;
        private System.Windows.Forms.ComboBox dlShDay;
        private System.Windows.Forms.CheckBox chkNoUAC;
        private System.Windows.Forms.CheckBox chkMsUpd;
        private System.Windows.Forms.CheckBox chkOld;
        private System.Windows.Forms.ComboBox dlSource;
        private System.Windows.Forms.CheckBox chkOffline;
        private System.Windows.Forms.CheckBox chkDownload;
        private System.Windows.Forms.CheckBox chkManual;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.RichTextBox logBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.LinkLabel lblSupport;
        private System.Windows.Forms.CheckBox chkHideWU;
        private ListViewExtended updateView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabOptions;
        private System.Windows.Forms.ComboBox dlAutoCheck;
        private System.Windows.Forms.CheckBox chkAutoRun;
        private System.Windows.Forms.TabPage tabAU;
        private System.Windows.Forms.CheckBox chkStore;
        private System.Windows.Forms.CheckBox chkDisableAU;
        private System.Windows.Forms.RadioButton radDefault;
        private System.Windows.Forms.RadioButton radSchedule;
        private System.Windows.Forms.RadioButton radDownload;
        private System.Windows.Forms.RadioButton radNotify;
        private System.Windows.Forms.RadioButton radDisable;
        private System.Windows.Forms.GroupBox gbStartup;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button btnSearchOff;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.CheckBox chkGrupe;
        private System.Windows.Forms.CheckBox chkAll;
        private System.Windows.Forms.LinkLabel lblPatreon;
    }
}

