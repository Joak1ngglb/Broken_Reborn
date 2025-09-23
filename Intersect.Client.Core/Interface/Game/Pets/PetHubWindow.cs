using System;
using System.Collections.Generic;
using System.Globalization;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Pets;
using Intersect.Configuration;


namespace Intersect.Client.Interface.Game.Pets
{
    public sealed class PetHubWindow : Window
    {
        // Layout
        private const int WindowWidth = 460;
        private const int WindowHeight = 720;

        private const int Margin = 16;
        private const int Gap = 12;
        private const int SectionGap = 18;

        private const int DetailLineHeight = 22;
        private const int StatColumnWidth = 150;
        private const int StatRowHeight = 20;
        private const int AttributeBarHeight = 32;
        private const int AttributeFillHeight = 22;
        private const int PreviewSize = 132;

        private const int DefaultAttributeCap = 100;

        // Fonts
        private const string FontName = "sourceproblack";
        private const int TitleSize = 18;
        private const int HeaderSize = 16;
        private const int BodySize = 13;
        private const int StatSize = 12;
        private const int ButtonSize = 14;

        // Controls
        private readonly Label _statusLabel;
        private readonly ScrollControl _contentScroll;
        private readonly Base _headerPanel;
        private readonly ImagePanel _previewPanel;
        private readonly ImagePanel _previewSprite;
        private readonly Base _detailsPanel;

        private readonly Label _customNameLabel;
        private readonly Label _descriptorNameLabel;
        private readonly Label _levelLabel;
        private readonly Label _experienceLabel;
        private readonly Label _experienceToNextLabel;
        private readonly Label _behaviorLabel;

        private readonly AttributeBar _energyBar;
        private readonly AttributeBar _moodBar;
        private readonly AttributeBar _maturityBar;

        private readonly Base _vitalsPanel;
        private readonly Label _vitalsHeader;
        private readonly Dictionary<Vital, Label> _vitalLabels = new();

        private readonly Label _statsHeader;
        private readonly Base _statsPanel;
        private readonly Label _noStatsLabel;
        private readonly Dictionary<Stat, Label> _statLabels = new();

        private readonly PetBehaviorWidget _behaviorWidget;

        private readonly Button _invokeButton;
        private readonly Button _dismissButton;
        private readonly Label _cooldownLabel;

