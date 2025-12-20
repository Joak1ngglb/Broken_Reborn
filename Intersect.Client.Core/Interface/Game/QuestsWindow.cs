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
using Intersect.GameObjects;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Game
{
    public partial class QuestsWindow : IQuestWindow
    {

        private readonly ScrollControl mQuestDescArea;
        private readonly RichLabel mQuestDescLabel;
        private readonly Label mQuestDescTemplateLabel;

        private readonly ListBox _questList;
        private readonly ScrollControl mQuestTasksContainer;
        private readonly ListBox mQuestTasksList;

        private readonly Label mQuestStatus;

        // Window + título
        private readonly WindowControl mQuestsWindow;
        private readonly Label mQuestTitle;

        private readonly Button mQuitButton;

        // Contenedor raíz de recompensas (ya existía)
        private readonly ScrollControl _rewardContainer;

        // NUEVO: sub-contenedores para recompensas
        private readonly ScrollControl _rewardItemsContainer;
        private readonly ScrollControl _rewardExpContainer;

        private QuestDescriptor mSelectedQuest;

        // Helpers de layout recompensas
        private const int RewardPaddingX = 10;
        private const int RewardPaddingY = 10;
        private const int RewardSpacing = 3;
        private const int RewardExpHeight = 44;
        private const int DetailPadding = 10;
        private const int MinimumDetailWidth = 360;
        public QuestsWindow(Canvas gameCanvas)
        {
            mQuestsWindow = new WindowControl(gameCanvas, Strings.QuestLog.Title, false, "QuestsWindow");
            mQuestsWindow.DisableResizing();

            mQuestTitle = new Label(mQuestsWindow, "QuestTitle");
            mQuestTitle.SetText("");

            mQuestStatus = new Label(mQuestsWindow, "QuestStatus");
            mQuestStatus.SetText("");

            mQuestDescArea = new ScrollControl(mQuestsWindow, "QuestDescription");
            mQuestDescArea.EnableScroll(false, true);
            mQuestDescTemplateLabel = new Label(mQuestDescArea, "QuestDescriptionTemplate");
            mQuestDescLabel = new RichLabel(mQuestDescArea);

            mQuestDescArea.BoundsChanged += (_, _) => UpdateDescriptionLayout();

            _questList = new ListBox(mQuestsWindow, "QuestList");
            _questList.EnableScroll(false, true);

            mQuestTasksContainer = new ScrollControl(mQuestsWindow, "QuestTasksContainer");
            mQuestTasksContainer.EnableScroll(false, true);
            mQuestTasksList = new ListBox(mQuestTasksContainer, "QuestTasksList");
            mQuestTasksList.EnableScroll(false, true);
            mQuestTasksList.Dock = Pos.Fill;
            mQuestTasksList.Margin = new Margin(0, 0, 0, 0);
            _rewardContainer = new ScrollControl(mQuestsWindow, "QuestRewardContainer");

            // Intentamos tomar sub-controles del JSON por nombre; si no existen, los creamos
            _rewardExpContainer = TryGetOrCreate(_rewardContainer, "QuestRewardExpContainer", 10, 10, 380, RewardExpHeight);
            _rewardItemsContainer = TryGetOrCreate(_rewardContainer, "QuestRewardItemContainer", 10, 60, 380, 50);


            mQuitButton = new Button(mQuestsWindow, "AbandonQuestButton");
            mQuitButton.SetText(Strings.QuestLog.Abandon);
            mQuitButton.Clicked += _quitButton_Clicked;

            mQuestsWindow.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

            ApplyCompactLayout();

            // Inicial oculto por defecto
            _rewardExpContainer.IsHidden = true;
            _rewardItemsContainer.IsHidden = true;
            _rewardContainer.IsHidden = true;
        }

        private void ApplyCompactLayout()
        {
            HideLegacyLists();

            var detailWidth = Math.Max(
                MinimumDetailWidth - DetailPadding * 2,
                Math.Max(mQuestDescArea.Width, _rewardContainer.Width)
            );

            var newWindowWidth = detailWidth + DetailPadding * 2;

            mQuestTitle.SetPosition(DetailPadding, mQuestTitle.Y);
            mQuestStatus.SetPosition(DetailPadding, mQuestStatus.Y);
            mQuestDescArea.SetPosition(DetailPadding, mQuestDescArea.Y);
            _rewardContainer.SetPosition(DetailPadding, _rewardContainer.Y);
            mQuitButton.SetPosition(DetailPadding, mQuitButton.Y);

            mQuestsWindow.SetSize(newWindowWidth, mQuestsWindow.Height);

            mQuestDescArea.SetSize(detailWidth, mQuestDescArea.Height);
            _rewardContainer.SetSize(detailWidth, _rewardContainer.Height);

            UpdateDescriptionLayout();
        }

        private void HideLegacyLists()
        {
            _questList.Hide();
            _questList.SetSize(0, 0);

            mQuestTasksList.RemoveAllRows();
            mQuestTasksList.SetSize(0, 0);

            mQuestTasksContainer.Hide();
            mQuestTasksContainer.SetSize(0, 0);
        }

        private static ScrollControl TryGetOrCreate(ScrollControl parent, string name, int x, int y, int w, int h)
        {
            // Busca un hijo con ese name; si no hay, crea uno
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

        private void _quitButton_Clicked(Base sender, MouseButtonState arguments)
        {
            if (mSelectedQuest != null)
            {
                _ = new InputBox(
                    title: Strings.QuestLog.AbandonTitle.ToString(mSelectedQuest.Name),
                    prompt: Strings.QuestLog.AbandonPrompt.ToString(mSelectedQuest.Name),
                    inputType: InputType.YesNo,
                    userData: mSelectedQuest.Id,
                    onSubmit: (s, e) =>
                    {
                        if (s is InputBox inputBox && inputBox.UserData is Guid questId)
                        {
                            PacketSender.SendAbandonQuest(questId);
                            Globals.RemoveQuestRewards(questId);
                            // Limpia visual
                            ClearRewardWidgets();
                            UpdateQuestList();
                            UpdateSelectedQuest();
                            UpdateQuestTasks();
                        }
                    }
                );
            }
        }

        private bool _shouldUpdateList;
        // Cache de recompensas creadas (para medir/posicionar)
        private readonly List<Base> _rewardItemWidgets = new();
        private readonly List<Base> _rewardExpWidgets = new();

        public void Update(bool shouldUpdateList)
        {
            if (!mQuestsWindow.IsVisibleInTree)
            {
                _shouldUpdateList |= shouldUpdateList;
                return;
            }

            UpdateInternal(shouldUpdateList);
        }

        public void NotifyQuestProgressUpdated(IEnumerable<Guid> questIds)
        {
            if (questIds == null)
            {
                return;
            }

            if (mSelectedQuest == null)
            {
                return;
            }

            var selectedQuestUpdated = questIds.Contains(mSelectedQuest.Id);

            if (!selectedQuestUpdated)
            {
                return;
            }

            if (mQuestsWindow.IsHidden || !mQuestsWindow.IsVisibleInTree)
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

            if (mQuestsWindow.IsHidden)
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

        private void UpdateQuestList()
        {
            if (Globals.Me == null)
            {
                mSelectedQuest = null;
                Globals.QuestWindowSelectedQuestId = null;

                return;
            }

            var questBuckets = BuildQuestDictionary();

            mSelectedQuest =
                SelectPreferredQuest(questBuckets, Globals.QuestWindowSelectedQuestId) ??
                SelectFirstQuest(questBuckets);

            Globals.QuestWindowSelectedQuestId = mSelectedQuest?.Id;
        }

        private Dictionary<string, List<Tuple<QuestDescriptor, int, Color>>> BuildQuestDictionary()
        {
            var dict = new Dictionary<string, List<Tuple<QuestDescriptor, int, Color>>>();

            foreach (QuestDescriptor quest in QuestDescriptor.Lookup.Values)
            {
                if (quest != null)
                {
                    AddQuestToDict(dict, quest);
                }
            }

            return dict;
        }

        private QuestDescriptor? SelectPreferredQuest(
            Dictionary<string, List<Tuple<QuestDescriptor, int, Color>>> questBuckets,
            Guid? preferredQuestId)
        {
            if (!preferredQuestId.HasValue)
            {
                return null;
            }

            if (!QuestDescriptor.TryGet(preferredQuestId.Value, out var questDescriptor))
            {
                return null;
            }

            return questBuckets.Values.Any(list => list.Any(q => q.Item1 == questDescriptor)) ? questDescriptor : null;
        }

        private QuestDescriptor? SelectFirstQuest(
            Dictionary<string, List<Tuple<QuestDescriptor, int, Color>>> questBuckets)
        {
            foreach (var category in Options.Instance.Quest.Categories)
            {
                if (questBuckets.TryGetValue(category, out var list))
                {
                    var sortedList = list.OrderBy(l => l.Item2).ThenBy(l => l.Item1.OrderValue).ToList();
                    if (sortedList.Count > 0)
                    {
                        return sortedList[0].Item1;
                    }
                }
            }

            if (questBuckets.TryGetValue(string.Empty, out var uncategorized))
            {
                var sortedList = uncategorized.OrderBy(l => l.Item2).ThenBy(l => l.Item1.OrderValue).ToList();
                if (sortedList.Count > 0)
                {
                    return sortedList[0].Item1;
                }
            }

            return null;
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

        private void UpdateSelectedQuest()
        {
            ApplyCompactLayout();

            if (mSelectedQuest == null)
            {
                Globals.QuestWindowSelectedQuestId = null;

                mQuestTitle.Hide();
                mQuestDescArea.Hide();
                mQuestStatus.Hide();
                mQuitButton.Hide();
                ClearRewardWidgets();
                return;
            }

            mQuestDescLabel.ClearText();
            mQuitButton.IsDisabled = true;

            if (Globals.Me.QuestProgress.ContainsKey(mSelectedQuest.Id))
            {
                if (Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId != Guid.Empty)
                {
                    // En progreso
                    mQuestStatus.SetText(Strings.QuestLog.InProgress);
                    mQuestStatus.SetTextColor(CustomColors.QuestWindow.InProgress, ComponentState.Normal);
                    mQuestDescTemplateLabel.SetTextColor(CustomColors.QuestWindow.QuestDesc, ComponentState.Normal);

                    if (mSelectedQuest.InProgressDescription.Length > 0)
                    {
                        mQuestDescLabel.AddText(mSelectedQuest.InProgressDescription, mQuestDescTemplateLabel);
                        mQuestDescLabel.AddLineBreak();
                        mQuestDescLabel.AddLineBreak();
                    }

                    mQuestDescLabel.AddText(Strings.QuestLog.CurrentTask, mQuestDescTemplateLabel);
                    mQuestDescLabel.AddLineBreak();

                    for (var i = 0; i < mSelectedQuest.Tasks.Count; i++)
                    {
                        if (mSelectedQuest.Tasks[i].Id == Globals.Me.QuestProgress[mSelectedQuest.Id].TaskId)
                        {
                            if (mSelectedQuest.Tasks[i].Description.Length > 0)
                            {
                                mQuestDescLabel.AddText(mSelectedQuest.Tasks[i].Description, mQuestDescTemplateLabel);
                                mQuestDescLabel.AddLineBreak();
                                mQuestDescLabel.AddLineBreak();
                            }

                            if (mSelectedQuest.Tasks[i].Objective == QuestObjective.GatherItems)
                            {
                                mQuestDescLabel.AddText(
                                    Strings.QuestLog.TaskItem.ToString(
                                        Globals.Me.QuestProgress[mSelectedQuest.Id].TaskProgress,
                                        mSelectedQuest.Tasks[i].Quantity,
                                        ItemDescriptor.GetName(mSelectedQuest.Tasks[i].TargetId)
                                    ),
                                    mQuestDescTemplateLabel
                                );
                            }
                            else if (mSelectedQuest.Tasks[i].Objective == QuestObjective.KillNpcs)
                            {
                                mQuestDescLabel.AddText(
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
                }
                else
                {
                    if (Globals.Me.QuestProgress[mSelectedQuest.Id].Completed)
                    {
                        if (mSelectedQuest.LogAfterComplete)
                        {
                            mQuestStatus.SetText(Strings.QuestLog.Completed);
                            mQuestStatus.SetTextColor(CustomColors.QuestWindow.Completed, ComponentState.Normal);
                            mQuestDescLabel.AddText(mSelectedQuest.EndDescription, mQuestDescTemplateLabel);
                        }
                    }
                    else
                    {
                        if (mSelectedQuest.LogBeforeOffer)
                        {
                            mQuestStatus.SetText(Strings.QuestLog.NotStarted);
                            mQuestStatus.SetTextColor(CustomColors.QuestWindow.NotStarted, ComponentState.Normal);
                            mQuestDescLabel.AddText(mSelectedQuest.BeforeDescription, mQuestDescTemplateLabel);
                            mQuitButton?.Hide();
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
                    mQuestDescLabel.AddText(mSelectedQuest.BeforeDescription, mQuestDescTemplateLabel);
                }
            }

            // Mostrar
 
            mQuestTitle.IsHidden = false;
            mQuestTitle.Text = mSelectedQuest.Name;
            mQuestDescArea.IsHidden = false;
            UpdateDescriptionLayout();
            mQuestStatus.Show();
            mQuitButton.Show();

            // Cargar recompensas de esta quest (ítems + exp) y acomodar
            LoadRewardWidgets(mSelectedQuest.Id);
        }

        private void UpdateDescriptionLayout()
        {
            var scrollbarWidth = mQuestDescArea?.VerticalScrollBar?.Width ?? 0;

            if (mQuestDescLabel != null && mQuestDescArea != null)
            {
                mQuestDescLabel.Width = Math.Max(0, mQuestDescArea.Width - scrollbarWidth);
                mQuestDescLabel.SizeToChildren(false, true);
                mQuestDescArea.EnableScroll(false, true);
                mQuestDescArea.VerticalScrollBar.ScrollAmount = 0;
            }
        }

        public void Show()
        {
            mSelectedQuest = null;
            UpdateSelectedQuest();

            if (_shouldUpdateList)
            {
                UpdateInternal(_shouldUpdateList);
                _shouldUpdateList = false;
            }

            mQuestsWindow.IsHidden = false;
        }

        public void ShowQuest(Guid questId)
        {
            Globals.QuestWindowSelectedQuestId = questId;
            _shouldUpdateList = true;
            mSelectedQuest = null;
            mQuestsWindow.IsHidden = false;
            UpdateInternal(true);
        }

        public bool IsVisible() => !mQuestsWindow.IsHidden;

        public void Hide()
        {
            mQuestsWindow.IsHidden = true;
            mSelectedQuest = null;
            UpdateSelectedQuest();
        }

        // ---------- Recompensas: API de IQuestWindow ----------
        // Reemplaza TODO el método por esto:
        // ---------- Recompensas: API de IQuestWindow ----------
        public void AddRewardWidget(Base widget)
        {
            if (widget == null) return;

            var n = widget.Name ?? string.Empty;

            // Mándalos al contenedor correcto por nombre
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

        // ---------- Recompensas: Carga & layout ----------
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

            // Posición base fija para EXP
            _rewardExpContainer.SetPosition(RewardPaddingX, RewardPaddingY);

            // Ancho disponible (cont. raíz - padding lateral)
            var availW = Math.Max(_rewardContainer.Width - 2 * RewardPaddingX, 1);

            int expHeight = 0;

            if (anyExp)
            {
                // Crea chips (se agregan a _rewardExpWidgets vía AddRewardWidget)
                _ = new QuestRewardExp(this, playerExp, jobExp, guildExp, factionHonor);

                // Layout con WRAP horizontal
                const int minW = 80;
                const int spacing = 4;
                int x = 0;
                int y = 0;
                int rowH = RewardExpHeight;

                foreach (var chip in _rewardExpWidgets)
                {
                    var w = Math.Max(chip.Width, minW);

                    // Salto de línea si no entra
                    if (x > 0 && x + w > availW)
                    {
                        x = 0;
                        y += rowH + spacing;
                    }

                    chip.SetPosition(x, y);
                    x += w + spacing;
                }

                expHeight = (_rewardExpWidgets.Count > 0) ? (y + rowH) : 0;

                _rewardExpContainer.SetSize(availW, Math.Max(expHeight, 1));
                _rewardExpContainer.IsHidden = _rewardExpWidgets.Count == 0;
            }
            else
            {
                _rewardExpContainer.IsHidden = true;
                _rewardExpContainer.SetSize(1, 1);
            }

            // --- ÍTEMS ---
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

            // Si no hay nada, escondemos raíz y salimos
            _rewardContainer.IsHidden = _rewardExpContainer.IsHidden && _rewardItemsContainer.IsHidden;
            if (_rewardContainer.IsHidden) return;

            // EXP arriba, ÍTEMS debajo (con separación)
            var itemsY = _rewardExpContainer.IsHidden
                ? RewardPaddingY
                : RewardPaddingY + _rewardExpContainer.Height + RewardSpacing;

            _rewardItemsContainer.SetPosition(RewardPaddingX, itemsY);

            // --- Grilla de ítems ---
            int itemsHeight = 0;

            if (!_rewardItemsContainer.IsHidden && _rewardItemWidgets.Count > 0)
            {
                // Ancho interno con padding visual propio
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
                itemsHeight = yPad * 2 + rows * itemH;

                _rewardItemsContainer.SetSize(availW, itemsHeight);
            }

            // Alto total del contenedor raíz (con paddings)
            var totalH = RewardPaddingY;
            if (!_rewardExpContainer.IsHidden) totalH += _rewardExpContainer.Height + RewardSpacing;
            if (!_rewardItemsContainer.IsHidden) totalH += _rewardItemsContainer.Height;
            totalH += RewardPaddingY;

            _rewardContainer.EnableScroll(false, true);
            _rewardContainer.SetSize(_rewardContainer.Width, totalH);
        }

        // ---------- Tareas ----------
        private bool IsTaskCompleted(QuestTaskDescriptor task) => QuestTaskProgressHelper.IsTaskCompleted(
            mSelectedQuest,
            task,
            Globals.Me?.QuestProgress
        );

        private int GetTaskProgress(QuestTaskDescriptor task) => QuestTaskProgressHelper.GetTaskProgress(
            mSelectedQuest,
            task,
            Globals.Me?.QuestProgress
        );
        private void UpdateQuestTasks()
        {
            mQuestTasksList.RemoveAllRows();
            mQuestTasksContainer.Hide();
        }

    }
}
