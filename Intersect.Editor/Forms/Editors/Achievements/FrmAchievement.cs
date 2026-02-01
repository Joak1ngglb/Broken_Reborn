using System.Linq;
using DarkUI.Forms;
using Intersect.Editor.Core;
using Intersect.Editor.Forms.Editors;
using Intersect.Editor.General;
using Intersect.Editor.Localization;
using Intersect.Editor.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Titles;

namespace Intersect.Editor.Forms.Editors.Achievements;

public partial class FrmAchievement : EditorForm
{
    private readonly List<AchievementDescriptor> _changed = [];
    private readonly List<string> _knownFolders = [];
    private AchievementDescriptor? _editorItem;
    private string? _copiedItem;
    private bool _updating;

    public FrmAchievement()
    {
        ApplyHooks();
        InitializeComponent();
        Icon = Program.Icon;
        _btnSave = btnSave;
        _btnCancel = btnCancel;
     
        lstGameObjects.Init(
            UpdateToolStripItems,
            AssignEditorItem,
            toolStripItemNew_Click,
            toolStripItemCopy_Click,
            toolStripItemUndo_Click,
            toolStripItemPaste_Click,
            toolStripItemDelete_Click
        );

        InitLocalization();
        UpdateEditor();
    }

    private void FrmAchievement_Load(object sender, EventArgs e)
    {
        cmbCategory.Items.Clear();
        cmbCategory.Items.AddRange(
            Enum.GetValues<AchievementCategory>()
                .Select(GetCategoryDisplayName)
                .ToArray()
        );
        cmbDifficulty.Items.Clear();
        cmbDifficulty.Items.AddRange(
            Enum.GetValues<AchievementDifficulty>()
                .Select(GetDifficultyDisplayName)
                .ToArray()
        );
        cmbCompletionMode.Items.Clear();
        cmbCompletionMode.Items.AddRange(Enum.GetNames(typeof(AchievementCompletionMode)));

        cmbResource.Items.Clear();
        cmbResource.Items.AddRange(ItemDescriptor.Names);
        if (cmbResource.Items.Count > 0)
        {
            cmbResource.SelectedIndex = 0;
        }

        nudExperience.Minimum = long.MinValue;
        nudExperience.Maximum = long.MaxValue;
        nudCurrency.Minimum = long.MinValue;
        nudCurrency.Maximum = long.MaxValue;

        InitEditor();
        UpdateEditor();
    }

    private void AssignEditorItem(Guid id)
    {
        _editorItem = AchievementDescriptor.Get(id);
        UpdateEditor();
    }

    private void InitLocalization()
    {
        Text = Strings.AchievementEditor.title;
        toolStripItemNew.Text = Strings.AchievementEditor.New;
        toolStripItemDelete.Text = Strings.AchievementEditor.delete;
        toolStripItemCopy.Text = Strings.AchievementEditor.copy;
        toolStripItemPaste.Text = Strings.AchievementEditor.paste;
        toolStripItemUndo.Text = Strings.AchievementEditor.undo;

        grpAchievements.Text = Strings.AchievementEditor.achievements;
        grpGeneral.Text = Strings.AchievementEditor.general;
        lblName.Text = Strings.AchievementEditor.name;
        lblDescription.Text = Strings.AchievementEditor.description;
        lblCategory.Text = Strings.AchievementEditor.category;
        lblDifficulty.Text = Strings.AchievementEditor.difficulty;
        lblCompletionMode.Text = Strings.AchievementEditor.completionmode;
        lblFolder.Text = Strings.AchievementEditor.folderlabel;

        grpRequirements.Text = Strings.AchievementEditor.requirements;
        btnEditRequirements.Text = Strings.AchievementEditor.editrequirements;

        grpRewards.Text = Strings.AchievementEditor.rewards;
        lblExperience.Text = Strings.AchievementEditor.experience;
        lblCurrency.Text = Strings.AchievementEditor.currency;
        lblResource.Text = Strings.AchievementEditor.resource;
        lblResourceAmount.Text = Strings.AchievementEditor.resourceamount;
        btnAddResource.Text = Strings.AchievementEditor.addresource;
        btnRemoveResource.Text = Strings.AchievementEditor.removeresource;
        lblTitleIds.Text = Strings.AchievementEditor.titleids;
        btnAddTitle.Text = Strings.AchievementEditor.addtitle;
        btnRemoveTitle.Text = Strings.AchievementEditor.removetitle;

        btnAlphabetical.ToolTipText = Strings.AchievementEditor.sortalphabetically;
        txtSearch.Text = Strings.AchievementEditor.searchplaceholder;

        btnSave.Text = Strings.AchievementEditor.save;
        btnCancel.Text = Strings.AchievementEditor.cancel;
    }

