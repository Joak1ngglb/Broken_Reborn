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
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Enums;
using Intersect.GameObjects;
using Intersect.Network.Packets.Localization;

namespace Intersect.Client.Interface.Game
{
    public partial class QuestOfferWindow : IQuestWindow
    {
        private Button mAcceptButton;
        private Button mDeclineButton;

        private string mQuestOfferText = string.Empty;

        private Guid mLastQuestId = Guid.Empty;

        // Controls
        private WindowControl mQuestOfferWindow;
        private ScrollControl mQuestPromptArea;
        private RichLabel mQuestPromptLabel;
        private Label mQuestPromptTemplate;
        private Label mQuestTitle;

        // Contenedores ya definidos en el JSON de UI
        private readonly ScrollControl _rewardContainer;
        private readonly ScrollControl _rewardItemContainer;
        private readonly ScrollControl _rewardExpContainer;

        // Helpers de layout de recompensas
        private const int RewardPaddingX = 10;
        private const int RewardPaddingY = 10;
        private const int RewardSpacing = 3;
        private const int RewardExpHeight = 44;
        private bool _localizationSubscribed;

        // Cache de widgets creados para medir/ubicar
        private readonly List<Base> _rewardItemWidgets = new();
        private readonly List<Base> _rewardExpWidgets = new();

        public QuestOfferWindow(Canvas gameCanvas)
        {
            mQuestOfferWindow = new WindowControl(gameCanvas, Strings.QuestOffer.Title, false, "QuestOfferWindow");
            mQuestOfferWindow.DisableResizing();
            mQuestOfferWindow.IsClosable = false;

            // Header
            mQuestTitle = new Label(mQuestOfferWindow, "QuestTitle");

            // Área de texto
            mQuestPromptArea = new ScrollControl(mQuestOfferWindow, "QuestOfferArea");
            mQuestPromptTemplate = new Label(mQuestPromptArea, "QuestOfferTemplate");
            mQuestPromptLabel = new RichLabel(mQuestPromptArea);

            _rewardContainer = new ScrollControl(mQuestOfferWindow, "QuestRewardContainer");

            // Contenedores de recompensas (deben existir en el JSON)
            _rewardExpContainer = TryGetOrCreate(_rewardContainer, "QuestRewardExpContainer", 10, 10, 380, RewardExpHeight);
            _rewardItemContainer = TryGetOrCreate(_rewardContainer, "QuestRewardItemContainer", 10, 60, 380, 50);

            // Botones
            mAcceptButton = new Button(mQuestOfferWindow, "AcceptButton");
            mAcceptButton.SetText(Strings.QuestOffer.Accept);
            mAcceptButton.Clicked += _acceptButton_Clicked;

            mDeclineButton = new Button(mQuestOfferWindow, "DeclineButton");
            mDeclineButton.SetText(Strings.QuestOffer.Decline);
            mDeclineButton.Clicked += _declineButton_Clicked;

            // Cargar layout
            mQuestOfferWindow.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

            // Bloqueo de input
            Interface.InputBlockingComponents.Add(mQuestOfferWindow);

            _rewardExpContainer.IsHidden = true;
            _rewardItemContainer.IsHidden = true;
            _rewardContainer.IsHidden = true;

            SubscribeToLocalizationUpdates();
        }

        public void AddRewardWidget(Base widget)
        {
            if (widget == null)
            {
                return;
            }

            var widgetName = widget.Name ?? string.Empty;
            var goesToExp =
                widgetName.Contains("RewardExp", StringComparison.OrdinalIgnoreCase) ||
                widgetName.Equals("ExpChip", StringComparison.OrdinalIgnoreCase) ||
                widgetName.Equals("QuestRewardExpChip", StringComparison.OrdinalIgnoreCase);

            widget.Parent = goesToExp ? _rewardExpContainer : _rewardItemContainer;
            if (goesToExp)
            {
                _rewardExpWidgets.Add(widget);
            }
            else
            {
                _rewardItemWidgets.Add(widget);
            }

            widget.Show();
        }

        public void ClearRewardWidgets()
        {
            ClearChildren(_rewardItemContainer);
            ClearChildren(_rewardExpContainer);

            _rewardItemWidgets.Clear();
            _rewardExpWidgets.Clear();

            _rewardItemContainer.IsHidden = true;
            _rewardExpContainer.IsHidden = true;
            _rewardContainer.IsHidden = true;
        }

