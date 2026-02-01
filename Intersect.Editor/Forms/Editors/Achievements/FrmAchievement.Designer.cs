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
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAchievement));
            grpAchievements = new DarkGroupBox();
            btnClearSearch = new DarkButton();
            txtSearch = new DarkTextBox();
            lstGameObjects = new Intersect.Editor.Forms.Controls.GameObjectList();
            pnlContainer = new Panel();
            grpRewards = new DarkGroupBox();
            lblTitleIds = new Label();
            cmbTitle = new DarkComboBox();
            btnRemoveTitle = new DarkButton();
            btnAddTitle = new DarkButton();
            lstTitles = new ListBox();
            btnRemoveResource = new DarkButton();
            btnAddResource = new DarkButton();
            lblResourceAmount = new Label();
            nudResourceAmount = new DarkNumericUpDown();
            lblResource = new Label();
            cmbResource = new DarkComboBox();
            lstResources = new ListBox();
            lblCurrency = new Label();
            nudCurrency = new DarkNumericUpDown();
            lblExperience = new Label();
            nudExperience = new DarkNumericUpDown();
            grpRequirements = new DarkGroupBox();
            btnEditRequirements = new DarkButton();
            grpGeneral = new DarkGroupBox();
            btnAddFolder = new DarkButton();
            cmbFolder = new DarkComboBox();
            lblFolder = new Label();
            lblCompletionMode = new Label();
            cmbCompletionMode = new DarkComboBox();
            lblDifficulty = new Label();
            cmbDifficulty = new DarkComboBox();
            lblCategory = new Label();
            cmbCategory = new DarkComboBox();
            lblDescription = new Label();
            txtDescription = new DarkTextBox();
            lblName = new Label();
            txtName = new DarkTextBox();
            btnSave = new DarkButton();
            btnCancel = new DarkButton();
            toolStrip = new DarkToolStrip();
            toolStripItemNew = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripItemDelete = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            btnAlphabetical = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            toolStripItemCopy = new ToolStripButton();
            toolStripItemPaste = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            toolStripItemUndo = new ToolStripButton();
            grpAchievements.SuspendLayout();
            pnlContainer.SuspendLayout();
            grpRewards.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudResourceAmount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudCurrency).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudExperience).BeginInit();
            grpRequirements.SuspendLayout();
            grpGeneral.SuspendLayout();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // grpAchievements
            // 
            grpAchievements.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            grpAchievements.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            grpAchievements.Controls.Add(btnClearSearch);
            grpAchievements.Controls.Add(txtSearch);
            grpAchievements.Controls.Add(lstGameObjects);
            grpAchievements.ForeColor = System.Drawing.Color.Gainsboro;
            grpAchievements.Location = new System.Drawing.Point(14, 39);
            grpAchievements.Margin = new Padding(4, 3, 4, 3);
            grpAchievements.Name = "grpAchievements";
            grpAchievements.Padding = new Padding(4, 3, 4, 3);
            grpAchievements.Size = new Size(257, 706);
            grpAchievements.TabIndex = 0;
            grpAchievements.TabStop = false;
            grpAchievements.Text = "Achievements";
            // 
            // btnClearSearch
            // 
            btnClearSearch.Location = new System.Drawing.Point(226, 23);
            btnClearSearch.Margin = new Padding(4, 3, 4, 3);
            btnClearSearch.Name = "btnClearSearch";
            btnClearSearch.Padding = new Padding(6, 6, 6, 6);
            btnClearSearch.Size = new Size(21, 23);
            btnClearSearch.TabIndex = 2;
            btnClearSearch.Text = "X";
            btnClearSearch.Click += btnClearSearch_Click;
            // 
            // txtSearch
            // 
            txtSearch.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            txtSearch.Location = new System.Drawing.Point(7, 23);
            txtSearch.Margin = new Padding(4, 3, 4, 3);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(212, 23);
            txtSearch.TabIndex = 1;
            txtSearch.Text = "Search...";
            txtSearch.Click += txtSearch_Click;
            txtSearch.TextChanged += txtSearch_TextChanged;
            txtSearch.Enter += txtSearch_Enter;
            txtSearch.Leave += txtSearch_Leave;
            // 
            // lstGameObjects
            // 
            lstGameObjects.AllowDrop = true;
            lstGameObjects.BackColor = System.Drawing.Color.FromArgb(60, 63, 65);
            lstGameObjects.BorderStyle = BorderStyle.None;
            lstGameObjects.ForeColor = System.Drawing.Color.Gainsboro;
            lstGameObjects.HideSelection = false;
            lstGameObjects.ImageIndex = 0;
            lstGameObjects.LineColor = System.Drawing.Color.FromArgb(150, 150, 150);
            lstGameObjects.Location = new System.Drawing.Point(7, 53);
            lstGameObjects.Margin = new Padding(4, 3, 4, 3);
            lstGameObjects.Name = "lstGameObjects";
            lstGameObjects.SelectedImageIndex = 0;
            lstGameObjects.Size = new Size(243, 646);
            lstGameObjects.TabIndex = 0;
            // 
            // pnlContainer
            // 
            pnlContainer.Controls.Add(grpRewards);
            pnlContainer.Controls.Add(grpRequirements);
            pnlContainer.Controls.Add(grpGeneral);
            pnlContainer.Controls.Add(btnSave);
            pnlContainer.Controls.Add(btnCancel);
            pnlContainer.Location = new System.Drawing.Point(278, 39);
            pnlContainer.Margin = new Padding(4, 3, 4, 3);
            pnlContainer.Name = "pnlContainer";
            pnlContainer.Size = new Size(954, 706);
            pnlContainer.TabIndex = 2;
            // 
            // grpRewards
            // 
            grpRewards.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            grpRewards.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            grpRewards.Controls.Add(lblTitleIds);
            grpRewards.Controls.Add(cmbTitle);
            grpRewards.Controls.Add(btnRemoveTitle);
            grpRewards.Controls.Add(btnAddTitle);
            grpRewards.Controls.Add(lstTitles);
            grpRewards.Controls.Add(btnRemoveResource);
            grpRewards.Controls.Add(btnAddResource);
            grpRewards.Controls.Add(lblResourceAmount);
            grpRewards.Controls.Add(nudResourceAmount);
            grpRewards.Controls.Add(lblResource);
            grpRewards.Controls.Add(cmbResource);
            grpRewards.Controls.Add(lstResources);
            grpRewards.Controls.Add(lblCurrency);
            grpRewards.Controls.Add(nudCurrency);
            grpRewards.Controls.Add(lblExperience);
            grpRewards.Controls.Add(nudExperience);
            grpRewards.ForeColor = System.Drawing.Color.Gainsboro;
            grpRewards.Location = new System.Drawing.Point(0, 316);
            grpRewards.Margin = new Padding(4, 3, 4, 3);
            grpRewards.Name = "grpRewards";
            grpRewards.Padding = new Padding(4, 3, 4, 3);
            grpRewards.Size = new Size(954, 309);
            grpRewards.TabIndex = 2;
            grpRewards.TabStop = false;
            grpRewards.Text = "Rewards";
            // 
            // lblTitleIds
            // 
            lblTitleIds.AutoSize = true;
            lblTitleIds.Location = new System.Drawing.Point(486, 48);
            lblTitleIds.Margin = new Padding(4, 0, 4, 0);
            lblTitleIds.Name = "lblTitleIds";
            lblTitleIds.Size = new Size(49, 15);
            lblTitleIds.TabIndex = 12;
            lblTitleIds.Text = "Title IDs";
            // 
            // cmbTitle
            // 
            cmbTitle.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbTitle.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbTitle.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbTitle.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbTitle.BorderStyle = ButtonBorderStyle.Solid;
            cmbTitle.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
            cmbTitle.DrawDropdownHoverOutline = false;
            cmbTitle.DrawFocusRectangle = false;
            cmbTitle.DrawMode = DrawMode.OwnerDrawFixed;
            cmbTitle.DropDownStyle = ComboBoxStyle.DropDown;
            cmbTitle.FlatStyle = FlatStyle.Flat;
            cmbTitle.ForeColor = System.Drawing.Color.Gainsboro;
            cmbTitle.FormattingEnabled = true;
            cmbTitle.Location = new System.Drawing.Point(597, 45);
            cmbTitle.Margin = new Padding(4, 3, 4, 3);
            cmbTitle.Name = "cmbTitle";
            cmbTitle.Size = new Size(334, 24);
            cmbTitle.TabIndex = 11;
            // 
            // btnRemoveTitle
            // 
            btnRemoveTitle.Location = new System.Drawing.Point(712, 75);
            btnRemoveTitle.Margin = new Padding(4, 3, 4, 3);
            btnRemoveTitle.Name = "btnRemoveTitle";
            btnRemoveTitle.Padding = new Padding(6, 6, 6, 6);
            btnRemoveTitle.Size = new Size(107, 28);
            btnRemoveTitle.TabIndex = 15;
            btnRemoveTitle.Text = "Remove";
            btnRemoveTitle.Click += btnRemoveTitle_Click;
            // 
            // btnAddTitle
            // 
            btnAddTitle.Location = new System.Drawing.Point(597, 75);
            btnAddTitle.Margin = new Padding(4, 3, 4, 3);
            btnAddTitle.Name = "btnAddTitle";
            btnAddTitle.Padding = new Padding(6, 6, 6, 6);
            btnAddTitle.Size = new Size(107, 28);
            btnAddTitle.TabIndex = 14;
            btnAddTitle.Text = "Add";
            btnAddTitle.Click += btnAddTitle_Click;
            // 
            // lstTitles
            // 
            lstTitles.BackColor = System.Drawing.Color.FromArgb(60, 63, 65);
            lstTitles.ForeColor = System.Drawing.Color.Gainsboro;
            lstTitles.FormattingEnabled = true;
            lstTitles.ItemHeight = 15;
            lstTitles.Location = new System.Drawing.Point(597, 108);
            lstTitles.Margin = new Padding(4, 3, 4, 3);
            lstTitles.Name = "lstTitles";
            lstTitles.Size = new Size(334, 94);
            lstTitles.TabIndex = 16;
            // 
            // btnRemoveResource
            // 
            btnRemoveResource.Location = new System.Drawing.Point(352, 88);
            btnRemoveResource.Margin = new Padding(4, 3, 4, 3);
            btnRemoveResource.Name = "btnRemoveResource";
            btnRemoveResource.Padding = new Padding(6, 6, 6, 6);
            btnRemoveResource.Size = new Size(107, 28);
            btnRemoveResource.TabIndex = 10;
            btnRemoveResource.Text = "Remove";
            btnRemoveResource.Click += btnRemoveResource_Click;
            // 
            // btnAddResource
            // 
            btnAddResource.Location = new System.Drawing.Point(238, 88);
            btnAddResource.Margin = new Padding(4, 3, 4, 3);
            btnAddResource.Name = "btnAddResource";
            btnAddResource.Padding = new Padding(6, 6, 6, 6);
            btnAddResource.Size = new Size(107, 28);
            btnAddResource.TabIndex = 9;
            btnAddResource.Text = "Add";
            btnAddResource.Click += btnAddResource_Click;
            // 
            // lblResourceAmount
            // 
            lblResourceAmount.AutoSize = true;
            lblResourceAmount.Location = new System.Drawing.Point(14, 60);
            lblResourceAmount.Margin = new Padding(4, 0, 4, 0);
            lblResourceAmount.Name = "lblResourceAmount";
            lblResourceAmount.Size = new Size(51, 15);
            lblResourceAmount.TabIndex = 8;
            lblResourceAmount.Text = "Amount";
            // 
            // nudResourceAmount
            // 
            nudResourceAmount.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            nudResourceAmount.ForeColor = System.Drawing.Color.Gainsboro;
            nudResourceAmount.Location = new System.Drawing.Point(104, 58);
            nudResourceAmount.Margin = new Padding(4, 3, 4, 3);
            nudResourceAmount.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            nudResourceAmount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudResourceAmount.Name = "nudResourceAmount";
            nudResourceAmount.Size = new Size(122, 23);
            nudResourceAmount.TabIndex = 7;
            nudResourceAmount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblResource
            // 
            lblResource.AutoSize = true;
            lblResource.Location = new System.Drawing.Point(14, 28);
            lblResource.Margin = new Padding(4, 0, 4, 0);
            lblResource.Name = "lblResource";
            lblResource.Size = new Size(55, 15);
            lblResource.TabIndex = 6;
            lblResource.Text = "Resource";
            // 
            // cmbResource
            // 
            cmbResource.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbResource.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbResource.BorderStyle = ButtonBorderStyle.Solid;
            cmbResource.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
            cmbResource.DrawDropdownHoverOutline = false;
            cmbResource.DrawFocusRectangle = false;
            cmbResource.DrawMode = DrawMode.OwnerDrawFixed;
            cmbResource.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbResource.FlatStyle = FlatStyle.Flat;
            cmbResource.ForeColor = System.Drawing.Color.Gainsboro;
            cmbResource.FormattingEnabled = true;
            cmbResource.Location = new System.Drawing.Point(104, 24);
            cmbResource.Margin = new Padding(4, 3, 4, 3);
            cmbResource.Name = "cmbResource";
            cmbResource.Size = new Size(355, 24);
            cmbResource.TabIndex = 5;
            cmbResource.Text = null;
            cmbResource.TextPadding = new Padding(2);
            // 
            // lstResources
            // 
            lstResources.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            lstResources.ForeColor = System.Drawing.Color.Gainsboro;
            lstResources.FormattingEnabled = true;
            lstResources.ItemHeight = 15;
            lstResources.Location = new System.Drawing.Point(18, 126);
            lstResources.Margin = new Padding(4, 3, 4, 3);
            lstResources.Name = "lstResources";
            lstResources.Size = new Size(442, 109);
            lstResources.TabIndex = 4;
            // 
            // lblCurrency
            // 
            lblCurrency.AutoSize = true;
            lblCurrency.Location = new System.Drawing.Point(264, 269);
            lblCurrency.Margin = new Padding(4, 0, 4, 0);
            lblCurrency.Name = "lblCurrency";
            lblCurrency.Size = new Size(55, 15);
            lblCurrency.TabIndex = 3;
            lblCurrency.Text = "Currency";
            // 
            // nudCurrency
            // 
            nudCurrency.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            nudCurrency.ForeColor = System.Drawing.Color.Gainsboro;
            nudCurrency.Location = new System.Drawing.Point(352, 267);
            nudCurrency.Margin = new Padding(4, 3, 4, 3);
            nudCurrency.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            nudCurrency.Name = "nudCurrency";
            nudCurrency.Size = new Size(107, 23);
            nudCurrency.TabIndex = 2;
            nudCurrency.Value = new decimal(new int[] { 0, 0, 0, 0 });
            nudCurrency.ValueChanged += nudCurrency_ValueChanged;
            // 
            // lblExperience
            // 
            lblExperience.AutoSize = true;
            lblExperience.Location = new System.Drawing.Point(14, 269);
            lblExperience.Margin = new Padding(4, 0, 4, 0);
            lblExperience.Name = "lblExperience";
            lblExperience.Size = new Size(63, 15);
            lblExperience.TabIndex = 1;
            lblExperience.Text = "Experience";
            // 
            // nudExperience
            // 
            nudExperience.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            nudExperience.ForeColor = System.Drawing.Color.Gainsboro;
            nudExperience.Location = new System.Drawing.Point(104, 267);
            nudExperience.Margin = new Padding(4, 3, 4, 3);
            nudExperience.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            nudExperience.Name = "nudExperience";
            nudExperience.Size = new Size(122, 23);
            nudExperience.TabIndex = 0;
            nudExperience.Value = new decimal(new int[] { 0, 0, 0, 0 });
            nudExperience.ValueChanged += nudExperience_ValueChanged;
            // 
            // grpRequirements
            // 
            grpRequirements.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            grpRequirements.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            grpRequirements.Controls.Add(btnEditRequirements);
            grpRequirements.ForeColor = System.Drawing.Color.Gainsboro;
            grpRequirements.Location = new System.Drawing.Point(0, 242);
            grpRequirements.Margin = new Padding(4, 3, 4, 3);
            grpRequirements.Name = "grpRequirements";
            grpRequirements.Padding = new Padding(4, 3, 4, 3);
            grpRequirements.Size = new Size(954, 67);
            grpRequirements.TabIndex = 1;
            grpRequirements.TabStop = false;
            grpRequirements.Text = "Requirements";
            // 
            // btnEditRequirements
            // 
            btnEditRequirements.Location = new System.Drawing.Point(18, 25);
            btnEditRequirements.Margin = new Padding(4, 3, 4, 3);
            btnEditRequirements.Name = "btnEditRequirements";
            btnEditRequirements.Padding = new Padding(6, 6, 6, 6);
            btnEditRequirements.Size = new Size(229, 27);
            btnEditRequirements.TabIndex = 0;
            btnEditRequirements.Text = "Edit Requirements";
            btnEditRequirements.Click += btnEditRequirements_Click;
            // 
            // grpGeneral
            // 
            grpGeneral.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            grpGeneral.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            grpGeneral.Controls.Add(btnAddFolder);
            grpGeneral.Controls.Add(cmbFolder);
            grpGeneral.Controls.Add(lblFolder);
            grpGeneral.Controls.Add(cmbCompletionMode);
            grpGeneral.Controls.Add(lblCompletionMode);
            grpGeneral.Controls.Add(lblDifficulty);
            grpGeneral.Controls.Add(cmbDifficulty);
            grpGeneral.Controls.Add(lblCategory);
            grpGeneral.Controls.Add(cmbCategory);
            grpGeneral.Controls.Add(lblDescription);
            grpGeneral.Controls.Add(txtDescription);
            grpGeneral.Controls.Add(lblName);
            grpGeneral.Controls.Add(txtName);
            grpGeneral.ForeColor = System.Drawing.Color.Gainsboro;
            grpGeneral.Location = new System.Drawing.Point(0, 0);
            grpGeneral.Margin = new Padding(4, 3, 4, 3);
            grpGeneral.Name = "grpGeneral";
            grpGeneral.Padding = new Padding(4, 3, 4, 3);
            grpGeneral.Size = new Size(954, 235);
            grpGeneral.TabIndex = 0;
            grpGeneral.TabStop = false;
            grpGeneral.Text = "General";
            // 
            // btnAddFolder
            // 
            btnAddFolder.Location = new System.Drawing.Point(830, 89);
            btnAddFolder.Margin = new Padding(4, 3, 4, 3);
            btnAddFolder.Name = "btnAddFolder";
            btnAddFolder.Padding = new Padding(6, 6, 6, 6);
            btnAddFolder.Size = new Size(103, 27);
            btnAddFolder.TabIndex = 10;
            btnAddFolder.Text = "Add Folder";
            btnAddFolder.Click += btnAddFolder_Click;
            // 
            // cmbFolder
            // 
            cmbFolder.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbFolder.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbFolder.BorderStyle = ButtonBorderStyle.Solid;
            cmbFolder.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
            cmbFolder.DrawDropdownHoverOutline = false;
            cmbFolder.DrawFocusRectangle = false;
            cmbFolder.DrawMode = DrawMode.OwnerDrawFixed;
            cmbFolder.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFolder.FlatStyle = FlatStyle.Flat;
            cmbFolder.ForeColor = System.Drawing.Color.Gainsboro;
            cmbFolder.FormattingEnabled = true;
            cmbFolder.Location = new System.Drawing.Point(597, 91);
            cmbFolder.Margin = new Padding(4, 3, 4, 3);
            cmbFolder.Name = "cmbFolder";
            cmbFolder.Size = new Size(224, 24);
            cmbFolder.TabIndex = 9;
            cmbFolder.Text = null;
            cmbFolder.TextPadding = new Padding(2);
            cmbFolder.SelectedIndexChanged += cmbFolder_SelectedIndexChanged;
            // 
            // lblFolder
            // 
            lblFolder.AutoSize = true;
            lblFolder.Location = new System.Drawing.Point(486, 95);
            lblFolder.Margin = new Padding(4, 0, 4, 0);
            lblFolder.Name = "lblFolder";
            lblFolder.Size = new Size(40, 15);
            lblFolder.TabIndex = 8;
            lblFolder.Text = "Folder";
            // 
            // lblCompletionMode
            // 
            lblCompletionMode.AutoSize = true;
            lblCompletionMode.Location = new System.Drawing.Point(486, 60);
            lblCompletionMode.Margin = new Padding(4, 0, 4, 0);
            lblCompletionMode.Name = "lblCompletionMode";
            lblCompletionMode.Size = new Size(102, 15);
            lblCompletionMode.TabIndex = 11;
            lblCompletionMode.Text = "Completion Mode";
            // 
            // cmbCompletionMode
            // 
            cmbCompletionMode.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbCompletionMode.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbCompletionMode.BorderStyle = ButtonBorderStyle.Solid;
            cmbCompletionMode.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
            cmbCompletionMode.DrawDropdownHoverOutline = false;
            cmbCompletionMode.DrawFocusRectangle = false;
            cmbCompletionMode.DrawMode = DrawMode.OwnerDrawFixed;
            cmbCompletionMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCompletionMode.FlatStyle = FlatStyle.Flat;
            cmbCompletionMode.ForeColor = System.Drawing.Color.Gainsboro;
            cmbCompletionMode.FormattingEnabled = true;
            cmbCompletionMode.Location = new System.Drawing.Point(597, 58);
            cmbCompletionMode.Margin = new Padding(4, 3, 4, 3);
            cmbCompletionMode.Name = "cmbCompletionMode";
            cmbCompletionMode.Size = new Size(224, 24);
            cmbCompletionMode.TabIndex = 5;
            cmbCompletionMode.Text = null;
            cmbCompletionMode.TextPadding = new Padding(2);
            cmbCompletionMode.SelectedIndexChanged += cmbCompletionMode_SelectedIndexChanged;
            // 
            // lblDifficulty
            // 
            lblDifficulty.AutoSize = true;
            lblDifficulty.Location = new System.Drawing.Point(14, 95);
            lblDifficulty.Margin = new Padding(4, 0, 4, 0);
            lblDifficulty.Name = "lblDifficulty";
            lblDifficulty.Size = new Size(55, 15);
            lblDifficulty.TabIndex = 7;
            lblDifficulty.Text = "Difficulty";
            // 
            // cmbDifficulty
            // 
            cmbDifficulty.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbDifficulty.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbDifficulty.BorderStyle = ButtonBorderStyle.Solid;
            cmbDifficulty.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
            cmbDifficulty.DrawDropdownHoverOutline = false;
            cmbDifficulty.DrawFocusRectangle = false;
            cmbDifficulty.DrawMode = DrawMode.OwnerDrawFixed;
            cmbDifficulty.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDifficulty.FlatStyle = FlatStyle.Flat;
            cmbDifficulty.ForeColor = System.Drawing.Color.Gainsboro;
            cmbDifficulty.FormattingEnabled = true;
            cmbDifficulty.Location = new System.Drawing.Point(104, 91);
            cmbDifficulty.Margin = new Padding(4, 3, 4, 3);
            cmbDifficulty.Name = "cmbDifficulty";
            cmbDifficulty.Size = new Size(355, 24);
            cmbDifficulty.TabIndex = 6;
            cmbDifficulty.Text = null;
            cmbDifficulty.TextPadding = new Padding(2);
            cmbDifficulty.SelectedIndexChanged += cmbDifficulty_SelectedIndexChanged;
            // 
            // lblCategory
            // 
            lblCategory.AutoSize = true;
            lblCategory.Location = new System.Drawing.Point(14, 60);
            lblCategory.Margin = new Padding(4, 0, 4, 0);
            lblCategory.Name = "lblCategory";
            lblCategory.Size = new Size(55, 15);
            lblCategory.TabIndex = 5;
            lblCategory.Text = "Category";
            // 
            // cmbCategory
            // 
            cmbCategory.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            cmbCategory.BorderColor = System.Drawing.Color.FromArgb(90, 90, 90);
            cmbCategory.BorderStyle = ButtonBorderStyle.Solid;
            cmbCategory.ButtonColor = System.Drawing.Color.FromArgb(43, 43, 43);
            cmbCategory.DrawDropdownHoverOutline = false;
            cmbCategory.DrawFocusRectangle = false;
            cmbCategory.DrawMode = DrawMode.OwnerDrawFixed;
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.FlatStyle = FlatStyle.Flat;
            cmbCategory.ForeColor = System.Drawing.Color.Gainsboro;
            cmbCategory.FormattingEnabled = true;
            cmbCategory.Location = new System.Drawing.Point(104, 58);
            cmbCategory.Margin = new Padding(4, 3, 4, 3);
            cmbCategory.Name = "cmbCategory";
            cmbCategory.Size = new Size(355, 24);
            cmbCategory.TabIndex = 4;
            cmbCategory.Text = null;
            cmbCategory.TextPadding = new Padding(2);
            cmbCategory.SelectedIndexChanged += cmbCategory_SelectedIndexChanged;
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new System.Drawing.Point(14, 135);
            lblDescription.Margin = new Padding(4, 0, 4, 0);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(67, 15);
            lblDescription.TabIndex = 3;
            lblDescription.Text = "Description";
            // 
            // txtDescription
            // 
            txtDescription.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            txtDescription.BorderStyle = BorderStyle.FixedSingle;
            txtDescription.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            txtDescription.Location = new System.Drawing.Point(104, 132);
            txtDescription.Margin = new Padding(4, 3, 4, 3);
            txtDescription.Multiline = true;
            txtDescription.Name = "txtDescription";
            txtDescription.Size = new Size(828, 84);
            txtDescription.TabIndex = 2;
            txtDescription.TextChanged += txtDescription_TextChanged;
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new System.Drawing.Point(14, 28);
            lblName.Margin = new Padding(4, 0, 4, 0);
            lblName.Name = "lblName";
            lblName.Size = new Size(39, 15);
            lblName.TabIndex = 1;
            lblName.Text = "Name";
            // 
            // txtName
            // 
            txtName.BackColor = System.Drawing.Color.FromArgb(69, 73, 74);
            txtName.BorderStyle = BorderStyle.FixedSingle;
            txtName.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            txtName.Location = new System.Drawing.Point(104, 24);
            txtName.Margin = new Padding(4, 3, 4, 3);
            txtName.Name = "txtName";
            txtName.Size = new Size(356, 23);
            txtName.TabIndex = 0;
            txtName.TextChanged += txtName_TextChanged;
            // 
            // btnSave
            // 
            btnSave.Location = new System.Drawing.Point(714, 651);
            btnSave.Margin = new Padding(4, 3, 4, 3);
            btnSave.Name = "btnSave";
            btnSave.Padding = new Padding(6, 6, 6, 6);
            btnSave.Size = new Size(112, 35);
            btnSave.TabIndex = 3;
            btnSave.Text = "Save";
            btnSave.Click += btnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(833, 651);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Padding = new Padding(6, 6, 6, 6);
            btnCancel.Size = new Size(112, 35);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.Click += btnCancel_Click;
            // 
            // toolStrip
            // 
            toolStrip.AutoSize = false;
            toolStrip.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            toolStrip.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStrip.Items.AddRange(new ToolStripItem[] { toolStripItemNew, toolStripSeparator1, toolStripItemDelete, toolStripSeparator2, btnAlphabetical, toolStripSeparator4, toolStripItemCopy, toolStripItemPaste, toolStripSeparator3, toolStripItemUndo });
            toolStrip.Location = new System.Drawing.Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Padding = new Padding(6, 0, 1, 0);
            toolStrip.Size = new Size(1246, 29);
            toolStrip.TabIndex = 64;
            toolStrip.Text = "toolStrip1";
            // 
            // toolStripItemNew
            // 
            toolStripItemNew.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripItemNew.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripItemNew.Image = (Image)resources.GetObject("toolStripItemNew.Image");
            toolStripItemNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripItemNew.Name = "toolStripItemNew";
            toolStripItemNew.Size = new Size(23, 26);
            toolStripItemNew.Text = "New";
            this.toolStripItemNew.Click += new System.EventHandler(this.toolStripItemNew_Click);
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripSeparator1.Margin = new Padding(0, 0, 2, 0);
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 29);
            // 
            // toolStripItemDelete
            // 
            toolStripItemDelete.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripItemDelete.Enabled = false;
            toolStripItemDelete.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripItemDelete.Image = (Image)resources.GetObject("toolStripItemDelete.Image");
            toolStripItemDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripItemDelete.Name = "toolStripItemDelete";
            toolStripItemDelete.Size = new Size(23, 26);
            toolStripItemDelete.Text = "Delete";
            this.toolStripItemDelete.Click += new System.EventHandler(this.toolStripItemDelete_Click);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripSeparator2.Margin = new Padding(0, 0, 2, 0);
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 29);
            // 
            // btnAlphabetical
            // 
            btnAlphabetical.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnAlphabetical.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            btnAlphabetical.Image = (Image)resources.GetObject("btnAlphabetical.Image");
            btnAlphabetical.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnAlphabetical.Name = "btnAlphabetical";
            btnAlphabetical.Size = new Size(23, 26);
            btnAlphabetical.Text = "Order Chronologically";
            this.btnAlphabetical.Click += new System.EventHandler(this.btnAlphabetical_Click);
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripSeparator4.Margin = new Padding(0, 0, 2, 0);
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 29);
            // 
            // toolStripItemCopy
            // 
            toolStripItemCopy.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripItemCopy.Enabled = false;
            toolStripItemCopy.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripItemCopy.Image = (Image)resources.GetObject("toolStripItemCopy.Image");
            toolStripItemCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripItemCopy.Name = "toolStripItemCopy";
            toolStripItemCopy.Size = new Size(23, 26);
            toolStripItemCopy.Text = "Copy";
            this.toolStripItemCopy.Click += new System.EventHandler(this.toolStripItemCopy_Click);
            // 
            // toolStripItemPaste
            // 
            toolStripItemPaste.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripItemPaste.Enabled = false;
            toolStripItemPaste.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripItemPaste.Image = (Image)resources.GetObject("toolStripItemPaste.Image");
            toolStripItemPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripItemPaste.Name = "toolStripItemPaste";
            toolStripItemPaste.Size = new Size(23, 26);
            toolStripItemPaste.Text = "Paste";
            this.toolStripItemPaste.Click += new System.EventHandler(this.toolStripItemPaste_Click);
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripSeparator3.Margin = new Padding(0, 0, 2, 0);
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 29);
            // 
            // toolStripItemUndo
            // 
            toolStripItemUndo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripItemUndo.Enabled = false;
            toolStripItemUndo.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            toolStripItemUndo.Image = (Image)resources.GetObject("toolStripItemUndo.Image");
            toolStripItemUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripItemUndo.Name = "toolStripItemUndo";
            toolStripItemUndo.Size = new Size(23, 26);
            toolStripItemUndo.Text = "Undo";
            toolStripItemUndo.Click += new System.EventHandler(this.toolStripItemUndo_Click);
            // 
            // FrmAchievement
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            ClientSize = new Size(1246, 759);
            Controls.Add(toolStrip);
            Controls.Add(pnlContainer);
            Controls.Add(grpAchievements);
            ForeColor = System.Drawing.Color.Gainsboro;
            KeyPreview = true;
            Margin = new Padding(4, 3, 4, 3);
            Name = "FrmAchievement";
            Text = "Achievement Editor";
            FormClosed += FrmAchievement_FormClosed;
            Load += FrmAchievement_Load;
            KeyDown += form_KeyDown;
            grpAchievements.ResumeLayout(false);
            grpAchievements.PerformLayout();
            pnlContainer.ResumeLayout(false);
            grpRewards.ResumeLayout(false);
            grpRewards.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudResourceAmount).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudCurrency).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudExperience).EndInit();
            grpRequirements.ResumeLayout(false);
            grpGeneral.ResumeLayout(false);
            grpGeneral.PerformLayout();
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private DarkGroupBox grpAchievements;
        private DarkButton btnClearSearch;
        private DarkTextBox txtSearch;
        private Intersect.Editor.Forms.Controls.GameObjectList lstGameObjects;
        private System.Windows.Forms.Panel pnlContainer;
        private DarkGroupBox grpRewards;
        private System.Windows.Forms.Label lblTitleIds;
        private DarkComboBox cmbTitle;
        private DarkButton btnRemoveTitle;
        private DarkButton btnAddTitle;
        private System.Windows.Forms.ListBox lstTitles;
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
        private System.Windows.Forms.Label lblCompletionMode;
        private DarkComboBox cmbCompletionMode;
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
        private DarkToolStrip toolStrip;
        private ToolStripButton toolStripItemNew;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton toolStripItemDelete;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton btnAlphabetical;
        private ToolStripSeparator toolStripSeparator4;
        public ToolStripButton toolStripItemCopy;
        public ToolStripButton toolStripItemPaste;
        private ToolStripSeparator toolStripSeparator3;
        public ToolStripButton toolStripItemUndo;
    }
}