    protected override void GameObjectUpdatedDelegate(GameObjectType type)
    {
        if (type == GameObjectType.Achievement)
        {
            InitEditor();
            if (_editorItem != null && !AchievementDescriptor.Lookup.Values.Contains(_editorItem))
            {
                _editorItem = null;
                UpdateEditor();
            }
        }

        if (type == GameObjectType.Title)
        {
            UpdateTitleOptions();
            UpdateTitleRewardsList();
        }
    }

    private void FrmAchievement_FormClosed(object sender, FormClosedEventArgs e)
    {
        btnCancel_Click(null, null);
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        foreach (var item in _changed)
        {
            item.RestoreBackup();
            item.DeleteBackup();
        }

        _editorItem = null;
        Hide();
        Globals.CurrentEditor = -1;
        Dispose();
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        foreach (var item in _changed)
        {
            TranslationSourceUpdater.UpdateEnglishSource(item.Type.ToString(), item.Id, "Name", item.Name);
            TranslationSourceUpdater.UpdateEnglishSource(item.Type.ToString(), item.Id, "Description", item.Description);
            PacketSender.SendSaveObject(item);
            item.DeleteBackup();
        }

        _editorItem = null;
        Hide();
        Globals.CurrentEditor = -1;
        Dispose();
    }

    private void UpdateEditor()
    {
        if (_editorItem != null)
        {
            pnlContainer.Show();
            UpdateEditorButtons(true);
            _updating = true;

            txtName.Text = _editorItem.Name;
            txtDescription.Text = _editorItem.Description;
            cmbCategory.SelectedIndex = (int)_editorItem.Category;
            cmbDifficulty.SelectedIndex = (int)_editorItem.Difficulty;
            cmbCompletionMode.SelectedIndex = (int)_editorItem.CompletionMode;
            cmbFolder.Text = _editorItem.Folder ?? string.Empty;
            nudExperience.Value = _editorItem.Rewards.Experience;
            nudCurrency.Value = _editorItem.Rewards.Currency;

            UpdateTitleOptions();
            UpdateResourceRewardsList();
            UpdateTitleRewardsList();

            if (!_changed.Contains(_editorItem))
            {
                _changed.Add(_editorItem);
                _editorItem.MakeBackup();
            }

            _updating = false;
        }
        else
        {
            _updating = false;
            pnlContainer.Hide();
            UpdateEditorButtons(false);
        }
    }

    private void UpdateResourceRewardsList()
    {
        lstResources.Items.Clear();
        if (_editorItem == null)
        {
            return;
        }

        foreach (var reward in _editorItem.Rewards.Items.OrderBy(entry => ItemDescriptor.GetName(entry.Key)))
        {
            var display = $"{ItemDescriptor.GetName(reward.Key)} x{reward.Value}";
            lstResources.Items.Add(new ItemRewardEntry(reward.Key, display));
        }
    }

    private void UpdateTitleOptions()
    {
        cmbTitle.Items.Clear();
        foreach (var title in TitleDescriptor.Lookup.Values.OfType<TitleDescriptor>().OrderBy(entry => entry.Name))
        {
            cmbTitle.Items.Add(new TitleRewardEntry(title.Id, title.Name));
        }

        if (cmbTitle.Items.Count > 0 && cmbTitle.SelectedIndex < 0)
        {
            cmbTitle.SelectedIndex = 0;
        }
    }

    private void UpdateTitleRewardsList()
    {
        lstTitles.Items.Clear();
        if (_editorItem == null)
        {
            return;
        }

        var validTitleIds = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var titleId in _editorItem.Rewards.TitleIds)
        {
            if (!seen.Add(titleId))
            {
                continue;
            }

            if (!TitleDescriptor.Lookup.Keys.Contains(titleId))
            {
                continue;
            }

            var title = TitleDescriptor.Get(titleId);
            var displayName = title?.Name ?? titleId.ToString();
            lstTitles.Items.Add(new TitleRewardEntry(titleId, displayName));
            validTitleIds.Add(titleId);
        }