        public PetHubWindow(Canvas gameCanvas)
            : base(gameCanvas, Strings.Pets.HubTitle, modal: false, name: nameof(PetHubWindow))
        {
            // Window chrome
      IsResizable = false;
            IsClosable = true;
            RestrictToParent = true;

            Alignment = new[] { Alignments.Bottom, Alignments.Left };
            AlignmentPadding = new Padding { Left = 8, Bottom = 8 };

            SetSize(WindowWidth, WindowHeight);
            SetPosition(16, 16);

            TitleLabel.FontName = FontName;
            TitleLabel.FontSize = TitleSize;
            TitleLabel.TextColorOverride = Color.White;

            // Status
            _statusLabel = new Label(this, "StatusLabel")
            {
                FontName = FontName,
                FontSize = BodySize,
                TextColor = Color.White,
                Text = Strings.Pets.StatusNoPet.ToString()
            };
            _statusLabel.SetPosition(Margin, 32);
            _statusLabel.SetSize(WindowWidth - Margin * 2, 24);

            var contentWidth = WindowWidth - Margin * 2;
            var buttonsTop = WindowHeight - Margin - 34;
            var scrollHeight = Math.Max(180, buttonsTop - 64 - Gap);

            _contentScroll = new ScrollControl(this, "ContentScroll")
            {
                AutoHideBars = true,
            };
            _contentScroll.SetPosition(Margin, 64);
            _contentScroll.SetSize(contentWidth, scrollHeight);
            _contentScroll.EnableScroll(horizontal: false, vertical: true);
            _contentScroll.InnerPanel.Padding = new Padding(0, 0, 4, 0);

            _headerPanel = new Base(_contentScroll, "HeaderPanel")
            {
                ShouldDrawBackground = false,
            };
            _headerPanel.SetBounds(0, 0, contentWidth, PreviewSize);

            _previewPanel = new ImagePanel(_headerPanel, "PetPreviewPanel")
            {
                ShouldDrawBackground = true,
                RenderColor = Color.FromArgb(220, 28, 32, 40),
                Padding = new Padding(12),
            };
            _previewPanel.SetBounds(0, 0, PreviewSize, PreviewSize);

            _previewSprite = new ImagePanel(_previewPanel, "PetPreviewSprite")
            {
                MaintainAspectRatio = true,
                IsHidden = true,
            };
            _previewSprite.SetSize(PreviewSize - 24, PreviewSize - 24);
            _previewSprite.AddAlignment(Alignments.Center);

            _detailsPanel = new Base(_headerPanel, "DetailsPanel")
            {
                ShouldDrawBackground = false,
            };
            _detailsPanel.SetBounds(PreviewSize + Gap, 0, contentWidth - PreviewSize - Gap, PreviewSize);

            _customNameLabel = CreateDetailLabel(_detailsPanel, "CustomNameLabel", 0);
            _descriptorNameLabel = CreateDetailLabel(_detailsPanel, "DescriptorNameLabel", DetailLineHeight);
            _levelLabel = CreateDetailLabel(_detailsPanel, "LevelLabel", DetailLineHeight * 2);
            _experienceLabel = CreateDetailLabel(_detailsPanel, "ExperienceLabel", DetailLineHeight * 3);
            _experienceToNextLabel = CreateDetailLabel(_detailsPanel, "ExperienceToNextLabel", DetailLineHeight * 4);
            _behaviorLabel = CreateDetailLabel(_detailsPanel, "BehaviorLabel", DetailLineHeight * 5);

            var headerHeight = Math.Max(PreviewSize, _behaviorLabel.Y + _behaviorLabel.Height);
            _headerPanel.SetSize(contentWidth, headerHeight);
            _detailsPanel.SetSize(_detailsPanel.Width, headerHeight);

            var currentY = _headerPanel.Y + _headerPanel.Height + SectionGap;

            var attributesPanel = new Base(_contentScroll, "AttributesPanel")
            {
                ShouldDrawBackground = false,
            };
            attributesPanel.SetBounds(0, currentY, contentWidth, AttributeBarHeight * 3 + Gap * 2);

            _energyBar = CreateAttributeBar(
                attributesPanel,
                "Energy",
                0,
                Color.FromArgb(255, 255, 200, 80),
                Color.FromArgb(220, 20, 22, 28)
            );
            _moodBar = CreateAttributeBar(
                attributesPanel,
                "Mood",
                AttributeBarHeight + Gap,
                Color.FromArgb(255, 120, 180, 255),
                Color.FromArgb(220, 20, 22, 28)
            );
            _maturityBar = CreateAttributeBar(
                attributesPanel,
                "Maturity",
                (AttributeBarHeight + Gap) * 2,
                Color.FromArgb(255, 180, 130, 255),
                Color.FromArgb(220, 20, 22, 28)
            );

            ResetAttributeBars();

            currentY = attributesPanel.Y + attributesPanel.Height + SectionGap;

            _vitalsHeader = new Label(_contentScroll, "VitalsHeader")
            {
                FontName = FontName,
                FontSize = HeaderSize,
                TextColor = Color.White,
                Text = Strings.Pets.VitalsHeader.ToString(),
            };
            _vitalsHeader.SetBounds(0, currentY, contentWidth, DetailLineHeight);

            _vitalsPanel = new Base(_contentScroll, "VitalsPanel")
            {
                ShouldDrawBackground = false,
            };
            _vitalsPanel.SetBounds(0, currentY + DetailLineHeight, contentWidth, DetailLineHeight * Enum.GetValues<Vital>().Length);

            var vitalIndex = 0;
            foreach (var vital in Enum.GetValues<Vital>())
            {
                var label = CreateDetailLabel(_vitalsPanel, $"Vital{vital}Label", vitalIndex * DetailLineHeight);
                _vitalLabels[vital] = label;
                vitalIndex++;
            }

            currentY = _vitalsPanel.Y + _vitalsPanel.Height + SectionGap;

            _statsHeader = new Label(_contentScroll, "StatsHeader")
            {
                FontName = FontName,
                FontSize = HeaderSize,
                TextColor = Color.White,
                Text = Strings.Pets.StatsHeader.ToString(),
            };
            _statsHeader.SetBounds(0, currentY, contentWidth, DetailLineHeight);

            _statsPanel = new Base(_contentScroll, "StatsPanel")
            {
                ShouldDrawBackground = false,
            };
            _statsPanel.SetBounds(0, currentY + DetailLineHeight, contentWidth, 0);

            var idx = 0;
            foreach (var stat in Enum.GetValues<Stat>())
            {
                var label = new Label(_statsPanel, $"Stat{stat}Label")
                {
                    FontName = FontName,
                    FontSize = StatSize,
                    TextColor = Color.White,
                    AutoSizeToContents = false,
                };

                var col = idx % 2;
                var row = idx / 2;

                label.SetSize(StatColumnWidth, StatRowHeight);
                label.SetPosition(col * (StatColumnWidth + 8), row * StatRowHeight);

                _statLabels[stat] = label;
                idx++;
            }

            _noStatsLabel = new Label(_statsPanel, "NoStatsLabel")
            {
                FontName = FontName,
                FontSize = StatSize,
                TextColor = Color.White,
                AutoSizeToContents = false,
                Text = Strings.Pets.NoStats.ToString(),
            };
            _noStatsLabel.SetSize(contentWidth, StatRowHeight);
            _noStatsLabel.SetPosition(0, 0);

            var statsRows = Math.Max(1, (int)Math.Ceiling(idx / 2f));
            _statsPanel.SetSize(contentWidth, statsRows * StatRowHeight);

            currentY = _statsPanel.Y + _statsPanel.Height + SectionGap;

            _behaviorWidget = new PetBehaviorWidget(_contentScroll)
            {
                FontName = FontName,
                FontSize = BodySize,
                TextColor = Color.White,
            };
            _behaviorWidget.SetBounds(0, currentY, contentWidth, 96);

            _contentScroll.SetInnerSize(contentWidth, _behaviorWidget.Y + _behaviorWidget.Height);

            // Botones (Invoke / Dismiss)
            _invokeButton = CreateActionButton(
                "InvokeButton",
                Margin,
                buttonsTop,
                Strings.Pets.InvokeButton.ToString(),
                OnInvokeClicked
            );

            _dismissButton = CreateActionButton(
                "DismissButton",
                Margin + 8 + _invokeButton.Width,
                buttonsTop,
                Strings.Pets.DismissButton.ToString(),
                OnDismissClicked
            );

            _cooldownLabel = new Label(this, "CooldownLabel")
            {
                FontName = FontName,
                FontSize = StatSize,
                TextColor = Color.White,
                Text = string.Empty,
                AutoSizeToContents = false,
            };
            _cooldownLabel.SetBounds(
                _invokeButton.X,
                buttonsTop - 20,
                _dismissButton.X + _dismissButton.Width - _invokeButton.X,
                16
            );
            _cooldownLabel.IsHidden = true;

            // Eventos del Hub
            Globals.PetHub.ActivePetChanged += OnPetHubStateChanged;
            Globals.PetHub.BehaviorChanged += OnPetHubStateChanged;
            Globals.PetHub.SpawnStateChanged += OnPetHubStateChanged;
            Globals.PetHub.CooldownChanged += OnPetHubCooldownChanged;
        }

