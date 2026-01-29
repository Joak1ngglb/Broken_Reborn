using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Intersect.Editor.Forms.WpfWindows;

namespace Intersect.Editor.Forms;

public sealed class FrmTranslationWorkbench : Form
{
    private readonly ElementHost _elementHost;
    private TranslationQueueWindow? _translationWindow;
    private string? _pendingEntityType;
    private string? _pendingEntityId;
    private string? _pendingSearchText;

    public FrmTranslationWorkbench(string? entityType = null, string? entityId = null, string? searchText = null)
    {
        Text = "Translation Workbench";
        Icon = Program.Icon;
        MinimumSize = new Size(960, 600);
        StartPosition = FormStartPosition.CenterParent;
        _pendingEntityType = entityType;
        _pendingEntityId = entityId;
        _pendingSearchText = searchText;

        _elementHost = new ElementHost
        {
            Dock = DockStyle.Fill,
        };

        Controls.Add(_elementHost);

        Load += OnLoad;
        Activated += OnActivated;
        FormClosed += OnFormClosed;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        if (_translationWindow != null)
        {
            return;
        }

        _translationWindow = new TranslationQueueWindow
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
        };
        _translationWindow.Closed += TranslationWindowOnClosed;

        _elementHost.Child = _translationWindow;
        ApplyFilter();
        _translationWindow.Focus();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        _elementHost.Focus();
        _translationWindow?.Focus();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        if (_translationWindow != null)
        {
            _translationWindow.Closed -= TranslationWindowOnClosed;
            _translationWindow.Close();
            _translationWindow = null;
        }

        _elementHost.Child = null;
        _elementHost.Dispose();
    }

    private void TranslationWindowOnClosed(object? sender, EventArgs e)
    {
        if (!IsDisposed)
        {
            Close();
        }
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
        if (_translationWindow == null)
        {
            return;
        }

        _translationWindow.ApplyFilter(_pendingEntityType, _pendingEntityId, _pendingSearchText);
        _pendingEntityType = null;
        _pendingEntityId = null;
        _pendingSearchText = null;
    }
}
