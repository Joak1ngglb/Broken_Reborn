using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.Quests;
using Intersect.Framework.Core.Localization;
using Intersect.GameObjects;
using Intersect.Utilities;
using Intersect.Network.Packets.Localization;

namespace Intersect.Client.Interface.Game
{
    public partial class QuestsWindow : Window, IQuestWindow
    {
        // --- Controles base ---
        private   ListBox _questList;

        private   ScrollControl mQuestDescArea;

        // Dentro del área scrolleable:
        private   Label mQuestTitle;
        private   Label mQuestStatus;

        private   RichLabel mQuestDescLabel;
        private   Label mQuestDescTemplateLabel;

        private   Label mQuestCurrentTaskTitle;
        private   RichLabel mQuestCurrentTaskLabel;

        private   ListBox mQuestTasksList;

        private   Button mQuitButton;

        // Recompensas
        private   ScrollControl _rewardContainer;
        private   ScrollControl _rewardItemsContainer;
        private   ScrollControl _rewardExpContainer;

        private QuestDescriptor mSelectedQuest;
        private bool _localizationSubscribed;

        // Helpers layout recompensas
        private const int RewardPaddingX = 10;
        private const int RewardPaddingY = 10;
        private const int RewardSpacing = 3;
        private const int RewardExpHeight = 44;

        // Template label para heredar estilo de tasks (dentro del details content)
        private   Label _taskTemplateLabel;

        // Colores tasks
        private static   Color TaskColorPending = new Color(220, 220, 220, 255);
        private static   Color TaskColorActive = new Color(255, 230, 110, 255);
        private static   Color TaskColorDone = new Color(120, 230, 120, 255);

        // Tamaño/spacing de filas tasks
        private const int TaskRowHeight = 22;
        private const int TaskIconSize = 22;

        private bool _shouldUpdateList;

        // Cache de recompensas
        private   List<Base> _rewardItemWidgets = new();
        private   List<Base> _rewardExpWidgets = new();

        // Layout (espaciados generales)
        private const int HeaderSpacing = 6;
        private const int SectionSpacing = 8;
        private const int TitleToStatusSpacing = 3;
        private const int TitleToDescSpacing = 6;

        public QuestsWindow(Canvas gameCanvas) : base(gameCanvas, Strings.QuestLog.Title, false, nameof(QuestsWindow))
        {
            IsResizable = false;

          
            // Si el JSON no define tamaño, fallback
            if (Width <= 1 || Height <= 1)
            {
                SetSize(760, 520);
            }

            // Agarra controles YA existentes en el árbol del JSON (o crea si faltan)
            _questList = TryGetOrCreateListBox(this, "QuestList");
            _questList.EnableScroll(false, true);
            _questList.IsDisabled = false;
            _questList.IsVisibleInTree = true;

            mQuestDescArea = TryGetOrCreateScroll(this, "QuestDescription");
            mQuestDescArea.EnableScroll(false, true);

            // Title + Status
            mQuestTitle = TryGetOrCreateLabel(mQuestDescArea, "QuestTitle");
            mQuestTitle.SetText("");

            mQuestStatus = TryGetOrCreateLabel(mQuestDescArea, "QuestStatus");
            mQuestStatus.SetText("");

            // Descripción
            mQuestDescTemplateLabel = TryGetOrCreateLabel(mQuestDescArea, "QuestDescriptionTemplate");
            mQuestDescLabel = TryGetOrCreateRichLabel(mQuestDescArea, "QuestDescriptionLabel");

            // Current task
            mQuestCurrentTaskTitle = TryGetOrCreateLabel(mQuestDescArea, "QuestCurrentTaskTitle");
            mQuestCurrentTaskTitle.SetText(Strings.QuestLog.CurrentTask);

            mQuestCurrentTaskLabel = TryGetOrCreateRichLabel(mQuestDescArea, "QuestCurrentTaskLabel");

            // Tasks list
            mQuestTasksList = TryGetOrCreateListBox(mQuestDescArea, "QuestTasksList");
            mQuestTasksList.EnableScroll(false, false);
            mQuestTasksList.Dock = Pos.None;
            mQuestTasksList.Margin = new Margin(0, 0, 0, 0);
            mQuestTasksList.MouseInputEnabled = true; // para wheel propagation

            // Template tasks
            _taskTemplateLabel = TryGetOrCreateLabel(mQuestDescArea, "QuestTaskTemplate");
            _taskTemplateLabel.IsHidden = true;
            if (_taskTemplateLabel.Font == null)
            {
                _taskTemplateLabel.Font = mQuestDescTemplateLabel.Font;
            }
            _taskTemplateLabel.SetTextColor(TaskColorPending, ComponentState.Normal);

            // Rewards: IMPORTANTE: parent correcto = mQuestDescArea (dentro del scroll)
            _rewardContainer = TryGetOrCreateScroll(mQuestDescArea, "QuestRewardContainer");
            _rewardContainer.EnableScroll(false, false);
            _rewardContainer.MouseInputEnabled = true;

            _rewardExpContainer = TryGetOrCreateScroll(_rewardContainer, "QuestRewardExpContainer");
            _rewardItemsContainer = TryGetOrCreateScroll(_rewardContainer, "QuestRewardItemContainer");

            InitRewardContainerDefaults();
            EnsureRewardsAreInsideDetailsScroll(); // << añade este método (abajo)

            // Botón abandonar (fuera del scroll)
            mQuitButton = TryGetOrCreateButton(this, "AbandonQuestButton");
            mQuitButton.SetText(Strings.QuestLog.Abandon);
            mQuitButton.Clicked -= _quitButton_Clicked;
            mQuitButton.Clicked += _quitButton_Clicked;
            // Carga JSON PRIMERO
            LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

            // Scroll fixes (JSON puede venir con zonas muertas)
            if (mQuestDescArea.InnerPanel != null)
            {
                mQuestDescArea.InnerPanel.MouseInputEnabled = true;
            }

            if (mQuestDescArea.VerticalScrollBar != null)
            {
                mQuestDescArea.VerticalScrollBar.IsHidden = false;
                mQuestDescArea.VerticalScrollBar.IsDisabled = false;
                mQuestDescArea.VerticalScrollBar.ScrollAmount = 24;
            }

            mQuestDescArea.BoundsChanged += (_, _) => UpdateDetailsLayout();

            // Estado inicial
            mQuestCurrentTaskTitle.Hide();
            mQuestCurrentTaskLabel.Hide();
            mQuestTasksList.Hide();
            ClearRewardWidgets();

            mQuestTitle.Hide();
            mQuestStatus.Hide();
            mQuestDescLabel.ClearText();

            SubscribeToLocalizationUpdates();
        }