        private static void ClearChildren(Base container)
        {
            var children = container.Children?.ToArray();
            if (children == null)
            {
                return;
            }

            foreach (var child in children)
            {
                container.RemoveChild(child, dispose: true);
            }
        }

        private void _declineButton_Clicked(Base sender, MouseButtonState arguments)
        {
            if (Globals.QuestOffers.Count > 0)
            {
                var questId = Globals.QuestOffers[0];
                PacketSender.SendDeclineQuest(questId);
                Globals.QuestOffers.RemoveAt(0);
                Globals.RemoveQuestRewards(questId);
                ClearRewardWidgets();
            }
        }

        private static ScrollControl TryGetOrCreate(ScrollControl parent, string name, int x, int y, int w, int h)
        {
            var child = parent.FindChildByName(name) as ScrollControl;
            if (child == null)
            {
                child = new ScrollControl(parent, name);
                child.SetPosition(x, y);
                child.SetSize(w, h);
                child.EnableScroll(false, true);
            }

            return child;
        }

        private void _acceptButton_Clicked(Base sender, MouseButtonState arguments)
        {
            if (Globals.QuestOffers.Count > 0)
            {
                var questId = Globals.QuestOffers[0];
                PacketSender.SendAcceptQuest(questId);
                Globals.QuestOffers.RemoveAt(0);
                Globals.RemoveQuestRewards(questId);
                ClearRewardWidgets();
            }
        }

        public void Update(QuestDescriptor quest)
        {
            if (quest == null)
            {
                Hide();
                ClearRewardWidgets();
                mLastQuestId = Guid.Empty;
                return;
            }

            RequestLocalization(quest);
            Show();
            var localizedName = GetLocalizedQuestField(quest, "Name", quest.Name);
            mQuestTitle.Text = localizedName;

            var localizedStartDescription = GetLocalizedQuestField(
                quest,
                "StartDescription",
                quest.StartDescription
            );
            if (mQuestOfferText != localizedStartDescription || quest.Id != mLastQuestId)
            {
                mQuestPromptLabel.ClearText();
                mQuestPromptLabel.Width = mQuestPromptArea.Width - mQuestPromptArea.VerticalScrollBar.Width;
                mQuestPromptLabel.AddText(localizedStartDescription, mQuestPromptTemplate);
                mQuestPromptLabel.SizeToChildren(false, true);
                mQuestOfferText = localizedStartDescription;
                LoadRewardWidgets(quest.Id);

                mLastQuestId = quest.Id;
            }
        }

        private void RequestLocalization(QuestDescriptor quest)
        {
            GameLocalization.RequestEntries(
                [
                    new LocalizationRequestEntry(quest.Type.ToString(), quest.Id.ToString(), "Name"),
                    new LocalizationRequestEntry(quest.Type.ToString(), quest.Id.ToString(), "StartDescription")
                ]
            );
        }

        private static string GetLocalizedQuestField(QuestDescriptor quest, string field, string fallback) =>
            GameLocalization.GetTextOrDefault(quest.Type.ToString(), quest.Id, field, fallback);

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
            if (!IsVisible() || mLastQuestId == Guid.Empty)
            {
                return;
            }

            if (!QuestDescriptor.TryGet(mLastQuestId, out var quest))
            {
                return;
            }

            var entityType = quest.Type.ToString();
            var entityId = quest.Id.ToString();
            if (requests.Any(
                    request => request.EntityType == entityType &&
                               request.EntityId == entityId &&
                               (request.Field == "Name" || request.Field == "StartDescription")
                ))
            {
                Update(quest);
            }
        }

