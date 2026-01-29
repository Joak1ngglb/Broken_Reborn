using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DarkUI.Controls;

namespace Intersect.Editor.Forms.Controls
{
    partial class TranslationQueueControl
    {
        private IContainer components = null;
        private SplitContainer splitContainerMain;
        private TableLayoutPanel layoutLeft;
        private TableLayoutPanel layoutFilters;
        private Label lblQueue;
        private Label lblEntityType;
        private Label lblStatus;
        private Label lblSearch;
        private Label lblScope;
        private DarkComboBox cmbEntityType;
        private DarkComboBox cmbStatus;
        private DarkComboBox cmbScope;
        private DarkTextBox txtSearch;
        private FlowLayoutPanel panelFilterButtons;
        private DarkButton btnBroken;
        private DarkButton btnImport;
        private DarkButton btnExport;
        private ListBox lstEntries;
        private FlowLayoutPanel panelNavButtons;
        private DarkButton btnPrev;
        private DarkButton btnNext;
        private TableLayoutPanel layoutRight;
        private Label lblSource;
        private DarkTextBox txtSource;
        private TabControl tabTranslations;
        private TableLayoutPanel layoutBottom;
        private FlowLayoutPanel panelStatus;
        private Label lblStatusLabel;
        private Label lblStatusValue;
        private FlowLayoutPanel panelTranslationButtons;
        private DarkButton btnCopySource;
        private DarkButton btnQuickPaste;
        private DarkButton btnSave;
        private DarkButton btnSaveNext;