        // -------------------------
        // Helpers "TryGetOrCreate"
        // -------------------------
        private static ScrollControl TryGetOrCreateScroll(Base parent, string name)
        {
            var existing = parent.FindChildByName(name) as ScrollControl;
            if (existing != null) return existing;

            var created = new ScrollControl(parent, name);
            created.EnableScroll(false, true);
            return created;
        }
        private void EnsureRewardsAreInsideDetailsScroll()
        {
            if (_rewardContainer.Parent != mQuestDescArea)
                _rewardContainer.Parent = mQuestDescArea;

            if (_rewardExpContainer.Parent != _rewardContainer)
                _rewardExpContainer.Parent = _rewardContainer;

            if (_rewardItemsContainer.Parent != _rewardContainer)
                _rewardItemsContainer.Parent = _rewardContainer;

            _rewardContainer.EnableScroll(false, false);
            _rewardExpContainer.EnableScroll(false, true);
            _rewardItemsContainer.EnableScroll(false, true);

            _rewardExpContainer.MouseInputEnabled = true;
            _rewardItemsContainer.MouseInputEnabled = true;
        }

        private static ListBox TryGetOrCreateListBox(Base parent, string name)
        {
            var existing = parent.FindChildByName(name) as ListBox;
            if (existing != null) return existing;

            var created = new ListBox(parent, name);
            return created;
        }

        private static Label TryGetOrCreateLabel(Base parent, string name)
        {
            var existing = parent.FindChildByName(name) as Label;
            if (existing != null) return existing;

            var created = new Label(parent, name);
            return created;
        }

        private static RichLabel TryGetOrCreateRichLabel(Base parent, string name)
        {
            // RichLabel no siempre soporta "name" en ctor en todas las builds.
            // Intentamos buscar primero, si no, creamos uno y le ponemos Name.
            var existing = parent.FindChildByName(name) as RichLabel;
            if (existing != null) return existing;

            var created = new RichLabel(parent);
            created.Name = name;
            return created;
        }

        private static Button TryGetOrCreateButton(Base parent, string name)
        {
            var existing = parent.FindChildByName(name) as Button;
            if (existing != null) return existing;

            var created = new Button(parent, name);
            return created;
        }

        private void InitRewardContainerDefaults()
        {
            // Root
            _rewardContainer.EnableScroll(false, false);

            // EXP container
            _rewardExpContainer.EnableScroll(false, true);
            if (_rewardExpContainer.Width <= 1) _rewardExpContainer.SetSize(380, RewardExpHeight);
            if (_rewardExpContainer.Height <= 1) _rewardExpContainer.SetSize(_rewardExpContainer.Width, RewardExpHeight);

            // Items container
            _rewardItemsContainer.EnableScroll(false, true);
            if (_rewardItemsContainer.Width <= 1) _rewardItemsContainer.SetSize(380, 60);
            if (_rewardItemsContainer.Height <= 1) _rewardItemsContainer.SetSize(_rewardItemsContainer.Width, 60);

            _rewardExpContainer.IsHidden = true;
            _rewardItemsContainer.IsHidden = true;
            _rewardContainer.IsHidden = true;
        }