        private void LoadRewardWidgets(Guid questId)
        {
            ClearRewardWidgets();

            Globals.QuestExperience.TryGetValue(questId, out var playerExp);
            Globals.QuestJobExperience.TryGetValue(questId, out Dictionary<JobType, long>? jobExp);
            Globals.QuestGuildExperience.TryGetValue(questId, out var guildExp);
            Globals.QuestFactionHonor.TryGetValue(questId, out Dictionary<Factions, int>? factionHonor);

            var anyExp =
                (playerExp > 0) ||
                (jobExp != null && jobExp.Count > 0) ||
                (guildExp > 0) ||
                (factionHonor != null && factionHonor.Count > 0);

            _rewardExpContainer.SetPosition(RewardPaddingX, RewardPaddingY);

            var availableWidth = Math.Max(_rewardContainer.Width - 2 * RewardPaddingX, 1);
            var expHeight = 0;

            if (anyExp)
            {
                _ = new QuestRewardExp(this, playerExp, jobExp, guildExp, factionHonor);

                const int minWidth = 80;
                const int spacing = 4;
                var x = 0;
                var y = 0;
                var rowHeight = RewardExpHeight;

                foreach (var chip in _rewardExpWidgets)
                {
                    var width = Math.Max(chip.Width, minWidth);

                    if (x > 0 && x + width > availableWidth)
                    {
                        x = 0;
                        y += rowHeight + spacing;
                    }

                    chip.SetPosition(x, y);
                    x += width + spacing;
                }

                expHeight = _rewardExpWidgets.Count > 0 ? y + rowHeight : 0;

                _rewardExpContainer.SetSize(availableWidth, Math.Max(expHeight, 1));
                _rewardExpContainer.IsHidden = _rewardExpWidgets.Count == 0;
            }
            else
            {
                _rewardExpContainer.IsHidden = true;
                _rewardExpContainer.SetSize(1, 1);
            }

            if (Globals.QuestRewards.TryGetValue(questId, out var rewards) && rewards.Count > 0)
            {
                foreach (var kv in rewards)
                {
                    _ = new QuestRewardItem(this, kv.Key, kv.Value);
                }

                _rewardItemContainer.IsHidden = _rewardItemWidgets.Count == 0;
            }
            else
            {
                _rewardItemContainer.IsHidden = true;
            }

            _rewardContainer.IsHidden = _rewardExpContainer.IsHidden && _rewardItemContainer.IsHidden;
            if (_rewardContainer.IsHidden)
            {
                return;
            }

            var itemsY = _rewardExpContainer.IsHidden
                ? RewardPaddingY
                : RewardPaddingY + _rewardExpContainer.Height + RewardSpacing;

            _rewardItemContainer.SetPosition(RewardPaddingX, itemsY);

            var itemsHeight = 0;

            if (!_rewardItemContainer.IsHidden && _rewardItemWidgets.Count > 0)
            {
                const int paddingX = 10;
                const int paddingY = 10;
                var insideWidth = Math.Max(availableWidth - 2 * paddingX, 1);

                _rewardItemContainer.SetSize(availableWidth, _rewardItemContainer.Height);

                var first = _rewardItemWidgets[0];
                var itemWidth = Math.Max(first.Width + first.Margin.Left + first.Margin.Right, 40);
                var itemHeight = Math.Max(first.Height + first.Margin.Top + first.Margin.Bottom, 40);

                var itemsPerRow = Math.Max(insideWidth / Math.Max(itemWidth, 1), 1);

                for (var i = 0; i < _rewardItemWidgets.Count; i++)
                {
                    var column = i % itemsPerRow;
                    var row = i / itemsPerRow;

                    var x = paddingX + column * itemWidth;
                    var y = paddingY + row * itemHeight;

                    _rewardItemWidgets[i].SetPosition(x, y);
                    _rewardItemWidgets[i].Show();
                }

                var rows = (int)Math.Ceiling(_rewardItemWidgets.Count / (double)itemsPerRow);
                itemsHeight = paddingY * 2 + rows * itemHeight;

                _rewardItemContainer.SetSize(availableWidth, itemsHeight);
            }

            var totalHeight = RewardPaddingY;
            if (!_rewardExpContainer.IsHidden)
            {
                totalHeight += _rewardExpContainer.Height + RewardSpacing;
            }

            if (!_rewardItemContainer.IsHidden)
            {
                totalHeight += _rewardItemContainer.Height;
            }

            totalHeight += RewardPaddingY;

            _rewardContainer.EnableScroll(false, true);
            _rewardContainer.SetSize(_rewardContainer.Width, totalHeight);
        }

        public void Show() => mQuestOfferWindow.IsHidden = false;
        public void Close() => mQuestOfferWindow.Close();
        public bool IsVisible() => !mQuestOfferWindow.IsHidden;
        public void Hide() => mQuestOfferWindow.IsHidden = true;
    }
}
