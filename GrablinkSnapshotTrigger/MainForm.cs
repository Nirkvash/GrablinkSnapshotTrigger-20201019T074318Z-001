/*
+-------------------------------- DISCLAIMER ---------------------------------+
|                                                                             |
| This application program is provided to you free of charge as an example.   |
| Despite the considerable efforts of Euresys personnel to create a usable    |
| example, you should not assume that this program is error-free or suitable  |
| for any purpose whatsoever.                                                 |
|                                                                             |
| EURESYS does not give any representation, warranty or undertaking that this |
| program is free of any defect or error or suitable for any purpose. EURESYS |
| shall not be liable, in contract, in torts or otherwise, for any damages,   |
| loss, costs, expenses or other claims for compensation, including those     |
| asserted by third parties, arising out of or in connection with the use of  |
| this program.                                                               |
|                                                                             |
+-----------------------------------------------------------------------------+
*/

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Euresys.MultiCam;
using System.Runtime.InteropServices;

namespace GrablinkSnapshotTrigger
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    /// 

    public class MainForm : System.Windows.Forms.Form
    {
        #region - Claim -
        // Creation of an event for asynchronous call to paint function
        public delegate void PaintDelegate(Graphics g);
        public delegate void UpdateStatusBarPanelReservedDelegate(String text);
        public delegate void UpdateStatusBarPanelStatusDelegate(String text);
        public delegate void EnableFormParamSettingDelegate(bool flag);

        ImageQueue IQ;
        SystemMgr SysMgr;
        String pathCam = Application.StartupPath + "\\XCM16K80SAT8_L16384RG.cam";
        SaveImageThread[] saveThreads;

        Stopwatch sw = new Stopwatch();
        bool bDisplayImages = true;

        // The object that will contain the acquired image pages
        private int iIndexPage = 0;
        private Bitmap DisplayImg;

        // Acquisition parameters
        private int yTop = 0;
        private int ActivityLength = 1;
        private int SeqLength_Pg = -1;
        private int SeqLength_Ln = -1;
        private int PageLength_Ln = -1;
        private int Expose_us = 0;
        private int iPageWidth = 0;
        private int LinePitch = 1;
        private int EncoderPitch = 1;
        private String AcquisitionMode;
        private String TrigMode;
        private String NextTrigMode;
        private String LineRateMode;

        // The object that will contain the palette information for the bitmap
        private ColorPalette imgpal = null;

        // The Mutex object that will protect image objects during processing
        private static Mutex imageMutex = new Mutex();

        // The MultiCam object that controls the acquisition
        UInt32 channel;

        // The MultiCam object that contains the acquired buffer
        private UInt32 currentSurface;

        MC.CALLBACK multiCamCallback; 
        #endregion

        #region - Form components -
        private System.Windows.Forms.MainMenu mainMenu;
        private System.Windows.Forms.StatusBar statusBar;
        private System.Windows.Forms.StatusBarPanel statusBarPanelStatus;
        private System.Windows.Forms.MenuItem Go;
        private System.Windows.Forms.MenuItem Stop;
        private SaveFileDialog saveFileDialog1;
        private PictureBox pictureBox1;
        private Panel panel1;
        private GroupBox gpAcquisitionMode;
        private ComboBox cbbAcquisitionMode;
        private StatusBarPanel statusBarPanelSize;
        private StatusBarPanel statusBarPanelPos;
        private StatusBarPanel statusBarPanelLevel;
        private StatusBarPanel statusBarPanelReserved;
        private GroupBox gpTrigMode;
        private ComboBox cbbTrigMode;
        private GroupBox gpSeqLength_Ln;
        private VScrollBar vsbSeqLength_Ln;
        private TextBox txtSeqLength_Ln;
        private GroupBox gpPageLength_ln;
        private VScrollBar vsbPageLength_Ln;
        private TextBox txtPageLength_Ln;
        private GroupBox gpExpose_us;
        private VScrollBar vsbExpose_us;
        private TextBox txtExpose_us;
        private GroupBox gpLineRateMode;
        private ComboBox cbbLineRateMode;
        private GroupBox gpSeqLength_Pg;
        private VScrollBar vsbSeqLength_Pg;
        private TextBox txtSeqLength_Pg;
        private ToolTip toolTip1;
        private TabControl tabControl1;
        private TabPage tabAcquisitionSettings;
        private TabPage tabInspectionSettings;
        private CheckBox ckbDisplayImages;
        private CheckBox ckbOriginalImages;
        private System.ComponentModel.IContainer components;
        #endregion

        #region - Windows Form Designer generated code -
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
            this.Go = new System.Windows.Forms.MenuItem();
            this.Stop = new System.Windows.Forms.MenuItem();
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.statusBarPanelStatus = new System.Windows.Forms.StatusBarPanel();
            this.statusBarPanelSize = new System.Windows.Forms.StatusBarPanel();
            this.statusBarPanelPos = new System.Windows.Forms.StatusBarPanel();
            this.statusBarPanelLevel = new System.Windows.Forms.StatusBarPanel();
            this.statusBarPanelReserved = new System.Windows.Forms.StatusBarPanel();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.gpAcquisitionMode = new System.Windows.Forms.GroupBox();
            this.cbbAcquisitionMode = new System.Windows.Forms.ComboBox();
            this.gpTrigMode = new System.Windows.Forms.GroupBox();
            this.cbbTrigMode = new System.Windows.Forms.ComboBox();
            this.gpSeqLength_Ln = new System.Windows.Forms.GroupBox();
            this.vsbSeqLength_Ln = new System.Windows.Forms.VScrollBar();
            this.txtSeqLength_Ln = new System.Windows.Forms.TextBox();
            this.gpPageLength_ln = new System.Windows.Forms.GroupBox();
            this.vsbPageLength_Ln = new System.Windows.Forms.VScrollBar();
            this.txtPageLength_Ln = new System.Windows.Forms.TextBox();
            this.gpExpose_us = new System.Windows.Forms.GroupBox();
            this.vsbExpose_us = new System.Windows.Forms.VScrollBar();
            this.txtExpose_us = new System.Windows.Forms.TextBox();
            this.gpLineRateMode = new System.Windows.Forms.GroupBox();
            this.cbbLineRateMode = new System.Windows.Forms.ComboBox();
            this.gpSeqLength_Pg = new System.Windows.Forms.GroupBox();
            this.vsbSeqLength_Pg = new System.Windows.Forms.VScrollBar();
            this.txtSeqLength_Pg = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.ckbDisplayImages = new System.Windows.Forms.CheckBox();
            this.ckbOriginalImages = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabAcquisitionSettings = new System.Windows.Forms.TabPage();
            this.tabInspectionSettings = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelStatus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelPos)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelLevel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelReserved)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel1.SuspendLayout();
            this.gpAcquisitionMode.SuspendLayout();
            this.gpTrigMode.SuspendLayout();
            this.gpSeqLength_Ln.SuspendLayout();
            this.gpPageLength_ln.SuspendLayout();
            this.gpExpose_us.SuspendLayout();
            this.gpLineRateMode.SuspendLayout();
            this.gpSeqLength_Pg.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabAcquisitionSettings.SuspendLayout();
            this.tabInspectionSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.Go,
            this.Stop});
            // 
            // Go
            // 
            this.Go.Index = 0;
            this.Go.Text = "Go";
            this.Go.Click += new System.EventHandler(this.Go_Click);
            // 
            // Stop
            // 
            this.Stop.Index = 1;
            this.Stop.Text = "Stop";
            this.Stop.Click += new System.EventHandler(this.Stop_Click);
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 958);
            this.statusBar.Margin = new System.Windows.Forms.Padding(4);
            this.statusBar.Name = "statusBar";
            this.statusBar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanelStatus,
            this.statusBarPanelSize,
            this.statusBarPanelPos,
            this.statusBarPanelLevel,
            this.statusBarPanelReserved});
            this.statusBar.ShowPanels = true;
            this.statusBar.Size = new System.Drawing.Size(1653, 39);
            this.statusBar.TabIndex = 0;
            // 
            // statusBarPanelStatus
            // 
            this.statusBarPanelStatus.Name = "statusBarPanelStatus";
            this.statusBarPanelStatus.Text = "Status: IDLE";
            this.statusBarPanelStatus.Width = 120;
            // 
            // statusBarPanelSize
            // 
            this.statusBarPanelSize.Name = "statusBarPanelSize";
            this.statusBarPanelSize.Text = "Size: ";
            this.statusBarPanelSize.Width = 110;
            // 
            // statusBarPanelPos
            // 
            this.statusBarPanelPos.Name = "statusBarPanelPos";
            this.statusBarPanelPos.Text = "Position: (X, Y) ";
            this.statusBarPanelPos.Width = 150;
            // 
            // statusBarPanelLevel
            // 
            this.statusBarPanelLevel.Name = "statusBarPanelLevel";
            this.statusBarPanelLevel.Text = "Pixel Level: ";
            this.statusBarPanelLevel.Width = 250;
            // 
            // statusBarPanelReserved
            // 
            this.statusBarPanelReserved.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
            this.statusBarPanelReserved.Name = "statusBarPanelReserved";
            this.statusBarPanelReserved.Width = 998;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(150, 75);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Location = new System.Drawing.Point(0, 3);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1366, 1020);
            this.panel1.TabIndex = 1;
            // 
            // gpAcquisitionMode
            // 
            this.gpAcquisitionMode.Controls.Add(this.cbbAcquisitionMode);
            this.gpAcquisitionMode.Font = new System.Drawing.Font("·s²Ó©úÅé", 9F);
            this.gpAcquisitionMode.Location = new System.Drawing.Point(9, 9);
            this.gpAcquisitionMode.Margin = new System.Windows.Forms.Padding(4);
            this.gpAcquisitionMode.Name = "gpAcquisitionMode";
            this.gpAcquisitionMode.Padding = new System.Windows.Forms.Padding(4);
            this.gpAcquisitionMode.Size = new System.Drawing.Size(230, 84);
            this.gpAcquisitionMode.TabIndex = 2;
            this.gpAcquisitionMode.TabStop = false;
            this.gpAcquisitionMode.Text = "Acquisition Mode";
            // 
            // cbbAcquisitionMode
            // 
            this.cbbAcquisitionMode.FormattingEnabled = true;
            this.cbbAcquisitionMode.Items.AddRange(new object[] {
            "PAGE",
            "WEB",
            "LONGPAGE"});
            this.cbbAcquisitionMode.Location = new System.Drawing.Point(20, 32);
            this.cbbAcquisitionMode.Margin = new System.Windows.Forms.Padding(4);
            this.cbbAcquisitionMode.Name = "cbbAcquisitionMode";
            this.cbbAcquisitionMode.Size = new System.Drawing.Size(188, 26);
            this.cbbAcquisitionMode.TabIndex = 0;
            this.toolTip1.SetToolTip(this.cbbAcquisitionMode, "Fundamental acquisition mode.");
            this.cbbAcquisitionMode.SelectedIndexChanged += new System.EventHandler(this.cbbAcquisitionMode_SelectedIndexChanged);
            // 
            // gpTrigMode
            // 
            this.gpTrigMode.Controls.Add(this.cbbTrigMode);
            this.gpTrigMode.Location = new System.Drawing.Point(9, 102);
            this.gpTrigMode.Margin = new System.Windows.Forms.Padding(4);
            this.gpTrigMode.Name = "gpTrigMode";
            this.gpTrigMode.Padding = new System.Windows.Forms.Padding(4);
            this.gpTrigMode.Size = new System.Drawing.Size(230, 84);
            this.gpTrigMode.TabIndex = 3;
            this.gpTrigMode.TabStop = false;
            this.gpTrigMode.Text = "Trigger Mode";
            // 
            // cbbTrigMode
            // 
            this.cbbTrigMode.FormattingEnabled = true;
            this.cbbTrigMode.Location = new System.Drawing.Point(20, 32);
            this.cbbTrigMode.Margin = new System.Windows.Forms.Padding(4);
            this.cbbTrigMode.Name = "cbbTrigMode";
            this.cbbTrigMode.Size = new System.Drawing.Size(188, 26);
            this.cbbTrigMode.TabIndex = 0;
            this.toolTip1.SetToolTip(this.cbbTrigMode, "Acquisition sequence triggering mode");
            this.cbbTrigMode.SelectedIndexChanged += new System.EventHandler(this.cbbTrigMode_SelectedIndexChanged);
            // 
            // gpSeqLength_Ln
            // 
            this.gpSeqLength_Ln.Controls.Add(this.vsbSeqLength_Ln);
            this.gpSeqLength_Ln.Controls.Add(this.txtSeqLength_Ln);
            this.gpSeqLength_Ln.Location = new System.Drawing.Point(9, 195);
            this.gpSeqLength_Ln.Margin = new System.Windows.Forms.Padding(4);
            this.gpSeqLength_Ln.Name = "gpSeqLength_Ln";
            this.gpSeqLength_Ln.Padding = new System.Windows.Forms.Padding(4);
            this.gpSeqLength_Ln.Size = new System.Drawing.Size(230, 82);
            this.gpSeqLength_Ln.TabIndex = 4;
            this.gpSeqLength_Ln.TabStop = false;
            this.gpSeqLength_Ln.Text = "SeqLength_Ln";
            this.toolTip1.SetToolTip(this.gpSeqLength_Ln, "Number of lines in a seqence.");
            // 
            // vsbSeqLength_Ln
            // 
            this.vsbSeqLength_Ln.Location = new System.Drawing.Point(184, 32);
            this.vsbSeqLength_Ln.Maximum = 65534;
            this.vsbSeqLength_Ln.Minimum = -1;
            this.vsbSeqLength_Ln.Name = "vsbSeqLength_Ln";
            this.vsbSeqLength_Ln.Size = new System.Drawing.Size(17, 33);
            this.vsbSeqLength_Ln.TabIndex = 1;
            this.vsbSeqLength_Ln.ValueChanged += new System.EventHandler(this.vsbSeqLength_Ln_ValueChanged);
            // 
            // txtSeqLength_Ln
            // 
            this.txtSeqLength_Ln.Location = new System.Drawing.Point(20, 32);
            this.txtSeqLength_Ln.Margin = new System.Windows.Forms.Padding(4);
            this.txtSeqLength_Ln.Name = "txtSeqLength_Ln";
            this.txtSeqLength_Ln.Size = new System.Drawing.Size(152, 29);
            this.txtSeqLength_Ln.TabIndex = 0;
            this.txtSeqLength_Ln.Text = "0";
            this.toolTip1.SetToolTip(this.txtSeqLength_Ln, "Number of lines in a seqence.");
            this.txtSeqLength_Ln.TextChanged += new System.EventHandler(this.txtSeqLength_Ln_TextChanged);
            // 
            // gpPageLength_ln
            // 
            this.gpPageLength_ln.Controls.Add(this.vsbPageLength_Ln);
            this.gpPageLength_ln.Controls.Add(this.txtPageLength_Ln);
            this.gpPageLength_ln.ForeColor = System.Drawing.Color.Maroon;
            this.gpPageLength_ln.Location = new System.Drawing.Point(9, 286);
            this.gpPageLength_ln.Margin = new System.Windows.Forms.Padding(4);
            this.gpPageLength_ln.Name = "gpPageLength_ln";
            this.gpPageLength_ln.Padding = new System.Windows.Forms.Padding(4);
            this.gpPageLength_ln.Size = new System.Drawing.Size(230, 82);
            this.gpPageLength_ln.TabIndex = 5;
            this.gpPageLength_ln.TabStop = false;
            this.gpPageLength_ln.Text = "PageLength_Ln";
            this.toolTip1.SetToolTip(this.gpPageLength_ln, "Number of lines in a page.");
            // 
            // vsbPageLength_Ln
            // 
            this.vsbPageLength_Ln.Location = new System.Drawing.Point(184, 32);
            this.vsbPageLength_Ln.Maximum = 65535;
            this.vsbPageLength_Ln.Minimum = 1;
            this.vsbPageLength_Ln.Name = "vsbPageLength_Ln";
            this.vsbPageLength_Ln.Size = new System.Drawing.Size(17, 33);
            this.vsbPageLength_Ln.TabIndex = 1;
            this.vsbPageLength_Ln.Value = 500;
            this.vsbPageLength_Ln.ValueChanged += new System.EventHandler(this.vsbPageLength_Ln_ValueChanged);
            // 
            // txtPageLength_Ln
            // 
            this.txtPageLength_Ln.Location = new System.Drawing.Point(20, 32);
            this.txtPageLength_Ln.Margin = new System.Windows.Forms.Padding(4);
            this.txtPageLength_Ln.Name = "txtPageLength_Ln";
            this.txtPageLength_Ln.Size = new System.Drawing.Size(152, 29);
            this.txtPageLength_Ln.TabIndex = 0;
            this.txtPageLength_Ln.Text = "0";
            this.toolTip1.SetToolTip(this.txtPageLength_Ln, "Number of lines in a page.");
            this.txtPageLength_Ln.TextChanged += new System.EventHandler(this.txtPageLength_Ln_TextChanged);
            // 
            // gpExpose_us
            // 
            this.gpExpose_us.Controls.Add(this.vsbExpose_us);
            this.gpExpose_us.Controls.Add(this.txtExpose_us);
            this.gpExpose_us.ForeColor = System.Drawing.Color.Maroon;
            this.gpExpose_us.Location = new System.Drawing.Point(9, 470);
            this.gpExpose_us.Margin = new System.Windows.Forms.Padding(4);
            this.gpExpose_us.Name = "gpExpose_us";
            this.gpExpose_us.Padding = new System.Windows.Forms.Padding(4);
            this.gpExpose_us.Size = new System.Drawing.Size(230, 82);
            this.gpExpose_us.TabIndex = 6;
            this.gpExpose_us.TabStop = false;
            this.gpExpose_us.Text = "Expose_us";
            this.toolTip1.SetToolTip(this.gpExpose_us, "Exposure time for single line.");
            // 
            // vsbExpose_us
            // 
            this.vsbExpose_us.Location = new System.Drawing.Point(184, 32);
            this.vsbExpose_us.Maximum = 5000000;
            this.vsbExpose_us.Name = "vsbExpose_us";
            this.vsbExpose_us.Size = new System.Drawing.Size(17, 33);
            this.vsbExpose_us.TabIndex = 1;
            this.vsbExpose_us.ValueChanged += new System.EventHandler(this.vsbExpose_us_ValueChanged);
            // 
            // txtExpose_us
            // 
            this.txtExpose_us.Location = new System.Drawing.Point(20, 32);
            this.txtExpose_us.Margin = new System.Windows.Forms.Padding(4);
            this.txtExpose_us.Name = "txtExpose_us";
            this.txtExpose_us.Size = new System.Drawing.Size(152, 29);
            this.txtExpose_us.TabIndex = 0;
            this.txtExpose_us.Text = "0";
            this.toolTip1.SetToolTip(this.txtExpose_us, "Exposure time for single line.");
            this.txtExpose_us.TextChanged += new System.EventHandler(this.txtExpose_us_TextChanged);
            // 
            // gpLineRateMode
            // 
            this.gpLineRateMode.Controls.Add(this.cbbLineRateMode);
            this.gpLineRateMode.Location = new System.Drawing.Point(9, 561);
            this.gpLineRateMode.Margin = new System.Windows.Forms.Padding(4);
            this.gpLineRateMode.Name = "gpLineRateMode";
            this.gpLineRateMode.Padding = new System.Windows.Forms.Padding(4);
            this.gpLineRateMode.Size = new System.Drawing.Size(230, 84);
            this.gpLineRateMode.TabIndex = 5;
            this.gpLineRateMode.TabStop = false;
            this.gpLineRateMode.Text = "Line Rate Mode";
            // 
            // cbbLineRateMode
            // 
            this.cbbLineRateMode.FormattingEnabled = true;
            this.cbbLineRateMode.Items.AddRange(new object[] {
            "PERIOD",
            "PULSE",
            "CONVERT"});
            this.cbbLineRateMode.Location = new System.Drawing.Point(20, 32);
            this.cbbLineRateMode.Margin = new System.Windows.Forms.Padding(4);
            this.cbbLineRateMode.Name = "cbbLineRateMode";
            this.cbbLineRateMode.Size = new System.Drawing.Size(188, 26);
            this.cbbLineRateMode.TabIndex = 0;
            this.toolTip1.SetToolTip(this.cbbLineRateMode, "Line rate generation method.");
            this.cbbLineRateMode.SelectedIndexChanged += new System.EventHandler(this.cbbLineRateMode_SelectedIndexChanged);
            // 
            // gpSeqLength_Pg
            // 
            this.gpSeqLength_Pg.Controls.Add(this.vsbSeqLength_Pg);
            this.gpSeqLength_Pg.Controls.Add(this.txtSeqLength_Pg);
            this.gpSeqLength_Pg.Location = new System.Drawing.Point(9, 378);
            this.gpSeqLength_Pg.Margin = new System.Windows.Forms.Padding(4);
            this.gpSeqLength_Pg.Name = "gpSeqLength_Pg";
            this.gpSeqLength_Pg.Padding = new System.Windows.Forms.Padding(4);
            this.gpSeqLength_Pg.Size = new System.Drawing.Size(230, 82);
            this.gpSeqLength_Pg.TabIndex = 5;
            this.gpSeqLength_Pg.TabStop = false;
            this.gpSeqLength_Pg.Text = "SeqLength_Pg";
            this.toolTip1.SetToolTip(this.gpSeqLength_Pg, "Number of pages in a seqence.");
            // 
            // vsbSeqLength_Pg
            // 
            this.vsbSeqLength_Pg.Location = new System.Drawing.Point(184, 32);
            this.vsbSeqLength_Pg.Maximum = 65534;
            this.vsbSeqLength_Pg.Minimum = -1;
            this.vsbSeqLength_Pg.Name = "vsbSeqLength_Pg";
            this.vsbSeqLength_Pg.Size = new System.Drawing.Size(17, 33);
            this.vsbSeqLength_Pg.TabIndex = 1;
            this.vsbSeqLength_Pg.ValueChanged += new System.EventHandler(this.vsbSeqLength_Pg_ValueChanged);
            // 
            // txtSeqLength_Pg
            // 
            this.txtSeqLength_Pg.Location = new System.Drawing.Point(20, 32);
            this.txtSeqLength_Pg.Margin = new System.Windows.Forms.Padding(4);
            this.txtSeqLength_Pg.Name = "txtSeqLength_Pg";
            this.txtSeqLength_Pg.Size = new System.Drawing.Size(152, 29);
            this.txtSeqLength_Pg.TabIndex = 0;
            this.txtSeqLength_Pg.Text = "0";
            this.toolTip1.SetToolTip(this.txtSeqLength_Pg, "Number of pages in a seqence.");
            this.txtSeqLength_Pg.TextChanged += new System.EventHandler(this.txtSeqLength_Pg_TextChanged);
            // 
            // ckbDisplayImages
            // 
            this.ckbDisplayImages.AutoSize = true;
            this.ckbDisplayImages.Checked = true;
            this.ckbDisplayImages.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ckbDisplayImages.Location = new System.Drawing.Point(22, 51);
            this.ckbDisplayImages.Margin = new System.Windows.Forms.Padding(4);
            this.ckbDisplayImages.Name = "ckbDisplayImages";
            this.ckbDisplayImages.Size = new System.Drawing.Size(142, 22);
            this.ckbDisplayImages.TabIndex = 3;
            this.ckbDisplayImages.Text = "Display Images";
            this.toolTip1.SetToolTip(this.ckbDisplayImages, "Display live image.");
            this.ckbDisplayImages.UseVisualStyleBackColor = true;
            this.ckbDisplayImages.CheckedChanged += new System.EventHandler(this.ckbDisplayImages_CheckedChanged);
            // 
            // ckbOriginalImages
            // 
            this.ckbOriginalImages.AutoSize = true;
            this.ckbOriginalImages.Location = new System.Drawing.Point(22, 18);
            this.ckbOriginalImages.Margin = new System.Windows.Forms.Padding(4);
            this.ckbOriginalImages.Name = "ckbOriginalImages";
            this.ckbOriginalImages.Size = new System.Drawing.Size(184, 22);
            this.ckbOriginalImages.TabIndex = 1;
            this.ckbOriginalImages.Text = "Save Original Images";
            this.toolTip1.SetToolTip(this.ckbOriginalImages, "Automatically save images.");
            this.ckbOriginalImages.UseVisualStyleBackColor = true;
            this.ckbOriginalImages.CheckedChanged += new System.EventHandler(this.ckbOriginalImages_CheckedChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabAcquisitionSettings);
            this.tabControl1.Controls.Add(this.tabInspectionSettings);
            this.tabControl1.Location = new System.Drawing.Point(1376, 3);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(260, 1020);
            this.tabControl1.TabIndex = 7;
            // 
            // tabAcquisitionSettings
            // 
            this.tabAcquisitionSettings.Controls.Add(this.gpAcquisitionMode);
            this.tabAcquisitionSettings.Controls.Add(this.gpSeqLength_Pg);
            this.tabAcquisitionSettings.Controls.Add(this.gpLineRateMode);
            this.tabAcquisitionSettings.Controls.Add(this.gpTrigMode);
            this.tabAcquisitionSettings.Controls.Add(this.gpSeqLength_Ln);
            this.tabAcquisitionSettings.Controls.Add(this.gpExpose_us);
            this.tabAcquisitionSettings.Controls.Add(this.gpPageLength_ln);
            this.tabAcquisitionSettings.Location = new System.Drawing.Point(4, 28);
            this.tabAcquisitionSettings.Margin = new System.Windows.Forms.Padding(4);
            this.tabAcquisitionSettings.Name = "tabAcquisitionSettings";
            this.tabAcquisitionSettings.Padding = new System.Windows.Forms.Padding(4);
            this.tabAcquisitionSettings.Size = new System.Drawing.Size(252, 988);
            this.tabAcquisitionSettings.TabIndex = 0;
            this.tabAcquisitionSettings.Text = "Acquisition";
            this.tabAcquisitionSettings.UseVisualStyleBackColor = true;
            // 
            // tabInspectionSettings
            // 
            this.tabInspectionSettings.Controls.Add(this.ckbDisplayImages);
            this.tabInspectionSettings.Controls.Add(this.ckbOriginalImages);
            this.tabInspectionSettings.Location = new System.Drawing.Point(4, 28);
            this.tabInspectionSettings.Margin = new System.Windows.Forms.Padding(4);
            this.tabInspectionSettings.Name = "tabInspectionSettings";
            this.tabInspectionSettings.Padding = new System.Windows.Forms.Padding(4);
            this.tabInspectionSettings.Size = new System.Drawing.Size(252, 988);
            this.tabInspectionSettings.TabIndex = 1;
            this.tabInspectionSettings.Text = "Image";
            this.tabInspectionSettings.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1653, 997);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusBar);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Menu = this.mainMenu;
            this.Name = "MainForm";
            this.Text = "GrablinkTrigger";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.Closed += new System.EventHandler(this.MainForm_Closed);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelStatus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelPos)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelLevel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelReserved)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.gpAcquisitionMode.ResumeLayout(false);
            this.gpTrigMode.ResumeLayout(false);
            this.gpSeqLength_Ln.ResumeLayout(false);
            this.gpSeqLength_Ln.PerformLayout();
            this.gpPageLength_ln.ResumeLayout(false);
            this.gpPageLength_ln.PerformLayout();
            this.gpExpose_us.ResumeLayout(false);
            this.gpExpose_us.PerformLayout();
            this.gpLineRateMode.ResumeLayout(false);
            this.gpSeqLength_Pg.ResumeLayout(false);
            this.gpSeqLength_Pg.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabAcquisitionSettings.ResumeLayout(false);
            this.tabInspectionSettings.ResumeLayout(false);
            this.tabInspectionSettings.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        #region - Main form -
        public MainForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            // Show scope of the sample program
            //MessageBox.Show("This program demonstrates line-scan acquisition on a Grablink Board.\n\n" +
            //"The Go! menu generates a soft trigger which starts line acquisition.\n\n" + 
            //"By default, this program requires an line-scan camera connected on connector M.\n",
            //"Sample program description", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new MainForm());
        }

        private void MainForm_Load(object sender, System.EventArgs e)
        {

            // + GrablinkSnapshotTrigger Sample Program

            try
            {
                // Open MultiCam driver
                //MC.OpenDriver();

                IQ = new ImageQueue();
                SysMgr = new SystemMgr();

                //InitParam();
                //GetParam();
                //UpdateForm();
                InitInspThreads();
            }
            catch (Euresys.MultiCamException exc)
            {
                // An exception has occurred in the try {...} block. 
                // Retrieve its description and display it in a message box.
                MessageBox.Show(exc.Message, "MultiCam Exception");
                Close();
            }

            // - GrablinkSnapshotTrigger Sample Program
        }
        
        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Stop_Click(sender, e);
                // Delete the channel
                if (channel != 0)
                {
                    MC.Delete(channel);
                    channel = 0;
                }
            }
            catch (Euresys.MultiCamException exc)
            {
                MessageBox.Show(exc.Message, "MultiCam Exception");
            }
        }

        private void MainForm_Closed(object sender, System.EventArgs e)
        {
            try
            {
                // Close MultiCam driver
                //MC.CloseDriver();
            }
            catch (Euresys.MultiCamException exc)
            {
                MessageBox.Show(exc.Message, "MultiCam Exception");
            }
        }

        private void InitInspThreads()
        {
            saveThreads = new SaveImageThread[SysMgr.NumSaveImageThread];
            for (int i = 0; i < SysMgr.NumSaveImageThread; i++)
            {
                saveThreads[i] = new SaveImageThread(i, IQ, SysMgr);
                saveThreads[i].Start();
            }
        }

        private void StopInspThreads()
        {
            for (int i = 0; i < saveThreads.Length; i++)
            {
                saveThreads[i].bStop = true;
                saveThreads[i].Join();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopInspThreads();
        }
        #endregion

        #region - Parameter related -
        private void InitParam()
        {
            MC.SetParam(MC.CONFIGURATION, "ErrorLog", "error.log");
            MC.Create("CHANNEL", out channel);
            MC.SetParam(channel, "DriverIndex", 0);
            MC.SetParam(channel, "Connector", "M");
            MC.SetParam(channel, "CamFile", pathCam);
            MC.SetParam(channel, "ColorFormat", "Y8");

            // Register the callback function
            multiCamCallback = new MC.CALLBACK(MultiCamCallback);
            MC.RegisterCallback(channel, multiCamCallback, channel);

            // Enable the signals corresponding to the callback functions
            MC.SetParam(channel, MC.SignalEnable + MC.SIG_SURFACE_PROCESSING, "ON");
            MC.SetParam(channel, MC.SignalEnable + MC.SIG_ACQUISITION_FAILURE, "ON");
            MC.SetParam(channel, MC.SignalEnable + MC.SIG_END_ACQUISITION_SEQUENCE, "ON");
        }

        private void GetParam()
        {
            MC.GetParam(channel, "ImageSizeX", out iPageWidth);
            MC.GetParam(channel, "ActivityLength", out ActivityLength);
            MC.GetParam(channel, "SeqLength_Ln", out SeqLength_Ln);
            MC.GetParam(channel, "SeqLength_Pg", out SeqLength_Pg);
            MC.GetParam(channel, "PageLength_Ln", out PageLength_Ln);
            MC.GetParam(channel, "AcquisitionMode", out AcquisitionMode);
            MC.GetParam(channel, "TrigMode", out TrigMode);
            MC.GetParam(channel, "NextTrigMode", out NextTrigMode);
            MC.GetParam(channel, "LineRateMode", out LineRateMode);
            MC.GetParam(channel, "LinePitch", out LinePitch);
            MC.GetParam(channel, "EncoderPitch", out EncoderPitch);
            MC.GetParam(channel, "Expose_us", out Expose_us);

            SysMgr.GetParam(pathCam);
        }

        private void UpdateParam()
        {
            MC.SetParam(channel, "AcquisitionMode", AcquisitionMode);
            MC.SetParam(channel, "ActivityLength", ActivityLength);
            MC.SetParam(channel, "SeqLength_Ln", SeqLength_Ln);
            MC.SetParam(channel, "SeqLength_Pg", SeqLength_Pg);
            MC.SetParam(channel, "PageLength_Ln", PageLength_Ln);
            MC.SetParam(channel, "TrigMode", TrigMode);
            MC.SetParam(channel, "Expose_us", Expose_us);
            MC.SetParam(channel, "LineRateMode", LineRateMode);
        }
        #endregion

        #region - Form related -
        private void UpdateForm()
        {
            pictureBox1.Width = iPageWidth;
            pictureBox1.Height = PageLength_Ln;
            statusBarPanelSize.Text = "Size: (" + iPageWidth.ToString() + ", " + PageLength_Ln.ToString() + ")";
            if (AcquisitionMode == "PAGE")
                cbbAcquisitionMode.SelectedIndex = 0;
            else if (AcquisitionMode == "WEB")
                cbbAcquisitionMode.SelectedIndex = 1;
            else if (AcquisitionMode == "LONGPAGE")
                cbbAcquisitionMode.SelectedIndex = 2;

            cbbTrigMode.Items.Clear();
            if (AcquisitionMode == "WEB")
                cbbTrigMode.Items.Add("IMMEDIATE");
            foreach (var mode in MC.sTRIG_MODE)
                cbbTrigMode.Items.Add(mode);
            if (cbbTrigMode.Items.Contains(TrigMode) == true)
            {
                cbbTrigMode.SelectedItem = TrigMode;
            }

            if (cbbLineRateMode.Items.Contains(LineRateMode) == true)
            {
                cbbLineRateMode.SelectedItem = LineRateMode;
            }

            txtSeqLength_Ln.Text = SeqLength_Ln.ToString();
            txtPageLength_Ln.Text = PageLength_Ln.ToString();
            txtExpose_us.Text = Expose_us.ToString();
            txtSeqLength_Pg.Text = SeqLength_Pg.ToString();

            vsbSeqLength_Ln.Value = SeqLength_Ln;
            vsbPageLength_Ln.Value = PageLength_Ln;
            vsbExpose_us.Value = Expose_us;
            vsbSeqLength_Pg.Value = SeqLength_Pg;

            statusBarPanelReserved.Text = "";

            ckbOriginalImages.Checked = SysMgr.SaveImages;
            ckbDisplayImages.Checked = bDisplayImages;
        }
        
        private void EnableFormParamSettingFunc(bool flag)
        {
            cbbAcquisitionMode.Enabled = flag;
            cbbTrigMode.Enabled = flag;
            cbbLineRateMode.Enabled = flag;
            txtSeqLength_Ln.Enabled = flag;
            txtPageLength_Ln.Enabled = flag;
            txtSeqLength_Pg.Enabled = flag;
        }

        #region - StatusBar -
        private void UpdateStatusBar(String text)
        {
            statusBarPanelStatus.Text = text;
        }

        private void UpdateStatusBarPanelReservedFunc(string text)
        {
            statusBarPanelReserved.Text = text;
        }
        
        private void UpdateStatusBarPanelStatusFunc(string text)
        {
            statusBarPanelStatus.Text = text;
        }
        #endregion

        #region - PictureBox -
        void Redraw(Graphics g)
        {
            // + GrablinkSnapshotTrigger Sample Program

            try
            {
                if (DisplayImg != null)
                {
                    g.DrawImage(DisplayImg, 0, 0);
                }
            }
            catch (System.Exception exc)
            {
                MessageBox.Show(exc.Message, "System Exception");
            }

            // - GrablinkSnapshotTrigger Sample Program
        }

        private void pictureBox1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (bDisplayImages == true)
            {
                pictureBox1.Width = panel1.Width;
                pictureBox1.Height = panel1.Height;
                Redraw(e.Graphics);
            }
        }
         
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            statusBarPanelPos.Text = "Position: (" + e.X.ToString() + ", " + e.Y.ToString() + ")";

            if (DisplayImg != null)
            {
                if (DisplayImg.PixelFormat == PixelFormat.Format8bppIndexed)
                    statusBarPanelLevel.Text = "Pixel Level: " + DisplayImg.GetPixel(e.X, e.Y).R;
                else
                    statusBarPanelLevel.Text = "Pixel Level: " + DisplayImg.GetPixel(e.X, e.Y);
            }
        }
       #endregion

        #region - ComboBox -
        private void cbbAcquisitionMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            AcquisitionMode = (string)cbbAcquisitionMode.SelectedItem;
            MC.SetParam(channel, "ChannelState", "IDLE");
            MC.SetParam(channel, "AcquisitionMode", AcquisitionMode);
            MC.GetParam(channel, "TrigMode", out TrigMode);
            MC.GetParam(channel, "NextTrigMode", out NextTrigMode);
            UpdateForm();
        }

        private void cbbTrigMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            TrigMode = (string)cbbTrigMode.SelectedItem;
            UpdateParam();
        }

        private void cbbLineRateMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            LineRateMode = (string)cbbLineRateMode.SelectedItem;
            UpdateParam();
        }
        #endregion

        #region - Text -
        private void txtSeqLength_Ln_TextChanged(object sender, EventArgs e)
        {
            SeqLength_Ln = Convert.ToInt32(txtSeqLength_Ln.Text);
            vsbSeqLength_Ln.Value = SeqLength_Ln;
            UpdateParam();
        }

        private void txtPageLength_Ln_TextChanged(object sender, EventArgs e)
        {
            PageLength_Ln = Convert.ToInt32(txtPageLength_Ln.Text);
            vsbPageLength_Ln.Value = PageLength_Ln;
            UpdateParam();
        }
        
        private void txtExpose_us_TextChanged(object sender, EventArgs e)
        {
            Expose_us = Convert.ToInt32(txtExpose_us.Text);
            vsbExpose_us.Value = Expose_us;
            UpdateParam();
        }

        private void txtSeqLength_Pg_TextChanged(object sender, EventArgs e)
        {
            SeqLength_Pg = Convert.ToInt32(txtSeqLength_Pg.Text);
            vsbSeqLength_Pg.Value = SeqLength_Pg;
            UpdateParam();
        }
        #endregion

        #region - Scrollbar -

        private void vsbSeqLength_Ln_ValueChanged(object sender, EventArgs e)
        {
            SeqLength_Ln = vsbSeqLength_Ln.Value;
            UpdateForm();
            UpdateParam();
        }

        private void vsbPageLength_Ln_ValueChanged(object sender, EventArgs e)
        {
            PageLength_Ln = vsbPageLength_Ln.Value;
            UpdateForm();
            UpdateParam();
        }

        private void vsbExpose_us_ValueChanged(object sender, EventArgs e)
        {
            Expose_us = vsbExpose_us.Value;
            UpdateForm();
            UpdateParam();
        }

        private void vsbSeqLength_Pg_ValueChanged(object sender, EventArgs e)
        {
            SeqLength_Pg = vsbSeqLength_Pg.Value;
            UpdateForm();
            UpdateParam();

        }
        #endregion

        #region - CheckBox -
        private void ckbOriginalImages_CheckedChanged(object sender, EventArgs e)
        {
            SysMgr.SaveImages = ckbOriginalImages.Checked;
        }

        private void ckbDisplayImages_CheckedChanged(object sender, EventArgs e)
        {
            bDisplayImages = ckbDisplayImages.Checked;
        }
        #endregion

        #endregion

        #region - Callback SIGNALINFO -
        private void MultiCamCallback(ref MC.SIGNALINFO signalInfo)
        {
            switch(signalInfo.Signal)
            {
                case MC.SIG_SURFACE_PROCESSING:
                    ProcessingCallback(signalInfo);
                    break;
                case MC.SIG_END_ACQUISITION_SEQUENCE:
                    EndSeqenceCallback(signalInfo);
                    break;
                case MC.SIG_ACQUISITION_FAILURE:
                    AcqFailureCallback(signalInfo);
                    break;
                default:
                    throw new Euresys.MultiCamException("Unknown signal");
            }
        }

        private void ProcessingCallback(MC.SIGNALINFO signalInfo)
        {
            UInt32 currentChannel = (UInt32)signalInfo.Context;

            this.Invoke(new UpdateStatusBarPanelStatusDelegate(UpdateStatusBarPanelStatusFunc), new object[] { "IQ: " + IQ.Count.ToString() });
            currentSurface = signalInfo.SignalInfo;

            // + GrablinkSnapshotTrigger Sample Program

            try
            {
                // Update the image with the acquired image buffer data 
                Int32 bufferPitch;//width, height, 
                IntPtr bufferAddress;
                MC.GetParam(currentChannel, "BufferPitch", out bufferPitch);
                MC.GetParam(currentSurface, "SurfaceAddr", out bufferAddress);

                try
                {
                    imageMutex.WaitOne();

                    ImageInfo Img = new ImageInfo();
                    Img.iIndex = iIndexPage;
                    Img.iTopY = yTop;
                    yTop += PageLength_Ln;
                    Img.SrcImg = new Mat(PageLength_Ln, iPageWidth, DepthType.Cv8U, 1);
                    Img.DT = DateTime.Now;

                    using (Mat TmpImg = new Mat(PageLength_Ln, iPageWidth, DepthType.Cv8U, 1, bufferAddress, bufferPitch))
                    {
                        TmpImg.CopyTo(Img.SrcImg);
                    }
                    IQ.push(Img);

                    if (bDisplayImages == true)
                    {
                        using (Mat downImg = new Mat())
                        {
                            CvInvoke.Resize(Img.SrcImg, downImg, this.panel1.Size);
                            DisplayImg = downImg.Bitmap;
                        }

                        imgpal = DisplayImg.Palette;
                        // Build bitmap palette Y8 for image
                        for (uint i = 0; i < 256; i++)
                        {
                            imgpal.Entries[i] = Color.FromArgb(
                            (byte)0xFF,
                            (byte)i,
                            (byte)i,
                            (byte)i);
                        }
                        DisplayImg.Palette = imgpal;

                        pictureBox1.Invalidate();
                    }

                    /* Insert image analysis and processing code here */
                    iIndexPage++;
                }
                finally
                {
                    imageMutex.ReleaseMutex();
                }
            }
            catch (Euresys.MultiCamException exc)
            {
                MessageBox.Show(exc.Message, "MultiCam Exception");
            }
            catch (System.Exception exc)
            {
                MessageBox.Show(exc.Message, "System Exception");
            }
            // - GrablinkSnapshotTrigger Sample Program
        }

        private void EndSeqenceCallback(MC.SIGNALINFO signalInfo)
        {
            // + GrablinkSnapshotTrigger Sample Program

            try
            {
                // Retrieve the channel state
                String channelState;
                MC.GetParam(channel, "ChannelState", out channelState);

                // Display frame rate and channel state
                this.Invoke(new UpdateStatusBarPanelStatusDelegate(UpdateStatusBarPanelStatusFunc), new object[] { String.Format("State: {0}", channelState) });
                this.Invoke(new EnableFormParamSettingDelegate(EnableFormParamSettingFunc), new object[] { true });                
            }
            catch (Euresys.MultiCamException exc)
            {
                MessageBox.Show(exc.Message, "MultiCam Exception");
            }
            catch (System.Exception exc)
            {
                MessageBox.Show(exc.Message, "System Exception");
            }
            // - GrablinkSnapshotTrigger Sample Program
        }

        private void AcqFailureCallback(MC.SIGNALINFO signalInfo)
        {
            UInt32 currentChannel = (UInt32)signalInfo.Context;

            // + GrablinkSnapshotTrigger Sample Program

            try
            {
                String channelState;
                MC.GetParam(channel, "ChannelState", out channelState);

                // Display frame rate and channel state
                this.Invoke(new UpdateStatusBarPanelStatusDelegate(UpdateStatusBarPanelStatusFunc), new object[] { String.Format("State: {0}", channelState) });
                this.Invoke(new EnableFormParamSettingDelegate(EnableFormParamSettingFunc), new object[] { true });

                MessageBox.Show("Acquisition Failed.");
            }
            catch (System.Exception exc)
            {
                MessageBox.Show(exc.Message, "System Exception");
            }

            // - GrablinkSnapshotTrigger Sample Program
        }
        #endregion 

        #region - Main func -

        private void Go_Click(object sender, System.EventArgs e)
        {           
            // + GrablinkSnapshotTrigger Sample Program

            // Start an acquisition sequence by activating the channel
            String channelState;
            MC.GetParam(channel, "ChannelState", out channelState);
            if (channelState != "ACTIVE")
            {
                MC.SetParam(channel, "ChannelState", "ACTIVE");
                iIndexPage = 0;
            }
            else if (AcquisitionMode == "LONGPAGE")
                iIndexPage = 0;
            
            EnableFormParamSettingFunc(false);

            statusBarPanelStatus.Text = "State: ACTIVE";
            statusBarPanelReserved.Text = "";

            // Generate a soft trigger event
            MC.SetParam(channel, "ForceTrig", "TRIG");
            Refresh();

            // - GrablinkSnapshotTrigger Sample Program
        }

        private void Stop_Click(object sender, System.EventArgs e)
        {
            // + GrablinkSnapshotTrigger Sample Program
            // Stop an acquisition sequence by deactivating the channel
            if (channel != 0)
                MC.SetParam(channel, "ChannelState", "IDLE");

            UpdateStatusBarPanelStatusFunc(String.Format("State: IDLE", 0));   // Frame Rate: {0:f2}, Channel 
            UpdateStatusBarPanelReservedFunc(String.Format("", 0));

            EnableFormParamSettingFunc(true);
            // - GrablinkSnapshotTrigger Sample Program

        }

        #endregion
    }
}
