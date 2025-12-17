using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DarkUI.Controls;
using DarkUI.Forms;
using Intersect.Editor.Core;
using Intersect.Editor.Forms.Controls;
using Intersect.Editor.Localization;
using Intersect.Editor.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Fishing;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;

namespace Intersect.Editor.Forms.Editors;

public partial class FrmFishingSpots : EditorForm
{
    private readonly Dictionary<Guid, FishingSpotBase> _changed = new();
    private FishingSpotBase? _editorItem;
    private string? _copiedItem;

    public FrmFishingSpots()
    {
        ApplyHooks();
        InitializeComponent();
        Icon = Program.Icon;

        _btnSave = btnSave;
        _btnCancel = btnCancel;

        lstGameObjects.Init(UpdateToolStripItems, AssignEditorItem,
            toolStripItemNew_Click, toolStripItemCopy_Click,
            toolStripItemUndo_Click, toolStripItemPaste_Click, toolStripItemDelete_Click);

        cmbHooks.Items.Add(Strings.General.None);
        cmbHooks.Items.AddRange(ItemDescriptor.Names);
    }

    public void InitEditor()
    {
        RefreshFolderList();
        RefreshList();
    }

    private void RefreshFolderList()
    {
        cmbFolder.Items.Clear();
        cmbFolder.Items.Add(Strings.General.None);

        foreach (var folder in FishingSpotBase.Lookup.Values.Select(f => f?.Folder ?? string.Empty).Where(f => !string.IsNullOrEmpty(f)).Distinct().OrderBy(s => s))
        {
            cmbFolder.Items.Add(folder);
        }
    }

    private void RefreshList()
    {
        var items = FishingSpotBase.Lookup
            .Select(pair => new KeyValuePair<Guid, KeyValuePair<string, string>>(pair.Key,
                new KeyValuePair<string, string>(pair.Value?.Name ?? FishingSpotBase.Deleted, pair.Value?.Folder ?? string.Empty)))
            .ToArray();

        var folders = FishingSpotBase.Lookup.Values
            .Select(f => f?.Folder ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        lstGameObjects.Repopulate(items, folders, false, false, string.Empty);
    }

    private void AssignEditorItem(Guid id)
    {
        if (!FishingSpotBase.TryGet(id, out _editorItem))
        {
            _editorItem = null;
        }

        UpdateEditor();
    }

    private void UpdateEditor()
    {
        var hasItem = _editorItem != null;
        UpdateEditorButtons(hasItem);

        if (!hasItem || _editorItem == null)
        {
            return;
        }

        EnsureBackup();

        txtName.Text = _editorItem.Name;
        cmbFolder.Text = string.IsNullOrEmpty(_editorItem.Folder) ? Strings.General.None : _editorItem.Folder;
        nudChance.Value = _editorItem.Chance;
        nudStrength.Value = _editorItem.Strength;
        nudSpeed.Value = _editorItem.Speed;

        RefreshHooks();
    }

    private void RefreshHooks()
    {
        lstHooks.Items.Clear();
        if (_editorItem == null)
        {
            return;
        }

        foreach (var hook in _editorItem.Hooks)
        {
            lstHooks.Items.Add(ItemDescriptor.GetName(hook));
        }
    }

    private void EnsureBackup()
    {
        if (_editorItem == null || _changed.ContainsKey(_editorItem.Id))
        {
            return;
        }

        _editorItem.MakeBackup();
        _changed[_editorItem.Id] = _editorItem;
    }

    private void toolStripItemNew_Click(object? sender, EventArgs? e)
    {
        PacketSender.SendCreateObject(GameObjectType.FishingSpot);
    }

    private void toolStripItemCopy_Click(object? sender, EventArgs? e)
    {
        if (_editorItem != null && lstGameObjects.Focused)
        {
            _copiedItem = _editorItem.JsonData;
            toolStripItemPaste.Enabled = true;
        }
    }

    private void toolStripItemUndo_Click(object? sender, EventArgs? e)
    {
        if (_editorItem == null || !_changed.ContainsKey(_editorItem.Id))
        {
            return;
        }

        if (DarkMessageBox.ShowWarning(Strings.FishingSpotEditor.UndoPrompt, Strings.FishingSpotEditor.UndoTitle, DarkDialogButton.YesNo, Icon) == DialogResult.No)
        {
            return;
        }

        _editorItem.RestoreBackup();
        UpdateEditor();
    }

    private void toolStripItemPaste_Click(object? sender, EventArgs? e)
    {
        if (_editorItem != null && _copiedItem != null && lstGameObjects.Focused)
        {
            _editorItem.Load(_copiedItem, true);
            UpdateEditor();
        }
    }

    private void toolStripItemDelete_Click(object? sender, EventArgs? e)
    {
        if (_editorItem == null || !lstGameObjects.Focused)
        {
            return;
        }

        if (DarkMessageBox.ShowWarning(Strings.FishingSpotEditor.DeletePrompt, Strings.FishingSpotEditor.DeleteTitle, DarkDialogButton.YesNo, Icon) == DialogResult.Yes)
        {
            PacketSender.SendDeleteObject(_editorItem);
        }
    }

    private void UpdateToolStripItems()
    {
        var enable = _editorItem != null && lstGameObjects.Focused;
        toolStripItemCopy.Enabled = enable;
        toolStripItemPaste.Enabled = enable && _copiedItem != null;
        toolStripItemDelete.Enabled = enable;
        toolStripItemUndo.Enabled = enable;
    }

    protected override void GameObjectUpdatedDelegate(GameObjectType type)
    {
        if (type == GameObjectType.FishingSpot)
        {
            InitEditor();
            if (_editorItem != null && !FishingSpotBase.Lookup.Values.Contains(_editorItem))
            {
                _editorItem = null;
                UpdateEditor();
            }
        }
    }

    private void txtName_TextChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        EnsureBackup();
        _editorItem.Name = txtName.Text;
        lstGameObjects.UpdateText(txtName.Text);
    }