        // -------------------------
        // Botón abandonar
        // -------------------------
        private void _quitButton_Clicked(Base sender, MouseButtonState arguments)
        {
            if (mSelectedQuest == null) return;

            var localizedName = GetLocalizedQuestField(mSelectedQuest, QuestFieldKey.Name, mSelectedQuest.Name);
            _ = new InputBox(
                title: Strings.QuestLog.AbandonTitle.ToString(localizedName),
                prompt: Strings.QuestLog.AbandonPrompt.ToString(localizedName),
                inputType: InputType.YesNo,
                userData: mSelectedQuest.Id,
                onSubmit: (s, e) =>
                {
                    if (s is InputBox inputBox && inputBox.UserData is Guid questId)
                    {
                        PacketSender.SendAbandonQuest(questId);
                        Globals.RemoveQuestRewards(questId);

                        ClearRewardWidgets();
                        UpdateQuestList();
                        UpdateSelectedQuest();
                        UpdateQuestTasks();
                    }
                }
            );
        }

        // -------------------------
        // Update loop
        // -------------------------
        public void Update(bool shouldUpdateList)
        {
            if (!IsVisibleInTree)
            {
                _shouldUpdateList |= shouldUpdateList;
                return;
            }

            UpdateInternal(shouldUpdateList);
        }

        public void NotifyQuestProgressUpdated(IEnumerable<Guid> questIds)
        {
            if (questIds == null || mSelectedQuest == null) return;

            if (!questIds.Contains(mSelectedQuest.Id)) return;

            if (IsHidden || !IsVisibleInTree)
            {
                _shouldUpdateList = true;
                return;
            }

            UpdateInternal(true);
        }

        private void UpdateInternal(bool shouldUpdateList)
        {
            if (shouldUpdateList)
            {
                UpdateQuestList();
                UpdateSelectedQuest();
            }

            if (IsHidden)
            {
                _shouldUpdateList |= shouldUpdateList;
                return;
            }

            if (mSelectedQuest != null)
            {
                UpdateQuestTasks();

                if (Globals.Me.QuestProgress.ContainsKey(mSelectedQuest.Id))
                {
                    if (Globals.Me.QuestProgress[mSelectedQuest.Id].Completed &&
                        Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId == Guid.Empty)
                    {
                        if (!mSelectedQuest.LogAfterComplete)
                        {
                            mSelectedQuest = null;
                            UpdateSelectedQuest();
                        }

                        return;
                    }
                    else
                    {
                        if (Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId == Guid.Empty)
                        {
                            if (!mSelectedQuest.LogBeforeOffer)
                            {
                                mSelectedQuest = null;
                                UpdateSelectedQuest();
                            }
                        }

                        return;
                    }
                }

                if (!mSelectedQuest.LogBeforeOffer)
                {
                    mSelectedQuest = null;
                    UpdateSelectedQuest();
                }
            }
        }

        // -------------------------
        // Lista de quests
        // -------------------------
        private void UpdateQuestList()
        {
            _questList.RemoveAllRows();
            if (Globals.Me == null) return;

            var quests = QuestDescriptor.Lookup.Values.OfType<QuestDescriptor>().ToList();
            var dict = new Dictionary<string, List<Tuple<QuestDescriptor, int, Color>>>();

            RequestQuestListLocalization(quests);

            foreach (QuestDescriptor quest in quests)
            {
                AddQuestToDict(dict, quest);
            }

            foreach (var category in Options.Instance.Quest.Categories)
            {
                if (dict.ContainsKey(category))
                {
                    AddCategoryToList(category, Color.White);

                    var sortedList = dict[category]
                        .OrderBy(l => l.Item2)
                        .ThenBy(l => l.Item1.OrderValue)
                        .ToList();

                    foreach (var qst in sortedList)
                    {
                        var localizedName = GetLocalizedQuestField(qst.Item1, QuestFieldKey.Name, qst.Item1.Name);
                        AddQuestToList(localizedName, qst.Item3, qst.Item1.Id, true);
                    }
                }
            }

            if (dict.ContainsKey(string.Empty))
            {
                var sortedList = dict[string.Empty]
                    .OrderBy(l => l.Item2)
                    .ThenBy(l => l.Item1.OrderValue)
                    .ToList();

                foreach (var qst in sortedList)
                {
                    var localizedName = GetLocalizedQuestField(qst.Item1, QuestFieldKey.Name, qst.Item1.Name);
                    AddQuestToList(localizedName, qst.Item3, qst.Item1.Id, false);
                }
            }
        }

        private void RequestQuestListLocalization(IEnumerable<QuestDescriptor> quests)
        {
            var requests = quests
                .Select(
                    quest => new LocalizationRequestEntry(quest.Type.ToString(), quest.Id.ToString(), QuestFieldKey.Name)
                )
                .ToList();

            GameLocalization.RequestEntries(requests);
        }