        protected override void OnClose(Base control, EventArgs args)
        {
            if (Globals.PetHub.ActivePet == null && Globals.PetHub.IsSpawnRequested)
            {
                _ = Globals.PetHub.DismissPet(closePetHub: true);
            }

            base.OnClose(control, args);
        }

        protected override void EnsureInitialized()
        {
            LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
            RefreshState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Globals.PetHub.ActivePetChanged -= OnPetHubStateChanged;
                Globals.PetHub.BehaviorChanged -= OnPetHubStateChanged;
                Globals.PetHub.SpawnStateChanged -= OnPetHubStateChanged;
                Globals.PetHub.CooldownChanged -= OnPetHubCooldownChanged;
            }

            base.Dispose(disposing);
        }

        private void OnPetHubStateChanged()
        {
            RefreshState();
        }

        private void OnPetHubCooldownChanged()
        {
            RefreshCooldown();
        }

        private void RefreshState()
        {
            var pet = Globals.PetHub.ActivePet as Pet;
            var hasPet = Globals.PetHub.HasActivePet && pet != null;
            var isSpawnRequested = Globals.PetHub.IsSpawnRequested;

            _invokeButton.Text = Strings.Pets.InvokeButton.ToString();
            _dismissButton.Text = Strings.Pets.DismissButton.ToString();

            _statusLabel.Text = hasPet && pet != null
                ? Strings.Pets.StatusWithPet.ToString(pet.Name)
                : Strings.Pets.StatusNoPet.ToString();

            _statusLabel.IsHidden = hasPet;

            _contentScroll.IsHidden = !hasPet;
            _headerPanel.IsHidden = !hasPet;
            _detailsPanel.IsHidden = !hasPet;
            _statsHeader.IsHidden = !hasPet;
            _statsPanel.IsHidden = !hasPet;
            _behaviorWidget.IsHidden = !hasPet;
            _vitalsHeader.IsHidden = !hasPet;
            _vitalsPanel.IsHidden = !hasPet;

            if (!hasPet)
            {
                _energyBar.Container.IsHidden = true;
                _moodBar.Container.IsHidden = true;
                _maturityBar.Container.IsHidden = true;
            }

            _dismissButton.IsDisabled = !isSpawnRequested;

            if (!hasPet || pet == null)
            {
                foreach (var label in _statLabels.Values)
                {
                    label.IsHidden = true;
                }

                foreach (var vitalLabel in _vitalLabels.Values)
                {
                    vitalLabel.IsHidden = true;
                }

                _noStatsLabel.IsHidden = false;
                _experienceLabel.IsHidden = true;
                _experienceToNextLabel.IsHidden = true;
                _previewSprite.Texture = null;
                _previewSprite.IsHidden = true;

                ResetAttributeBars();

                RefreshCooldown();
                UpdateContentLayout();

                return;
            }

            var descriptor = pet.Descriptor;

            UpdatePetPreview(pet);
            _customNameLabel.Text = Strings.Pets.CustomNameLabel.ToString(pet.Name);

            var descriptorName = descriptor?.Name;
            if (string.IsNullOrWhiteSpace(descriptorName))
            {
                descriptorName = Strings.Pets.UnknownDescriptorName.ToString();
            }

            _descriptorNameLabel.Text = Strings.Pets.DescriptorNameLabel.ToString(descriptorName);
            _levelLabel.Text = Strings.Pets.LevelLabel.ToString(pet.Level);

            _experienceLabel.Text = Strings.Pets.ExperienceLabel.ToString(FormatNumber(pet.Experience));
            var experienceToNext = pet.ExperienceToNextLevel;
            var experienceToNextText = experienceToNext >= 0
                ? FormatNumber(experienceToNext)
                : Strings.Pets.MaxLevelReached.ToString();
            _experienceToNextLabel.Text = Strings.Pets.ExperienceToNextLabel.ToString(experienceToNextText);
            _experienceLabel.IsHidden = false;
            _experienceToNextLabel.IsHidden = false;

            var behaviorText = GetBehaviorLabel(Globals.PetHub.Behavior);
            _behaviorLabel.Text = Strings.Pets.BehaviorLabel.ToString(behaviorText);

            UpdateAttributes(pet);
            UpdateVitals(pet);
            UpdateStats(descriptor);

            RefreshCooldown();
            UpdateContentLayout();
        }

