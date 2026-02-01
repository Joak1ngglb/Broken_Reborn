using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DarkUI.Controls;

namespace Intersect.Editor.Forms.Editors;

partial class ItemTranslationWindow
{
    private IContainer components = null;
    private SplitContainer splitContainerMain;
    private DarkGroupBox grpItems;
    private TableLayoutPanel layoutLeft;
    private TableLayoutPanel layoutFilters;
    private Label lblSearch;
    private DarkTextBox txtSearch;
    private Label lblFolder;
    private DarkComboBox cmbFolder;
    private ListBox lstItems;
    private TableLayoutPanel layoutRight;
    private DarkGroupBox grpName;
    private TableLayoutPanel layoutName;
    private Panel panelNameHeader;
    private Label lblNameSource;
    private DarkButton btnCopyNameSource;
    private DarkTextBox txtNameSource;
    private TabControl tabNameTranslations;
    private DarkGroupBox grpDescription;
    private TableLayoutPanel layoutDescription;
    private Panel panelDescriptionHeader;
    private Label lblDescriptionSource;
    private DarkButton btnCopyDescriptionSource;
    private DarkTextBox txtDescriptionSource;
    private TabControl tabDescriptionTranslations;
    private FlowLayoutPanel panelFooter;
    private DarkButton btnSave;
    private DarkButton btnSaveNext;
    private DarkButton btnNext;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        splitContainerMain = new SplitContainer();
        layoutLeft = new TableLayoutPanel();
        grpItems = new DarkGroupBox();
        layoutFilters = new TableLayoutPanel();
        lblSearch = new Label();
        txtSearch = new DarkTextBox();
        lblFolder = new Label();
        cmbFolder = new DarkComboBox();
        lstItems = new ListBox();
        layoutRight = new TableLayoutPanel();
        grpName = new DarkGroupBox();
        layoutName = new TableLayoutPanel();
        panelNameHeader = new Panel();
        lblNameSource = new Label();
        btnCopyNameSource = new DarkButton();
        txtNameSource = new DarkTextBox();
        tabNameTranslations = new TabControl();
        grpDescription = new DarkGroupBox();
        layoutDescription = new TableLayoutPanel();
        panelDescriptionHeader = new Panel();
        lblDescriptionSource = new Label();
        btnCopyDescriptionSource = new DarkButton();
        txtDescriptionSource = new DarkTextBox();
        tabDescriptionTranslations = new TabControl();
        panelFooter = new FlowLayoutPanel();
        btnSave = new DarkButton();
        btnSaveNext = new DarkButton();
        btnNext = new DarkButton();
        ((ISupportInitialize)splitContainerMain).BeginInit();
        splitContainerMain.Panel1.SuspendLayout();
        splitContainerMain.Panel2.SuspendLayout();
        splitContainerMain.SuspendLayout();
        layoutLeft.SuspendLayout();
        grpItems.SuspendLayout();
        layoutFilters.SuspendLayout();
        layoutRight.SuspendLayout();
        grpName.SuspendLayout();
        layoutName.SuspendLayout();
        panelNameHeader.SuspendLayout();
        grpDescription.SuspendLayout();
        layoutDescription.SuspendLayout();
        panelDescriptionHeader.SuspendLayout();
        panelFooter.SuspendLayout();
        SuspendLayout();
        //
        // splitContainerMain
        //
        splitContainerMain.Dock = DockStyle.Fill;
        splitContainerMain.Location = new Point(0, 0);
        splitContainerMain.Name = "splitContainerMain";
        splitContainerMain.Panel1.Controls.Add(layoutLeft);
        splitContainerMain.Panel2.Controls.Add(layoutRight);
        splitContainerMain.Size = new Size(1280, 720);
        splitContainerMain.SplitterDistance = 320;
        splitContainerMain.TabIndex = 0;
        //
        // layoutLeft
        //
        layoutLeft.ColumnCount = 1;
        layoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutLeft.Controls.Add(grpItems, 0, 0);
        layoutLeft.Dock = DockStyle.Fill;
        layoutLeft.Location = new Point(0, 0);
        layoutLeft.Name = "layoutLeft";
        layoutLeft.Padding = new Padding(12);
        layoutLeft.RowCount = 1;
        layoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutLeft.Size = new Size(320, 720);
        layoutLeft.TabIndex = 0;
        //
        // grpItems
        //
        grpItems.BackColor = Color.FromArgb(45, 45, 48);
        grpItems.BorderColor = Color.FromArgb(90, 90, 90);
        grpItems.Controls.Add(layoutFilters);
        grpItems.Controls.Add(lstItems);
        grpItems.Dock = DockStyle.Fill;
        grpItems.ForeColor = Color.Gainsboro;
        grpItems.Location = new Point(15, 15);
        grpItems.Name = "grpItems";
        grpItems.Padding = new Padding(10);
        grpItems.Size = new Size(290, 690);
        grpItems.TabIndex = 0;
        grpItems.TabStop = false;
        grpItems.Text = "Items";
        //
        // layoutFilters
        //
        layoutFilters.ColumnCount = 2;
        layoutFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        layoutFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutFilters.Controls.Add(lblSearch, 0, 0);
        layoutFilters.Controls.Add(txtSearch, 1, 0);
        layoutFilters.Controls.Add(lblFolder, 0, 1);
        layoutFilters.Controls.Add(cmbFolder, 1, 1);
        layoutFilters.Dock = DockStyle.Top;
        layoutFilters.Location = new Point(10, 26);
        layoutFilters.Name = "layoutFilters";
        layoutFilters.RowCount = 2;
        layoutFilters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layoutFilters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layoutFilters.Size = new Size(270, 60);
        layoutFilters.TabIndex = 0;
        //
        // lblSearch
        //
        lblSearch.AutoSize = true;
        lblSearch.ForeColor = Color.Gainsboro;
        lblSearch.Location = new Point(3, 6);
        lblSearch.Margin = new Padding(3, 6, 3, 3);
        lblSearch.Name = "lblSearch";
        lblSearch.Size = new Size(45, 15);
        lblSearch.TabIndex = 0;
        lblSearch.Text = "Search";
        //
        // txtSearch
        //
        txtSearch.BackColor = Color.FromArgb(69, 73, 74);
        txtSearch.BorderStyle = BorderStyle.FixedSingle;
        txtSearch.ForeColor = Color.FromArgb(220, 220, 220);
        txtSearch.Location = new Point(73, 3);
        txtSearch.Name = "txtSearch";
        txtSearch.Size = new Size(194, 23);
        txtSearch.TabIndex = 1;
        //
        // lblFolder
        //
        lblFolder.AutoSize = true;
        lblFolder.ForeColor = Color.Gainsboro;
        lblFolder.Location = new Point(3, 35);
        lblFolder.Margin = new Padding(3, 6, 3, 3);
        lblFolder.Name = "lblFolder";
        lblFolder.Size = new Size(42, 15);
        lblFolder.TabIndex = 2;
        lblFolder.Text = "Folder";
        //
        // cmbFolder
        //
        cmbFolder.BackColor = Color.FromArgb(69, 73, 74);
        cmbFolder.DrawMode = DrawMode.OwnerDrawFixed;
        cmbFolder.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFolder.ForeColor = Color.Gainsboro;
        cmbFolder.FormattingEnabled = true;
        cmbFolder.Location = new Point(73, 32);
        cmbFolder.Name = "cmbFolder";
        cmbFolder.Size = new Size(194, 24);
        cmbFolder.TabIndex = 3;
        //
        // lstItems
        //
        lstItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lstItems.BackColor = Color.FromArgb(69, 73, 74);
        lstItems.BorderStyle = BorderStyle.FixedSingle;
        lstItems.ForeColor = Color.Gainsboro;
        lstItems.FormattingEnabled = true;
        lstItems.ItemHeight = 15;
        lstItems.Location = new Point(10, 92);
        lstItems.Name = "lstItems";
        lstItems.Size = new Size(270, 572);
        lstItems.TabIndex = 1;
        //
        // layoutRight
        //
        layoutRight.ColumnCount = 1;
        layoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutRight.Controls.Add(grpName, 0, 0);
        layoutRight.Controls.Add(grpDescription, 0, 1);
        layoutRight.Controls.Add(panelFooter, 0, 2);
        layoutRight.Dock = DockStyle.Fill;
        layoutRight.Location = new Point(0, 0);
        layoutRight.Name = "layoutRight";
        layoutRight.Padding = new Padding(12);
        layoutRight.RowCount = 3;
        layoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layoutRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layoutRight.Size = new Size(956, 720);
        layoutRight.TabIndex = 0;
        //
        // grpName
        //
        grpName.BackColor = Color.FromArgb(45, 45, 48);
        grpName.BorderColor = Color.FromArgb(90, 90, 90);
        grpName.Controls.Add(layoutName);
        grpName.Dock = DockStyle.Fill;
        grpName.ForeColor = Color.Gainsboro;
        grpName.Location = new Point(15, 15);
        grpName.Name = "grpName";
        grpName.Padding = new Padding(10);
        grpName.Size = new Size(926, 329);
        grpName.TabIndex = 0;
        grpName.TabStop = false;
        grpName.Text = "Name";
        //
        // layoutName
        //
        layoutName.ColumnCount = 1;
        layoutName.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutName.Controls.Add(panelNameHeader, 0, 0);
        layoutName.Controls.Add(txtNameSource, 0, 1);
        layoutName.Controls.Add(tabNameTranslations, 0, 2);
        layoutName.Dock = DockStyle.Fill;
        layoutName.Location = new Point(10, 26);
        layoutName.Name = "layoutName";
        layoutName.RowCount = 3;
        layoutName.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layoutName.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        layoutName.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutName.Size = new Size(906, 293);
        layoutName.TabIndex = 0;
        //
        // panelNameHeader
        //
        panelNameHeader.Controls.Add(lblNameSource);
        panelNameHeader.Controls.Add(btnCopyNameSource);
        panelNameHeader.Dock = DockStyle.Fill;
        panelNameHeader.Location = new Point(3, 3);
        panelNameHeader.Name = "panelNameHeader";
        panelNameHeader.Size = new Size(900, 24);
        panelNameHeader.TabIndex = 0;
        //
        // lblNameSource
        //
        lblNameSource.AutoSize = true;
        lblNameSource.ForeColor = Color.Gainsboro;
        lblNameSource.Location = new Point(0, 4);
        lblNameSource.Name = "lblNameSource";
        lblNameSource.Size = new Size(72, 15);
        lblNameSource.TabIndex = 0;
        lblNameSource.Text = "Source (EN)";
        //
        // btnCopyNameSource
        //
        btnCopyNameSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCopyNameSource.Location = new Point(780, 0);
        btnCopyNameSource.Name = "btnCopyNameSource";
        btnCopyNameSource.Padding = new Padding(6);
        btnCopyNameSource.Size = new Size(120, 24);
        btnCopyNameSource.TabIndex = 1;
        btnCopyNameSource.Text = "Copy Source";
        btnCopyNameSource.Click += btnCopyNameSource_Click;
        //
        // txtNameSource
        //
        txtNameSource.BackColor = Color.FromArgb(69, 73, 74);
        txtNameSource.BorderStyle = BorderStyle.FixedSingle;
        txtNameSource.ForeColor = Color.FromArgb(220, 220, 220);
        txtNameSource.Location = new Point(3, 33);
        txtNameSource.Multiline = true;
        txtNameSource.Name = "txtNameSource";
        txtNameSource.ReadOnly = true;
        txtNameSource.ScrollBars = ScrollBars.Vertical;
        txtNameSource.Size = new Size(900, 64);
        txtNameSource.TabIndex = 2;
        //
        // tabNameTranslations
        //
        tabNameTranslations.Dock = DockStyle.Fill;
        tabNameTranslations.Location = new Point(3, 103);
        tabNameTranslations.Name = "tabNameTranslations";
        tabNameTranslations.SelectedIndex = 0;
        tabNameTranslations.Size = new Size(900, 187);
        tabNameTranslations.TabIndex = 3;
        //
        // grpDescription
        //
        grpDescription.BackColor = Color.FromArgb(45, 45, 48);
        grpDescription.BorderColor = Color.FromArgb(90, 90, 90);
        grpDescription.Controls.Add(layoutDescription);
        grpDescription.Dock = DockStyle.Fill;
        grpDescription.ForeColor = Color.Gainsboro;
        grpDescription.Location = new Point(15, 350);
        grpDescription.Name = "grpDescription";
        grpDescription.Padding = new Padding(10);
        grpDescription.Size = new Size(926, 329);
        grpDescription.TabIndex = 1;
        grpDescription.TabStop = false;
        grpDescription.Text = "Description";
        //
        // layoutDescription
        //
        layoutDescription.ColumnCount = 1;
        layoutDescription.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutDescription.Controls.Add(panelDescriptionHeader, 0, 0);
        layoutDescription.Controls.Add(txtDescriptionSource, 0, 1);
        layoutDescription.Controls.Add(tabDescriptionTranslations, 0, 2);
        layoutDescription.Dock = DockStyle.Fill;
        layoutDescription.Location = new Point(10, 26);
        layoutDescription.Name = "layoutDescription";
        layoutDescription.RowCount = 3;
        layoutDescription.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layoutDescription.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        layoutDescription.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutDescription.Size = new Size(906, 293);
        layoutDescription.TabIndex = 0;
        //
        // panelDescriptionHeader
        //
        panelDescriptionHeader.Controls.Add(lblDescriptionSource);
        panelDescriptionHeader.Controls.Add(btnCopyDescriptionSource);
        panelDescriptionHeader.Dock = DockStyle.Fill;
        panelDescriptionHeader.Location = new Point(3, 3);
        panelDescriptionHeader.Name = "panelDescriptionHeader";
        panelDescriptionHeader.Size = new Size(900, 24);
        panelDescriptionHeader.TabIndex = 0;
        //
        // lblDescriptionSource
        //
        lblDescriptionSource.AutoSize = true;
        lblDescriptionSource.ForeColor = Color.Gainsboro;
        lblDescriptionSource.Location = new Point(0, 4);
        lblDescriptionSource.Name = "lblDescriptionSource";
        lblDescriptionSource.Size = new Size(72, 15);
        lblDescriptionSource.TabIndex = 0;
        lblDescriptionSource.Text = "Source (EN)";
        //
        // btnCopyDescriptionSource
        //
        btnCopyDescriptionSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCopyDescriptionSource.Location = new Point(780, 0);
        btnCopyDescriptionSource.Name = "btnCopyDescriptionSource";
        btnCopyDescriptionSource.Padding = new Padding(6);
        btnCopyDescriptionSource.Size = new Size(120, 24);
        btnCopyDescriptionSource.TabIndex = 1;
        btnCopyDescriptionSource.Text = "Copy Source";
        btnCopyDescriptionSource.Click += btnCopyDescriptionSource_Click;
        //
        // txtDescriptionSource
        //
        txtDescriptionSource.BackColor = Color.FromArgb(69, 73, 74);
        txtDescriptionSource.BorderStyle = BorderStyle.FixedSingle;
        txtDescriptionSource.ForeColor = Color.FromArgb(220, 220, 220);
        txtDescriptionSource.Location = new Point(3, 33);
        txtDescriptionSource.Multiline = true;
        txtDescriptionSource.Name = "txtDescriptionSource";
        txtDescriptionSource.ReadOnly = true;
        txtDescriptionSource.ScrollBars = ScrollBars.Vertical;
        txtDescriptionSource.Size = new Size(900, 64);
        txtDescriptionSource.TabIndex = 2;
        //
        // tabDescriptionTranslations
        //
        tabDescriptionTranslations.Dock = DockStyle.Fill;
        tabDescriptionTranslations.Location = new Point(3, 103);
        tabDescriptionTranslations.Name = "tabDescriptionTranslations";
        tabDescriptionTranslations.SelectedIndex = 0;
        tabDescriptionTranslations.Size = new Size(900, 187);
        tabDescriptionTranslations.TabIndex = 3;
        //
        // panelFooter
        //
        panelFooter.AutoSize = true;
        panelFooter.Controls.Add(btnSave);
        panelFooter.Controls.Add(btnSaveNext);
        panelFooter.Controls.Add(btnNext);
        panelFooter.Dock = DockStyle.Fill;
        panelFooter.FlowDirection = FlowDirection.RightToLeft;
        panelFooter.Location = new Point(15, 685);
        panelFooter.Name = "panelFooter";
        panelFooter.Padding = new Padding(0, 8, 0, 0);
        panelFooter.Size = new Size(926, 20);
        panelFooter.TabIndex = 2;
        //
        // btnSave
        //
        btnSave.Location = new Point(816, 11);
        btnSave.Name = "btnSave";
        btnSave.Padding = new Padding(6);
        btnSave.Size = new Size(107, 27);
        btnSave.TabIndex = 2;
        btnSave.Text = "Save";
        //
        // btnSaveNext
        //
        btnSaveNext.Location = new Point(683, 11);
        btnSaveNext.Name = "btnSaveNext";
        btnSaveNext.Padding = new Padding(6);
        btnSaveNext.Size = new Size(127, 27);
        btnSaveNext.TabIndex = 1;
        btnSaveNext.Text = "Save + Next";
        //
        // btnNext
        //
        btnNext.Location = new Point(525, 11);
        btnNext.Name = "btnNext";
        btnNext.Padding = new Padding(6);
        btnNext.Size = new Size(152, 27);
        btnNext.TabIndex = 0;
        btnNext.Text = "Next (sin guardar)";
        //
        // ItemTranslationWindow
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 720);
        Controls.Add(splitContainerMain);
        MinimumSize = new Size(1100, 680);
        Name = "ItemTranslationWindow";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Item Translation";
        splitContainerMain.Panel1.ResumeLayout(false);
        splitContainerMain.Panel2.ResumeLayout(false);
        ((ISupportInitialize)splitContainerMain).EndInit();
        splitContainerMain.ResumeLayout(false);
        layoutLeft.ResumeLayout(false);
        grpItems.ResumeLayout(false);
        layoutFilters.ResumeLayout(false);
        layoutFilters.PerformLayout();
        layoutRight.ResumeLayout(false);
        layoutRight.PerformLayout();
        grpName.ResumeLayout(false);
        layoutName.ResumeLayout(false);
        layoutName.PerformLayout();
        panelNameHeader.ResumeLayout(false);
        panelNameHeader.PerformLayout();
        grpDescription.ResumeLayout(false);
        layoutDescription.ResumeLayout(false);
        layoutDescription.PerformLayout();
        panelDescriptionHeader.ResumeLayout(false);
        panelDescriptionHeader.PerformLayout();
        panelFooter.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
}