        if (!validTitleIds.SequenceEqual(_editorItem.Rewards.TitleIds))
        {
            _editorItem.Rewards.TitleIds = validTitleIds;
        }
    }

    private void txtName_TextChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.Name = txtName.Text;
        lstGameObjects.UpdateText(txtName.Text);
    }

    private void txtDescription_TextChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.Description = txtDescription.Text;
    }

    private void cmbCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.Category = (AchievementCategory)cmbCategory.SelectedIndex;
    }

    private void cmbDifficulty_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.Difficulty = (AchievementDifficulty)cmbDifficulty.SelectedIndex;
    }

    private void cmbCompletionMode_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.CompletionMode = (AchievementCompletionMode)cmbCompletionMode.SelectedIndex;
    }

    private static string GetCategoryDisplayName(AchievementCategory category) =>
        category switch
        {
            AchievementCategory.Dungeons => Strings.AchievementEditor.categorydungeons,
            AchievementCategory.Exploration => Strings.AchievementEditor.categoryexploration,
            AchievementCategory.Monsters => Strings.AchievementEditor.categorymonsters,
            AchievementCategory.Quests => Strings.AchievementEditor.categoryquests,
            AchievementCategory.Professions => Strings.AchievementEditor.categoryprofessions,
            AchievementCategory.Events => Strings.AchievementEditor.categoryevents,
            _ => category.ToString()
        };

    private static string GetDifficultyDisplayName(AchievementDifficulty difficulty) =>
        difficulty switch
        {
            AchievementDifficulty.Discovery => Strings.AchievementEditor.difficultydiscovery,
            AchievementDifficulty.Natural => Strings.AchievementEditor.difficultynatural,
            AchievementDifficulty.Epic => Strings.AchievementEditor.difficultyepic,
            AchievementDifficulty.Meta => Strings.AchievementEditor.difficultymeta,
            _ => difficulty.ToString()
        };

    private void cmbFolder_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.Folder = cmbFolder.Text;
        InitEditor();
    }

    private void btnAddFolder_Click(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        var folderName = string.Empty;
        var result = DarkInputBox.ShowInformation(
            Strings.AchievementEditor.folderprompt,
            Strings.AchievementEditor.foldertitle,
            ref folderName,
            DarkDialogButton.OkCancel
        );

        if (result == DialogResult.OK && !string.IsNullOrEmpty(folderName))
        {
            if (!cmbFolder.Items.Contains(folderName))
            {
                _editorItem.Folder = folderName;
                lstGameObjects.ExpandFolder(folderName);
                InitEditor();
                cmbFolder.Text = folderName;
            }
        }
    }

    private void btnEditRequirements_Click(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        var frm = new FrmDynamicRequirements(_editorItem.Requirements, RequirementType.Achievement);
        frm.ShowDialog();
    }

    private void nudExperience_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.Rewards.Experience = (long)nudExperience.Value;
    }

    private void nudCurrency_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || _updating)
        {
            return;
        }

        _editorItem.Rewards.Currency = (long)nudCurrency.Value;
    }

    private void btnAddResource_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || cmbResource.SelectedIndex < 0)
        {
            return;
        }

        var itemId = ItemDescriptor.IdFromList(cmbResource.SelectedIndex);
        if (itemId == Guid.Empty)
        {
            return;
        }

        _editorItem.Rewards.Items[itemId] = (int)nudResourceAmount.Value;
        UpdateResourceRewardsList();
    }

    private void btnRemoveResource_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || lstResources.SelectedItem is not ItemRewardEntry entry)
        {
            return;
        }

        _editorItem.Rewards.Items.Remove(entry.ItemId);
        UpdateResourceRewardsList();
    }

    private void btnAddTitle_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || cmbTitle.SelectedItem is not TitleRewardEntry entry)
        {
            return;
        }

        if (_editorItem.Rewards.TitleIds.Contains(entry.TitleId))
        {
            return;
        }

        _editorItem.Rewards.TitleIds.Add(entry.TitleId);
        UpdateTitleRewardsList();
    }

    private void btnRemoveTitle_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || lstTitles.SelectedItem is not TitleRewardEntry entry)
        {
            return;
        }

        _editorItem.Rewards.TitleIds.Remove(entry.TitleId);
        UpdateTitleRewardsList();
    }

    private void toolStripItemNew_Click(object sender, EventArgs e)
    {
        PacketSender.SendCreateObject(GameObjectType.Achievement);
    }

    private void toolStripItemDelete_Click(object sender, EventArgs e)
    {
        if (_editorItem != null && lstGameObjects.Focused)
        {
            if (DarkMessageBox.ShowWarning(
                    Strings.AchievementEditor.deleteprompt,
                    Strings.AchievementEditor.deletetitle,
                    DarkDialogButton.YesNo,
                    Icon
                ) == DialogResult.Yes)
            {
                PacketSender.SendDeleteObject(_editorItem);
            }
        }
    }

    private void toolStripItemCopy_Click(object sender, EventArgs e)
    {
        if (_editorItem != null && lstGameObjects.Focused)
        {
            _copiedItem = _editorItem.JsonData;
            toolStripItemPaste.Enabled = true;
        }
    }

    private void toolStripItemPaste_Click(object sender, EventArgs e)
    {
        if (_editorItem != null && _copiedItem != null && lstGameObjects.Focused)
        {
            _editorItem.Load(_copiedItem, true);
            UpdateEditor();
        }
    }

    private void toolStripItemUndo_Click(object sender, EventArgs e)
    {
        if (_changed.Contains(_editorItem) && _editorItem != null)
        {
            if (DarkMessageBox.ShowWarning(
                    Strings.AchievementEditor.undoprompt,
                    Strings.AchievementEditor.undotitle,
                    DarkDialogButton.YesNo,
                    Icon
                ) == DialogResult.Yes)
            {
                _editorItem.RestoreBackup();
                UpdateEditor();
            }
        }
    }

    private void UpdateToolStripItems()
    {
        toolStripItemCopy.Enabled = _editorItem != null && lstGameObjects.Focused;
        toolStripItemPaste.Enabled = _editorItem != null && _copiedItem != null && lstGameObjects.Focused;
        toolStripItemDelete.Enabled = _editorItem != null && lstGameObjects.Focused;
        toolStripItemUndo.Enabled = _editorItem != null && lstGameObjects.Focused;
    }

    private void form_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Control)
        {
            if (e.KeyCode == Keys.N)
            {
                toolStripItemNew_Click(null, null);
            }
        }
    }

    public void InitEditor()
    {
        var folders = new List<string>();
        foreach (var achievement in AchievementDescriptor.Lookup)
        {
            if (!string.IsNullOrEmpty(((AchievementDescriptor)achievement.Value).Folder) &&
                !folders.Contains(((AchievementDescriptor)achievement.Value).Folder))
            {
                folders.Add(((AchievementDescriptor)achievement.Value).Folder);
                if (!_knownFolders.Contains(((AchievementDescriptor)achievement.Value).Folder))
                {
                    _knownFolders.Add(((AchievementDescriptor)achievement.Value).Folder);
                }
            }
        }

        folders.Sort();
        _knownFolders.Sort();
        cmbFolder.Items.Clear();
        cmbFolder.Items.Add("");
        cmbFolder.Items.AddRange(_knownFolders.ToArray());

        var items = AchievementDescriptor.Lookup
            .OrderBy(p => p.Value?.Name)
            .Select(
                pair => new KeyValuePair<Guid, KeyValuePair<string, string>>(
                    pair.Key,
                    new KeyValuePair<string, string>(
                        ((AchievementDescriptor)pair.Value)?.Name ?? Models.DatabaseObject<AchievementDescriptor>.Deleted,
                        ((AchievementDescriptor)pair.Value)?.Folder ?? ""
                    )
                )
            )
            .ToArray();

        lstGameObjects.Repopulate(items, folders, btnAlphabetical.Checked, CustomSearch(), txtSearch.Text);
    }

    private void btnAlphabetical_Click(object sender, EventArgs e)
    {
        btnAlphabetical.Checked = !btnAlphabetical.Checked;
        InitEditor();
    }

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        InitEditor();
    }

    private void txtSearch_Leave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSearch.Text))
        {
            txtSearch.Text = Strings.AchievementEditor.searchplaceholder;
        }
    }

    private void txtSearch_Enter(object sender, EventArgs e)
    {
        txtSearch.SelectAll();
        txtSearch.Focus();
    }

    private void btnClearSearch_Click(object sender, EventArgs e)
    {
        txtSearch.Text = Strings.AchievementEditor.searchplaceholder;
    }

    private bool CustomSearch()
    {
        return !string.IsNullOrWhiteSpace(txtSearch.Text) && txtSearch.Text != Strings.AchievementEditor.searchplaceholder;
    }

    private void txtSearch_Click(object sender, EventArgs e)
    {
        if (txtSearch.Text == Strings.AchievementEditor.searchplaceholder)
        {
            txtSearch.SelectAll();
        }
    }

    private sealed class ItemRewardEntry
    {
        public ItemRewardEntry(Guid itemId, string display)
        {
            ItemId = itemId;
            Display = display;
        }

        public Guid ItemId { get; }
        public string Display { get; }

        public override string ToString() => Display;
    }

    private sealed class TitleRewardEntry
    {
        public TitleRewardEntry(Guid titleId, string display)
        {
            TitleId = titleId;
            Display = display;
        }

        public Guid TitleId { get; }
        public string Display { get; }

        public override string ToString() => Display;
    }
}