        private void RequestQuestLocalization(QuestDescriptor quest)
        {
            var entries = new List<LocalizationRequestEntry>
            {
                new LocalizationRequestEntry(quest.Type.ToString(), quest.Id.ToString(), QuestFieldKey.Name),
                new LocalizationRequestEntry(quest.Type.ToString(), quest.Id.ToString(), QuestFieldKey.BeforeDescription),
                new LocalizationRequestEntry(quest.Type.ToString(), quest.Id.ToString(), QuestFieldKey.InProgressDescription),
                new LocalizationRequestEntry(quest.Type.ToString(), quest.Id.ToString(), QuestFieldKey.EndDescription)
            };

            foreach (var task in quest.Tasks)
            {
                entries.Add(new LocalizationRequestEntry(
                    quest.Type.ToString(),
                    quest.Id.ToString(),
                    GetQuestTaskLocalizationField(task)
                ));
            }

            GameLocalization.RequestEntries(entries);
        }

        private static string GetLocalizedQuestField(QuestDescriptor quest, string field, string fallback) =>
            GameLocalization.GetTextOrDefault(quest.Type.ToString(), quest.Id, field, fallback);

        private static string GetQuestTaskLocalizationField(QuestTaskDescriptor task) =>
            QuestFieldKey.TaskDescription(task.Id);

        private static string GetLocalizedQuestTaskField(
            QuestDescriptor quest,
            QuestTaskDescriptor task,
            string fallback
        ) =>
            GameLocalization.GetTextOrDefault(
                quest.Type.ToString(),
                quest.Id,
                GetQuestTaskLocalizationField(task),
                fallback
            );

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

            if (mSelectedQuest != null)
            {
                var entityType = mSelectedQuest.Type.ToString();
                var entityId = mSelectedQuest.Id.ToString();
                if (requests.Any(
                        request => request.EntityType == entityType &&
                                   request.EntityId == entityId &&
                                   (request.Field == QuestFieldKey.Name ||
                                    request.Field == QuestFieldKey.BeforeDescription ||
                                    request.Field == QuestFieldKey.InProgressDescription ||
                                    request.Field == QuestFieldKey.EndDescription ||
                                    request.Field.StartsWith("Task:", StringComparison.Ordinal))
                    ))
                {
                    UpdateSelectedQuest();
                }
            }

            UpdateQuestList();
        }

        private void AddQuestToDict(Dictionary<string, List<Tuple<QuestDescriptor, int, Color>>> dict, QuestDescriptor quest)
        {
            var category = string.Empty;
            var add = false;
            var color = Color.White;
            var orderVal = -1;

            if (Globals.Me.QuestProgress.ContainsKey(quest.Id))
            {
                if (Globals.Me.QuestProgress[quest.Id].TaskId != Guid.Empty)
                {
                    add = true;
                    category = !TextUtils.IsNone(quest.InProgressCategory) ? quest.InProgressCategory : "";
                    color = CustomColors.QuestWindow.InProgress;
                    orderVal = 1;
                }
                else
                {
                    if (Globals.Me.QuestProgress[quest.Id].Completed)
                    {
                        if (quest.LogAfterComplete)
                        {
                            add = true;
                            category = !TextUtils.IsNone(quest.CompletedCategory) ? quest.CompletedCategory : "";
                            color = CustomColors.QuestWindow.Completed;
                            orderVal = 3;
                        }
                    }
                    else if (quest.LogBeforeOffer && !Globals.Me.HiddenQuests.Contains(quest.Id))
                    {
                        add = true;
                        category = !TextUtils.IsNone(quest.UnstartedCategory) ? quest.UnstartedCategory : "";
                        color = CustomColors.QuestWindow.NotStarted;
                        orderVal = 2;
                    }
                }
            }
            else if (quest.LogBeforeOffer && !Globals.Me.HiddenQuests.Contains(quest.Id))
            {
                add = true;
                category = !TextUtils.IsNone(quest.UnstartedCategory) ? quest.UnstartedCategory : "";
                color = CustomColors.QuestWindow.NotStarted;
                orderVal = 2;
            }

            if (!add) return;

            if (!dict.ContainsKey(category))
            {
                dict.Add(category, new List<Tuple<QuestDescriptor, int, Color>>());
            }

            dict[category].Add(new Tuple<QuestDescriptor, int, Color>(quest, orderVal, color));
        }

        private void AddQuestToList(string name, Color clr, Guid questId, bool indented = true)
        {
            var item = _questList.AddRow((indented ? "\t\t\t" : "") + name);
            item.UserData = questId;
            item.Clicked -= QuestListItem_Clicked;
            item.Clicked += QuestListItem_Clicked;

            // Quitamos el Selected handler que deseleccionaba todo (rompía UX)
            item.SetTextColor(clr);
            item.SetSize(200, 25);
        }

        private void AddCategoryToList(string name, Color clr)
        {
            var item = _questList.AddRow(name);
            item.MouseInputEnabled = false;
            item.SetTextColor(clr);
            item.SetSize(200, 25);
        }

        private void QuestListItem_Clicked(Base sender, MouseButtonState arguments)
        {
            if (sender.UserData is not Guid questId) return;

            if (!QuestDescriptor.TryGet(questId, out var questDescriptor))
            {
                _questList.UnselectAll();
                return;
            }

            mSelectedQuest = questDescriptor;
            UpdateSelectedQuest();
            UpdateQuestTasks();
        }

