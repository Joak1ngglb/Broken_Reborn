using System;
using System.Windows.Forms;
using Intersect.Editor.Core;
using Intersect.Editor.Forms.Controls;

namespace Intersect.Editor.Forms;

public sealed class FrmTranslationWorkbench : Form
{
    private readonly TranslationQueueControl _translationControl;
    private string? _pendingEntityType;
    private string? _pendingEntityId;
    private string? _pendingSearchText;

    public FrmTranslationWorkbench(string? entityType = null, string? entityId = null, string? searchText = null)
    {
        Text = "Translation Workbench";
        Icon = Program.Icon;
        MinimumSize = new System.Drawing.Size(960, 600);
        StartPosition = FormStartPosition.CenterParent;
        _pendingEntityType = entityType;
        _pendingEntityId = entityId;
        _pendingSearchText = searchText;

        _translationControl = new TranslationQueueControl
        {
            Dock = DockStyle.Fill,
        };

        Controls.Add(_translationControl);

        Load += OnLoad;
        FormClosed += OnFormClosed;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        ApplyFilter();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        _translationControl.Dispose();
    }

    public void SetFilter(string? entityType, string? entityId, string? searchText = null)
    {
        _pendingEntityType = entityType;
        _pendingEntityId = entityId;
        _pendingSearchText = searchText;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _translationControl.ApplyFilter(_pendingEntityType, _pendingEntityId, _pendingSearchText);
        _pendingEntityType = null;
        _pendingEntityId = null;
        _pendingSearchText = null;
    }
}
