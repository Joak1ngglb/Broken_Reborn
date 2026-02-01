using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.Localization;
using Intersect.Editor.Localization;
using Intersect.Localization;

namespace Intersect.Editor.Forms.Editors;

public partial class ItemTranslationWindow : Form
{
    private const string AllFoldersLabel = "All";
    private readonly IReadOnlyList<ItemDescriptor> _itemBases;
    private readonly Dictionary<string, DarkUI.Controls.DarkTextBox> _nameTranslationBoxes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DarkUI.Controls.DarkTextBox> _descriptionTranslationBoxes =
        new(StringComparer.OrdinalIgnoreCase);
    private Guid? _selectedItemId;

    private readonly Guid? _initialItemId;

    public ItemTranslationWindow(ItemDescriptor? item)
    {
        InitializeComponent();
        Icon = Program.Icon;
        _initialItemId = item?.Id;
        Text = "Item Translation";
        txtNameSource.Text = string.Empty;
        txtDescriptionSource.Text = string.Empty;
        BuildLanguageTabs();
        _itemBases = LoadItemBases();
        PopulateFolderFilter();
        WireEvents();
        ApplyFilters();
        if (_initialItemId.HasValue)
        {
            SelectItemById(_initialItemId.Value);
        }

        TranslationRepository.Default.PendingTranslationsUpdated += OnPendingTranslationsUpdated;
    }

    private void BuildLanguageTabs()
    {
        var languages = SupportedLanguages.All.Where(language => !string.Equals(language.Code, "en", StringComparison.OrdinalIgnoreCase));
        foreach (var language in languages)
        {
            _nameTranslationBoxes[language.Code] = AddTranslationTab(tabNameTranslations, language.Label);
            _descriptionTranslationBoxes[language.Code] = AddTranslationTab(tabDescriptionTranslations, language.Label);
        }
    }

    private static DarkUI.Controls.DarkTextBox AddTranslationTab(TabControl tabControl, string label)
    {
        var tabPage = new TabPage(label)
        {
            BackColor = System.Drawing.Color.FromArgb(45, 45, 48),
        };

        var textBox = new DarkUI.Controls.DarkTextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };

        tabPage.Controls.Add(textBox);
        tabControl.TabPages.Add(tabPage);
        return textBox;
    }

    private static IReadOnlyList<ItemDescriptor> LoadItemBases()
    {
        return ItemDescriptor.Lookup.Values
            .OfType<ItemDescriptor>()
            .Select(item => new ItemDescriptor(item.Id)
            {
                Name = item.Name,
                Folder = item.Folder,
            })
            .OrderBy(item => item.Name)
            .ToList();
    }

    private void WireEvents()
    {
        txtSearch.TextChanged += (_, _) => ApplyFilters();
        cmbFolder.SelectedIndexChanged += (_, _) => ApplyFilters();
        lstItems.SelectedIndexChanged += (_, _) => OnItemSelected();
        FormClosed += (_, _) => TranslationRepository.Default.PendingTranslationsUpdated -= OnPendingTranslationsUpdated;
    }

    private void PopulateFolderFilter()
    {
        var folders = _itemBases
            .Select(item => item.Folder ?? string.Empty)
            .Where(folder => !string.IsNullOrWhiteSpace(folder))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(folder => folder)
            .ToList();

        cmbFolder.Items.Clear();
        cmbFolder.Items.Add(AllFoldersLabel);
        foreach (var folder in folders)
        {
            cmbFolder.Items.Add(folder);
        }

        cmbFolder.SelectedIndex = 0;
    }

    private void ApplyFilters()
    {
        var search = txtSearch.Text?.Trim() ?? string.Empty;
        var selectedFolder = cmbFolder.SelectedItem as string;

        var filtered = _itemBases
            .Where(item => string.IsNullOrWhiteSpace(selectedFolder) ||
                string.Equals(selectedFolder, AllFoldersLabel, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Folder ?? string.Empty, selectedFolder, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(search) ||
                item.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        lstItems.BeginUpdate();
        lstItems.DisplayMember = nameof(ItemDescriptor.Name);
        lstItems.Items.Clear();
        foreach (var item in filtered)
        {
            lstItems.Items.Add(item);
        }
        lstItems.EndUpdate();

        if (_selectedItemId.HasValue)
        {
            SelectItemById(_selectedItemId.Value);
        }
    }

    private void SelectItemById(Guid itemId)
    {
        for (var i = 0; i < lstItems.Items.Count; i++)
        {
            if (lstItems.Items[i] is ItemDescriptor item && item.Id == itemId)
            {
                lstItems.SelectedIndex = i;
                return;
            }
        }
    }

    private void OnItemSelected()
    {
        if (lstItems.SelectedItem is not ItemDescriptor selected)
        {
            return;
        }

        _selectedItemId = selected.Id;
        var fullItem = ItemDescriptor.Get(selected.Id);
        txtNameSource.Text = fullItem?.Name ?? selected.Name ?? string.Empty;
        txtDescriptionSource.Text = fullItem?.Description ?? string.Empty;
        Text = $"Item Translation - {txtNameSource.Text}";

        ClearTranslationBoxes();
        RequestTranslations(selected.Id);
    }

    private void ClearTranslationBoxes()
    {
        foreach (var textBox in _nameTranslationBoxes.Values)
        {
            textBox.Text = string.Empty;
        }

        foreach (var textBox in _descriptionTranslationBoxes.Values)
        {
            textBox.Text = string.Empty;
        }
    }

    private void RequestTranslations(Guid itemId)
    {
        TranslationRepository.Default.RequestPending(
            LocalizationEntityTypes.Item,
            itemId.ToString(),
            null,
            null,
            null,
            200,
            0
        );
    }

    private void OnPendingTranslationsUpdated(IReadOnlyList<TranslationPendingEntry> entries, long totalCount)
    {
        if (IsDisposed || !_selectedItemId.HasValue)
        {
            return;
        }

        var itemId = _selectedItemId.Value.ToString();
        var relevant = entries
            .Where(entry => string.Equals(entry.EntityType, LocalizationEntityTypes.Item, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entry.EntityId, itemId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (relevant.Count == 0)
        {
            return;
        }

        foreach (var entry in relevant)
        {
            if (string.Equals(entry.Field, "Name", StringComparison.OrdinalIgnoreCase) &&
                _nameTranslationBoxes.TryGetValue(entry.Language, out var nameBox))
            {
                nameBox.Text = entry.TranslatedText ?? string.Empty;
                continue;
            }

            if (string.Equals(entry.Field, "Description", StringComparison.OrdinalIgnoreCase) &&
                _descriptionTranslationBoxes.TryGetValue(entry.Language, out var descriptionBox))
            {
                descriptionBox.Text = entry.TranslatedText ?? string.Empty;
            }
        }
    }

    private void btnCopyNameSource_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(txtNameSource.Text))
        {
            Clipboard.SetText(txtNameSource.Text);
        }
    }

    private void btnCopyDescriptionSource_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(txtDescriptionSource.Text))
        {
            Clipboard.SetText(txtDescriptionSource.Text);
        }
    }
}
