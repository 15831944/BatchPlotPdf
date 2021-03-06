/*
 * Created by SharpDevelop.
 * User: Tim Willey
 * Date: 2/22/2007
 * v2.0 6/26/07 - Added the option to add a plot stamp, adds information to a title block, or plan text if not found.
 *  Added the ability to select tabs to plot (Layout1, All paper space tabs, or current tab).
 * v2.1 6/29/07 - Added the option to grab the current layout's settings to fill out the form, and the plot to file option.
 * v2.2 7/02/07 - Added the the options to supply the linetype scales (model and paper space), and the ability to turn on
 *   all viewports when plotting.  Also change the format of the dialog (added a tab control).
 * v2.3 7/03/07 - Added the options to select the layouts to plot (per drawing, from a treeview)
 * v2.4 3/28/14 - Modified by Robbo to plot to metric (mm) sizes and other general tweaks to suit personal requirements.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.Interop.Common;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using PCM = Autodesk.AutoCAD.PlottingServices.PlotConfigManager;

[assembly: CommandClass(typeof(AutoCAD3M.MyBPlot))]

namespace AutoCAD3M
{
	/// <summary>
	/// Description of MyPlot.
	/// </summary>
	/// 
	public class MyPlotParams {
		private string DwgPath;
		private string DeviceName;
		private string PaperSize;
		private string ctbName;
		private bool ScLw;
		private int Cnt;
		private Autodesk.AutoCAD.DatabaseServices.StdScaleType ScTyp;
		private Autodesk.AutoCAD.DatabaseServices.PlotRotation PltRot;
		private string CanonicalPaperName;
		private bool CurLo;
		private bool ShouldStamp;
		private bool Plt2File;
		private string PltFileName;
		private double LtScModel;
		private double LtScPaper;
		private bool VpOn;
		private bool CurLtSc;
		private string[] LoNames;
		
		public MyPlotParams(){}
		public MyPlotParams(string DwgPath, string DeviceName, string PaperSize, string ctbName, bool ScLw, int Cnt, Autodesk.AutoCAD.DatabaseServices.StdScaleType ScTyp, Autodesk.AutoCAD.DatabaseServices.PlotRotation PltRot, string CanonicalMedia) {
			this.DwgPath = DwgPath;
			this.DeviceName = DeviceName;
			this.Paper = PaperSize;
			this.ctbName = ctbName;
			this.ScLw = ScLw;
			this.Cnt = Cnt;
			this.ScTyp = ScTyp;
			this.PltRot = PltRot;
			this.CanonicalPaperName = CanonicalMedia;
		}
		
		public string DrawingPath {
			get {return DwgPath;}
			set {DwgPath = value;}
		}
		
		public string Device {
			get {return DeviceName;}
			set {DeviceName = value;}
		}
		
		public string Paper {
			get {return PaperSize;}
			set {PaperSize = value;}
		}
		
		public string ctbFile {
			get {return ctbName;}
			set {ctbName = value;}
		}
		
		public bool ScaleLineweight {
			get {return ScLw;}
			set {ScLw = value;}
		}
		
		public int Amount {
			get {return Cnt;}
			set {Cnt = value;}
		}
		
		public Autodesk.AutoCAD.DatabaseServices.StdScaleType AcScaleType {
			get {return ScTyp;}
			set {ScTyp = value;}
		}
		
		public Autodesk.AutoCAD.DatabaseServices.PlotRotation AcPlotRotation {
			get {return PltRot;}
			set {PltRot = value;}
		}
		
		public string CanonicalPaper {
			get {return CanonicalPaperName;}
			set {CanonicalPaperName = value;}
		}
		
		public bool PlotCurrentLayout {
			get {return CurLo;}
			set {CurLo = value;}
		}
		
		public bool ApplyStamp {
			get {return ShouldStamp;}
			set {ShouldStamp = value;}
		}
		
		public bool PlotToFile {
			get {return Plt2File;}
			set {Plt2File = value;}
		}
		
		public string PlotFileLocation {
			get {return PltFileName;}
			set {PltFileName = value;}
		}
		
		public bool ChangeLinetypeScale {
			get {return CurLtSc;}
			set {CurLtSc = value;}
		}
		
		public double LinetypeScaleModel {
			get {return LtScModel;}
			set {LtScModel = value;}
		}
		
		public double LinetypeScalePaper {
			get {return LtScPaper;}
			set {LtScPaper = value;}
		}
		
		public bool TurnOnViewports {
			get {return VpOn;}
			set {VpOn = value;}
		}
		
		public string[] LayoutsToPlot {
			get {return LoNames;}
			set {LoNames = value;}
		}
	}
	
	public class MyBPlot : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button LoListBtn;
		private System.Windows.Forms.GroupBox LtScGrp;
		private System.Windows.Forms.ComboBox ScaleComboBox;
		private System.Windows.Forms.CheckBox Lo1Ckbox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ContextMenu MyContextMenu;
		private System.Windows.Forms.TabPage LoSelTab;
		private System.Windows.Forms.Button CpCurSettingsBtn;
		private System.Windows.Forms.ComboBox LtScModel;
		private System.Windows.Forms.ComboBox AmountComboBox;
		private System.Windows.Forms.ComboBox comboBox2;
		private System.Windows.Forms.Button RemoveSettingsBtn;
		private System.Windows.Forms.CheckBox SelLoCkbox;
		private System.Windows.Forms.GroupBox PlotTabGrp;
		private System.Windows.Forms.ContextMenu LoTreeviewCtMenu;
		private System.Windows.Forms.ComboBox ctbFileComboBox;
		private System.Windows.Forms.TreeView LoTreeview;
		private System.Windows.Forms.TabPage PlotTab;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button ApplySettingsBtn;
		private System.Windows.Forms.ComboBox PlotterComboBox;
		private System.Windows.Forms.Button PlotBtn;
		private System.Windows.Forms.CheckBox VpChkBox;
		private System.Windows.Forms.CheckBox CurCkbox;
		private System.Windows.Forms.TabControl PlotTabs;
		private System.Windows.Forms.ListView DrawingListView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.CheckBox ScaleLwChkBox;
//		private System.Windows.Forms.CheckBox PltStpChkBox;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.CheckBox LtScChkBox;
		private System.Windows.Forms.ComboBox PaperComboBox;
		private System.Windows.Forms.Button PlotDirBtn;
		private System.Windows.Forms.Button SelectDrawingsBtn;
		private System.Windows.Forms.CheckBox Plot2FileChkBox;
		private System.Windows.Forms.ComboBox LtScPaper;
		private System.Windows.Forms.TabPage MiscTab;
		private System.Windows.Forms.CheckBox LandscapeChkBox;
		private static string[,] ScaleValueArray;
		private static object[] PlotObjectsArray;
        private static string GlbDeviceName = "PDF-XChange Printer 2012.pc3";
		private static string GlbPaper = "A1";
		private static string GlbctbFile = "G-STANDARD.ctb";
		private static string GlbScale = "1:1";
		private static string[] GlbCanonicalArray;
		private string PlotDate = DateTime.Now.Date.ToShortDateString();
		public MyBPlot()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		
		#region Windows Forms Designer generated code
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent() {
			this.LandscapeChkBox = new System.Windows.Forms.CheckBox();
			this.MiscTab = new System.Windows.Forms.TabPage();
			this.LtScPaper = new System.Windows.Forms.ComboBox();
			this.Plot2FileChkBox = new System.Windows.Forms.CheckBox();
			this.SelectDrawingsBtn = new System.Windows.Forms.Button();
			this.PlotDirBtn = new System.Windows.Forms.Button();
			this.PaperComboBox = new System.Windows.Forms.ComboBox();
			this.LtScChkBox = new System.Windows.Forms.CheckBox();
			this.CancelBtn = new System.Windows.Forms.Button();
//			this.PltStpChkBox = new System.Windows.Forms.CheckBox();
			this.ScaleLwChkBox = new System.Windows.Forms.CheckBox();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.DrawingListView = new System.Windows.Forms.ListView();
			this.PlotTabs = new System.Windows.Forms.TabControl();
			this.CurCkbox = new System.Windows.Forms.CheckBox();
			this.VpChkBox = new System.Windows.Forms.CheckBox();
			this.PlotBtn = new System.Windows.Forms.Button();
			this.PlotterComboBox = new System.Windows.Forms.ComboBox();
			this.ApplySettingsBtn = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.PlotTab = new System.Windows.Forms.TabPage();
			this.LoTreeview = new System.Windows.Forms.TreeView();
			this.ctbFileComboBox = new System.Windows.Forms.ComboBox();
			this.LoTreeviewCtMenu = new System.Windows.Forms.ContextMenu();
			this.PlotTabGrp = new System.Windows.Forms.GroupBox();
			this.SelLoCkbox = new System.Windows.Forms.CheckBox();
			this.RemoveSettingsBtn = new System.Windows.Forms.Button();
			this.comboBox2 = new System.Windows.Forms.ComboBox();
			this.AmountComboBox = new System.Windows.Forms.ComboBox();
			this.LtScModel = new System.Windows.Forms.ComboBox();
			this.CpCurSettingsBtn = new System.Windows.Forms.Button();
			this.LoSelTab = new System.Windows.Forms.TabPage();
			this.MyContextMenu = new System.Windows.Forms.ContextMenu();
			this.label4 = new System.Windows.Forms.Label();
			this.Lo1Ckbox = new System.Windows.Forms.CheckBox();
			this.ScaleComboBox = new System.Windows.Forms.ComboBox();
			this.LtScGrp = new System.Windows.Forms.GroupBox();
			this.LoListBtn = new System.Windows.Forms.Button();
			this.MiscTab.SuspendLayout();
			this.PlotTabs.SuspendLayout();
			this.PlotTab.SuspendLayout();
			this.PlotTabGrp.SuspendLayout();
			this.LoSelTab.SuspendLayout();
			this.LtScGrp.SuspendLayout();
			this.SuspendLayout();
			// 
			// LandscapeChkBox
			// 
			this.LandscapeChkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LandscapeChkBox.BackColor = System.Drawing.Color.SteelBlue;
			this.LandscapeChkBox.Checked = true;
			this.LandscapeChkBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.LandscapeChkBox.ForeColor = System.Drawing.SystemColors.ControlText;
			this.LandscapeChkBox.Location = new System.Drawing.Point(6, 168);
			this.LandscapeChkBox.Name = "LandscapeChkBox";
			this.LandscapeChkBox.Size = new System.Drawing.Size(80, 24);
			this.LandscapeChkBox.TabIndex = 7;
			this.LandscapeChkBox.Text = "Landscape";
			// 
			// MiscTab
			// 
			this.MiscTab.BackColor = System.Drawing.Color.SteelBlue;
			this.MiscTab.Controls.Add(this.LtScGrp);
			this.MiscTab.Controls.Add(this.Plot2FileChkBox);
			this.MiscTab.Controls.Add(this.PlotDirBtn);
			this.MiscTab.Controls.Add(this.LtScChkBox);
			this.MiscTab.Controls.Add(this.comboBox2);
			this.MiscTab.Controls.Add(this.VpChkBox);
			this.MiscTab.Location = new System.Drawing.Point(4, 22);
			this.MiscTab.Name = "MiscTab";
			this.MiscTab.Size = new System.Drawing.Size(315, 270);
			this.MiscTab.TabIndex = 1;
			this.MiscTab.Text = "Misc. settings";
			// 
			// LtScPaper
			// 
			this.LtScPaper.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LtScPaper.BackColor = System.Drawing.Color.Silver;
			this.LtScPaper.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple;
			this.LtScPaper.ForeColor = System.Drawing.Color.Navy;
			this.LtScPaper.Location = new System.Drawing.Point(136, 23);
			this.LtScPaper.MaxDropDownItems = 12;
			this.LtScPaper.Name = "LtScPaper";
			this.LtScPaper.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.LtScPaper.Size = new System.Drawing.Size(48, 21);
			this.LtScPaper.Sorted = true;
			this.LtScPaper.TabIndex = 7;
			this.LtScPaper.Text = "1";
			// 
			// Plot2FileChkBox
			// 
			this.Plot2FileChkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.Plot2FileChkBox.BackColor = System.Drawing.Color.SteelBlue;
			this.Plot2FileChkBox.ForeColor = System.Drawing.SystemColors.ControlText;
			this.Plot2FileChkBox.Location = new System.Drawing.Point(8, 144);
			this.Plot2FileChkBox.Name = "Plot2FileChkBox";
			this.Plot2FileChkBox.Size = new System.Drawing.Size(79, 24);
			this.Plot2FileChkBox.TabIndex = 8;
			this.Plot2FileChkBox.Text = "Plot to file";
			this.Plot2FileChkBox.CheckedChanged += new System.EventHandler(this.Plot2FileChkBoxCheckedChanged);
			// 
			// SelectDrawingsBtn
			// 
			this.SelectDrawingsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.SelectDrawingsBtn.BackColor = System.Drawing.Color.Silver;
			this.SelectDrawingsBtn.ForeColor = System.Drawing.Color.Navy;
			this.SelectDrawingsBtn.Location = new System.Drawing.Point(373, 304);
			this.SelectDrawingsBtn.Name = "SelectDrawingsBtn";
			this.SelectDrawingsBtn.Size = new System.Drawing.Size(125, 23);
			this.SelectDrawingsBtn.TabIndex = 4;
			this.SelectDrawingsBtn.Text = "Select drawings";
			this.SelectDrawingsBtn.Click += new System.EventHandler(this.SelectDrawingsBtnClick);
			// 
			// PlotDirBtn
			// 
			this.PlotDirBtn.BackColor = System.Drawing.Color.Silver;
			this.PlotDirBtn.Enabled = false;
			this.PlotDirBtn.ForeColor = System.Drawing.Color.Navy;
			this.PlotDirBtn.Location = new System.Drawing.Point(96, 144);
			this.PlotDirBtn.Name = "PlotDirBtn";
			this.PlotDirBtn.Size = new System.Drawing.Size(104, 23);
			this.PlotDirBtn.TabIndex = 4;
			this.PlotDirBtn.Text = "Plot to directory";
			this.PlotDirBtn.Click += new System.EventHandler(this.PlotDirBtnClick);
			// 
			// PaperComboBox
			// 
			this.PaperComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.PaperComboBox.BackColor = System.Drawing.Color.Silver;
			this.PaperComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.PaperComboBox.ForeColor = System.Drawing.Color.Navy;
			this.PaperComboBox.Location = new System.Drawing.Point(6, 40);
			this.PaperComboBox.MaxDropDownItems = 12;
			this.PaperComboBox.Name = "PaperComboBox";
			this.PaperComboBox.Size = new System.Drawing.Size(224, 21);
			this.PaperComboBox.TabIndex = 6;
			// 
			// LtScChkBox
			// 
			this.LtScChkBox.Location = new System.Drawing.Point(8, 16);
			this.LtScChkBox.Name = "LtScChkBox";
			this.LtScChkBox.Size = new System.Drawing.Size(136, 24);
			this.LtScChkBox.TabIndex = 0;
			this.LtScChkBox.Text = "Set lintype scale?";
			this.LtScChkBox.CheckedChanged += new System.EventHandler(this.LtScChkBoxCheckedChanged);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.BackColor = System.Drawing.Color.Silver;
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.ForeColor = System.Drawing.Color.Navy;
			this.CancelBtn.Location = new System.Drawing.Point(608, 376);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.TabIndex = 3;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtnClick);
			// 
			// PltStpChkBox
			// 
//			this.PltStpChkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
//			this.PltStpChkBox.BackColor = System.Drawing.Color.SteelBlue;
//			this.PltStpChkBox.Checked = false;
//			this.PltStpChkBox.CheckState = System.Windows.Forms.CheckState.Unchecked;
//			this.PltStpChkBox.ForeColor = System.Drawing.SystemColors.ControlText;
//			this.PltStpChkBox.Location = new System.Drawing.Point(227, 168);
//			this.PltStpChkBox.Name = "PltStpChkBox";
//			this.PltStpChkBox.Size = new System.Drawing.Size(79, 24);
//			this.PltStpChkBox.TabIndex = 7;
//			this.PltStpChkBox.Text = "Plot Stamp";
			// 
			// ScaleLwChkBox
			// 
			this.ScaleLwChkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ScaleLwChkBox.BackColor = System.Drawing.Color.SteelBlue;
			this.ScaleLwChkBox.Checked = false;
			this.ScaleLwChkBox.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.ScaleLwChkBox.ForeColor = System.Drawing.SystemColors.ControlText;
			this.ScaleLwChkBox.Location = new System.Drawing.Point(99, 168);
			this.ScaleLwChkBox.Name = "ScaleLwChkBox";
			this.ScaleLwChkBox.Size = new System.Drawing.Size(112, 24);
			this.ScaleLwChkBox.TabIndex = 7;
			this.ScaleLwChkBox.Text = "Scale lineweights";
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Plot?";
			this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "List of Drawings";
			this.columnHeader1.Width = 300;
			// 
			// DrawingListView
			// 
			this.DrawingListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
						| System.Windows.Forms.AnchorStyles.Left) 
						| System.Windows.Forms.AnchorStyles.Right)));
			this.DrawingListView.AutoArrange = false;
			this.DrawingListView.BackColor = System.Drawing.Color.Silver;
			this.DrawingListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
						this.columnHeader1,
						this.columnHeader2});
			this.DrawingListView.ContextMenu = this.MyContextMenu;
			this.DrawingListView.ForeColor = System.Drawing.Color.Navy;
			this.DrawingListView.GridLines = true;
			this.DrawingListView.HideSelection = false;
			this.DrawingListView.Location = new System.Drawing.Point(0, 0);
			this.DrawingListView.Name = "DrawingListView";
			this.DrawingListView.Size = new System.Drawing.Size(368, 455);
			this.DrawingListView.TabIndex = 5;
			this.DrawingListView.View = System.Windows.Forms.View.Details;
			// 
			// PlotTabs
			// 
			this.PlotTabs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
						| System.Windows.Forms.AnchorStyles.Right)));
			this.PlotTabs.Controls.Add(this.PlotTab);
			this.PlotTabs.Controls.Add(this.MiscTab);
			this.PlotTabs.Controls.Add(this.LoSelTab);
			this.PlotTabs.Location = new System.Drawing.Point(368, 0);
			this.PlotTabs.Name = "PlotTabs";
			this.PlotTabs.SelectedIndex = 0;
			this.PlotTabs.Size = new System.Drawing.Size(323, 296);
			this.PlotTabs.TabIndex = 9;
			// 
			// CurCkbox
			// 
			this.CurCkbox.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.CurCkbox.BackColor = System.Drawing.Color.SteelBlue;
            this.CurCkbox.Checked = true;
            this.CurCkbox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.CurCkbox.ForeColor = System.Drawing.SystemColors.ControlText;
			this.CurCkbox.Location = new System.Drawing.Point(208, 23);
			this.CurCkbox.Name = "CurCkbox";
			this.CurCkbox.Size = new System.Drawing.Size(62, 24);
			this.CurCkbox.TabIndex = 7;
			this.CurCkbox.Text = "Current";
			this.CurCkbox.CheckedChanged += new System.EventHandler(this.CurCkboxCheckedChanged);
			// 
			// VpChkBox
			// 
			this.VpChkBox.Location = new System.Drawing.Point(8, 112);
			this.VpChkBox.Name = "VpChkBox";
			this.VpChkBox.Size = new System.Drawing.Size(136, 24);
			this.VpChkBox.TabIndex = 0;
			this.VpChkBox.Text = "Turn on all viewports?";
			// 
			// PlotBtn
			// 
			this.PlotBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.PlotBtn.BackColor = System.Drawing.Color.Silver;
			this.PlotBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.PlotBtn.ForeColor = System.Drawing.Color.Navy;
			this.PlotBtn.Location = new System.Drawing.Point(504, 376);
			this.PlotBtn.Name = "PlotBtn";
			this.PlotBtn.TabIndex = 3;
			this.PlotBtn.Text = "Plot";
			this.PlotBtn.Click += new System.EventHandler(this.PlotBtnClick);
			// 
			// PlotterComboBox
			// 
			this.PlotterComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.PlotterComboBox.BackColor = System.Drawing.Color.Silver;
			this.PlotterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.PlotterComboBox.ForeColor = System.Drawing.Color.Navy;
			this.PlotterComboBox.Location = new System.Drawing.Point(6, 8);
			this.PlotterComboBox.MaxDropDownItems = 12;
			this.PlotterComboBox.Name = "PlotterComboBox";
			this.PlotterComboBox.Size = new System.Drawing.Size(224, 21);
			this.PlotterComboBox.TabIndex = 6;
			this.PlotterComboBox.SelectedIndexChanged += new System.EventHandler(this.DeviceNameChanged);
			// 
			// ApplySettingsBtn
			// 
			this.ApplySettingsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ApplySettingsBtn.BackColor = System.Drawing.Color.Silver;
			this.ApplySettingsBtn.ForeColor = System.Drawing.Color.Navy;
			this.ApplySettingsBtn.Location = new System.Drawing.Point(373, 336);
			this.ApplySettingsBtn.Name = "ApplySettingsBtn";
			this.ApplySettingsBtn.Size = new System.Drawing.Size(125, 23);
			this.ApplySettingsBtn.TabIndex = 4;
			this.ApplySettingsBtn.Text = "Apply settings";
			this.ApplySettingsBtn.Click += new System.EventHandler(this.ApplySettingsBtnClick);
			// 
			// label5
			// 
			this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label5.Location = new System.Drawing.Point(238, 104);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(72, 22);
			this.label5.TabIndex = 1;
			this.label5.Text = "Plot scale";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label6.Location = new System.Drawing.Point(72, 23);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(48, 22);
			this.label6.TabIndex = 8;
			this.label6.Text = "Model";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label7
			// 
			this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label7.Location = new System.Drawing.Point(192, 23);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(48, 22);
			this.label7.TabIndex = 8;
			this.label7.Text = "Paper size";
			this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.Location = new System.Drawing.Point(238, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(72, 22);
			this.label1.TabIndex = 1;
			this.label1.Text = "Printer/plotter";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.Location = new System.Drawing.Point(238, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 22);
			this.label2.TabIndex = 1;
			this.label2.Text = "Paper size";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label3.Location = new System.Drawing.Point(238, 72);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(72, 22);
			this.label3.TabIndex = 1;
			this.label3.Text = ".ctb File";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// PlotTab
			// 
			this.PlotTab.BackColor = System.Drawing.Color.SteelBlue;
			this.PlotTab.Controls.Add(this.label1);
//			this.PlotTab.Controls.Add(this.PltStpChkBox);
			this.PlotTab.Controls.Add(this.AmountComboBox);
			this.PlotTab.Controls.Add(this.label4);
			this.PlotTab.Controls.Add(this.LandscapeChkBox);
			this.PlotTab.Controls.Add(this.ScaleComboBox);
			this.PlotTab.Controls.Add(this.ctbFileComboBox);
			this.PlotTab.Controls.Add(this.PaperComboBox);
			this.PlotTab.Controls.Add(this.label5);
			this.PlotTab.Controls.Add(this.label3);
			this.PlotTab.Controls.Add(this.label2);
			this.PlotTab.Controls.Add(this.PlotterComboBox);
			this.PlotTab.Controls.Add(this.ScaleLwChkBox);
			this.PlotTab.Controls.Add(this.PlotTabGrp);
			this.PlotTab.Location = new System.Drawing.Point(4, 22);
			this.PlotTab.Name = "PlotTab";
			this.PlotTab.Size = new System.Drawing.Size(315, 270);
			this.PlotTab.TabIndex = 0;
			this.PlotTab.Text = "Plot settings";
			// 
			// LoTreeview
			// 
			this.LoTreeview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
						| System.Windows.Forms.AnchorStyles.Left) 
						| System.Windows.Forms.AnchorStyles.Right)));
			this.LoTreeview.BackColor = System.Drawing.Color.Silver;
			this.LoTreeview.CheckBoxes = true;
			this.LoTreeview.ContextMenu = this.LoTreeviewCtMenu;
			this.LoTreeview.ForeColor = System.Drawing.Color.Navy;
			this.LoTreeview.HideSelection = false;
			this.LoTreeview.ImageIndex = -1;
			this.LoTreeview.Location = new System.Drawing.Point(0, 0);
			this.LoTreeview.Name = "LoTreeview";
			this.LoTreeview.SelectedImageIndex = -1;
			this.LoTreeview.Size = new System.Drawing.Size(315, 248);
			this.LoTreeview.TabIndex = 0;
			// 
			// ctbFileComboBox
			// 
			this.ctbFileComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ctbFileComboBox.BackColor = System.Drawing.Color.Silver;
			this.ctbFileComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ctbFileComboBox.ForeColor = System.Drawing.Color.Navy;
			this.ctbFileComboBox.Location = new System.Drawing.Point(6, 72);
			this.ctbFileComboBox.MaxDropDownItems = 12;
			this.ctbFileComboBox.Name = "ctbFileComboBox";
			this.ctbFileComboBox.Size = new System.Drawing.Size(224, 21);
			this.ctbFileComboBox.TabIndex = 6;
			// 
			// LoTreeviewCtMenu
			// 
			this.LoTreeviewCtMenu.Popup += new System.EventHandler(this.LoTreeviewPopUp);
			// 
			// PlotTabGrp
			// 
			this.PlotTabGrp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.PlotTabGrp.Controls.Add(this.Lo1Ckbox);
			this.PlotTabGrp.Controls.Add(this.SelLoCkbox);
			this.PlotTabGrp.Controls.Add(this.CurCkbox);
			this.PlotTabGrp.ForeColor = System.Drawing.SystemColors.ControlText;
			this.PlotTabGrp.Location = new System.Drawing.Point(3, 200);
			this.PlotTabGrp.Name = "PlotTabGrp";
			this.PlotTabGrp.Size = new System.Drawing.Size(273, 62);
			this.PlotTabGrp.TabIndex = 8;
			this.PlotTabGrp.TabStop = false;
			this.PlotTabGrp.Text = "Plot layout options";
			// 
			// SelLoCkbox
			// 
			this.SelLoCkbox.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.SelLoCkbox.BackColor = System.Drawing.Color.SteelBlue;
            this.SelLoCkbox.Checked = false;
            this.SelLoCkbox.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.SelLoCkbox.ForeColor = System.Drawing.SystemColors.ControlText;
			this.SelLoCkbox.Location = new System.Drawing.Point(88, 23);
			this.SelLoCkbox.Name = "SelLoCkbox";
			this.SelLoCkbox.Size = new System.Drawing.Size(114, 24);
			this.SelLoCkbox.TabIndex = 7;
			this.SelLoCkbox.Text = "Selected layouts";
			this.SelLoCkbox.CheckedChanged += new System.EventHandler(this.AllLosCkboxCheckedChanged);
			// 
			// RemoveSettingsBtn
			// 
			this.RemoveSettingsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RemoveSettingsBtn.BackColor = System.Drawing.Color.Silver;
			this.RemoveSettingsBtn.ForeColor = System.Drawing.Color.Navy;
			this.RemoveSettingsBtn.Location = new System.Drawing.Point(525, 336);
			this.RemoveSettingsBtn.Name = "RemoveSettingsBtn";
			this.RemoveSettingsBtn.Size = new System.Drawing.Size(125, 23);
			this.RemoveSettingsBtn.TabIndex = 3;
			this.RemoveSettingsBtn.Text = "Remove settings";
			this.RemoveSettingsBtn.Click += new System.EventHandler(this.RemoveSettingsBtnClick);
			// 
			// comboBox2
			// 
			this.comboBox2.Location = new System.Drawing.Point(336, 16);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new System.Drawing.Size(121, 21);
			this.comboBox2.TabIndex = 1;
			this.comboBox2.Text = "comboBox1";
			// 
			// AmountComboBox
			// 
			this.AmountComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.AmountComboBox.BackColor = System.Drawing.Color.Silver;
			this.AmountComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple;
			this.AmountComboBox.ForeColor = System.Drawing.Color.Navy;
			this.AmountComboBox.Location = new System.Drawing.Point(6, 136);
			this.AmountComboBox.MaxDropDownItems = 12;
			this.AmountComboBox.Name = "AmountComboBox";
			this.AmountComboBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.AmountComboBox.Size = new System.Drawing.Size(224, 21);
			this.AmountComboBox.Sorted = true;
			this.AmountComboBox.TabIndex = 6;
			this.AmountComboBox.Text = "1";
			// 
			// LtScModel
			// 
			this.LtScModel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LtScModel.BackColor = System.Drawing.Color.Silver;
			this.LtScModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple;
			this.LtScModel.ForeColor = System.Drawing.Color.Navy;
			this.LtScModel.Location = new System.Drawing.Point(16, 23);
			this.LtScModel.MaxDropDownItems = 12;
			this.LtScModel.Name = "LtScModel";
			this.LtScModel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.LtScModel.Size = new System.Drawing.Size(48, 21);
			this.LtScModel.Sorted = true;
			this.LtScModel.TabIndex = 7;
			this.LtScModel.Text = "1";
			// 
			// CpCurSettingsBtn
			// 
			this.CpCurSettingsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CpCurSettingsBtn.BackColor = System.Drawing.Color.Silver;
			this.CpCurSettingsBtn.ForeColor = System.Drawing.Color.Navy;
			this.CpCurSettingsBtn.Location = new System.Drawing.Point(525, 304);
			this.CpCurSettingsBtn.Name = "CpCurSettingsBtn";
			this.CpCurSettingsBtn.Size = new System.Drawing.Size(125, 23);
			this.CpCurSettingsBtn.TabIndex = 3;
			this.CpCurSettingsBtn.Text = "Get current settings";
			this.CpCurSettingsBtn.Click += new System.EventHandler(this.CpCurSettingsBtnClick);
			// 
			// LoSelTab
			// 
			this.LoSelTab.BackColor = System.Drawing.Color.SteelBlue;
			this.LoSelTab.Controls.Add(this.LoListBtn);
			this.LoSelTab.Controls.Add(this.LoTreeview);
			this.LoSelTab.Location = new System.Drawing.Point(4, 22);
			this.LoSelTab.Name = "LoSelTab";
			this.LoSelTab.Size = new System.Drawing.Size(315, 270);
			this.LoSelTab.TabIndex = 2;
			this.LoSelTab.Text = "Layout selection";
			// 
			// MyContextMenu
			// 
			this.MyContextMenu.Popup += new System.EventHandler(this.DrawingListPopUp);
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label4.Location = new System.Drawing.Point(238, 136);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(72, 22);
			this.label4.TabIndex = 1;
			this.label4.Text = "No. of copies";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// Lo1Ckbox
			// 
			this.Lo1Ckbox.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Lo1Ckbox.BackColor = System.Drawing.Color.SteelBlue;
			this.Lo1Ckbox.Checked = false;
			this.Lo1Ckbox.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.Lo1Ckbox.ForeColor = System.Drawing.SystemColors.ControlText;
			this.Lo1Ckbox.Location = new System.Drawing.Point(14, 22);
			this.Lo1Ckbox.Name = "Lo1Ckbox";
			this.Lo1Ckbox.Size = new System.Drawing.Size(70, 24);
			this.Lo1Ckbox.TabIndex = 7;
			this.Lo1Ckbox.Text = "Layout1";
			this.Lo1Ckbox.CheckedChanged += new System.EventHandler(this.Lo1CkboxCheckedChanged);
			// 
			// ScaleComboBox
			// 
			this.ScaleComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ScaleComboBox.BackColor = System.Drawing.Color.Silver;
			this.ScaleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ScaleComboBox.ForeColor = System.Drawing.Color.Navy;
			this.ScaleComboBox.Location = new System.Drawing.Point(6, 104);
			this.ScaleComboBox.MaxDropDownItems = 12;
			this.ScaleComboBox.Name = "ScaleComboBox";
			this.ScaleComboBox.Size = new System.Drawing.Size(224, 21);
			this.ScaleComboBox.Sorted = true;
			this.ScaleComboBox.TabIndex = 6;
			// 
			// LtScGrp
			// 
			this.LtScGrp.Controls.Add(this.label6);
			this.LtScGrp.Controls.Add(this.LtScModel);
			this.LtScGrp.Controls.Add(this.LtScPaper);
			this.LtScGrp.Controls.Add(this.label7);
			this.LtScGrp.Enabled = false;
			this.LtScGrp.Location = new System.Drawing.Point(8, 40);
			this.LtScGrp.Name = "LtScGrp";
			this.LtScGrp.Size = new System.Drawing.Size(272, 64);
			this.LtScGrp.TabIndex = 9;
			this.LtScGrp.TabStop = false;
			this.LtScGrp.Text = "Linetype scale values";
			// 
			// LoListBtn
			// 
			this.LoListBtn.BackColor = System.Drawing.Color.SteelBlue;
			this.LoListBtn.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.LoListBtn.ForeColor = System.Drawing.Color.Black;
			this.LoListBtn.Location = new System.Drawing.Point(0, 247);
			this.LoListBtn.Name = "LoListBtn";
			this.LoListBtn.Size = new System.Drawing.Size(315, 23);
			this.LoListBtn.TabIndex = 1;
			this.LoListBtn.Text = "List layouts of selected drawings";
			this.LoListBtn.Click += new System.EventHandler(this.LoListBtnClick);
			// 
			// MyBPlot
			// 
			this.AcceptButton = this.PlotBtn;
			//this.AutoScale = false;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.SteelBlue;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(693, 406);
			this.Controls.Add(this.PlotTabs);
			this.Controls.Add(this.DrawingListView);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.PlotBtn);
			this.Controls.Add(this.SelectDrawingsBtn);
			this.Controls.Add(this.ApplySettingsBtn);
			this.Controls.Add(this.RemoveSettingsBtn);
			this.Controls.Add(this.CpCurSettingsBtn);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(701, 440);
			this.Name = "MyBPlot";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Batch Plot [ v2.4 ] updated by Robbo 2014";
			this.Resize += new System.EventHandler(this.FormResized);
			this.Load += new System.EventHandler(this.MyPlotLoad);
			this.MiscTab.ResumeLayout(false);
			this.PlotTabs.ResumeLayout(false);
			this.PlotTab.ResumeLayout(false);
			this.PlotTabGrp.ResumeLayout(false);
			this.LoSelTab.ResumeLayout(false);
			this.LtScGrp.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		#endregion
		void MyPlotLoad(object sender, System.EventArgs e)
		{
			string tempStr;
			bool tempTest = false;
			PlotConfigInfoCollection PCIC = PCM.Devices;
			foreach (PlotConfigInfo pci in PCIC) {
				PlotterComboBox.Items.Add(pci.DeviceName);
				if (string.Compare(pci.DeviceName, GlbDeviceName) == 0) tempTest = true;
			}
			if (tempTest) {
				PlotterComboBox.Text = GlbDeviceName;
				PaperComboBox.Text = GlbPaper;
				tempTest = false;
			}
			else {
				PlotterComboBox.Text = PlotterComboBox.Items[0].ToString();
				UpdatePaperListbox(PlotterComboBox.Items[0].ToString());
			}
			StringCollection ctbNames = PCM.ColorDependentPlotStyles;
			foreach (string str in ctbNames) {
				string[] tempStrArray = str.Split(new char[] {'\\'});
				ctbFileComboBox.Items.Add(tempStrArray[tempStrArray.Length - 1]);
				if (string.Compare(tempStrArray[tempStrArray.Length - 1], GlbctbFile) == 0) tempTest = true;
			}
			if (tempTest) {
				ctbFileComboBox.Text = GlbctbFile;
				tempTest = false;
			}
			else ctbFileComboBox.Text = ctbFileComboBox.Items[0].ToString();
			Type Scales = typeof(StdScaleType);
			string[] ScaleArray = Enum.GetNames(Scales);
			int i = 0;
			ScaleValueArray = new string[ScaleArray.Length, 2];
			foreach (string str in Enum.GetNames(Scales)) {
				tempStr = FormatStandardScale(str);
				ScaleComboBox.Items.Add(tempStr);
				ScaleValueArray[i, 0] = tempStr;
				ScaleValueArray[i, 1] = str;
				++i;
				if (string.Compare(tempStr, GlbScale) == 0) tempTest = true;
			}
			if (tempTest) {
				ScaleComboBox.Text = GlbScale;
			}
			else ScaleComboBox.Text = ScaleComboBox.Items[0].ToString();
		}
		
		public string FormatStandardScale (string str) {
			if (IsInString(str, "StdScale")) {
				str = str.Substring(8);
				if (IsInString(str, "Millimeter")) {
					str = str.Replace("To", "/");
                    str = str.Replace("Millimeter", "\"");
					str = str.Replace("Is", " = ");
					str = str.Replace("m", "\'");
					return str;
				}
				else if (string.Compare(str, "1mIs1m") == 0) {
					return "1\' = 1\'";
				}
				else {
					str = str.Replace("To", ":");
					return str;
				}
			}
			else return "Scale to Fit";
		}
		
		public bool IsInString (string ToCheck, string InQuestion) {
			for (int i = 0; i + InQuestion.Length < ToCheck.Length; ++i) {
				if (string.Compare(ToCheck.Substring(i, InQuestion.Length), InQuestion) == 0) {
					return true;
				}
			}
			return false;
		}
		
		public void UpdatePaperListbox (string DeviceName) {
			PaperComboBox.Items.Clear();
			PlotConfig pc = PCM.SetCurrentConfig(DeviceName);
			GlbCanonicalArray = new string[pc.CanonicalMediaNames.Count];
			int i = 0;
			foreach (string str in pc.CanonicalMediaNames) {
				PaperComboBox.Items.Add(pc.GetLocalMediaName(str));
				GlbCanonicalArray[i] = str;
				++i;
			}
			PaperComboBox.Text = PaperComboBox.Items[0].ToString();
		}
		
		void DeviceNameChanged (object sender, System.EventArgs e)
		{
			UpdatePaperListbox(PlotterComboBox.SelectedItem.ToString());
		}
		
		void CancelBtnClick(object sender, System.EventArgs e)
		{
			Close();
		}
		
		void PlotBtnClick(object sender, System.EventArgs e)
		{
			DrawingListView.Sort();			GlbDeviceName = PlotterComboBox.Text;
			GlbPaper = PaperComboBox.Text;
			GlbctbFile = ctbFileComboBox.Text;
			GlbScale = ScaleComboBox.Text;
			ListView.ListViewItemCollection lvic = DrawingListView.Items;
			PlotObjectsArray = new object[lvic.Count];
			for (int i = 0; i < lvic.Count; ++i) {
				PlotObjectsArray[i] = lvic[i].Tag as MyPlotParams;
			}
			this.Close();
		}
		
		void SelectDrawingsBtnClick(object sender, System.EventArgs e)
		{
			Autodesk.AutoCAD.Windows.OpenFileDialog Dia =
				new Autodesk.AutoCAD.Windows.OpenFileDialog("Select Drawings to Plot",
				                                            "",
				                                            "dwg",
				                                            "",
				                                            Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.AllowMultiple
				                                           );
			if (Dia.ShowDialog() == DialogResult.OK) {
				string[] FileNames = Dia.GetFilenames();
				Array.Sort(FileNames);
				foreach (string str in FileNames) {
					DrawingListView.Items.Add(str);
				}
			}
		}
		
		void ApplySettingsBtnClick(object sender, System.EventArgs e)
		{
			string RealScale = "";
			string DeviceName = PlotterComboBox.SelectedItem.ToString();
			string PaperSize = PaperComboBox.SelectedItem.ToString();
			string ctbFile = ctbFileComboBox.SelectedItem.ToString();
			string Scale = ScaleComboBox.SelectedItem.ToString();
			int Amount = Convert.ToInt16(AmountComboBox.Text);
			Autodesk.AutoCAD.DatabaseServices.PlotRotation PltRot = Autodesk.AutoCAD.DatabaseServices.PlotRotation.Degrees000;
			if (
			    string.Compare(DeviceName, string.Empty) != 0
			    &&
			    string.Compare(PaperSize, string.Empty) != 0
			    &&
			    string.Compare(ctbFile, string.Empty) !=0
			    &&
			    string.Compare(Scale, string.Empty) !=0
			   )
			{
				for (int i = 0; i < ScaleValueArray.Length; ++i) {
					if (string.Compare(Scale, ScaleValueArray[i,0]) == 0) {
						RealScale = ScaleValueArray[i,1];
						i = ScaleValueArray.Length;
					}
				}
				StdScaleType ScaleType = (StdScaleType) Enum.Parse(typeof(StdScaleType), RealScale, false);
				foreach (ListViewItem lvi in DrawingListView.SelectedItems) {
					lvi.SubItems.Add("Yes");
					if (!LandscapeChkBox.Checked) PltRot = Autodesk.AutoCAD.DatabaseServices.PlotRotation.Degrees000;
					MyPlotParams mpp = new MyPlotParams(
					                           lvi.Text,
					                           DeviceName,
					                           PaperSize,
					                           ctbFile,
					                           ScaleLwChkBox.Checked,
					                           Amount,
					                           ScaleType,
					                           PltRot,
					                           GlbCanonicalArray[PaperComboBox.SelectedIndex]
					                          );
					mpp.PlotCurrentLayout = CurCkbox.Checked;
//					mpp.ApplyStamp = PltStpChkBox.Checked;
					mpp.PlotToFile = Plot2FileChkBox.Checked;
					if (Plot2FileChkBox.Checked) {
						FileInfo fi = new FileInfo(lvi.Text);
						string[] tempArray = fi.Name.Split('.');
						string PltFileDir = PlotDirBtn.Tag as string;
						mpp.PlotFileLocation = PltFileDir + "\\" + tempArray[0] + ".plt";
					}
					mpp.TurnOnViewports = VpChkBox.Checked;
					mpp.ChangeLinetypeScale = LtScChkBox.Checked;
					mpp.LinetypeScaleModel = Convert.ToDouble(LtScModel.Text);
					mpp.LinetypeScalePaper = Convert.ToDouble(LtScPaper.Text);
					if (SelLoCkbox.Checked) {
						try {
							string[] DrawingNames = LoTreeview.Tag as string[];
							TreeNode MainNode = LoTreeview.Nodes[Array.IndexOf(DrawingNames, lvi.Text as object)];
							string[] LoNames = new string[MainNode.Nodes.Count];
							int cnt = 0;
							foreach (TreeNode tn in MainNode.Nodes) {
								if (tn.Checked) {
									LoNames[cnt] = tn.Text;
									++cnt;
								}
							}
							mpp.LayoutsToPlot = LoNames;
						}
						catch (System.Exception ex) { MessageBox.Show(ex.Message); }
					}
					else if (Lo1Ckbox.Checked) mpp.LayoutsToPlot = new string[1]{"Layout1"};
					lvi.Tag = mpp;
				}
			}
		}
		
		void RemoveSettingsBtnClick(object sender, System.EventArgs e)
		{
			string tempStr;
			foreach (ListViewItem lvi in DrawingListView.SelectedItems) {
				tempStr = lvi.Text;
				lvi.SubItems.Clear();
				lvi.Text = tempStr;
				lvi.Tag = null;
			}
			
		}
		
		void ViewSettingsCtMenu (object sender, System.EventArgs e)
		{
			string Message = string.Empty;
			MyPlotParams tempPltParams;
			foreach (ListViewItem lvi in DrawingListView.SelectedItems) {
				tempPltParams = lvi.Tag as MyPlotParams;
				if (tempPltParams != null) {
					if (Message != string.Empty) Message = Message + "\n\n";
					Message = Message +
						"Drawing path: " + tempPltParams.DrawingPath +
						"\n    Printer/plotter: " + tempPltParams.Device +
						"\n    Paper size: " + tempPltParams.Paper + 
						"\n    .ctb File: " + tempPltParams.ctbFile +
						"\n    Plot scale: " + tempPltParams.AcScaleType.ToString() +
						"\n    No. of copies: " + tempPltParams.Amount.ToString() +
						"\n    Landscape: " + tempPltParams.AcPlotRotation.ToString() +
						"\n    Scale lineweights: " + tempPltParams.ScaleLineweight.ToString() +
//					    "\n    Apply plot stamp: " + tempPltParams.ApplyStamp +
						"\n    Turn on viewports: " + tempPltParams.TurnOnViewports.ToString() +
						"\n    Use current linetype scale: " + tempPltParams.ChangeLinetypeScale.ToString();
				}
			}
			if (Message != string.Empty) MessageBox.Show(Message, "Plot settings.");
		}
		
		void DrawingListPopUp (object sender, System.EventArgs e)
		{
			System.Windows.Forms.Menu.MenuItemCollection mic = MyContextMenu.MenuItems;
			mic.Clear();
			System.Windows.Forms.MenuItem mi1 = new System.Windows.Forms.MenuItem("&Apply settings");
			mi1.Click += new EventHandler(ApplySettingsBtnClick);
			mic.Add(mi1);
			mi1.Enabled = new EventHandler(ApplySettingsBtnClick) != null;
			System.Windows.Forms.MenuItem mi2 = new System.Windows.Forms.MenuItem("&Remove settings");
			mi2.Click += new EventHandler(RemoveSettingsBtnClick);
			mic.Add(mi2);
			mi2.Enabled = new EventHandler(RemoveSettingsBtnClick) != null;
			System.Windows.Forms.MenuItem mi3 = new System.Windows.Forms.MenuItem("&View settings");
			mi3.Click += new EventHandler(ViewSettingsCtMenu);
			mic.Add(mi3);
			mi3.Enabled = new EventHandler(ViewSettingsCtMenu) != null;
		}
				
		public void MyPlottingPart (MyPlotParams PltParams, bool IsModel) {
			object OldBkGdPlt = AcadApp.GetSystemVariable("BackGroundPlot");
			AcadApp.SetSystemVariable("BackGroundPlot", 0);
			Database db = HostApplicationServices.WorkingDatabase;
			PlotInfo PltInfo = new PlotInfo();
			PltInfo.Layout = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
			PlotSettings PltSet = new PlotSettings(IsModel);
			PlotSettingsValidator PltSetVald = PlotSettingsValidator.Current;
			PlotPageInfo PltPgInfo = new PlotPageInfo();
			PlotProgressDialog PltPrgDia = new PlotProgressDialog(false, 1, true);
			PlotEngine PltEng = PlotFactory.CreatePublishEngine();
			PCM.SetCurrentConfig(PltParams.Device);
			PCM.RefreshList(RefreshCode.All);
			PlotConfig pc = PCM.SetCurrentConfig(PltParams.Device);
			try {
				PltSetVald.SetPlotConfigurationName(PltSet, PltParams.Device, PltParams.CanonicalPaper);
				PltSetVald.RefreshLists(PltSet);
				
                PltSetVald.SetCurrentStyleSheet(PltSet, PltParams.ctbFile);

				PltSetVald.SetPlotOrigin(PltSet, new Point2d(0.0, 0.0));
				PltSetVald.SetPlotPaperUnits(PltSet, PlotPaperUnit.Millimeters);
				PltSetVald.SetPlotRotation(PltSet, PltParams.AcPlotRotation);
				PltSetVald.SetPlotType(PltSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
				PltSetVald.SetUseStandardScale(PltSet, true);
				PltSetVald.SetStdScaleType(PltSet, PltParams.AcScaleType);
                PltSetVald.SetPlotCentered(PltSet, true);
				PltSet.ScaleLineweights = PltParams.ScaleLineweight;
				PltSetVald.SetZoomToPaperOnUpdate(PltSet, false);
				PltInfo.OverrideSettings = PltSet;
				PlotInfoValidator PltInfoVald = new PlotInfoValidator();
				PltInfoVald.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
				PltInfoVald.Validate(PltInfo);
				PltPrgDia.OnBeginPlot();
				PltPrgDia.IsVisible = true;
				PltEng.BeginPlot(PltPrgDia, null);
				if (PltParams.PlotToFile) PltEng.BeginDocument(PltInfo, db.Filename, null, PltParams.Amount, true, PltParams.PlotFileLocation);
				else PltEng.BeginDocument(PltInfo, db.Filename, null, PltParams.Amount, false, string.Empty);
				PltEng.BeginPage(PltPgInfo, PltInfo, true, null);
				PltEng.BeginGenerateGraphics(null);
				PltEng.EndGenerateGraphics(null);
				PltEng.EndPage(null);
				PltEng.EndDocument(null);
				PltEng.EndPlot(null);
				PltPrgDia.OnEndPlot();
			}
			catch (Autodesk.AutoCAD.Runtime.Exception AcadEr) {
				MessageBox.Show(AcadEr.Message, "Printing error (AutoCAD).");
			}
			catch (System.Exception ex) {
				MessageBox.Show(ex.Message, "Printing error (System).");
			}
			PltPrgDia.Destroy();
			PltEng.Destroy();
			AcadApp.SetSystemVariable("BackGroundPlot", OldBkGdPlt);
		}
		
		[CommandMethod("MyBPlot", CommandFlags.Session)]
		public void MethodCall () {
			DialogResult DiaRslt;
			using (MyBPlot modalForm = new MyBPlot()) {
				DiaRslt = Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(modalForm);
			}
			if (DiaRslt == DialogResult.OK) {
				Document tempDoc = null;
				DocumentCollection DocCol = AcadApp.DocumentManager;
				foreach (MyPlotParams mpp in PlotObjectsArray) {
					if (mpp != null) {
						try {
							tempDoc = DocCol.Open(mpp.DrawingPath, true);
							Database tempDb = tempDoc.Database;
							LayoutManager tempLoMan = LayoutManager.Current;
							using (DocumentLock DocLock = tempDoc.LockDocument()) {
								if (mpp.TurnOnViewports) TurnOnViewports(tempDoc, tempDb);
								if (mpp.PlotCurrentLayout) {
									if (mpp.ApplyStamp) AddPlotText(tempDb, tempLoMan.GetLayoutId(tempLoMan.CurrentLayout));
									MyPlottingPart(mpp, CheckSpaceSetLtScale(tempLoMan.CurrentLayout, mpp));
								}
								else if (!mpp.LayoutsToPlot.Length.Equals(0)) {
									foreach (string LoName in mpp.LayoutsToPlot) {
										try {
											tempLoMan.CurrentLayout = LoName;
											if (mpp.ApplyStamp) AddPlotText(tempDb, tempLoMan.GetLayoutId(tempLoMan.CurrentLayout));
											MyPlottingPart(mpp, CheckSpaceSetLtScale(LoName, mpp));
										}
										catch {}
									}
								}
							}
						}
						catch (Autodesk.AutoCAD.Runtime.Exception AcadEr) {
							MessageBox.Show(AcadEr.Message, "Drawing error (AutoCAD).");
						}
						catch (System.Exception ex){
							MessageBox.Show(ex.Message, "Drawing error (System).");
						}
						finally {
							if (tempDoc != null) tempDoc.CloseAndDiscard();
						}
					}
				}
			}
			try {
				Array.Clear(ScaleValueArray, 0, ScaleValueArray.Length);
				Array.Clear(PlotObjectsArray, 0, PlotObjectsArray.Length);
			}
			catch {}
		}
		
		void FormResized (object sender, System.EventArgs e)
		{
			ListView.ColumnHeaderCollection HdrCol = DrawingListView.Columns;
			int Wth = DrawingListView.Width;
			if (Wth < 364) HdrCol[1].Width = 60;
			else HdrCol[0].Width = Wth - 64;
		}
		void Lo1CkboxCheckedChanged(object sender, System.EventArgs e)
		{
			if (Lo1Ckbox.Checked == true) {
				SelLoCkbox.Checked = false;
				CurCkbox.Checked = false;
			}
		}
		
		void AllLosCkboxCheckedChanged(object sender, System.EventArgs e)
		{
			if (SelLoCkbox.Checked == true) {
				Lo1Ckbox.Checked = false;
				CurCkbox.Checked = false;
			}
		}
		
		void CurCkboxCheckedChanged(object sender, System.EventArgs e)
		{
			if (CurCkbox.Checked == true) {
				SelLoCkbox.Checked = false;
				Lo1Ckbox.Checked = false;
			}
		}
		
		public void AddPlotText(Database db, ObjectId LoId) {
			using (Transaction Trans = db.TransactionManager.StartTransaction()) {
				Layout Lo = (Layout)Trans.GetObject(LoId, OpenMode.ForRead);
			BlockTableRecord BlkTblRec = (BlockTableRecord)Trans.GetObject(Lo.BlockTableRecordId, OpenMode.ForRead);
				foreach (ObjectId ObjId in BlkTblRec) {
					BlockReference BlkRef = Trans.GetObject(ObjId, OpenMode.ForRead) as BlockReference;
					if (BlkRef != null) {
						BlockTableRecord tempBlkTblRec = (BlockTableRecord)Trans.GetObject(BlkRef.BlockTableRecord, OpenMode.ForRead);
						string BlkName = tempBlkTblRec.Name;
					if (
                            string.Compare(BlkName, "G-TTLB-ZN00-LINE", true) == 0
//						    ||
//						    string.Compare(BlkName, "3M-BORDER-B", true) == 0
//						    ||
//						    string.Compare(BlkName, "3M-BORDER-C", true) == 0
//						    ||
//						    string.Compare(BlkName, "3M-BORDER-D", true) == 0
//						    ||
//						    string.Compare(BlkName, "3M-BORDER-E", true) == 0
//                            ||
//						    string.Compare(BlkName, "3M-BORDER-E1", true) == 0
						   ) {
							Autodesk.AutoCAD.DatabaseServices.AttributeCollection AttCol = BlkRef.AttributeCollection;
							AttributeReference AttRef = (AttributeReference)Trans.GetObject(AttCol[64], OpenMode.ForWrite);
							AttRef.TextString = PlotDate + " - IRR -" + db.Filename;
							Trans.Commit();
							return;
						}
					}
				}
				DBText TextObj = new DBText();
				TextObj.TextString = PlotDate + " - IRR -" + db.Filename;
				TextObj.HorizontalMode = TextHorizontalMode.TextRight;
			    TextObj.VerticalMode = TextVerticalMode.TextTop;
				Point3d MinPt = (Point3d)AcadApp.GetSystemVariable("ExtMin");
				Point3d MaxPt = (Point3d)AcadApp.GetSystemVariable("ExtMax");
				TextObj.AlignmentPoint = new Point3d(MaxPt.X, MinPt.Y, MinPt.X);
				double TxtHt = (2.5) * (double)AcadApp.GetSystemVariable("DimScale");
				if (TxtHt.Equals(0.0)) TxtHt = 2.5;
				TextObj.Height = TxtHt;
				BlkTblRec.UpgradeOpen();
				BlkTblRec.AppendEntity(TextObj);
				Trans.AddNewlyCreatedDBObject(TextObj, true);
				Trans.Commit();
				return;
			}
		}
		
		void CpCurSettingsBtnClick(object sender, System.EventArgs e)
		{
			Database db = HostApplicationServices.WorkingDatabase;
			using (Transaction Trans = db.TransactionManager.StartTransaction()) {
				try {
					string CurLoName = LayoutManager.Current.CurrentLayout;
					DBDictionary LoDict = (DBDictionary)Trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
					ObjectId LoId = (ObjectId)LoDict[CurLoName];
					Layout Lo = (Layout)Trans.GetObject(LoId, OpenMode.ForRead);
					if (PlotterComboBox.Items.Contains(Lo.PlotConfigurationName)) {
						PlotterComboBox.Text = Lo.PlotConfigurationName;
						PlotConfig pc = PCM.SetCurrentConfig(Lo.PlotConfigurationName);
						string LocalPaperName = pc.GetLocalMediaName(Lo.CanonicalMediaName);
						if (PaperComboBox.Items.Contains(LocalPaperName)) PaperComboBox.Text = LocalPaperName;
					}
					if (ctbFileComboBox.Items.Contains(Lo.PlotSettingsName)) ctbFileComboBox.Text = Lo.PlotSettingsName;
					string StdScStr = FormatStandardScale(Lo.StdScaleType.ToString());
					if (ScaleComboBox.Items.Contains(StdScStr)) ScaleComboBox.Text = StdScStr;
					if (Lo.ScaleLineweights) ScaleLwChkBox.Checked = true;
					else ScaleLwChkBox.Checked = false;
					if (Lo.PlotRotation == PlotRotation.Degrees090) LandscapeChkBox.Checked = true;
					else LandscapeChkBox.Checked = false;
				}
				catch (System.Exception ex) { MessageBox.Show(ex.Message); }
			}
		}
		
		void PlotDirBtnClick(object sender, System.EventArgs e)
		{
			Autodesk.AutoCAD.Windows.OpenFileDialog Dia =
				new Autodesk.AutoCAD.Windows.OpenFileDialog("Select directory to place .plt files:",
				                                            "",
				                                            "dwg",
				                                            "",
				                                            Autodesk.AutoCAD.Windows.OpenFileDialog.OpenFileDialogFlags.AllowFoldersOnly
				                                           );
			if (Dia.ShowDialog() == DialogResult.OK) PlotDirBtn.Tag = Dia.Filename;
		}
		
		private void TurnOnViewports(Document Doc, Database db) {
			Editor ed = Doc.Editor;
			TypedValue[] FltrList = { new TypedValue((int)DxfCode.Start, "Viewport") };
			PromptSelectionResult psr = ed.SelectAll(new SelectionFilter(FltrList));
			if (psr.Value.Count.Equals(0)) return;
			SelectionSet ss = psr.Value as SelectionSet;
			using (Transaction Trans = db.TransactionManager.StartTransaction()) {
				foreach (ObjectId ObjId in ss.GetObjectIds()) {
					Viewport vp = (Viewport)Trans.GetObject(ObjId, OpenMode.ForWrite);
					vp.On = true;
					vp.UpdateDisplay();
				}
				Trans.Commit();
			}
		}
		
		void Plot2FileChkBoxCheckedChanged(object sender, System.EventArgs e)
		{
			if (Plot2FileChkBox.Checked) PlotDirBtn.Enabled = true;
			else PlotDirBtn.Enabled = false;
		}
		
		void LtScChkBoxCheckedChanged(object sender, System.EventArgs e)
		{
			if (LtScChkBox.Checked) LtScGrp.Enabled = true;
			else LtScGrp.Enabled = false;
		}
		
		void LoListBtnClick(object sender, System.EventArgs e)
		{
			LoTreeview.Nodes.Clear();
			string[] DrawingNames = new string[DrawingListView.SelectedItems.Count];
			int cnt = 0;
			foreach (ListViewItem lvi in DrawingListView.SelectedItems) {
				try {
					using (Database db = new Database(false, true)) {
						db.ReadDwgFile (lvi.Text, System.IO.FileShare.Read, true, null);
						using (Transaction Trans = db.TransactionManager.StartTransaction()) {
							DBDictionary LoDict = (DBDictionary)Trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
							TreeNode MainNode = new TreeNode(lvi.Text);
							foreach (DictionaryEntry de in LoDict) {
								MainNode.Nodes.Add(de.Key as string);
							}
							LoTreeview.Nodes.Add(MainNode);
						}
					}
					DrawingNames[cnt] = lvi.Text;
					++cnt;
				}
				catch (System.Exception ex) { MessageBox.Show(ex.Message); }
			}
			LoTreeview.Tag = DrawingNames;
		}
		
		void LoTreeviewPopUp(object sender, System.EventArgs e) {
			System.Windows.Forms.Menu.MenuItemCollection mic = LoTreeviewCtMenu.MenuItems;
			mic.Clear();
			System.Windows.Forms.MenuItem mi1 = new System.Windows.Forms.MenuItem("&Select all layouts (non-Model)");
			mi1.Click += new EventHandler(SelectAllLayouts);
			mic.Add(mi1);
			mi1.Enabled = new EventHandler(SelectAllLayouts) != null;
			System.Windows.Forms.MenuItem mi1a = new System.Windows.Forms.MenuItem("&Select all layouts (include Model)");
			mi1a.Click += new EventHandler(SelectAllLayouts);
			mic.Add(mi1a);
			mi1a.Enabled = new EventHandler(SelectAllLayouts) != null;
			mic.Add("----------------------");
			System.Windows.Forms.MenuItem mi2 = new System.Windows.Forms.MenuItem("&Expand all");
			mi2.Click += new EventHandler(AllExpand);
			mic.Add(mi2);
			mi2.Enabled = new EventHandler(AllExpand) != null;
			System.Windows.Forms.MenuItem mi3 = new System.Windows.Forms.MenuItem("&Collapse all");
			mi3.Click += new EventHandler(AllCollapse);
			mic.Add(mi3);
			mi3.Enabled = new EventHandler(AllCollapse) != null;
		}
		
		void SelectAllLayouts(object sender, System.EventArgs e) {
			string tempStr = sender.ToString();
			string[] tempStrAr = tempStr.Split(' ');
			tempStr = tempStrAr[tempStrAr.Length - 1];
			if (string.Compare(tempStr, "(non-Model)").Equals(0)) {
				foreach (TreeNode MainNode in LoTreeview.Nodes) {
					foreach (TreeNode SubNode in MainNode.Nodes) {
						if (!string.Compare(SubNode.Text, "Model", true).Equals(0)) SubNode.Checked = true;
						else SubNode.Checked = false;
					}
				}
			}
			else {
				foreach (TreeNode MainNode in LoTreeview.Nodes) {
					foreach (TreeNode SubNode in MainNode.Nodes) {
						SubNode.Checked = true;
					}
				}
			}
		}
		
		void AllExpand (object sender, System.EventArgs e) {
			LoTreeview.ExpandAll();
		}
		
		void AllCollapse (object sender, System.EventArgs e) {
			LoTreeview.CollapseAll();
		}
		
		private bool CheckSpaceSetLtScale (string LoName, MyPlotParams mpp) {
			if (string.Compare("Model", LoName) == 0) {
				if (mpp.ChangeLinetypeScale) {
					AcadApp.SetSystemVariable("PsLtScale", 1);
					AcadApp.SetSystemVariable("LtScale", mpp.LinetypeScaleModel);
				}
				return true;
			}
			else {
				if (mpp.ChangeLinetypeScale) {
					AcadApp.SetSystemVariable("LtScale", mpp.LinetypeScalePaper);
				}
				return false;
			}
		}
		
	}
}