        private void InitializeComponent()
        {
            splitContainerMain = new SplitContainer();
            layoutLeft = new TableLayoutPanel();
            lblQueue = new Label();
            layoutFilters = new TableLayoutPanel();
            lblEntityType = new Label();
            lblStatus = new Label();
            lblSearch = new Label();
            lblScope = new Label();
            cmbEntityType = new DarkComboBox();
            cmbStatus = new DarkComboBox();
            cmbScope = new DarkComboBox();
            txtSearch = new DarkTextBox();
            panelFilterButtons = new FlowLayoutPanel();
            btnBroken = new DarkButton();
            btnImport = new DarkButton();
            btnExport = new DarkButton();
            lstEntries = new ListBox();
            panelNavButtons = new FlowLayoutPanel();
            btnPrev = new DarkButton();
            btnNext = new DarkButton();
            layoutRight = new TableLayoutPanel();
            lblSource = new Label();
            txtSource = new DarkTextBox();
            tabTranslations = new TabControl();
            layoutBottom = new TableLayoutPanel();
            panelStatus = new FlowLayoutPanel();
            lblStatusLabel = new Label();
            lblStatusValue = new Label();
            panelTranslationButtons = new FlowLayoutPanel();
            btnCopySource = new DarkButton();
            btnQuickPaste = new DarkButton();
            btnSave = new DarkButton();
            btnSaveNext = new DarkButton();
            ((ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            layoutLeft.SuspendLayout();
            layoutFilters.SuspendLayout();
            panelFilterButtons.SuspendLayout();
            panelNavButtons.SuspendLayout();
            layoutRight.SuspendLayout();
            layoutBottom.SuspendLayout();
            panelTranslationButtons.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new System.Drawing.Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            splitContainerMain.Panel1.Controls.Add(layoutLeft);
            splitContainerMain.Panel2.Controls.Add(layoutRight);
            splitContainerMain.Size = new Size(1200, 720);
            splitContainerMain.SplitterDistance = 320;
            splitContainerMain.TabIndex = 0;
            // 
            // layoutLeft
            // 
            layoutLeft.ColumnCount = 1;
            layoutLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutLeft.Controls.Add(lblQueue, 0, 0);
            layoutLeft.Controls.Add(layoutFilters, 0, 1);
            layoutLeft.Controls.Add(lstEntries, 0, 2);
            layoutLeft.Controls.Add(panelNavButtons, 0, 3);
            layoutLeft.Dock = DockStyle.Fill;
            layoutLeft.Padding = new Padding(12);
            layoutLeft.RowCount = 4;
            layoutLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutLeft.TabIndex = 0;
            // 
            // lblQueue
            // 
            lblQueue.AutoSize = true;
            lblQueue.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblQueue.ForeColor = System.Drawing.    Color.Gainsboro;
            lblQueue.Margin = new Padding(0, 0, 0, 8);
            lblQueue.Text = "Queue";
            // 
            // layoutFilters
            // 
            layoutFilters.AutoSize = true;
            layoutFilters.ColumnCount = 2;
            layoutFilters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layoutFilters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutFilters.Controls.Add(lblEntityType, 0, 0);
            layoutFilters.Controls.Add(cmbEntityType, 1, 0);
            layoutFilters.Controls.Add(lblStatus, 0, 1);
            layoutFilters.Controls.Add(cmbStatus, 1, 1);
            layoutFilters.Controls.Add(lblSearch, 0, 2);
            layoutFilters.Controls.Add(txtSearch, 1, 2);
            layoutFilters.Controls.Add(lblScope, 0, 3);
            layoutFilters.Controls.Add(cmbScope, 1, 3);
            layoutFilters.Controls.Add(panelFilterButtons, 0, 4);
            layoutFilters.Dock = DockStyle.Top;
            layoutFilters.Margin = new Padding(0, 0, 0, 12);
            layoutFilters.RowCount = 5;
            layoutFilters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutFilters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutFilters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutFilters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutFilters.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutFilters.TabIndex = 1;
            // 
            // lblEntityType
            // 
            lblEntityType.AutoSize = true;
            lblEntityType.ForeColor = System.Drawing.Color.Gainsboro;
            lblEntityType.Margin = new Padding(0, 6, 8, 6);
            lblEntityType.Text = "Entity Type";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = System.Drawing.Color.Gainsboro;
            lblStatus.Margin = new Padding(0, 6, 8, 6);
            lblStatus.Text = "Status";
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.ForeColor = System.Drawing.Color.Gainsboro;
            lblSearch.Margin = new Padding(0, 6, 8, 6);
            lblSearch.Text = "Search";
            // 
            // lblScope
            // 
            lblScope.AutoSize = true;
            lblScope.ForeColor = System.Drawing.Color.Gainsboro;
            lblScope.Margin = new Padding(0, 6, 8, 6);
            lblScope.Text = "Scope";
            // 
            // cmbEntityType
            // 
            cmbEntityType.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbEntityType.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbEntityType.BorderStyle = ButtonBorderStyle.Solid;
            cmbEntityType.DrawMode = DrawMode.OwnerDrawFixed;
            cmbEntityType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEntityType.FlatStyle = FlatStyle.Flat;
            cmbEntityType.ForeColor = System.Drawing.Color.Gainsboro;
            cmbEntityType.Margin = new Padding(0, 0, 0, 6);
            cmbEntityType.Dock = DockStyle.Fill;
            // 
            // cmbStatus
            // 
            cmbStatus.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbStatus.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbStatus.BorderStyle = ButtonBorderStyle.Solid;
            cmbStatus.DrawMode = DrawMode.OwnerDrawFixed;
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.FlatStyle = FlatStyle.Flat;
            cmbStatus.ForeColor = System.Drawing.Color.Gainsboro;
            cmbStatus.Margin = new Padding(0, 0, 0, 6);
            cmbStatus.Dock = DockStyle.Fill;
            // 
            // cmbScope
            // 
            cmbScope.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbScope.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbScope.BorderStyle = ButtonBorderStyle.Solid;
            cmbScope.DrawMode = DrawMode.OwnerDrawFixed;
            cmbScope.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbScope.FlatStyle = FlatStyle.Flat;
            cmbScope.ForeColor = System.Drawing.Color.Gainsboro;
            cmbScope.Margin = new Padding(0, 0, 0, 6);
            cmbScope.Dock = DockStyle.Fill;
            // 
            // txtSearch
            // 
            txtSearch.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.ForeColor = System.Drawing.Color.Gainsboro;
            txtSearch.Margin = new Padding(0, 0, 0, 6);
            txtSearch.Dock = DockStyle.Fill;
            // 
            // panelFilterButtons
            // 
            panelFilterButtons.AutoSize = true;
            panelFilterButtons.Dock = DockStyle.Fill;
            panelFilterButtons.FlowDirection = FlowDirection.RightToLeft;
            panelFilterButtons.Controls.Add(btnExport);
            panelFilterButtons.Controls.Add(btnImport);
            panelFilterButtons.Controls.Add(btnBroken);
            panelFilterButtons.Margin = new Padding(0, 6, 0, 0);
            panelFilterButtons.WrapContents = false;
            layoutFilters.SetColumnSpan(panelFilterButtons, 2);
            // 
            // btnBroken
            // 
            btnBroken.Padding = new Padding(6);
            btnBroken.Size = new Size(76, 28);
            btnBroken.Text = "Broken";
            // 
            // btnImport
            // 
            btnImport.Padding = new Padding(6);
            btnImport.Size = new Size(94, 28);
            btnImport.Text = "Import CSV";
            // 
            // btnExport
            // 
            btnExport.Padding = new Padding(6);
            btnExport.Size = new Size(94, 28);
            btnExport.Text = "Export CSV";
            // 
            // lstEntries
            // 
            lstEntries.BackColor = System.Drawing.Color.FromArgb(60, 63, 65);
            lstEntries.BorderStyle = BorderStyle.FixedSingle;
            lstEntries.Dock = DockStyle.Fill;
            lstEntries.ForeColor = System.Drawing.Color.Gainsboro;
            lstEntries.IntegralHeight = false;
            lstEntries.ItemHeight = 52;
            lstEntries.Margin = new Padding(0, 0, 0, 12);
            lstEntries.DrawMode = DrawMode.OwnerDrawFixed;
            // 
            // panelNavButtons
            // 
            panelNavButtons.AutoSize = true;
            panelNavButtons.Dock = DockStyle.Fill;
            panelNavButtons.FlowDirection = FlowDirection.RightToLeft;
            panelNavButtons.Controls.Add(btnNext);
            panelNavButtons.Controls.Add(btnPrev);
            panelNavButtons.WrapContents = false;
            // 
            // btnPrev
            // 
            btnPrev.Padding = new Padding(6);
            btnPrev.Size = new Size(64, 28);
            btnPrev.Text = "Prev";
            // 
            // btnNext
            // 
            btnNext.Padding = new Padding(6);
            btnNext.Size = new Size(64, 28);
            btnNext.Text = "Next";
            // 
            // layoutRight
            // 
            layoutRight.ColumnCount = 1;
            layoutRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutRight.Controls.Add(lblSource, 0, 0);
            layoutRight.Controls.Add(txtSource, 0, 1);
            layoutRight.Controls.Add(tabTranslations, 0, 2);
            layoutRight.Controls.Add(layoutBottom, 0, 3);
            layoutRight.Dock = DockStyle.Fill;
            layoutRight.Padding = new Padding(12);
            layoutRight.RowCount = 4;
            layoutRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            layoutRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutRight.TabIndex = 1;
            // 
            // lblSource
            // 
            lblSource.AutoSize = true;
            lblSource.ForeColor = System.Drawing.Color.Gainsboro;
            lblSource.Margin = new Padding(0, 0, 0, 6);
            lblSource.Text = "Source";
            // 
            // txtSource
            // 
            txtSource.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            txtSource.BorderStyle = BorderStyle.FixedSingle;
            txtSource.ForeColor = System.Drawing.Color.Gainsboro;
            txtSource.Margin = new Padding(0, 0, 0, 12);
            txtSource.Multiline = true;
            txtSource.ReadOnly = true;
            txtSource.ScrollBars = ScrollBars.Vertical;
            txtSource.Dock = DockStyle.Fill;
            // 
            // tabTranslations
            // 
            tabTranslations.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            tabTranslations.Dock = DockStyle.Fill;
            tabTranslations.ForeColor = System.Drawing.Color.Gainsboro;
            tabTranslations.Margin = new Padding(0, 0, 0, 12);
            tabTranslations.SizeMode = TabSizeMode.FillToRight;
            // 
            // layoutBottom
            // 
            layoutBottom.ColumnCount = 2;
            layoutBottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layoutBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutBottom.Controls.Add(panelStatus, 0, 0);
            layoutBottom.Controls.Add(panelTranslationButtons, 1, 0);
            layoutBottom.Dock = DockStyle.Fill;
            layoutBottom.RowCount = 1;
            layoutBottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutBottom.TabIndex = 3;
            // 
            // panelStatus
            // 
            panelStatus.AutoSize = true;
            panelStatus.Controls.Add(lblStatusLabel);
            panelStatus.Controls.Add(lblStatusValue);
            panelStatus.Dock = DockStyle.Fill;
            panelStatus.FlowDirection = FlowDirection.LeftToRight;
            panelStatus.WrapContents = false;
            panelStatus.Margin = new Padding(0, 0, 12, 0);
            // 
            // lblStatusLabel
            // 
            lblStatusLabel.AutoSize = true;
            lblStatusLabel.ForeColor = System.Drawing.Color.Gainsboro;
            lblStatusLabel.Text = "Status:";
            lblStatusLabel.Margin = new Padding(0, 6, 6, 0);
            // 
            // lblStatusValue
            // 
            lblStatusValue.AutoSize = true;
            lblStatusValue.ForeColor = System.Drawing.Color.Gainsboro;
            lblStatusValue.Margin = new Padding(0, 6, 0, 0);
            // 
            // panelTranslationButtons
            // 
            panelTranslationButtons.AutoSize = true;
            panelTranslationButtons.Dock = DockStyle.Fill;
            panelTranslationButtons.FlowDirection = FlowDirection.RightToLeft;
            panelTranslationButtons.Controls.Add(btnSaveNext);
            panelTranslationButtons.Controls.Add(btnSave);
            panelTranslationButtons.Controls.Add(btnQuickPaste);
            panelTranslationButtons.Controls.Add(btnCopySource);
            panelTranslationButtons.WrapContents = false;
            // 
            // btnCopySource
            // 
            btnCopySource.Padding = new Padding(6);
            btnCopySource.Size = new Size(96, 28);
            btnCopySource.Text = "Copy Source";
            // 
            // btnQuickPaste
            // 
            btnQuickPaste.Padding = new Padding(6);
            btnQuickPaste.Size = new Size(96, 28);
            btnQuickPaste.Text = "QuickPaste";
            // 
            // btnSave
            // 
            btnSave.Padding = new Padding(6);
            btnSave.Size = new Size(64, 28);
            btnSave.Text = "Save";
            // 
            // btnSaveNext
            // 
            btnSaveNext.Padding = new Padding(6);
            btnSaveNext.Size = new Size(64, 28);
            btnSaveNext.Text = "Next";
            // 
            // TranslationQueueControl
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            Controls.Add(splitContainerMain);
            ForeColor = System.Drawing.Color.Gainsboro;
            Name = "TranslationQueueControl";
            Size = new Size(1200, 720);
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            layoutLeft.ResumeLayout(false);
            layoutLeft.PerformLayout();
            layoutFilters.ResumeLayout(false);
            layoutFilters.PerformLayout();
            panelFilterButtons.ResumeLayout(false);
            panelNavButtons.ResumeLayout(false);
            layoutRight.ResumeLayout(false);
            layoutRight.PerformLayout();
            layoutBottom.ResumeLayout(false);
            layoutBottom.PerformLayout();
            panelTranslationButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}