    private void cmbFolder_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        EnsureBackup();
        _editorItem.Folder = cmbFolder.Text == Strings.General.None ? string.Empty : cmbFolder.Text;
    }

    private void nudChance_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        EnsureBackup();
        _editorItem.Chance = (int)nudChance.Value;
    }

    private void nudStrength_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        EnsureBackup();
        _editorItem.Strength = (int)nudStrength.Value;
    }

    private void nudSpeed_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        EnsureBackup();
        _editorItem.Speed = (int)nudSpeed.Value;
    }

    private void btnAddHook_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || cmbHooks.SelectedIndex < 1)
        {
            return;
        }

        EnsureBackup();
        var id = ItemDescriptor.IdFromList(cmbHooks.SelectedIndex - 1);
        if (id == Guid.Empty || _editorItem.Hooks.Contains(id))
        {
            return;
        }

        _editorItem.Hooks.Add(id);
        RefreshHooks();
    }

    private void btnRemoveHook_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || lstHooks.SelectedIndex < 0)
        {
            return;
        }

        EnsureBackup();
        var hookId = _editorItem.Hooks.ElementAtOrDefault(lstHooks.SelectedIndex);
        if (hookId != Guid.Empty)
        {
            _editorItem.Hooks.Remove(hookId);
            RefreshHooks();
        }
    }

    private void btnRequirements_Click(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        var frm = new FrmDynamicRequirements(_editorItem.Requirements, RequirementType.Resource);
        frm.ShowDialog();
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        foreach (var item in _changed.Values)
        {
            PacketSender.SendSaveObject(item);
            item.DeleteBackup();
        }

        Hide();
        Globals.CurrentEditor = -1;
        Dispose();
    }

    private void btnCancel_Click(object? sender, EventArgs? e)
    {
        foreach (var item in _changed.Values)
        {
            item.RestoreBackup();
            item.DeleteBackup();
        }

        Hide();
        Globals.CurrentEditor = -1;
        Dispose();
    }

    private void InitializeComponent()
    {
        toolStrip = new ToolStrip();
        toolStripItemNew = new ToolStripButton();
        toolStripItemCopy = new ToolStripButton();
        toolStripItemUndo = new ToolStripButton();
        toolStripItemPaste = new ToolStripButton();
        toolStripItemDelete = new ToolStripButton();
        lstGameObjects = new GameObjectList();
        lblName = new DarkLabel();
        txtName = new DarkTextBox();
        lblFolder = new DarkLabel();
        cmbFolder = new DarkComboBox();
        lblChance = new DarkLabel();
        nudChance = new NumericUpDown();
        lblStrength = new DarkLabel();
        nudStrength = new NumericUpDown();
        lblSpeed = new DarkLabel();
        nudSpeed = new NumericUpDown();
        lblHooks = new DarkLabel();
        lstHooks = new ListBox();
        cmbHooks = new DarkComboBox();
        btnAddHook = new DarkButton();
        btnRemoveHook = new DarkButton();
        btnRequirements = new DarkButton();
        btnSave = new DarkButton();
        btnCancel = new DarkButton();

        toolStrip.Items.AddRange(new ToolStripItem[]
        {
            toolStripItemNew,
            toolStripItemCopy,
            toolStripItemUndo,
            toolStripItemPaste,
            toolStripItemDelete
        });

        toolStripItemNew.Text = Strings.General.New;
        toolStripItemNew.Click += toolStripItemNew_Click;
        toolStripItemCopy.Text = Strings.General.Copy;
        toolStripItemCopy.Click += toolStripItemCopy_Click;
        toolStripItemUndo.Text = Strings.General.Undo;
        toolStripItemUndo.Click += toolStripItemUndo_Click;
        toolStripItemPaste.Text = Strings.General.Paste;
        toolStripItemPaste.Click += toolStripItemPaste_Click;
        toolStripItemDelete.Text = Strings.General.Delete;
        toolStripItemDelete.Click += toolStripItemDelete_Click;

        toolStripItemPaste.Enabled = false;
        toolStripItemCopy.Enabled = false;
        toolStripItemUndo.Enabled = false;
        toolStripItemDelete.Enabled = false;

        lstGameObjects.Location = new Point(12, 40);
        lstGameObjects.Size = new Size(220, 380);

        lblName.Location = new Point(250, 50);
        lblName.Text = Strings.General.Name;
        txtName.Location = new Point(330, 45);
        txtName.Size = new Size(220, 23);
        txtName.TextChanged += txtName_TextChanged;

        lblFolder.Location = new Point(250, 85);
        lblFolder.Text = Strings.General.Folder;
        cmbFolder.Location = new Point(330, 80);
        cmbFolder.Size = new Size(220, 23);
        cmbFolder.SelectedIndexChanged += cmbFolder_SelectedIndexChanged;

        lblChance.Location = new Point(250, 120);
        lblChance.Text = Strings.FishingSpotEditor.Chance;
        nudChance.Location = new Point(330, 115);
        nudChance.Maximum = 1000;
        nudChance.ValueChanged += nudChance_ValueChanged;

        lblStrength.Location = new Point(250, 150);
        lblStrength.Text = Strings.FishingSpotEditor.Strength;
        nudStrength.Location = new Point(330, 145);
        nudStrength.Maximum = 1000;
        nudStrength.ValueChanged += nudStrength_ValueChanged;

        lblSpeed.Location = new Point(250, 180);
        lblSpeed.Text = Strings.FishingSpotEditor.Speed;
        nudSpeed.Location = new Point(330, 175);
        nudSpeed.Maximum = 1000;
        nudSpeed.ValueChanged += nudSpeed_ValueChanged;

        lblHooks.Location = new Point(250, 215);
        lblHooks.Text = Strings.FishingSpotEditor.Hooks;
        lstHooks.Location = new Point(250, 240);
        lstHooks.Size = new Size(200, 120);

        cmbHooks.Location = new Point(470, 240);
        cmbHooks.Size = new Size(180, 23);

        btnAddHook.Location = new Point(470, 270);
        btnAddHook.Text = Strings.FishingSpotEditor.AddHook;
        btnAddHook.Click += btnAddHook_Click;

        btnRemoveHook.Location = new Point(470, 300);
        btnRemoveHook.Text = Strings.FishingSpotEditor.RemoveHook;
        btnRemoveHook.Click += btnRemoveHook_Click;

        btnRequirements.Location = new Point(250, 370);
        btnRequirements.Size = new Size(220, 30);
        btnRequirements.Text = Strings.FishingSpotEditor.Requirements;
        btnRequirements.Click += btnRequirements_Click;

        btnSave.Location = new Point(600, 420);
        btnSave.Size = new Size(90, 30);
        btnSave.Text = Strings.General.Save;
        btnSave.Click += btnSave_Click;

        btnCancel.Location = new Point(700, 420);
        btnCancel.Size = new Size(90, 30);
        btnCancel.Text = Strings.General.Cancel;
        btnCancel.Click += btnCancel_Click;

        Controls.Add(toolStrip);
        Controls.Add(lstGameObjects);
        Controls.Add(lblName);
        Controls.Add(txtName);
        Controls.Add(lblFolder);
        Controls.Add(cmbFolder);
        Controls.Add(lblChance);
        Controls.Add(nudChance);
        Controls.Add(lblStrength);
        Controls.Add(nudStrength);
        Controls.Add(lblSpeed);
        Controls.Add(nudSpeed);
        Controls.Add(lblHooks);
        Controls.Add(lstHooks);
        Controls.Add(cmbHooks);
        Controls.Add(btnAddHook);
        Controls.Add(btnRemoveHook);
        Controls.Add(btnRequirements);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);

        Text = Strings.FishingSpotEditor.Title;
        ClientSize = new Size(820, 470);
    }

    private ToolStrip toolStrip;
    private ToolStripButton toolStripItemNew;
    private ToolStripButton toolStripItemCopy;
    private ToolStripButton toolStripItemUndo;
    private ToolStripButton toolStripItemPaste;
    private ToolStripButton toolStripItemDelete;
    private GameObjectList lstGameObjects;
    private DarkLabel lblName;
    private DarkTextBox txtName;
    private DarkLabel lblFolder;
    private DarkComboBox cmbFolder;
    private DarkLabel lblChance;
    private NumericUpDown nudChance;
    private DarkLabel lblStrength;
    private NumericUpDown nudStrength;
    private DarkLabel lblSpeed;
    private NumericUpDown nudSpeed;
    private DarkLabel lblHooks;
    private ListBox lstHooks;
    private DarkComboBox cmbHooks;
    private DarkButton btnAddHook;
    private DarkButton btnRemoveHook;
    private DarkButton btnRequirements;
    private DarkButton btnSave;
    private DarkButton btnCancel;
}
