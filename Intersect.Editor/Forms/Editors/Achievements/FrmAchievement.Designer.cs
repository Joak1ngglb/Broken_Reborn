using DarkUI.Controls;

namespace Intersect.Editor.Forms.Editors.Achievements
{
    partial class FrmAchievement
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.grpAchievements = new DarkGroupBox();
            this.btnClearSearch = new DarkButton();
            this.txtSearch = new DarkTextBox();
            this.lstGameObjects = new Intersect.Editor.Forms.Controls.GameObjectList();
            this.toolStrip = new DarkToolStrip();
            this.toolStripItemNew = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripItemDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnAlphabetical = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripItemCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStripItemPaste = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripItemUndo = new System.Windows.Forms.ToolStripButton();
            this.pnlContainer = new System.Windows.Forms.Panel();
            this.grpRewards = new DarkGroupBox();
            this.lblOrnamentIds = new System.Windows.Forms.Label();
            this.txtOrnamentIds = new DarkTextBox();
            this.lblTitleIds = new System.Windows.Forms.Label();
            this.txtTitleIds = new DarkTextBox();
            this.btnRemoveResource = new DarkButton();
            this.btnAddResource = new DarkButton();
            this.lblResourceAmount = new System.Windows.Forms.Label();
            this.nudResourceAmount = new DarkNumericUpDown();
            this.lblResource = new System.Windows.Forms.Label();
            this.cmbResource = new DarkComboBox();
            this.lstResources = new System.Windows.Forms.ListBox();
            this.lblCurrency = new System.Windows.Forms.Label();
            this.nudCurrency = new DarkNumericUpDown();
            this.lblExperience = new System.Windows.Forms.Label();
            this.nudExperience = new DarkNumericUpDown();
            this.grpRequirements = new DarkGroupBox();
            this.btnEditRequirements = new DarkButton();
            this.grpGeneral = new DarkGroupBox();
            this.btnAddFolder = new DarkButton();
            this.cmbFolder = new DarkComboBox();
            this.lblFolder = new System.Windows.Forms.Label();
            this.lblDifficulty = new System.Windows.Forms.Label();
            this.cmbDifficulty = new DarkComboBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbCategory = new DarkComboBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new DarkTextBox();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new DarkTextBox();
            this.btnSave = new DarkButton();
            this.btnCancel = new DarkButton();
            this.grpAchievements.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.pnlContainer.SuspendLayout();
            this.grpRewards.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudResourceAmount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCurrency)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudExperience)).BeginInit();
            this.grpRequirements.SuspendLayout();
            this.grpGeneral.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpAchievements
            // 
            this.grpAchievements.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.grpAchievements.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.grpAchievements.Controls.Add(this.btnClearSearch);
            this.grpAchievements.Controls.Add(this.txtSearch);
            this.grpAchievements.Controls.Add(this.lstGameObjects);
            this.grpAchievements.ForeColor = System.Drawing.Color.Gainsboro;
            this.grpAchievements.Location = new System.Drawing.Point(12, 34);
            this.grpAchievements.Name = "grpAchievements";
            this.grpAchievements.Size = new System.Drawing.Size(220, 612);
            this.grpAchievements.TabIndex = 0;
            this.grpAchievements.TabStop = false;
            this.grpAchievements.Text = "Achievements";
            // 
            // btnClearSearch
            // 
            this.btnClearSearch.Location = new System.Drawing.Point(194, 20);
            this.btnClearSearch.Name = "btnClearSearch";
            this.btnClearSearch.Padding = new System.Windows.Forms.Padding(5);
            this.btnClearSearch.Size = new System.Drawing.Size(18, 20);
            this.btnClearSearch.TabIndex = 2;
            this.btnClearSearch.Text = "X";
            this.btnClearSearch.Click += new System.EventHandler(this.btnClearSearch_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.txtSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtSearch.Location = new System.Drawing.Point(6, 20);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(182, 20);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.Text = "Search...";
            this.txtSearch.Click += new System.EventHandler(this.txtSearch_Click);
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_Enter);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            // 
            // lstGameObjects
            // 
            this.lstGameObjects.AllowDrop = true;
            this.lstGameObjects.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.lstGameObjects.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstGameObjects.ForeColor = System.Drawing.Color.Gainsboro;
            this.lstGameObjects.HideSelection = false;
            this.lstGameObjects.ImageIndex = 0;
            this.lstGameObjects.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.lstGameObjects.Location = new System.Drawing.Point(6, 46);
            this.lstGameObjects.Name = "lstGameObjects";
            this.lstGameObjects.SelectedImageIndex = 0;
            this.lstGameObjects.Size = new System.Drawing.Size(208, 560);
            this.lstGameObjects.TabIndex = 0;
            // 
            // toolStrip
            // 
            this.toolStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.toolStrip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripItemNew,
            this.toolStripSeparator1,
            this.toolStripItemDelete,
            this.toolStripSeparator2,
            this.btnAlphabetical,
            this.toolStripSeparator4,
            this.toolStripItemCopy,
            this.toolStripItemPaste,
            this.toolStripSeparator3,
            this.toolStripItemUndo});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(5, 0, 1, 0);
            this.toolStrip.Size = new System.Drawing.Size(1068, 25);
            this.toolStrip.TabIndex = 1;
            this.toolStrip.Text = "toolStrip";
            // 
            // toolStripItemNew
            // 
            this.toolStripItemNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripItemNew.ForeColor = System.Drawing.Color.Gainsboro;
            this.toolStripItemNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripItemNew.Name = "toolStripItemNew";
            this.toolStripItemNew.Size = new System.Drawing.Size(35, 22);
            this.toolStripItemNew.Text = "New";
            this.toolStripItemNew.Click += new System.EventHandler(this.toolStripItemNew_Click);
            // 
            // toolStripItemDelete
            // 
            this.toolStripItemDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripItemDelete.ForeColor = System.Drawing.Color.Gainsboro;
            this.toolStripItemDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripItemDelete.Name = "toolStripItemDelete";
            this.toolStripItemDelete.Size = new System.Drawing.Size(44, 22);
            this.toolStripItemDelete.Text = "Delete";
            this.toolStripItemDelete.Click += new System.EventHandler(this.toolStripItemDelete_Click);
            // 
            // btnAlphabetical
            // 
            this.btnAlphabetical.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnAlphabetical.ForeColor = System.Drawing.Color.Gainsboro;
            this.btnAlphabetical.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAlphabetical.Name = "btnAlphabetical";
            this.btnAlphabetical.Size = new System.Drawing.Size(76, 22);
            this.btnAlphabetical.Text = "Alphabetical";
            this.btnAlphabetical.Click += new System.EventHandler(this.btnAlphabetical_Click);
            // 
            // toolStripItemCopy
            // 
            this.toolStripItemCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripItemCopy.ForeColor = System.Drawing.Color.Gainsboro;
            this.toolStripItemCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripItemCopy.Name = "toolStripItemCopy";
            this.toolStripItemCopy.Size = new System.Drawing.Size(39, 22);
            this.toolStripItemCopy.Text = "Copy";
            this.toolStripItemCopy.Click += new System.EventHandler(this.toolStripItemCopy_Click);
            // 
            // toolStripItemPaste
            // 
            this.toolStripItemPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripItemPaste.ForeColor = System.Drawing.Color.Gainsboro;
            this.toolStripItemPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripItemPaste.Name = "toolStripItemPaste";
            this.toolStripItemPaste.Size = new System.Drawing.Size(39, 22);
            this.toolStripItemPaste.Text = "Paste";
            this.toolStripItemPaste.Click += new System.EventHandler(this.toolStripItemPaste_Click);
            // 
            // toolStripItemUndo
            // 
            this.toolStripItemUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripItemUndo.ForeColor = System.Drawing.Color.Gainsboro;
            this.toolStripItemUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripItemUndo.Name = "toolStripItemUndo";
            this.toolStripItemUndo.Size = new System.Drawing.Size(40, 22);
            this.toolStripItemUndo.Text = "Undo";
            this.toolStripItemUndo.Click += new System.EventHandler(this.toolStripItemUndo_Click);
            // 
            // pnlContainer
            // 
            this.pnlContainer.Controls.Add(this.grpRewards);
            this.pnlContainer.Controls.Add(this.grpRequirements);
            this.pnlContainer.Controls.Add(this.grpGeneral);
            this.pnlContainer.Controls.Add(this.btnSave);
            this.pnlContainer.Controls.Add(this.btnCancel);
            this.pnlContainer.Location = new System.Drawing.Point(238, 34);
            this.pnlContainer.Name = "pnlContainer";
            this.pnlContainer.Size = new System.Drawing.Size(818, 612);
            this.pnlContainer.TabIndex = 2;
            // 
            // grpRewards
            // 
            this.grpRewards.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.grpRewards.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.grpRewards.Controls.Add(this.lblOrnamentIds);
            this.grpRewards.Controls.Add(this.txtOrnamentIds);
            this.grpRewards.Controls.Add(this.lblTitleIds);
            this.grpRewards.Controls.Add(this.txtTitleIds);
            this.grpRewards.Controls.Add(this.btnRemoveResource);
            this.grpRewards.Controls.Add(this.btnAddResource);
            this.grpRewards.Controls.Add(this.lblResourceAmount);
            this.grpRewards.Controls.Add(this.nudResourceAmount);
            this.grpRewards.Controls.Add(this.lblResource);
            this.grpRewards.Controls.Add(this.cmbResource);
            this.grpRewards.Controls.Add(this.lstResources);
            this.grpRewards.Controls.Add(this.lblCurrency);
            this.grpRewards.Controls.Add(this.nudCurrency);
            this.grpRewards.Controls.Add(this.lblExperience);
            this.grpRewards.Controls.Add(this.nudExperience);
            this.grpRewards.ForeColor = System.Drawing.Color.Gainsboro;
            this.grpRewards.Location = new System.Drawing.Point(0, 274);
            this.grpRewards.Name = "grpRewards";
            this.grpRewards.Size = new System.Drawing.Size(818, 268);
            this.grpRewards.TabIndex = 2;
            this.grpRewards.TabStop = false;
            this.grpRewards.Text = "Rewards";
            // 
            // lblOrnamentIds
            // 
            this.lblOrnamentIds.AutoSize = true;
            this.lblOrnamentIds.Location = new System.Drawing.Point(417, 140);
            this.lblOrnamentIds.Name = "lblOrnamentIds";
            this.lblOrnamentIds.Size = new System.Drawing.Size(72, 13);
            this.lblOrnamentIds.TabIndex = 14;
            this.lblOrnamentIds.Text = "Ornament IDs";
            // 
            // txtOrnamentIds
            // 
            this.txtOrnamentIds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.txtOrnamentIds.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOrnamentIds.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtOrnamentIds.Location = new System.Drawing.Point(512, 137);
            this.txtOrnamentIds.Multiline = true;
            this.txtOrnamentIds.Name = "txtOrnamentIds";
            this.txtOrnamentIds.Size = new System.Drawing.Size(287, 83);
            this.txtOrnamentIds.TabIndex = 13;
            this.txtOrnamentIds.TextChanged += new System.EventHandler(this.txtOrnamentIds_TextChanged);
            // 
            // lblTitleIds
            // 
            this.lblTitleIds.AutoSize = true;
            this.lblTitleIds.Location = new System.Drawing.Point(417, 42);
            this.lblTitleIds.Name = "lblTitleIds";
            this.lblTitleIds.Size = new System.Drawing.Size(49, 13);
            this.lblTitleIds.TabIndex = 12;
            this.lblTitleIds.Text = "Title IDs";
            // 
            // txtTitleIds
            // 
            this.txtTitleIds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.txtTitleIds.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTitleIds.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtTitleIds.Location = new System.Drawing.Point(512, 39);
            this.txtTitleIds.Multiline = true;
            this.txtTitleIds.Name = "txtTitleIds";
            this.txtTitleIds.Size = new System.Drawing.Size(287, 83);
            this.txtTitleIds.TabIndex = 11;
            this.txtTitleIds.TextChanged += new System.EventHandler(this.txtTitleIds_TextChanged);
            // 
            // btnRemoveResource
            // 
            this.btnRemoveResource.Location = new System.Drawing.Point(302, 76);
            this.btnRemoveResource.Name = "btnRemoveResource";
            this.btnRemoveResource.Padding = new System.Windows.Forms.Padding(5);
            this.btnRemoveResource.Size = new System.Drawing.Size(92, 24);
            this.btnRemoveResource.TabIndex = 10;
            this.btnRemoveResource.Text = "Remove";
            this.btnRemoveResource.Click += new System.EventHandler(this.btnRemoveResource_Click);
            // 
            // btnAddResource
            // 
            this.btnAddResource.Location = new System.Drawing.Point(204, 76);
            this.btnAddResource.Name = "btnAddResource";
            this.btnAddResource.Padding = new System.Windows.Forms.Padding(5);
            this.btnAddResource.Size = new System.Drawing.Size(92, 24);
            this.btnAddResource.TabIndex = 9;
            this.btnAddResource.Text = "Add";
            this.btnAddResource.Click += new System.EventHandler(this.btnAddResource_Click);
            // 
            // lblResourceAmount
            // 
            this.lblResourceAmount.AutoSize = true;
            this.lblResourceAmount.Location = new System.Drawing.Point(12, 52);
            this.lblResourceAmount.Name = "lblResourceAmount";
            this.lblResourceAmount.Size = new System.Drawing.Size(46, 13);
            this.lblResourceAmount.TabIndex = 8;
            this.lblResourceAmount.Text = "Amount";
            // 
            // nudResourceAmount
            // 
            this.nudResourceAmount.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.nudResourceAmount.ForeColor = System.Drawing.Color.Gainsboro;
            this.nudResourceAmount.Location = new System.Drawing.Point(89, 50);
            this.nudResourceAmount.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudResourceAmount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudResourceAmount.Name = "nudResourceAmount";
            this.nudResourceAmount.Size = new System.Drawing.Size(105, 20);
            this.nudResourceAmount.TabIndex = 7;
            this.nudResourceAmount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblResource
            // 
            this.lblResource.AutoSize = true;
            this.lblResource.Location = new System.Drawing.Point(12, 24);
            this.lblResource.Name = "lblResource";
            this.lblResource.Size = new System.Drawing.Size(53, 13);
            this.lblResource.TabIndex = 6;
            this.lblResource.Text = "Resource";
            // 
            // cmbResource
            // 
            this.cmbResource.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.cmbResource.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.cmbResource.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.cmbResource.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.cmbResource.DrawDropdownHoverOutline = false;
            this.cmbResource.DrawFocusRectangle = false;
            this.cmbResource.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbResource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResource.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbResource.ForeColor = System.Drawing.Color.Gainsboro;
            this.cmbResource.FormattingEnabled = true;
            this.cmbResource.Location = new System.Drawing.Point(89, 21);
            this.cmbResource.Name = "cmbResource";
            this.cmbResource.Size = new System.Drawing.Size(305, 21);
            this.cmbResource.TabIndex = 5;
            // 
            // lstResources
            // 
            this.lstResources.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.lstResources.ForeColor = System.Drawing.Color.Gainsboro;
            this.lstResources.FormattingEnabled = true;
            this.lstResources.Location = new System.Drawing.Point(15, 109);
            this.lstResources.Name = "lstResources";
            this.lstResources.Size = new System.Drawing.Size(379, 95);
            this.lstResources.TabIndex = 4;
            // 
            // lblCurrency
            // 
            this.lblCurrency.AutoSize = true;
            this.lblCurrency.Location = new System.Drawing.Point(226, 233);
            this.lblCurrency.Name = "lblCurrency";
            this.lblCurrency.Size = new System.Drawing.Size(49, 13);
            this.lblCurrency.TabIndex = 3;
            this.lblCurrency.Text = "Currency";
            // 
            // nudCurrency
            // 
            this.nudCurrency.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.nudCurrency.ForeColor = System.Drawing.Color.Gainsboro;
            this.nudCurrency.Location = new System.Drawing.Point(302, 231);
            this.nudCurrency.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudCurrency.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.nudCurrency.Name = "nudCurrency";
            this.nudCurrency.Size = new System.Drawing.Size(92, 20);
            this.nudCurrency.TabIndex = 2;
            this.nudCurrency.ValueChanged += new System.EventHandler(this.nudCurrency_ValueChanged);
            // 
            // lblExperience
            // 
            this.lblExperience.AutoSize = true;
            this.lblExperience.Location = new System.Drawing.Point(12, 233);
            this.lblExperience.Name = "lblExperience";
            this.lblExperience.Size = new System.Drawing.Size(60, 13);
            this.lblExperience.TabIndex = 1;
            this.lblExperience.Text = "Experience";
            // 
            // nudExperience
            // 
            this.nudExperience.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.nudExperience.ForeColor = System.Drawing.Color.Gainsboro;
            this.nudExperience.Location = new System.Drawing.Point(89, 231);
            this.nudExperience.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nudExperience.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.nudExperience.Name = "nudExperience";
            this.nudExperience.Size = new System.Drawing.Size(105, 20);
            this.nudExperience.TabIndex = 0;
            this.nudExperience.ValueChanged += new System.EventHandler(this.nudExperience_ValueChanged);
            // 
            // grpRequirements
            // 
            this.grpRequirements.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.grpRequirements.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.grpRequirements.Controls.Add(this.btnEditRequirements);
            this.grpRequirements.ForeColor = System.Drawing.Color.Gainsboro;
            this.grpRequirements.Location = new System.Drawing.Point(0, 210);
            this.grpRequirements.Name = "grpRequirements";
            this.grpRequirements.Size = new System.Drawing.Size(818, 58);
            this.grpRequirements.TabIndex = 1;
            this.grpRequirements.TabStop = false;
            this.grpRequirements.Text = "Requirements";
            // 
            // btnEditRequirements
            // 
            this.btnEditRequirements.Location = new System.Drawing.Point(15, 22);
            this.btnEditRequirements.Name = "btnEditRequirements";
            this.btnEditRequirements.Padding = new System.Windows.Forms.Padding(5);
            this.btnEditRequirements.Size = new System.Drawing.Size(196, 23);
            this.btnEditRequirements.TabIndex = 0;
            this.btnEditRequirements.Text = "Edit Requirements";
            this.btnEditRequirements.Click += new System.EventHandler(this.btnEditRequirements_Click);
            // 
            // grpGeneral
            // 
            this.grpGeneral.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.grpGeneral.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.grpGeneral.Controls.Add(this.btnAddFolder);
            this.grpGeneral.Controls.Add(this.cmbFolder);
            this.grpGeneral.Controls.Add(this.lblFolder);
            this.grpGeneral.Controls.Add(this.lblDifficulty);
            this.grpGeneral.Controls.Add(this.cmbDifficulty);
            this.grpGeneral.Controls.Add(this.lblCategory);
            this.grpGeneral.Controls.Add(this.cmbCategory);
            this.grpGeneral.Controls.Add(this.lblDescription);
            this.grpGeneral.Controls.Add(this.txtDescription);
            this.grpGeneral.Controls.Add(this.lblName);
            this.grpGeneral.Controls.Add(this.txtName);
            this.grpGeneral.ForeColor = System.Drawing.Color.Gainsboro;
            this.grpGeneral.Location = new System.Drawing.Point(0, 0);
            this.grpGeneral.Name = "grpGeneral";
            this.grpGeneral.Size = new System.Drawing.Size(818, 204);
            this.grpGeneral.TabIndex = 0;
            this.grpGeneral.TabStop = false;
            this.grpGeneral.Text = "General";
            // 
            // btnAddFolder
            // 
            this.btnAddFolder.Location = new System.Drawing.Point(711, 77);
            this.btnAddFolder.Name = "btnAddFolder";
            this.btnAddFolder.Padding = new System.Windows.Forms.Padding(5);
            this.btnAddFolder.Size = new System.Drawing.Size(88, 23);
            this.btnAddFolder.TabIndex = 10;
            this.btnAddFolder.Text = "Add Folder";
            this.btnAddFolder.Click += new System.EventHandler(this.btnAddFolder_Click);
            // 
            // cmbFolder
            // 
            this.cmbFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.cmbFolder.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.cmbFolder.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.cmbFolder.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.cmbFolder.DrawDropdownHoverOutline = false;
            this.cmbFolder.DrawFocusRectangle = false;
            this.cmbFolder.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbFolder.ForeColor = System.Drawing.Color.Gainsboro;
            this.cmbFolder.FormattingEnabled = true;
            this.cmbFolder.Location = new System.Drawing.Point(512, 79);
            this.cmbFolder.Name = "cmbFolder";
            this.cmbFolder.Size = new System.Drawing.Size(193, 21);
            this.cmbFolder.TabIndex = 9;
            this.cmbFolder.SelectedIndexChanged += new System.EventHandler(this.cmbFolder_SelectedIndexChanged);
            // 
            // lblFolder
            // 
            this.lblFolder.AutoSize = true;
            this.lblFolder.Location = new System.Drawing.Point(417, 82);
            this.lblFolder.Name = "lblFolder";
            this.lblFolder.Size = new System.Drawing.Size(36, 13);
            this.lblFolder.TabIndex = 8;
            this.lblFolder.Text = "Folder";
            // 
            // lblDifficulty
            // 
            this.lblDifficulty.AutoSize = true;
            this.lblDifficulty.Location = new System.Drawing.Point(12, 82);
            this.lblDifficulty.Name = "lblDifficulty";
            this.lblDifficulty.Size = new System.Drawing.Size(47, 13);
            this.lblDifficulty.TabIndex = 7;
            this.lblDifficulty.Text = "Difficulty";
            // 
            // cmbDifficulty
            // 
            this.cmbDifficulty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.cmbDifficulty.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.cmbDifficulty.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.cmbDifficulty.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.cmbDifficulty.DrawDropdownHoverOutline = false;
            this.cmbDifficulty.DrawFocusRectangle = false;
            this.cmbDifficulty.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbDifficulty.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDifficulty.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbDifficulty.ForeColor = System.Drawing.Color.Gainsboro;
            this.cmbDifficulty.FormattingEnabled = true;
            this.cmbDifficulty.Location = new System.Drawing.Point(89, 79);
            this.cmbDifficulty.Name = "cmbDifficulty";
            this.cmbDifficulty.Size = new System.Drawing.Size(305, 21);
            this.cmbDifficulty.TabIndex = 6;
            this.cmbDifficulty.SelectedIndexChanged += new System.EventHandler(this.cmbDifficulty_SelectedIndexChanged);
            // 
            // lblCategory
            // 
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(12, 52);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(49, 13);
            this.lblCategory.TabIndex = 5;
            this.lblCategory.Text = "Category";
            // 
            // cmbCategory
            // 
            this.cmbCategory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.cmbCategory.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.cmbCategory.BorderStyle = System.Windows.Forms.ButtonBorderStyle.Solid;
            this.cmbCategory.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.cmbCategory.DrawDropdownHoverOutline = false;
            this.cmbCategory.DrawFocusRectangle = false;
            this.cmbCategory.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbCategory.ForeColor = System.Drawing.Color.Gainsboro;
            this.cmbCategory.FormattingEnabled = true;
            this.cmbCategory.Location = new System.Drawing.Point(89, 50);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(305, 21);
            this.cmbCategory.TabIndex = 4;
            this.cmbCategory.SelectedIndexChanged += new System.EventHandler(this.cmbCategory_SelectedIndexChanged);
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(12, 117);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(60, 13);
            this.lblDescription.TabIndex = 3;
            this.lblDescription.Text = "Description";
            // 
            // txtDescription
            // 
            this.txtDescription.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.txtDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtDescription.Location = new System.Drawing.Point(89, 114);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(710, 73);
            this.txtDescription.TabIndex = 2;
            this.txtDescription.TextChanged += new System.EventHandler(this.txtDescription_TextChanged);
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 24);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(35, 13);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "Name";
            // 
            // txtName
            // 
            this.txtName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(69)))), ((int)(((byte)(73)))), ((int)(((byte)(74)))));
            this.txtName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.txtName.Location = new System.Drawing.Point(89, 21);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(305, 20);
            this.txtName.TabIndex = 0;
            this.txtName.TextChanged += new System.EventHandler(this.txtName_TextChanged);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(612, 564);
            this.btnSave.Name = "btnSave";
            this.btnSave.Padding = new System.Windows.Forms.Padding(5);
            this.btnSave.Size = new System.Drawing.Size(96, 30);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(714, 564);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(5);
            this.btnCancel.Size = new System.Drawing.Size(96, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // FrmAchievement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ClientSize = new System.Drawing.Size(1068, 658);
            this.Controls.Add(this.pnlContainer);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.grpAchievements);
            this.ForeColor = System.Drawing.Color.Gainsboro;
            this.KeyPreview = true;
            this.Name = "FrmAchievement";
            this.Text = "Achievement Editor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmAchievement_FormClosed);
            this.Load += new System.EventHandler(this.FrmAchievement_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.form_KeyDown);
            this.grpAchievements.ResumeLayout(false);
            this.grpAchievements.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.pnlContainer.ResumeLayout(false);
            this.grpRewards.ResumeLayout(false);
            this.grpRewards.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudResourceAmount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCurrency)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudExperience)).EndInit();
            this.grpRequirements.ResumeLayout(false);
            this.grpGeneral.ResumeLayout(false);
            this.grpGeneral.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DarkGroupBox grpAchievements;
        private DarkButton btnClearSearch;
        private DarkTextBox txtSearch;
        private Intersect.Editor.Forms.Controls.GameObjectList lstGameObjects;
        private DarkToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripItemNew;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripItemDelete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnAlphabetical;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton toolStripItemCopy;
        private System.Windows.Forms.ToolStripButton toolStripItemPaste;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripItemUndo;
        private System.Windows.Forms.Panel pnlContainer;
        private DarkGroupBox grpRewards;
        private System.Windows.Forms.Label lblOrnamentIds;
        private DarkTextBox txtOrnamentIds;
        private System.Windows.Forms.Label lblTitleIds;
        private DarkTextBox txtTitleIds;
        private DarkButton btnRemoveResource;
        private DarkButton btnAddResource;
        private System.Windows.Forms.Label lblResourceAmount;
        private DarkNumericUpDown nudResourceAmount;
        private System.Windows.Forms.Label lblResource;
        private DarkComboBox cmbResource;
        private System.Windows.Forms.ListBox lstResources;
        private System.Windows.Forms.Label lblCurrency;
        private DarkNumericUpDown nudCurrency;
        private System.Windows.Forms.Label lblExperience;
        private DarkNumericUpDown nudExperience;
        private DarkGroupBox grpRequirements;
        private DarkButton btnEditRequirements;
        private DarkGroupBox grpGeneral;
        private DarkButton btnAddFolder;
        private DarkComboBox cmbFolder;
        private System.Windows.Forms.Label lblFolder;
        private System.Windows.Forms.Label lblDifficulty;
        private DarkComboBox cmbDifficulty;
        private System.Windows.Forms.Label lblCategory;
        private DarkComboBox cmbCategory;
        private System.Windows.Forms.Label lblDescription;
        private DarkTextBox txtDescription;
        private System.Windows.Forms.Label lblName;
        private DarkTextBox txtName;
        private DarkButton btnSave;
        private DarkButton btnCancel;
    }
}