        // -------------------------
        // Selección quest / details
        // -------------------------
        private void UpdateSelectedQuest()
        {
            _questList.Show();

            // Limpia siempre
            mQuestDescLabel.ClearText();
            mQuestCurrentTaskLabel.ClearText();
            mQuitButton.IsDisabled = true;

            mQuestCurrentTaskTitle.Hide();
            mQuestCurrentTaskLabel.Hide();

            mQuestTasksList.RemoveAllRows();
            mQuestTasksList.Hide();

            ClearRewardWidgets();

            if (mSelectedQuest == null)
            {
                mQuestTitle.Hide();
                mQuestStatus.Hide();
                mQuestDescArea.Hide();
                mQuitButton.Hide();
                UpdateDetailsLayout();
                return;
            }

            RequestQuestLocalization(mSelectedQuest);
            // Mostrar panel derecho
            mQuestDescArea.IsHidden = false;

            // Title + Status dentro del scroll
            mQuestTitle.IsHidden = false;
            mQuestTitle.Text = GetLocalizedQuestField(mSelectedQuest, QuestFieldKey.Name, mSelectedQuest.Name);

            mQuestStatus.IsHidden = false;
            mQuestStatus.SetText("");

            // Determinar estado y texto
            if (Globals.Me.QuestProgress.ContainsKey(mSelectedQuest.Id))
            {
                if (Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId != Guid.Empty)
                {
                    // En progreso
                    mQuestStatus.SetText(Strings.QuestLog.InProgress);
                    mQuestStatus.SetTextColor(CustomColors.QuestWindow.InProgress, ComponentState.Normal);
                    mQuestDescTemplateLabel.SetTextColor(CustomColors.QuestWindow.QuestDesc, ComponentState.Normal);

                    var localizedInProgressDescription = GetLocalizedQuestField(
                        mSelectedQuest,
                        QuestFieldKey.InProgressDescription,
                        mSelectedQuest.InProgressDescription
                    );
                    if (localizedInProgressDescription.Length > 0)
                    {
                        mQuestDescLabel.AddText(localizedInProgressDescription, mQuestDescTemplateLabel);
                        mQuestDescLabel.AddLineBreak();
                        mQuestDescLabel.AddLineBreak();
                    }

                    // Current task text
                    for (var i = 0; i < mSelectedQuest.Tasks.Count; i++)
                    {
                        if (mSelectedQuest.Tasks[i].Id == Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId)
                        {
                            var localizedTaskDescription = GetLocalizedQuestTaskField(
                                mSelectedQuest,
                                mSelectedQuest.Tasks[i],
                                mSelectedQuest.Tasks[i].Description
                            );
                            if (localizedTaskDescription.Length > 0)
                            {
                                mQuestCurrentTaskLabel.AddText(localizedTaskDescription, mQuestDescTemplateLabel);
                                mQuestCurrentTaskLabel.AddLineBreak();
                                mQuestCurrentTaskLabel.AddLineBreak();
                            }

                            if (mSelectedQuest.Tasks[i].Objective == QuestObjective.GatherItems)
                            {
                                mQuestCurrentTaskLabel.AddText(
                                    Strings.QuestLog.TaskItem.ToString(
                                        Globals.Me.QuestProgress[mSelectedQuest.Id].TaskProgress,
                                        mSelectedQuest.Tasks[i].Quantity,
                                        GetLocalizedItemName(mSelectedQuest.Tasks[i].TargetId)
                                    ),
                                    mQuestDescTemplateLabel
                                );
                            }
                            else if (mSelectedQuest.Tasks[i].Objective == QuestObjective.KillNpcs)
                            {
                                mQuestCurrentTaskLabel.AddText(
                                    Strings.QuestLog.TaskNpc.ToString(
                                        Globals.Me.QuestProgress[mSelectedQuest.Id].TaskProgress,
                                        mSelectedQuest.Tasks[i].Quantity,
                                        NPCDescriptor.GetName(mSelectedQuest.Tasks[i].TargetId)
                                    ),
                                    mQuestDescTemplateLabel
                                );
                            }
                        }
                    }

                    mQuitButton.IsDisabled = !mSelectedQuest.Quitable;
                    mQuestCurrentTaskTitle.Show();
                    mQuestCurrentTaskLabel.Show();
                }
                else
                {
                    if (Globals.Me.QuestProgress[mSelectedQuest.Id].Completed)
                    {
                        if (mSelectedQuest.LogAfterComplete)
                        {
                            mQuestStatus.SetText(Strings.QuestLog.Completed);
                            mQuestStatus.SetTextColor(CustomColors.QuestWindow.Completed, ComponentState.Normal);
                            var localizedEndDescription = GetLocalizedQuestField(
                                mSelectedQuest,
                                QuestFieldKey.EndDescription,
                                mSelectedQuest.EndDescription
                            );
                            mQuestDescLabel.AddText(localizedEndDescription, mQuestDescTemplateLabel);
                        }
                    }
                    else
                    {
                        if (mSelectedQuest.LogBeforeOffer)
                        {
                            mQuestStatus.SetText(Strings.QuestLog.NotStarted);
                            mQuestStatus.SetTextColor(CustomColors.QuestWindow.NotStarted, ComponentState.Normal);
                            var localizedBeforeDescription = GetLocalizedQuestField(
                                mSelectedQuest,
                                QuestFieldKey.BeforeDescription,
                                mSelectedQuest.BeforeDescription
                            );
                            mQuestDescLabel.AddText(localizedBeforeDescription, mQuestDescTemplateLabel);
                        }
                    }
                }
            }
            else
            {
                if (mSelectedQuest.LogBeforeOffer)
                {
                    mQuestStatus.SetText(Strings.QuestLog.NotStarted);
                    mQuestStatus.SetTextColor(CustomColors.QuestWindow.NotStarted, ComponentState.Normal);
                    var localizedBeforeDescription = GetLocalizedQuestField(
                        mSelectedQuest,
                        QuestFieldKey.BeforeDescription,
                        mSelectedQuest.BeforeDescription
                    );
                    mQuestDescLabel.AddText(localizedBeforeDescription, mQuestDescTemplateLabel);
                }
            }

            // Botón abandonar (en window, fuera del scroll)
            mQuitButton.Show();

            // Recompensas
            LoadRewardWidgets(mSelectedQuest.Id);

            // Layout final
            UpdateDetailsLayout();
        }

