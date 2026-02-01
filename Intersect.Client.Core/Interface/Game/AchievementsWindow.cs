using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Network.Packets.Localization;

namespace Intersect.Client.Interface.Game;

public sealed partial class AchievementsWindow : Window
{
    private readonly ListBox _achievementList;
    private readonly ScrollControl _detailsArea;
    private readonly Label _titleLabel;
    private readonly Label _categoryLabel;
    private readonly Label _difficultyLabel;
    private readonly Label _statusLabel;
    private readonly Label _progressTitleLabel;
    private readonly Label _progressValueLabel;
    private readonly ProgressBar _progressBar;
    private readonly RichLabel _descriptionLabel;
    private readonly Label _descriptionTemplateLabel;
    private readonly Label _rewardTitleLabel;
    private readonly RichLabel _rewardLabel;
    private readonly Label _rewardTemplateLabel;
    private readonly LabeledCheckBox _filterCompleted;
    private readonly LabeledCheckBox _filterInProgress;
    private readonly LabeledCheckBox _filterPending;

    private AchievementDescriptor? _selectedAchievement;
    private bool _shouldUpdateList;
    private bool _localizationSubscribed;

    private const int ListWidth = 250;
    private const int Padding = 10;
    private const int FilterSpacing = 4;
    private const int SectionSpacing = 8;
    private const int ProgressBarHeight = 20;

    public AchievementsWindow(Canvas gameCanvas)
        : base(gameCanvas, Strings.Achievements.Title, false, nameof(AchievementsWindow))
    {
        IsResizable = false;

        if (Width <= 1 || Height <= 1)
        {
            SetSize(760, 520);
        }

        _filterCompleted = new LabeledCheckBox(this, nameof(_filterCompleted))
        {
            Text = Strings.Achievements.FilterCompleted,
            IsChecked = true,
        };
        _filterCompleted.CheckChanged += FilterChanged;

        _filterInProgress = new LabeledCheckBox(this, nameof(_filterInProgress))
        {
            Text = Strings.Achievements.FilterInProgress,
            IsChecked = true,
        };
        _filterInProgress.CheckChanged += FilterChanged;

        _filterPending = new LabeledCheckBox(this, nameof(_filterPending))
        {
            Text = Strings.Achievements.FilterPending,
            IsChecked = true,
        };
        _filterPending.CheckChanged += FilterChanged;

        _achievementList = new ListBox(this, nameof(_achievementList));
        _achievementList.EnableScroll(false, true);

        _detailsArea = new ScrollControl(this, nameof(_detailsArea));
        _detailsArea.EnableScroll(false, true);

        _titleLabel = new Label(_detailsArea, nameof(_titleLabel));
        _categoryLabel = new Label(_detailsArea, nameof(_categoryLabel));
        _difficultyLabel = new Label(_detailsArea, nameof(_difficultyLabel));
        _statusLabel = new Label(_detailsArea, nameof(_statusLabel));

        _descriptionTemplateLabel = new Label(_detailsArea, nameof(_descriptionTemplateLabel))
        {
            IsHidden = true,
        };
        _descriptionLabel = new RichLabel(_detailsArea) { Name = nameof(_descriptionLabel) };

        _progressTitleLabel = new Label(_detailsArea, nameof(_progressTitleLabel))
        {
            Text = Strings.Achievements.Progress,
        };
        _progressValueLabel = new Label(_detailsArea, nameof(_progressValueLabel));
        _progressBar = new ProgressBar(_detailsArea)
        {
            AutoLabel = false,
            Text = string.Empty,
            Height = ProgressBarHeight,
        };

        _rewardTitleLabel = new Label(_detailsArea, nameof(_rewardTitleLabel))
        {
            Text = Strings.Achievements.RewardsTitle,
        };
        _rewardTemplateLabel = new Label(_detailsArea, nameof(_rewardTemplateLabel))
        {
            IsHidden = true,
        };
        _rewardLabel = new RichLabel(_detailsArea) { Name = nameof(_rewardLabel) };
        ApplyUiDefaults();
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

      // <-- AGREGA ESTO

     

        if (_detailsArea.InnerPanel != null)
        {
            _detailsArea.InnerPanel.MouseInputEnabled = true;
        }

        if (_detailsArea.VerticalScrollBar != null)
        {
            _detailsArea.VerticalScrollBar.IsHidden = false;
            _detailsArea.VerticalScrollBar.IsDisabled = false;
            _detailsArea.VerticalScrollBar.ScrollAmount = 24;
        }

        _detailsArea.BoundsChanged += (_, _) => UpdateDetailsLayout();
        BoundsChanged += (_, _) => UpdateLayout();

        ClearSelectedAchievement();
        SubscribeToLocalizationUpdates();
        UpdateLayout();
    }