        private void RefreshCooldown()
        {
            var now = Timing.Global.Milliseconds;
            var remaining = Globals.PetHub.GetInvokeCooldownRemaining(now);
            var hasPet = Globals.PetHub.HasActivePet;
            var isSpawnRequested = Globals.PetHub.IsSpawnRequested;

            if (remaining > 0)
            {
                var seconds = (int)Math.Ceiling(remaining / 1000.0);
                _cooldownLabel.Text = Strings.Pets.CooldownLabel.ToString(seconds);
                _cooldownLabel.IsHidden = false;
                _invokeButton.IsDisabled = true;
            }
            else
            {
                _cooldownLabel.IsHidden = true;
                _invokeButton.IsDisabled = isSpawnRequested || !hasPet;
            }
        }

        private void UpdateAttributes(Pet pet)
        {
            var descriptor = pet.Descriptor;

            var energyCap = Math.Max(DefaultAttributeCap, descriptor?.BaseEnergy ?? 0);
            if (energyCap <= 0)
            {
                energyCap = DefaultAttributeCap;
            }

            var moodCap = Math.Max(DefaultAttributeCap, descriptor?.BaseMood ?? 0);
            if (moodCap <= 0)
            {
                moodCap = DefaultAttributeCap;
            }

            var maturityCap = Math.Max(DefaultAttributeCap, descriptor?.BaseMaturity ?? 0);
            if (maturityCap <= 0)
            {
                maturityCap = DefaultAttributeCap;
            }

            _energyBar.Update(
                pet.Energy,
                energyCap,
                Strings.Pets.EnergyLabel.ToString(pet.Energy, energyCap)
            );

            var moodName = GetMoodDisplayName(pet.Mood);
            _moodBar.Update(
                pet.MoodValue,
                moodCap,
                Strings.Pets.MoodLabelDetailed.ToString(pet.MoodValue, moodCap, moodName)
            );

            var careText = FormatCareDuration(pet.CareMilliseconds);
            _maturityBar.Update(
                pet.Maturity,
                maturityCap,
                Strings.Pets.MaturityTimeLabel.ToString(pet.Maturity, maturityCap, careText)
            );
        }