        private int GetDetailsContentWidth()
        {
            var w = mQuestDescArea.Width;

            var sb = mQuestDescArea.VerticalScrollBar;
            if (sb != null && !sb.IsHidden)
            {
                w -= sb.Width;
            }

            return Math.Max(0, w);
        }

        private void UpdateDetailsLayout()
        {
            var contentWidth = GetDetailsContentWidth();
            var y = 0;
            const int bottomPadding = 12;

            // Title
            if (!mQuestTitle.IsHidden)
            {
                mQuestTitle.Width = contentWidth;
                mQuestTitle.SizeToChildren(false, true);
                mQuestTitle.SetPosition(0, y);
                y += mQuestTitle.Height + 3;
            }

            // Status
            if (!mQuestStatus.IsHidden)
            {
                mQuestStatus.Width = contentWidth;
                mQuestStatus.SizeToChildren(false, true);
                mQuestStatus.SetPosition(0, y);
                y += mQuestStatus.Height + 6;
            }

            // Description
            mQuestDescLabel.Width = contentWidth;
            mQuestDescLabel.SizeToChildren(false, true);
            mQuestDescLabel.SetPosition(0, y);
            y += mQuestDescLabel.Height + 8;

            // Current task
            if (!mQuestCurrentTaskTitle.IsHidden && !mQuestCurrentTaskLabel.IsHidden)
            {
                mQuestCurrentTaskTitle.Width = contentWidth;
                mQuestCurrentTaskTitle.SizeToChildren(false, true);
                mQuestCurrentTaskTitle.SetPosition(0, y);
                y += mQuestCurrentTaskTitle.Height + 2;

                mQuestCurrentTaskLabel.Width = contentWidth;
                mQuestCurrentTaskLabel.SizeToChildren(false, true);
                mQuestCurrentTaskLabel.SetPosition(0, y);
                y += mQuestCurrentTaskLabel.Height + 8;
            }

            // Tasks list
            if (!mQuestTasksList.IsHidden)
            {
                mQuestTasksList.Width = contentWidth;
                mQuestTasksList.SetPosition(0, y);
                y += mQuestTasksList.Height + 8;
            }

            // Rewards
            if (!_rewardContainer.IsHidden)
            {
                _rewardContainer.SetSize(contentWidth, _rewardContainer.Height);
                _rewardContainer.SetPosition(0, y);
                y += _rewardContainer.Height + 8;
            }

            y += bottomPadding;

            // CLAVE: el content interno tiene que ser EXACTAMENTE el total calculado
            // CLAVE: inner size del scroll = content size real
            mQuestDescArea.SetInnerSize(Math.Max(contentWidth, 1), Math.Max(y, 1));

            // Scroll ON (sin resets)
            mQuestDescArea.EnableScroll(false, true);

            if (mQuestDescArea.VerticalScrollBar != null)
            {
                mQuestDescArea.VerticalScrollBar.ScrollAmount = 24;
            }
        }

        // -------------------------
        // Show/Hide
        // -------------------------
        protected override void EnsureInitialized()
        {
            LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
            UpdateQuestList();
            UpdateSelectedQuest();
            UpdateQuestTasks();
        }

        public void Show()
        {
            mSelectedQuest = null;
            _questList.UnselectAll();

            UpdateSelectedQuest();
            UpdateQuestTasks();

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
            mSelectedQuest = null;
            _questList.UnselectAll();
            UpdateSelectedQuest();
            UpdateQuestTasks();
        }