    public void Update(bool shouldUpdateList)
    {
        if (!IsVisibleInTree)
        {
            _shouldUpdateList |= shouldUpdateList;
            return;
        }

        UpdateInternal(shouldUpdateList);
    }

    public void Show()
    {
        _selectedAchievement = null;
        _achievementList.UnselectAll();
        UpdateSelectedAchievement();

        if (_shouldUpdateList)
        {
            UpdateInternal(_shouldUpdateList);
            _shouldUpdateList = false;
        }

        IsHidden = false;
    }

    public bool IsVisible() => !IsHidden;

    public void Hide()
    {
        IsHidden = true;
        _selectedAchievement = null;
        _achievementList.UnselectAll();
        UpdateSelectedAchievement();
    }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
        UpdateAchievementList();
        UpdateSelectedAchievement();
    }

    private void UpdateInternal(bool shouldUpdateList)
    {
        if (shouldUpdateList)
        {
            UpdateAchievementList();
            UpdateSelectedAchievement();
        }

        if (IsHidden)
        {
            _shouldUpdateList |= shouldUpdateList;
        }
    }

    private void FilterChanged(ICheckbox sender, ValueChangedEventArgs<bool> args)
    {
        UpdateAchievementList();
    }

    private void UpdateLayout()
    {
        var filterY = Padding;
        var listLeft = Padding;
        var detailsLeft = listLeft + ListWidth + Padding;
        var listHeightAvailable = Height - Padding * 2;

        var filterControls = new[] { _filterCompleted, _filterInProgress, _filterPending };
        foreach (var filter in filterControls)
        {
            filter.SizeToChildren(false, true);
            filter.SetPosition(listLeft, filterY);
            filterY += filter.Height + FilterSpacing;
        }

        _achievementList.SetPosition(listLeft, filterY);
        _achievementList.SetSize(ListWidth, Math.Max(0, listHeightAvailable - (filterY - Padding)));

        _detailsArea.SetPosition(detailsLeft, Padding);
        _detailsArea.SetSize(Math.Max(0, Width - detailsLeft - Padding), listHeightAvailable);

        UpdateDetailsLayout();
    }

    private void UpdateDetailsLayout()
    {
        var contentWidth = GetDetailsContentWidth();
        var y = 0;
        const int bottomPadding = 12;

        if (!_titleLabel.IsHidden)
        {
            _titleLabel.Width = contentWidth;
            _titleLabel.SizeToChildren(false, true);
            _titleLabel.SetPosition(0, y);
            y += _titleLabel.Height + 3;
        }

        if (!_categoryLabel.IsHidden)
        {
            _categoryLabel.Width = contentWidth;
            _categoryLabel.SizeToChildren(false, true);
            _categoryLabel.SetPosition(0, y);
            y += _categoryLabel.Height + 3;
        }

        if (!_difficultyLabel.IsHidden)
        {
            _difficultyLabel.Width = contentWidth;
            _difficultyLabel.SizeToChildren(false, true);
            _difficultyLabel.SetPosition(0, y);
            y += _difficultyLabel.Height + 3;
        }

        if (!_statusLabel.IsHidden)
        {
            _statusLabel.Width = contentWidth;
            _statusLabel.SizeToChildren(false, true);
            _statusLabel.SetPosition(0, y);
            y += _statusLabel.Height + SectionSpacing;
        }

        _descriptionLabel.Width = contentWidth;
        _descriptionLabel.SizeToChildren(false, true);
        _descriptionLabel.SetPosition(0, y);
        y += _descriptionLabel.Height + SectionSpacing;

        if (!_progressTitleLabel.IsHidden)
        {
            _progressTitleLabel.Width = contentWidth;
            _progressTitleLabel.SizeToChildren(false, true);
            _progressTitleLabel.SetPosition(0, y);
            y += _progressTitleLabel.Height + 2;

            _progressValueLabel.Width = contentWidth;
            _progressValueLabel.SizeToChildren(false, true);
            _progressValueLabel.SetPosition(0, y);
            y += _progressValueLabel.Height + 4;

            _progressBar.Width = contentWidth;
            _progressBar.SetPosition(0, y);
            y += _progressBar.Height + SectionSpacing;
        }

        if (!_rewardTitleLabel.IsHidden)
        {
            _rewardTitleLabel.Width = contentWidth;
            _rewardTitleLabel.SizeToChildren(false, true);
            _rewardTitleLabel.SetPosition(0, y);
            y += _rewardTitleLabel.Height + 3;

            _rewardLabel.Width = contentWidth;
            _rewardLabel.SizeToChildren(false, true);
            _rewardLabel.SetPosition(0, y);
            y += _rewardLabel.Height + SectionSpacing;
        }

        y += bottomPadding;
        _detailsArea.SetInnerSize(Math.Max(contentWidth, 1), Math.Max(y, 1));
        _detailsArea.EnableScroll(false, true);

        if (_detailsArea.VerticalScrollBar != null)
        {
            _detailsArea.VerticalScrollBar.ScrollAmount = 24;
        }
    }

    private int GetDetailsContentWidth()
    {
        var width = _detailsArea.Width;
        if (_detailsArea.VerticalScrollBar is { IsHidden: false } scrollbar)
        {
            width -= scrollbar.Width;
        }

        return Math.Max(0, width);
    }

    private void UpdateAchievementList()
    {
        _achievementList.RemoveAllRows();
        if (Globals.Me == null)
        {
            return;
        }

        var achievements = AchievementDescriptor.Lookup.Values.OfType<AchievementDescriptor>().ToList();
        RequestAchievementListLocalization(achievements);

        var list = new Dictionary<AchievementCategory, List<(AchievementDescriptor Achievement, int Order, Color Color)>>();

        foreach (var achievement in achievements)
        {
            if (!ShouldIncludeAchievement(achievement))
            {
                continue;
            }

            var status = GetAchievementStatus(achievement);
            var order = status switch
            {
                AchievementStatus.InProgress => 1,
                AchievementStatus.Pending => 2,
                AchievementStatus.Completed => 3,
                _ => 4,
            };

            var color = status switch
            {
                AchievementStatus.Completed => CustomColors.QuestWindow.Completed,
                AchievementStatus.InProgress => CustomColors.QuestWindow.InProgress,
                _ => CustomColors.QuestWindow.NotStarted,
            };

            if (!list.TryGetValue(achievement.Category, out var entries))
            {
                entries = new List<(AchievementDescriptor Achievement, int Order, Color Color)>();
                list.Add(achievement.Category, entries);
            }

            entries.Add((achievement, order, color));
        }

        foreach (var category in Enum.GetValues<AchievementCategory>())
        {
            if (!list.TryGetValue(category, out var entries) || entries.Count == 0)
            {
                continue;
            }

            AddCategoryToList(GetCategoryDisplayName(category), Color.White);

            foreach (var entry in entries
                         .OrderBy(e => e.Order)
                         .ThenBy(e => e.Achievement.OrderValue))
            {
                var localizedName = GetLocalizedAchievementField(entry.Achievement, "Name", entry.Achievement.Name);
                AddAchievementToList(localizedName, entry.Color, entry.Achievement.Id);
            }
        }
    }

    private bool ShouldIncludeAchievement(AchievementDescriptor achievement)
    {
        var status = GetAchievementStatus(achievement);
        return status switch
        {
            AchievementStatus.Completed => _filterCompleted.IsChecked,
            AchievementStatus.InProgress => _filterInProgress.IsChecked,
            AchievementStatus.Pending => _filterPending.IsChecked,
            _ => true,
        };
    }

    private void AddAchievementToList(string name, Color color, Guid achievementId, bool indented = true)
    {
        var item = _achievementList.AddRow((indented ? "\t\t\t" : "") + name);
        item.UserData = achievementId;
        item.Clicked -= AchievementListItem_Clicked;
        item.Clicked += AchievementListItem_Clicked;
        item.SetTextColor(color);
        item.SetSize(ListWidth - 20, 25);
    }

    private void AddCategoryToList(string name, Color color)
    {
        var item = _achievementList.AddRow(name);
        item.MouseInputEnabled = false;
        item.SetTextColor(color);
        item.SetSize(ListWidth - 20, 25);
    }

    private void AchievementListItem_Clicked(Base sender, MouseButtonState arguments)
    {
        if (sender.UserData is not Guid achievementId)
        {
            return;
        }

        if (!AchievementDescriptor.TryGet(achievementId, out var achievement))
        {
            _achievementList.UnselectAll();
            return;
        }

        _selectedAchievement = achievement;
        UpdateSelectedAchievement();
    }

    private void UpdateSelectedAchievement()
    {
        _achievementList.Show();
        _descriptionLabel.ClearText();
        _rewardLabel.ClearText();

        if (_selectedAchievement == null)
        {
            ClearSelectedAchievement();
            UpdateDetailsLayout();
            return;
        }

        RequestAchievementLocalization(_selectedAchievement);

        _detailsArea.IsHidden = false;
        _titleLabel.IsHidden = false;
        _statusLabel.IsHidden = false;
        _categoryLabel.IsHidden = false;
        _difficultyLabel.IsHidden = false;
        _progressTitleLabel.IsHidden = false;
        _progressValueLabel.IsHidden = false;
        _progressBar.IsHidden = false;
        _rewardTitleLabel.IsHidden = false;
        _rewardLabel.IsHidden = false;

        _titleLabel.Text = GetLocalizedAchievementField(_selectedAchievement, "Name", _selectedAchievement.Name);
        _categoryLabel.Text = Strings.Achievements.CategoryLabel.ToString(
            GetCategoryDisplayName(_selectedAchievement.Category)
        );
        _difficultyLabel.Text = Strings.Achievements.DifficultyLabel.ToString(
            GetDifficultyDisplayName(_selectedAchievement.Difficulty)
        );

        var status = GetAchievementStatus(_selectedAchievement);
        switch (status)
        {
            case AchievementStatus.Completed:
                _statusLabel.Text = Strings.Achievements.Completed;
                _statusLabel.SetTextColor(CustomColors.QuestWindow.Completed, ComponentState.Normal);
                break;
            case AchievementStatus.InProgress:
                _statusLabel.Text = Strings.Achievements.InProgress;
                _statusLabel.SetTextColor(CustomColors.QuestWindow.InProgress, ComponentState.Normal);
                break;
            default:
                _statusLabel.Text = Strings.Achievements.Pending;
                _statusLabel.SetTextColor(CustomColors.QuestWindow.NotStarted, ComponentState.Normal);
                break;
        }

        _descriptionLabel.AddText(
            GetLocalizedAchievementField(_selectedAchievement, "Description", _selectedAchievement.Description),
            _descriptionTemplateLabel
        );

        UpdateProgressDetails(_selectedAchievement);
        UpdateRewardDetails(_selectedAchievement);

        UpdateDetailsLayout();
    }

    private void UpdateProgressDetails(AchievementDescriptor achievement)
    {
        var (completed, total) = GetAchievementObjectiveCounts(achievement);
        _progressValueLabel.Text = Strings.Achievements.ProgressValue.ToString(completed, total);
        _progressBar.Value = total > 0
            ? Math.Clamp(completed / (float)total, 0f, 1f)
            : 0f;
    }

    private void UpdateRewardDetails(AchievementDescriptor achievement)
    {
        _rewardLabel.ClearText();
        var rewards = achievement.Rewards;
        var lines = new List<string>();

        if (rewards.Experience > 0)
        {
            lines.Add(Strings.Achievements.RewardExperience.ToString(rewards.Experience));
        }

        if (rewards.Currency > 0)
        {
            lines.Add(Strings.Achievements.RewardCurrency.ToString(rewards.Currency));
        }

        foreach (var item in rewards.Items.Where(item => item.Value > 0))
        {
            var itemName = GetLocalizedItemName(item.Key);
            lines.Add(Strings.Achievements.RewardItem.ToString(item.Value, itemName));
        }

        if (rewards.TitleIds.Count > 0)
        {
            lines.Add(Strings.Achievements.RewardTitles.ToString(rewards.TitleIds.Count));
        }

        if (lines.Count == 0)
        {
            lines.Add(Strings.Achievements.RewardNone);
        }

        for (var i = 0; i < lines.Count; i++)
        {
            _rewardLabel.AddText(lines[i], _rewardTemplateLabel);
            if (i < lines.Count - 1)
            {
                _rewardLabel.AddLineBreak();
            }
        }
    }

    private void ClearSelectedAchievement()
    {
        _titleLabel.Hide();
        _categoryLabel.Hide();
        _difficultyLabel.Hide();
        _statusLabel.Hide();
        _progressTitleLabel.Hide();
        _progressValueLabel.Hide();
        _progressBar.Hide();
        _rewardTitleLabel.Hide();
        _rewardLabel.Hide();
        _detailsArea.Hide();
    }

    private static string GetCategoryDisplayName(AchievementCategory category) =>
        category switch
        {
            AchievementCategory.Dungeons => Strings.Achievements.CategoryDungeons,
            AchievementCategory.Exploration => Strings.Achievements.CategoryExploration,
            AchievementCategory.Monsters => Strings.Achievements.CategoryMonsters,
            AchievementCategory.Quests => Strings.Achievements.CategoryQuests,
            AchievementCategory.Professions => Strings.Achievements.CategoryProfessions,
            AchievementCategory.Events => Strings.Achievements.CategoryEvents,
            _ => category.ToString()
        };

    private static string GetDifficultyDisplayName(AchievementDifficulty difficulty) =>
        difficulty switch
        {
            AchievementDifficulty.Discovery => Strings.Achievements.DifficultyDiscovery,
            AchievementDifficulty.Natural => Strings.Achievements.DifficultyNatural,
            AchievementDifficulty.Epic => Strings.Achievements.DifficultyEpic,
            AchievementDifficulty.Meta => Strings.Achievements.DifficultyMeta,
            _ => difficulty.ToString()
        };

    private void RequestAchievementListLocalization(IEnumerable<AchievementDescriptor> achievements)
    {
        var requests = achievements
            .Select(
                achievement => new LocalizationRequestEntry(
                    achievement.Type.ToString(),
                    achievement.Id.ToString(),
                    "Name"
                )
            )
            .ToList();

        GameLocalization.RequestEntries(requests);
    }

    private void RequestAchievementLocalization(AchievementDescriptor achievement)
    {
        GameLocalization.RequestEntries(
            [
                new LocalizationRequestEntry(achievement.Type.ToString(), achievement.Id.ToString(), "Name"),
                new LocalizationRequestEntry(achievement.Type.ToString(), achievement.Id.ToString(), "Description")
            ]
        );
    }

    private static string GetLocalizedAchievementField(
        AchievementDescriptor achievement,
        string field,
        string fallback
    ) => GameLocalization.GetTextOrDefault(achievement.Type.ToString(), achievement.Id, field, fallback);

    private static string GetLocalizedItemName(Guid itemId)
    {
        if (!ItemDescriptor.TryGet(itemId, out var item) || item == null)
        {
            return ItemDescriptor.GetName(itemId);
        }

        return GameLocalization.GetTextOrDefault(item.Type.ToString(), item.Id, "Name", item.Name);
    }

    private void SubscribeToLocalizationUpdates()
    {
        if (_localizationSubscribed)
        {
            return;
        }

        GameLocalization.LocalizedTextsUpdated += OnLocalizedTextsUpdated;
        _localizationSubscribed = true;
    }

    private void OnLocalizedTextsUpdated(string language, IReadOnlyCollection<LocalizationRequestEntry> requests)
    {
        if (!IsVisibleInTree)
        {
            _shouldUpdateList = true;
            return;
        }

        if (_selectedAchievement != null)
        {
            var entityType = _selectedAchievement.Type.ToString();
            var entityId = _selectedAchievement.Id.ToString();
            if (requests.Any(
                    request => request.EntityType == entityType &&
                               request.EntityId == entityId &&
                               (request.Field == "Name" ||
                                request.Field == "Description")
                ))
            {
                UpdateSelectedAchievement();
            }
        }

        UpdateAchievementList();
    }

    private AchievementStatus GetAchievementStatus(AchievementDescriptor achievement)
    {
        if (!Globals.AchievementProgress.TryGetValue(achievement.Id, out var progress))
        {
            if (achievement.MetaAchievementIds.Count > 0 &&
                achievement.MetaAchievementIds.Any(id => Globals.AchievementProgress.TryGetValue(id, out var meta) && meta.Completed))
            {
                return AchievementStatus.InProgress;
            }

            return AchievementStatus.Pending;
        }

        if (progress.Completed)
        {
            return AchievementStatus.Completed;
        }

        return progress.Progress > 0 ? AchievementStatus.InProgress : AchievementStatus.Pending;
    }

    private (int completed, int total) GetAchievementObjectiveCounts(AchievementDescriptor achievement)
    {
        var objectives = GetAchievementObjectives(achievement);
        var total = objectives.Count;
        var completed = objectives.Count(objective => objective.IsCompleted);
        return (completed, total);
    }

    private List<ObjectiveProgress> GetAchievementObjectives(AchievementDescriptor achievement)
    {
        if (Globals.AchievementProgress.TryGetValue(achievement.Id, out var progress) &&
            progress.Objectives.Count > 0)
        {
            return progress.Objectives;
        }

        if (achievement.MetaAchievementIds.Count > 0)
        {
            return achievement.MetaAchievementIds
                .Select(
                    id => new ObjectiveProgress(
                        Globals.AchievementProgress.TryGetValue(id, out var meta) && meta.Completed ? 1 : 0,
                        1,
                        ProgressMode.Binary
                    )
                )
                .ToList();
        }

        return [];
    }
    // --- UI Defaults/Styling ----------------------------------------------------

    private void ApplyUiDefaults()
    {
        // Window defaults
        IsResizable = false;

        // Si tu JSON UI no define size, ponemos uno base coherente.
        if (Width <= 1 || Height <= 1)
        {
            SetSize(760, 520);
        }

        // Controles base: tamaños iniciales (UpdateLayout los recalcula, pero esto evita "flash" raro)
        _achievementList.SetSize(ListWidth, Height - (Padding * 2));
        _detailsArea.SetSize(Math.Max(0, Width - (Padding * 3) - ListWidth), Height - (Padding * 2));

        // Checkboxes: tamaño decente (antes de SizeToChildren)
        _filterCompleted.SetSize(ListWidth, 20);
        _filterInProgress.SetSize(ListWidth, 20);
        _filterPending.SetSize(ListWidth, 20);

        // Labels: ancho flexible y estilo
        ApplyLabelStyle(_titleLabel, isTitle: true);
        ApplyLabelStyle(_categoryLabel);
        ApplyLabelStyle(_difficultyLabel);

        // Status: el color cambia por estado, pero le ponemos override habilitado
        ApplyLabelStyle(_statusLabel, isStatus: true);

        ApplyLabelStyle(_progressTitleLabel);
        ApplyLabelStyle(_progressValueLabel);

        ApplyLabelStyle(_rewardTitleLabel);
        ApplyLabelStyle(_descriptionTemplateLabel, isTemplate: true);
        ApplyLabelStyle(_rewardTemplateLabel, isTemplate: true);

        // Rich labels: al menos asegura wrap y color (si tu RichLabel soporta)
        ApplyRichLabelStyle(_descriptionLabel);
        ApplyRichLabelStyle(_rewardLabel);

        // Progress bar: altura ya la tienes; reforzamos
        _progressBar.Height = ProgressBarHeight;

        // Scrollbar: ya lo haces abajo, pero lo dejo consistente aquí
        if (_detailsArea.VerticalScrollBar != null)
        {
            _detailsArea.VerticalScrollBar.IsHidden = false;
            _detailsArea.VerticalScrollBar.IsDisabled = false;
            _detailsArea.VerticalScrollBar.ScrollAmount = 24;
        }

        // Posiciones iniciales (UpdateLayout manda, pero esto deja todo bien desde el arranque)
        UpdateLayout();
    }

    private void ApplyLabelStyle(Label label, bool isTitle = false, bool isStatus = false, bool isTemplate = false)
    {
        // Template labels no deberían verse (ya lo haces). Solo los dejamos "limpios".
        if (isTemplate)
        {
            label.IsHidden = true;
            return;
        }

        // Colores: usa tu paleta. Title más fuerte, los demás un poco más suaves.
        var baseColor = isTitle ? Color.White : new Color(220, 220, 220, 255);

        // Para status solo habilitamos override; el color final lo pones según estado.
        if (!isStatus)
        {
            SetTextColorSmart(label, baseColor);
        }
        else
        {
            EnableTextColorOverrideIfExists(label);
        }

        // Fuente: si tu UI JSON define la font, esto no estorba; si no, lo fija.
        // Ajusta nombres/tamaños a tu proyecto (SourceSansPro etc).
        TrySetProperty(label, "FontName", isTitle ? "sourcesanspro-semibold" : "sourcesanspro");
        TrySetProperty(label, "FontSize", isTitle ? 14 : 12);

        // En algunos builds existe "TextPadding" o "Padding"
        TrySetProperty(label, "Padding", Padding);
    }

    private void ApplyRichLabelStyle(RichLabel richLabel)
    {
        // RichLabel a veces no tiene SetTextColor por línea, pero el template sí.
        // Igual habilitamos override si existe, para que el template pinte bonito.
        EnableTextColorOverrideIfExists(richLabel);

        TrySetProperty(richLabel, "FontName", "sourcesanspro");
        TrySetProperty(richLabel, "FontSize", 12);
        TrySetProperty(richLabel, "Padding", 0);
    }

    // --- Safe Styling Helpers ---------------------------------------------------

    private static void SetTextColorSmart(Base control, Color color)
    {
        // 1) Si existe TextColorOverride / TextColor, lo usamos
        if (TrySetProperty(control, "TextColorOverride", true))
        {
            // Algunas versiones usan TextColor, otras TextColorOverride + TextColor
            TrySetProperty(control, "TextColor", color);
            return;
        }

        // 2) Fallback: Gwen clásico
        switch (control)
        {
            case Label lbl:
                lbl.SetTextColor(color, ComponentState.Normal);
                break;
            case ListBoxRow row:
                row.SetTextColor(color);
                break;
            default:
                // Intento final: algunas clases tienen SetTextColor(Color)
                InvokeIfExists(control, "SetTextColor", color);
                break;
        }
    }

    private static void EnableTextColorOverrideIfExists(Base control)
    {
        TrySetProperty(control, "TextColorOverride", true);
    }

    private static bool TrySetProperty(object obj, string propName, object value)
    {
        var prop = obj.GetType().GetProperty(propName);
        if (prop == null || !prop.CanWrite) return false;

        try
        {
            // Conversión simple si el tipo no coincide exacto
            var targetType = prop.PropertyType;
            var converted = value;

            if (value != null && !targetType.IsInstanceOfType(value))
            {
                converted = Convert.ChangeType(value, targetType);
            }

            prop.SetValue(obj, converted);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool InvokeIfExists(object obj, string methodName, params object[] args)
    {
        var methods = obj.GetType().GetMethods().Where(m => m.Name == methodName).ToList();
        foreach (var m in methods)
        {
            var p = m.GetParameters();
            if (p.Length != args.Length) continue;

            try
            {
                m.Invoke(obj, args);
                return true;
            }
            catch
            {
                // ignore
            }
        }

        return false;
    }

    private enum AchievementStatus
    {
        Pending,
        InProgress,
        Completed,
    }
}