        private void UpdateVitals(Pet pet)
        {
            var hasVitals = false;

            foreach (var (vital, label) in _vitalLabels)
            {
                var max = pet.MaxVital[(int)vital];
                var current = pet.Vital[(int)vital];

                var vitalName = GetVitalName(vital);
                label.Text = Strings.Pets.VitalFormat.ToString(vitalName, current, max);
                label.IsHidden = false;
                hasVitals = true;
            }

            _vitalsHeader.IsHidden = !hasVitals;
            _vitalsPanel.IsHidden = !hasVitals;
        }

        private void UpdateStats(PetDescriptor? descriptor)
        {
            var statIndex = 0;

            if (descriptor?.StatsLookup == null || descriptor.StatsLookup.Count == 0)
            {
                foreach (var label in _statLabels.Values)
                {
                    label.IsHidden = true;
                }

                _noStatsLabel.IsHidden = false;
                _noStatsLabel.SetPosition(0, 0);
                _statsPanel.SetSize(_statsPanel.Width, StatRowHeight);
                return;
            }

            foreach (var stat in Enum.GetValues<Stat>())
            {
                var label = _statLabels[stat];
                var statValue = descriptor.StatsLookup.TryGetValue(stat, out var value)
                    ? value
                    : 0;

                var statName = GetStatName(stat);
                label.Text = Strings.Pets.StatFormat.ToString(statName, statValue);

                var column = statIndex % 2;
                var row = statIndex / 2;
                label.SetPosition(column * (StatColumnWidth + 8), row * StatRowHeight);
                label.IsHidden = false;

                statIndex++;
            }

            _noStatsLabel.IsHidden = statIndex != 0;

            var rows = Math.Max(1, (int)Math.Ceiling(statIndex / 2f));
            _statsPanel.SetSize(_statsPanel.Width, rows * StatRowHeight);
        }

