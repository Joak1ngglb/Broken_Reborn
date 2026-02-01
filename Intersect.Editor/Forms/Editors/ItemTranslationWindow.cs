using System;
using System.Linq;
using System.Windows.Forms;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Localization;

namespace Intersect.Editor.Forms.Editors;

public partial class ItemTranslationWindow : Form
{
    private readonly ItemDescriptor? _item;

    public ItemTranslationWindow(ItemDescriptor? item)
    {
        InitializeComponent();
        Icon = Program.Icon;
        _item = item;
        Text = _item == null ? "Item Translation" : $"Item Translation - {_item.Name}";
        txtNameSource.Text = _item?.Name ?? string.Empty;
        txtDescriptionSource.Text = _item?.Description ?? string.Empty;
        BuildLanguageTabs();
    }

    private void BuildLanguageTabs()
    {
        var languages = SupportedLanguages.All.Where(language => !string.Equals(language.Code, "en", StringComparison.OrdinalIgnoreCase));
        foreach (var language in languages)
        {
            AddTranslationTab(tabNameTranslations, language.Label);
            AddTranslationTab(tabDescriptionTranslations, language.Label);
        }
    }

    private static void AddTranslationTab(TabControl tabControl, string label)
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
