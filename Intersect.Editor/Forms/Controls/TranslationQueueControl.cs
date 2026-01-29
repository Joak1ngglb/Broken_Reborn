using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DarkUI.Controls;
using Intersect.Editor.Forms.WpfWindows;
using Intersect.Editor.Localization;
using Intersect.Framework.Core.Localization;

namespace Intersect.Editor.Forms.Controls;

public sealed partial class TranslationQueueControl : UserControl
{
    private readonly TranslationQueueViewModel _viewModel;
    private readonly Dictionary<TranslationEntryViewModel, TranslationTabView> _tabViews = new();
    private bool _updatingFromViewModel;

    public TranslationQueueControl()
    {
        InitializeComponent();
        _viewModel = new TranslationQueueViewModel();
        BindFilters();
        HookEvents();
        RefreshEntryList();
        UpdateSelectedEntry();
        UpdateCommandButtons();
    }

    public void ApplyFilter(string? entityType, string? entityId, string? searchText = null)
    {
        _viewModel.ApplyFilter(entityType, entityId, searchText);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnhookTranslations();
            _viewModel.FilteredEntries.CollectionChanged -= OnEntriesCollectionChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            UnhookCommandEvents();
            _viewModel.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BindFilters()
    {
        cmbEntityType.DataSource = _viewModel.EntityTypes.ToList();
        cmbStatus.DataSource = _viewModel.Statuses.ToList();
        cmbScope.DataSource = _viewModel.Scopes.ToList();

        SetFilterSelection(cmbEntityType, _viewModel.SelectedEntityType);
        SetFilterSelection(cmbStatus, _viewModel.SelectedStatus);
        SetFilterSelection(cmbScope, _viewModel.SelectedScope);
        txtSearch.Text = _viewModel.SearchText;
    }

    private void HookEvents()
    {
        _viewModel.FilteredEntries.CollectionChanged += OnEntriesCollectionChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        cmbEntityType.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingFromViewModel)
            {
                return;
            }

            _viewModel.SelectedEntityType = cmbEntityType.SelectedItem as FilterOption<string?>;
        };