        private static string GetStatName(Stat stat) =>
            Strings.Combat.Stats.TryGetValue(stat, out var label)
                ? label.ToString()
                : stat.ToString();

        private static string GetVitalName(Vital vital) =>
            Strings.ItemDescription.Vitals.TryGetValue((int)vital, out var label)
                ? label.ToString().TrimEnd(':')
                : vital.ToString();

        private static string GetMoodDisplayName(PetMood mood) => mood switch
        {
            PetMood.Miserable => Strings.Pets.MoodStateMiserable.ToString(),
            PetMood.Irritable => Strings.Pets.MoodStateIrritable.ToString(),
            PetMood.Happy => Strings.Pets.MoodStateHappy.ToString(),
            PetMood.Joyful => Strings.Pets.MoodStateJoyful.ToString(),
            _ => Strings.Pets.MoodStateContent.ToString(),
        };

        private static string GetBehaviorLabel(PetState behavior) => behavior switch
        {
            PetState.Follow => Strings.Pets.BehaviorFollow.ToString(),
            PetState.Stay => Strings.Pets.BehaviorStay.ToString(),
            PetState.Defend => Strings.Pets.BehaviorDefend.ToString(),
            PetState.Passive => Strings.Pets.BehaviorPassive.ToString(),
            _ => Strings.Pets.BehaviorUnknown.ToString(),
        };

        private static string FormatNumber(long value) => value.ToString("N0", CultureInfo.CurrentCulture);

        private static string FormatCareDuration(long milliseconds)
        {
            if (milliseconds <= 0)
            {
                return "00:00:00";
            }

            var span = TimeSpan.FromMilliseconds(milliseconds);
            var totalHours = (int)Math.Min(Math.Floor(span.TotalHours), 9999);
            var minutes = span.Minutes;
            var seconds = span.Seconds;

            return $"{totalHours:D2}:{minutes:D2}:{seconds:D2}";
        }

        private AttributeBar CreateAttributeBar(
            Base parent,
            string name,
            int y,
            Color fillColor,
            Color backgroundColor
        )
        {
            var container = new Base(parent, $"{name}Container")
            {
                ShouldDrawBackground = false,
            };
            container.SetBounds(0, y, parent.Width, AttributeBarHeight);

            var background = new Base(container, $"{name}Background")
            {
                ShouldDrawBackground = true,
                RenderColor = backgroundColor,
            };
            background.SetBounds(
                0,
                (AttributeBarHeight - AttributeFillHeight) / 2,
                parent.Width,
                AttributeFillHeight
            );

            var fill = new Base(background, $"{name}Fill")
            {
                ShouldDrawBackground = true,
                RenderColor = fillColor,
                IsHidden = true,
            };
            fill.SetBounds(0, 0, 0, AttributeFillHeight);

            var label = new Label(background, $"{name}Label")
            {
                FontName = FontName,
                FontSize = BodySize,
                TextColor = Color.White,
                AutoSizeToContents = false,
                TextAlign = Pos.Center,
            };
            label.SetBounds(0, 0, background.Width, background.Height);

            return new AttributeBar(container, background, fill, label);
        }