        // -------------------------
        // Rewards: IQuestWindow API
        // -------------------------
        public void AddRewardWidget(Base widget)
        {
            if (widget == null) return;

            var n = widget.Name ?? string.Empty;

            var goesToExp =
                n.Contains("RewardExp", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("ExpChip", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("QuestRewardExpChip", StringComparison.OrdinalIgnoreCase);

            if (goesToExp)
            {
                widget.Parent = _rewardExpContainer;
                _rewardExpWidgets.Add(widget);
            }
            else
            {
                widget.Parent = _rewardItemsContainer;
                _rewardItemWidgets.Add(widget);
            }

            widget.Show();
        }

        public void ClearRewardWidgets()
        {
            ClearChildren(_rewardItemsContainer);
            ClearChildren(_rewardExpContainer);
            _rewardItemWidgets.Clear();
            _rewardExpWidgets.Clear();

            _rewardItemsContainer.IsHidden = true;
            _rewardExpContainer.IsHidden = true;
            _rewardContainer.IsHidden = true;
        }

        private static void ClearChildren(Base container)
        {
            var kids = container.Children?.ToArray();
            if (kids == null) return;

            foreach (var c in kids)
            {
                container.RemoveChild(c, true);
            }
        }

        private void LoadRewardWidgets(Guid questId)
        {
            ClearRewardWidgets();

            // --- EXP ---
            Globals.QuestExperience.TryGetValue(questId, out var playerExp);
            Globals.QuestJobExperience.TryGetValue(questId, out Dictionary<JobType, long>? jobExp);
            Globals.QuestGuildExperience.TryGetValue(questId, out var guildExp);
            Globals.QuestFactionHonor.TryGetValue(questId, out Dictionary<Factions, int>? factionHonor);

            var anyExp =
                (playerExp > 0) ||
                (jobExp != null && jobExp.Count > 0) ||
                (guildExp > 0) ||
                (factionHonor != null && factionHonor.Count > 0);

            // Ancho disponible del root rewards
            var contentWidth = GetDetailsContentWidth();
            _rewardContainer.SetSize(contentWidth, _rewardContainer.Height);

            var availW = Math.Max(_rewardContainer.Width - 2 * RewardPaddingX, 1);

            int expHeight = 0;

            if (anyExp)
            {
                // Crea chips (se agregan via AddRewardWidget)
                _ = new QuestRewardExp(this, playerExp, jobExp, guildExp, factionHonor);

                // Layout wrap horizontal
                const int minW = 80;
                const int spacing = 4;
                int x = 0;
                int y = 0;
                int rowH = RewardExpHeight;

                foreach (var chip in _rewardExpWidgets)
                {
                    var w = Math.Max(chip.Width, minW);

                    if (x > 0 && x + w > availW)
                    {
                        x = 0;
                        y += rowH + spacing;
                    }

                    chip.SetPosition(x, y);
                    x += w + spacing;
                }

                expHeight = (_rewardExpWidgets.Count > 0) ? (y + rowH) : 0;

                _rewardExpContainer.SetPosition(RewardPaddingX, RewardPaddingY);
                _rewardExpContainer.SetSize(availW, Math.Max(expHeight, 1));
                _rewardExpContainer.IsHidden = _rewardExpWidgets.Count == 0;
            }
            else
            {
                _rewardExpContainer.IsHidden = true;
                _rewardExpContainer.SetSize(1, 1);
            }

            // --- Ítems ---
            if (Globals.QuestRewards.TryGetValue(questId, out var rewards) && rewards.Count > 0)
            {
                foreach (var kv in rewards)
                {
                    _ = new QuestRewardItem(this, kv.Key, kv.Value);
                }

                _rewardItemsContainer.IsHidden = _rewardItemWidgets.Count == 0;
            }
            else
            {
                _rewardItemsContainer.IsHidden = true;
            }

            // Si no hay nada, escondemos root
            _rewardContainer.IsHidden = _rewardExpContainer.IsHidden && _rewardItemsContainer.IsHidden;
            if (_rewardContainer.IsHidden) return;

            // Posición items debajo de exp
            var itemsY = _rewardExpContainer.IsHidden
                ? RewardPaddingY
                : RewardPaddingY + _rewardExpContainer.Height + RewardSpacing;

            _rewardItemsContainer.SetPosition(RewardPaddingX, itemsY);

            // Grilla items
            if (!_rewardItemsContainer.IsHidden && _rewardItemWidgets.Count > 0)
            {
                const int xPad = 10, yPad = 10;
                var insideW = Math.Max(availW - 2 * xPad, 1);

                _rewardItemsContainer.SetSize(availW, _rewardItemsContainer.Height);

                var first = _rewardItemWidgets[0];
                var itemW = Math.Max(first.Width + first.Margin.Left + first.Margin.Right, 40);
                var itemH = Math.Max(first.Height + first.Margin.Top + first.Margin.Bottom, 40);

                var perRow = Math.Max(insideW / Math.Max(itemW, 1), 1);

                for (int i = 0; i < _rewardItemWidgets.Count; i++)
                {
                    var col = i % perRow;
                    var row = i / perRow;

                    var x = xPad + col * itemW;
                    var y = yPad + row * itemH;

                    _rewardItemWidgets[i].SetPosition(x, y);
                    _rewardItemWidgets[i].Show();
                }

                var rows = (int)Math.Ceiling(_rewardItemWidgets.Count / (double)perRow);
                var itemsHeight = yPad * 2 + rows * itemH;

                _rewardItemsContainer.SetSize(availW, itemsHeight);
            }

            // Alto total root rewards
            var totalH = RewardPaddingY;
            if (!_rewardExpContainer.IsHidden) totalH += _rewardExpContainer.Height + RewardSpacing;
            if (!_rewardItemsContainer.IsHidden) totalH += _rewardItemsContainer.Height;
            totalH += RewardPaddingY;

            _rewardContainer.SetSize(_rewardContainer.Width, totalH);
            _rewardContainer.Show();
        }

        // -------------------------
        // Tasks helpers
        // -------------------------
        private bool IsTaskCompleted(QuestTaskDescriptor task)
        {
            if (mSelectedQuest == null || Globals.Me?.QuestProgress == null) return false;
            if (!Globals.Me.QuestProgress.TryGetValue(mSelectedQuest.Id, out var progress)) return false;

            if (progress.Completed) return true;

            var currentIndex = mSelectedQuest.GetTaskIndex(progress.TaskId);
            var taskIndex = mSelectedQuest.GetTaskIndex(task.Id);

            return currentIndex > taskIndex;
        }

        private int GetTaskProgress(QuestTaskDescriptor task)
        {
            if (mSelectedQuest == null || Globals.Me?.QuestProgress == null) return 0;
            if (!Globals.Me.QuestProgress.TryGetValue(mSelectedQuest.Id, out var progress)) return 0;

            var currentIndex = mSelectedQuest.GetTaskIndex(progress.TaskId);
            var taskIndex = mSelectedQuest.GetTaskIndex(task.Id);

            if (progress.Completed || currentIndex > taskIndex) return task.Quantity;
            if (currentIndex == taskIndex) return progress.TaskProgress;

            return 0;
        }

        private static void ApplyTaskStyle(Label label, Label template, Color color)
        {
            if (template?.Font != null)
            {
                label.Font = template.Font;
            }

            label.SetTextColor(color, ComponentState.Normal);

            int h = Math.Max(TaskRowHeight - 2, 12);
            label.Height = h;
        }

        private void UpdateQuestTasks()
        {
            mQuestTasksList.RemoveAllRows();

            if (mSelectedQuest == null || mSelectedQuest.Tasks.Count == 0)
            {
                mQuestTasksList.Hide();
                UpdateDetailsLayout();
                return;
            }

            mQuestTasksList.Show();

            int currentIndex = -1;
            bool questCompleted = false;

            if (Globals.Me?.QuestProgress != null &&
                Globals.Me.QuestProgress.TryGetValue(mSelectedQuest.Id, out var prog))
            {
                questCompleted = prog.Completed;
                currentIndex = mSelectedQuest.GetTaskIndex(prog.TaskId);
            }

            for (int i = 0; i < mSelectedQuest.Tasks.Count; i++)
            {
                var task = mSelectedQuest.Tasks[i];

                bool completed = IsTaskCompleted(task);
                int progress = GetTaskProgress(task);
                bool isCurrent = !questCompleted && (currentIndex == i);

                var desc = GetLocalizedQuestTaskField(mSelectedQuest, task, task.Description);

                switch (task.Objective)
                {
                    case QuestObjective.GatherItems:
                        desc = Strings.QuestLog.TaskItem.ToString(
                            progress, task.Quantity, GetLocalizedItemName(task.TargetId));
                        break;

                    case QuestObjective.KillNpcs:
                        desc = Strings.QuestLog.TaskNpc.ToString(
                            progress, task.Quantity, NPCDescriptor.GetName(task.TargetId));
                        break;
                }

                var row = mQuestTasksList.AddRow(string.Empty);
                row.Height = TaskRowHeight;

                var icon = new ImagePanel(row) { Width = TaskIconSize, Height = TaskIconSize };

                var texture = GameContentManager.Current.GetTexture(
                    Framework.Content.TextureType.Gui,
                    completed ? "checkboxfull.png" : "checkboxempty.png"
                );

                if (texture != null) icon.Texture = texture;
                icon.SetPosition(2, (TaskRowHeight - TaskIconSize) / 2);

                var lbl = new Label(row) { Text = "• " + desc };
                lbl.SetPosition(2 + TaskIconSize + 4, 1);

                ApplyTaskStyle(lbl, _taskTemplateLabel, completed ? TaskColorDone : (isCurrent ? TaskColorActive : TaskColorPending));
            }

            mQuestTasksList.Invalidate();

            // Tamaño consistente con el ancho disponible (no uses Width actual, puede estar 0)
            var contentWidth = GetDetailsContentWidth();
            var taskHeight = mSelectedQuest.Tasks.Count * TaskRowHeight;

            mQuestTasksList.SetSize(Math.Max(contentWidth, 1), taskHeight);

            UpdateDetailsLayout();
        }
    }
}
