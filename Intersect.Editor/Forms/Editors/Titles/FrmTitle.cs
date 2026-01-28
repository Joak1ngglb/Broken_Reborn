using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DarkUI.Forms;
using Intersect.Editor.Core;
using Intersect.Editor.Forms.Editors;
using Intersect.Editor.General;
using Intersect.Editor.Localization;
using Intersect.Editor.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Models;

namespace Intersect.Editor.Forms.Editors.Titles;

public partial class FrmTitle : EditorForm
{
    private readonly List<TitleDescriptor> _changed = [];
    private readonly List<string> _knownFolders = [];
    private TitleDescriptor? _editorItem;
    private string? _copiedItem;
    private bool _updating;

    public FrmTitle()
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
        InitEditor();
        UpdateEditor();
    }

    private void AssignEditorItem(Guid id)
    {
        _editorItem = TitleDescriptor.Get(id);
        UpdateEditor();
    }

    private void InitLocalization()
    {
        Text = Strings.TitleEditor.title;
        toolStripItemNew.Text = Strings.TitleEditor.New;
        toolStripItemDelete.Text = Strings.TitleEditor.delete;
        toolStripItemCopy.Text = Strings.TitleEditor.copy;
        toolStripItemPaste.Text = Strings.TitleEditor.paste;
        toolStripItemUndo.Text = Strings.TitleEditor.undo;

        grpTitles.Text = Strings.TitleEditor.titles;
        grpGeneral.Text = Strings.TitleEditor.general;
        lblName.Text = Strings.TitleEditor.name;
        lblDescription.Text = Strings.TitleEditor.description;
        lblFolder.Text = Strings.TitleEditor.folderlabel;
        btnAddFolder.Text = Strings.TitleEditor.addfolder;

        btnAlphabetical.ToolTipText = Strings.TitleEditor.sortalphabetically;
        txtSearch.Text = Strings.TitleEditor.searchplaceholder;

        btnSave.Text = Strings.TitleEditor.save;
        btnCancel.Text = Strings.TitleEditor.cancel;
    }

    protected override void GameObjectUpdatedDelegate(GameObjectType type)
    {
        if (type == GameObjectType.Title)
        {
            InitEditor();
            if (_editorItem != null && !TitleDescriptor.Lookup.Values.Contains(_editorItem))
            {
                _editorItem = null;
                UpdateEditor();
            }
        }
    }

    private void FrmTitle_FormClosed(object sender, FormClosedEventArgs e)
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
            cmbFolder.Text = _editorItem.Folder ?? string.Empty;

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
            Strings.TitleEditor.folderprompt,
            Strings.TitleEditor.foldertitle,
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

    private void toolStripItemNew_Click(object sender, EventArgs e)
    {
        PacketSender.SendCreateObject(GameObjectType.Title);
    }

    private void toolStripItemDelete_Click(object sender, EventArgs e)
    {
        if (_editorItem != null && lstGameObjects.Focused)
        {
            if (DarkMessageBox.ShowWarning(
                    Strings.TitleEditor.deleteprompt,
                    Strings.TitleEditor.deletetitle,
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
                    Strings.TitleEditor.undoprompt,
                    Strings.TitleEditor.undotitle,
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
        foreach (var title in TitleDescriptor.Lookup)
        {
            if (!string.IsNullOrEmpty(((TitleDescriptor)title.Value).Folder) &&
                !folders.Contains(((TitleDescriptor)title.Value).Folder))
            {
                folders.Add(((TitleDescriptor)title.Value).Folder);
                if (!_knownFolders.Contains(((TitleDescriptor)title.Value).Folder))
                {
                    _knownFolders.Add(((TitleDescriptor)title.Value).Folder);
                }
            }
        }

        folders.Sort();
        _knownFolders.Sort();
        cmbFolder.Items.Clear();
        cmbFolder.Items.Add("");
        cmbFolder.Items.AddRange(_knownFolders.ToArray());

        var items = TitleDescriptor.Lookup
            .OrderBy(p => p.Value?.Name)
            .Select(
                pair => new KeyValuePair<Guid, KeyValuePair<string, string>>(
                    pair.Key,
                    new KeyValuePair<string, string>(
                        ((TitleDescriptor)pair.Value)?.Name ?? DatabaseObject<TitleDescriptor>.Deleted,
                        ((TitleDescriptor)pair.Value)?.Folder ?? ""
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
            txtSearch.Text = Strings.TitleEditor.searchplaceholder;
        }
    }

    private void txtSearch_Enter(object sender, EventArgs e)
    {
        txtSearch.SelectAll();
        txtSearch.Focus();
    }

    private void btnClearSearch_Click(object sender, EventArgs e)
    {
        txtSearch.Text = Strings.TitleEditor.searchplaceholder;
    }

    private bool CustomSearch()
    {
        return !string.IsNullOrWhiteSpace(txtSearch.Text) && txtSearch.Text != Strings.TitleEditor.searchplaceholder;
    }

    private void txtSearch_Click(object sender, EventArgs e)
    {
        if (txtSearch.Text == Strings.TitleEditor.searchplaceholder)
        {
            txtSearch.SelectAll();
        }
    }
}