        private void UpdatePetPreview(Pet pet)
        {
            var sprite = pet.Sprite;
            if (string.IsNullOrWhiteSpace(sprite))
            {
                _previewSprite.Texture = null;
                _previewSprite.IsHidden = true;
                return;
            }

            var entityTexture = Globals.ContentManager.GetTexture(
                Framework.Content.TextureType.Entity,
                sprite
            );

            if (entityTexture == null)
            {
                _previewSprite.Texture = null;
                _previewSprite.IsHidden = true;
                return;
            }

            var frameWidth = entityTexture.Width / Options.Instance.Sprites.NormalFrames;
            var frameHeight = entityTexture.Height / Options.Instance.Sprites.Directions;

            _previewSprite.Texture = entityTexture;
            _previewSprite.SetTextureRect(0, 0, frameWidth, frameHeight);
            _previewSprite.SetSize(frameWidth, frameHeight);
            Align.Center(_previewSprite);
            _previewSprite.IsHidden = false;
        }

        private void ResetAttributeBars()
        {
            _energyBar.Reset();
            _moodBar.Reset();
            _maturityBar.Reset();
        }

        private void UpdateContentLayout()
        {
            if (_contentScroll.IsHidden)
            {
                return;
            }

            var bottom = _behaviorWidget.IsHidden
                ? (_statsPanel.IsHidden
                    ? (_vitalsPanel.IsHidden
                        ? _headerPanel.Height
                        : _vitalsPanel.Y + _vitalsPanel.Height)
                    : _statsPanel.Y + _statsPanel.Height)
                : _behaviorWidget.Y + _behaviorWidget.Height;

            bottom = Math.Max(bottom, _headerPanel.Y + _headerPanel.Height);
            _contentScroll.SetInnerSize(_contentScroll.Width, bottom + SectionGap);
        }

        private Label CreateDetailLabel(Base parent, string name, int y)
        {
            var label = new Label(parent, name)
            {
                FontName = FontName,
                FontSize = BodySize,
                TextColor = Color.White,
                AutoSizeToContents = false,
                TextAlign = Pos.Left | Pos.CenterV,
            };

            label.SetPosition(0, y);
            label.SetSize(parent.Width, DetailLineHeight);
            return label;
        }

        private Button CreateActionButton(string name, int x, int y, string text, Base.GwenEventHandler<MouseButtonState> onClick)
        {
            var button = new Button(this, name)
            {
                FontName = FontName,
                FontSize = ButtonSize,
                TextColor = Color.White,
                Text = text,
            };

            button.SetPosition(x, y);
            button.SetSize(136, 34);
            button.Clicked += onClick;
            return button;
        }

        private void OnInvokeClicked(Base sender, MouseButtonState arguments)
        {
            if (Globals.PetHub.InvokePet())
            {
                _invokeButton.IsDisabled = true;
                _dismissButton.IsDisabled = false;
            }
        }

        private void OnDismissClicked(Base sender, MouseButtonState arguments)
        {
            if (Globals.PetHub.DismissPet())
            {
                _dismissButton.IsDisabled = true;
                _invokeButton.IsDisabled = false;
            }
        }

        private sealed class AttributeBar
        {
            public AttributeBar(Base container, Base background, Base fill, Label valueLabel)
            {
                Container = container;
                Background = background;
                Fill = fill;
                ValueLabel = valueLabel;
            }

            public Base Container { get; }

            public Base Background { get; }

            public Base Fill { get; }

            public Label ValueLabel { get; }

            public void Update(int value, int cap, string text)
            {
                Container.IsHidden = false;
                ValueLabel.Text = text;
                ValueLabel.SetBounds(0, 0, Background.Width, Background.Height);

                var ratio = cap > 0 ? value / (float)cap : 0f;
                ratio = Math.Max(0f, Math.Min(1f, ratio));

                var width = (int)Math.Round(ratio * Background.Width);
                Fill.SetSize(width, Background.Height);
                Fill.IsHidden = width <= 0;
            }

            public void Reset()
            {
                ValueLabel.Text = string.Empty;
                Fill.SetSize(0, Fill.Height);
                Fill.IsHidden = true;
                Container.IsHidden = true;
            }
        }
    }
}