        cmbStatus.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingFromViewModel)
            {
                return;
            }

            _viewModel.SelectedStatus = cmbStatus.SelectedItem as FilterOption<TranslationStatus?>;
        };

        cmbScope.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingFromViewModel)
            {
                return;
            }

            _viewModel.SelectedScope = cmbScope.SelectedItem as FilterOption<string?>;
        };

        txtSearch.TextChanged += (_, _) =>
        {
            if (_updatingFromViewModel)
            {
                return;
            }

            _viewModel.SearchText = txtSearch.Text;
        };

        lstEntries.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingFromViewModel)
            {
                return;
            }

            _viewModel.SelectedEntry = lstEntries.SelectedItem as TranslationQueueEntryViewModel;
        };

        lstEntries.DrawItem += LstEntriesOnDrawItem;

        btnBroken.Click += (_, _) => ExecuteCommand(_viewModel.SetBrokenStatusCommand);
        btnImport.Click += (_, _) => ExecuteCommand(_viewModel.ImportCsvCommand);
        btnExport.Click += (_, _) => ExecuteCommand(_viewModel.ExportCsvCommand);
        btnPrev.Click += (_, _) => ExecuteCommand(_viewModel.PreviousCommand);
        btnNext.Click += (_, _) => ExecuteCommand(_viewModel.NextCommand);

        tabTranslations.SelectedIndexChanged += (_, _) => UpdateActiveTranslation();

        btnCopySource.Click += (_, _) => ExecuteEntryCommand(entry => entry.CopySourceCommand);
        btnQuickPaste.Click += (_, _) => ExecuteEntryCommand(entry => entry.QuickPasteCommand);
        btnSave.Click += (_, _) => ExecuteEntryCommand(entry => entry.SaveCommand);
        btnSaveNext.Click += (_, _) => ExecuteEntryCommand(entry => entry.SaveAndNextCommand);

        HookCommandEvents();
    }

    private void HookCommandEvents()
    {
        if (_viewModel.PreviousCommand is RelayCommand previousCommand)
        {
            previousCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
        }

        if (_viewModel.NextCommand is RelayCommand nextCommand)
        {
            nextCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
        }

        if (_viewModel.ExportCsvCommand is RelayCommand exportCommand)
        {
            exportCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
        }
    }

    private void UnhookCommandEvents()
    {
        if (_viewModel.PreviousCommand is RelayCommand previousCommand)
        {
            previousCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;
        }

        if (_viewModel.NextCommand is RelayCommand nextCommand)
        {
            nextCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;
        }

        if (_viewModel.ExportCsvCommand is RelayCommand exportCommand)
        {
            exportCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;
        }
    }

    private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
    {
        UpdateCommandButtons();
    }

    private void OnEntriesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshEntryList();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(TranslationQueueViewModel.SelectedEntry), StringComparison.Ordinal))
        {
            UpdateSelectedEntry();
            return;
        }

        if (string.Equals(e.PropertyName, nameof(TranslationQueueViewModel.SelectedEntityType), StringComparison.Ordinal))
        {
            SetFilterSelection(cmbEntityType, _viewModel.SelectedEntityType);
            return;
        }

        if (string.Equals(e.PropertyName, nameof(TranslationQueueViewModel.SelectedStatus), StringComparison.Ordinal))
        {
            SetFilterSelection(cmbStatus, _viewModel.SelectedStatus);
            return;
        }

        if (string.Equals(e.PropertyName, nameof(TranslationQueueViewModel.SelectedScope), StringComparison.Ordinal))
        {
            SetFilterSelection(cmbScope, _viewModel.SelectedScope);
            return;
        }

        if (string.Equals(e.PropertyName, nameof(TranslationQueueViewModel.SearchText), StringComparison.Ordinal))
        {
            _updatingFromViewModel = true;
            try
            {
                txtSearch.Text = _viewModel.SearchText;
            }
            finally
            {
                _updatingFromViewModel = false;
            }
        }
    }

    private void RefreshEntryList()
    {
        _updatingFromViewModel = true;
        try
        {
            lstEntries.BeginUpdate();
            lstEntries.DataSource = null;
            lstEntries.Items.Clear();
            lstEntries.Items.AddRange(_viewModel.FilteredEntries.Cast<object>().ToArray());
        }
        finally
        {
            lstEntries.EndUpdate();
            _updatingFromViewModel = false;
        }
    }

    private void UpdateSelectedEntry()
    {
        _updatingFromViewModel = true;
        try
        {
            if (_viewModel.SelectedEntry != null)
            {
                lstEntries.SelectedItem = _viewModel.SelectedEntry;
            }
            else
            {
                lstEntries.SelectedIndex = -1;
            }
        }
        finally
        {
            _updatingFromViewModel = false;
        }

        BuildTranslationTabs();
        UpdateActiveTranslation();
    }

    private void BuildTranslationTabs()
    {
        UnhookTranslations();
        tabTranslations.TabPages.Clear();
        _tabViews.Clear();

        if (_viewModel.SelectedEntry == null)
        {
            txtSource.Text = string.Empty;
            lblStatusValue.Text = "";
            UpdateCommandButtons();
            return;
        }

        foreach (var translation in _viewModel.SelectedEntry.TranslationEntries)
        {
            var tabView = CreateTranslationTab(translation);
            _tabViews[translation] = tabView;
            tabTranslations.TabPages.Add(tabView.TabPage);
            translation.PropertyChanged += OnTranslationPropertyChanged;
        }

        if (tabTranslations.TabPages.Count > 0)
        {
            tabTranslations.SelectedIndex = 0;
        }
    }

    private TranslationTabView CreateTranslationTab(TranslationEntryViewModel translation)
    {
        var tabPage = new TabPage(translation.LanguageName)
        {
            BackColor =System.Drawing.Color.FromArgb(45, 45, 48),
            ForeColor = System.Drawing.Color.Gainsboro,
            Padding = new Padding(3),
            Tag = translation,
        };

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 3,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblStatus = new Label
        {
            AutoSize = true,
            ForeColor = System.Drawing.Color.Gainsboro,
            Text = $"Status: {translation.Status}",
        };

        var lblWarning = new Label
        {
            AutoSize = true,
            ForeColor = System.Drawing.Color.Orange,
            Text = "Warning: translation needs review (stale).",
            Visible = translation.Status == TranslationStatus.NeedsReview,
            Margin = new Padding(0, 4, 0, 0),
        };

        var statusPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        statusPanel.Controls.Add(lblStatus);
        statusPanel.Controls.Add(lblWarning);

        var txtTranslation = new DarkTextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = translation.TranslationText,
            MinimumSize = new Size(0, 160),
        };
        txtTranslation.TextChanged += (_, _) =>
        {
            if (_updatingFromViewModel)
            {
                return;
            }

            translation.TranslationText = txtTranslation.Text;
        };

        var lblFallback = new Label
        {
            AutoSize = true,
            ForeColor = System.Drawing.Color.Gray,
            Text = translation.FallbackText,
            Margin = new Padding(0, 6, 0, 0),
        };

        layout.Controls.Add(statusPanel, 0, 0);
        layout.Controls.Add(txtTranslation, 0, 1);
        layout.Controls.Add(lblFallback, 0, 2);

        tabPage.Controls.Add(layout);

        return new TranslationTabView(tabPage, lblStatus, lblWarning, txtTranslation, lblFallback);
    }

    private void UpdateActiveTranslation()
    {
        var translation = GetActiveTranslation();
        if (translation == null)
        {
            txtSource.Text = string.Empty;
            lblStatusValue.Text = string.Empty;
            UpdateCommandButtons();
            return;
        }

        _updatingFromViewModel = true;
        try
        {
            txtSource.Text = translation.SourceText;
            lblStatusValue.Text = translation.Status.ToString();
        }
        finally
        {
            _updatingFromViewModel = false;
        }

        UpdateCommandButtons();
    }

    private TranslationEntryViewModel? GetActiveTranslation()
    {
        if (tabTranslations.SelectedTab?.Tag is TranslationEntryViewModel translation)
        {
            return translation;
        }

        return _viewModel.SelectedEntry?.TranslationEntries.FirstOrDefault();
    }

    private void OnTranslationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TranslationEntryViewModel translation || !_tabViews.TryGetValue(translation, out var tabView))
        {
            return;
        }

        if (string.IsNullOrEmpty(e.PropertyName) ||
            string.Equals(e.PropertyName, nameof(TranslationEntryViewModel.TranslationText), StringComparison.Ordinal))
        {
            if (!_updatingFromViewModel && tabView.TranslationTextBox.Text != translation.TranslationText)
            {
                _updatingFromViewModel = true;
                try
                {
                    tabView.TranslationTextBox.Text = translation.TranslationText;
                }
                finally
                {
                    _updatingFromViewModel = false;
                }
            }
        }

        if (string.IsNullOrEmpty(e.PropertyName) ||
            string.Equals(e.PropertyName, nameof(TranslationEntryViewModel.Status), StringComparison.Ordinal))
        {
            tabView.StatusLabel.Text = $"Status: {translation.Status}";
            tabView.WarningLabel.Visible = translation.Status == TranslationStatus.NeedsReview;

            if (translation == GetActiveTranslation())
            {
                lblStatusValue.Text = translation.Status.ToString();
            }
        }

        if (string.IsNullOrEmpty(e.PropertyName) ||
            string.Equals(e.PropertyName, nameof(TranslationEntryViewModel.FallbackText), StringComparison.Ordinal))
        {
            tabView.FallbackLabel.Text = translation.FallbackText;
        }
    }

    private void UnhookTranslations()
    {
        foreach (var translation in _tabViews.Keys)
        {
            translation.PropertyChanged -= OnTranslationPropertyChanged;
        }
    }

    private void UpdateCommandButtons()
    {
        btnPrev.Enabled = _viewModel.PreviousCommand.CanExecute(null);
        btnNext.Enabled = _viewModel.NextCommand.CanExecute(null);
        btnExport.Enabled = _viewModel.ExportCsvCommand.CanExecute(null);

        var hasEntry = _viewModel.SelectedEntry != null && GetActiveTranslation() != null;
        btnCopySource.Enabled = hasEntry;
        btnQuickPaste.Enabled = hasEntry;
        btnSave.Enabled = hasEntry;
        btnSaveNext.Enabled = hasEntry;
    }

    private static void ExecuteCommand(System.Windows.Input.ICommand command)
    {
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private void ExecuteEntryCommand(Func<TranslationQueueEntryViewModel, System.Windows.Input.ICommand> commandSelector)
    {
        var entry = _viewModel.SelectedEntry;
        if (entry == null)
        {
            return;
        }

        var command = commandSelector(entry);
        ExecuteCommand(command);
    }

    private void SetFilterSelection<T>(ComboBox comboBox, FilterOption<T>? option)
    {
        if (option == null)
        {
            return;
        }

        _updatingFromViewModel = true;
        try
        {
            comboBox.SelectedItem = option;
        }
        finally
        {
            _updatingFromViewModel = false;
        }
    }

    private void LstEntriesOnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstEntries.Items.Count)
        {
            return;
        }

        e.DrawBackground();
        var entry = lstEntries.Items[e.Index] as TranslationQueueEntryViewModel;
        var bounds = e.Bounds;
        var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var background = isSelected ? System.Drawing.Color.FromArgb(70, 70, 74) : lstEntries.BackColor;
        using var backgroundBrush = new SolidBrush(background);
        e.Graphics.FillRectangle(backgroundBrush, bounds);

        if (entry == null)
        {
            return;
        }

        var lineHeight = e.Font.Height + 2;
        var y = bounds.Top + 4;
        var textColor = isSelected ? System.Drawing.Color.White : lstEntries.ForeColor;
        using var textBrush = new SolidBrush(textColor);
        using var subTextBrush = new SolidBrush(System.Drawing.Color.Gray);

        e.Graphics.DrawString(
            $"{entry.EntityType} #{entry.EntityId}{entry.EntityNameDisplay}",
            e.Font,
            textBrush,
            bounds.Left + 6,
            y
        );

        y += lineHeight;
        e.Graphics.DrawString(
            $"{entry.Field} {entry.SubPath}",
            e.Font,
            subTextBrush,
            bounds.Left + 6,
            y
        );

        y += lineHeight;
        e.Graphics.DrawString(
            entry.Status,
            e.Font,
            subTextBrush,
            bounds.Left + 6,
            y
        );

        e.DrawFocusRectangle();
    }

    private sealed class TranslationTabView
    {
        public TranslationTabView(
            TabPage tabPage,
            Label statusLabel,
            Label warningLabel,
            DarkTextBox translationTextBox,
            Label fallbackLabel
        )
        {
            TabPage = tabPage;
            StatusLabel = statusLabel;
            WarningLabel = warningLabel;
            TranslationTextBox = translationTextBox;
            FallbackLabel = fallbackLabel;
        }

        public TabPage TabPage { get; }

        public Label StatusLabel { get; }

        public Label WarningLabel { get; }

        public DarkTextBox TranslationTextBox { get; }

        public Label FallbackLabel { get; }
    }
}
