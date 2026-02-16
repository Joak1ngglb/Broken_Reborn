using Intersect.Client.Core;
using Intersect.Client.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Breaking;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Interface.Game.DescriptionWindows.Components;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.PlayerClass;

namespace Intersect.Client.Interface.Game.Character;

public partial class CharacterWindow : Window
{
    private const int WindowWidth = 700;
    private const int WindowHeight = 520;
    private const int Margin = 16;

    private const int StatRowHeight = 24;

    private const string TitleFont = "sourcesansproblack";
    private const string BodyFont = "source-sans-pro";

    //Equipment List
    public List<EquipmentItem> Items = new List<EquipmentItem>();

    // Containers
    private Base mCharacterInfoContainer;
    private Base mStatsContainer;
    private Base mEquipmentContainer;
    private Base mExtraBuffsContainer;

    private ScrollControl mExtraBuffsScroll;
    private Base mExtraBuffsList;
    private RowContainerComponent mStatsRows;
    private RowContainerComponent mExtraBuffsRows;

    // Character UI
    private ImagePanel mCharacterContainer;
    private Label mCharacterLevelAndClass;
    private Label mCharacterName;
    private Button mFactionButton;

    private ImagePanel mCharacterPortrait; // (si lo usas después)
    private string mCharacterPortraitImg = string.Empty;
    private string mCurrentSprite = string.Empty;

    public ImagePanel[] PaperdollPanels;
    public string[] PaperdollTextures;

    // Stats UI
    private KeyValueRowComponent mAttackRow;
    private KeyValueRowComponent mAbilityPwrRow;
    private KeyValueRowComponent mDefenseRow;
    private KeyValueRowComponent mMagicRstRow;
    private KeyValueRowComponent mSpeedRow;
    private KeyValueRowComponent mAgilityRow;
    private KeyValueRowComponent mDamageRow;
    private KeyValueRowComponent mCureRow;
    private KeyValueRowComponent mCritChanceRow;
    private Label mBasicAttackDamageLabel;
    private KeyValueRowComponent mPointsRow;

    private Button mAddAttackBtn;
    private Button mAddAbilityPwrBtn;
    private Button mAddDefenseBtn;
    private Button mAddMagicResistBtn;
    private Button mAddAgilityBtn; // (ojo: en tu código estaba “IncreaseSpeedButton” pero variable “AgilityBtn”)

    // Extra Buffs UI
    private KeyValueRowComponent mHpRegenRow;
    private KeyValueRowComponent mManaRegenRow;
    private KeyValueRowComponent mLifeStealRow;
    private KeyValueRowComponent mAttackSpeedRow;
    private KeyValueRowComponent mExtraExpRow;
    private KeyValueRowComponent mLuckRow;
    private KeyValueRowComponent mTenacityRow;
    private KeyValueRowComponent mCooldownReductionRow;
    private KeyValueRowComponent mManaStealRow;
    private KeyValueRowComponent mSpeedBuffRow;
    private KeyValueRowComponent mDamageBuffRow;
    private KeyValueRowComponent mCureBuffRow;

    private KeyValueRowComponent mAccuracyRow;
    private KeyValueRowComponent mEvasionRow;
    private KeyValueRowComponent mCritBonusRow;
    private KeyValueRowComponent mAntiCritRow;
    private KeyValueRowComponent mArmorPenetrationRow;
    private KeyValueRowComponent mDamageReductionRow;
    private KeyValueRowComponent mDamageReflectRow;
    private KeyValueRowComponent mFlatDamageRow;
    private KeyValueRowComponent mFlatCuresRow;

    //Location
    public int X;
    public int Y;

    private Player? _player;
    private ItemProperties mItemProperties = null;
    private ClassDescriptor mClassDescriptor;

    public Player? DisplayedPlayer => _player ?? Globals.Me;

    long HpRegenAmount;
    long ManaRegenAmount;

    int LifeStealAmount = 0;
    int ExtraExpAmount = 0;
    int LuckAmount = 0;
    int TenacityAmount = 0;
    int CooldownAmount = 0;
    int ManaStealAmount = 0;

    private int _accuracy = default;
    private int _evasion = default;
    private int _critBonus = default;
    private int _antiCrit = default;
    private int _armorPenetration = default;
    private int _damageReduction = default;
    private int _damageReflect = default;
    private int _flatDamage = default;
    private int _flatCures = default;

    // -------------------------
    // Helpers
    // -------------------------

    private Base CreateContainer(string name, int x, int y, int w, int h, bool drawBg = false)
    {
        var panel = new Base(this, name);
        panel.SetPosition(x, y);
        panel.SetSize(w, h);
        panel.ShouldDrawBackground = drawBg;
        return panel;
    }

    private Label CreateSectionTitle(Base parent, string name, int x, int y, string text)
    {
        var label = new Label(parent, name)
        {
            FontName = TitleFont,
            FontSize = 14,
            Text = text,
            TextColorOverride = Color.White,
            AutoSizeToContents = true
        };

        label.SetPosition(x, y);
        return label;
    }

    private Label CreateBodyLabel(Base parent, string name, int x, int y, int width, int height = StatRowHeight)
    {
        var label = new Label(parent, name)
        {
            FontName = BodyFont,
            FontSize = 11,
            TextColorOverride = Color.White,
            AutoSizeToContents = false,
        
        };

        label.SetSize(width, height);
        label.SetPosition(x, y);
        return label;
    }

    private Button CreateStatButton(Base parent, string name, int x, int y)
    {
        var button = new Button(parent, name)
        {
            Width = 28,
            Height = 22,
            TextColorOverride = Color.White,
            FontName = TitleFont,
            FontSize = 11,
            Text = "+"
        };

        button.SetPosition(x, y);
        return button;
    }

    private int Stack(Base ctrl, int x, int y, int spacing = 4)
    {
        ctrl.SetPosition(x, y);
        return y + ctrl.Height + spacing;
    }

    private string FormatEffectValue(int value) => value == 0 ? "0" : $"{value}%";

    // -------------------------
    // Init
    // -------------------------

    public CharacterWindow(Canvas gameCanvas) : base(gameCanvas, Strings.Character.Title, false, nameof(CharacterWindow))
    {
        SetSize(WindowWidth, WindowHeight);
        IsResizable = false;

        TitleLabel.FontName = TitleFont;
        TitleLabel.FontSize = 14;
        TitleLabel.TextColorOverride = Color.White;

        // Macro layout
        var contentX = Margin;
        var contentY = Margin;

        var leftW = 420;
        var rightW = WindowWidth - (Margin * 2) - leftW;
        var topH = 250;
        var bottomH = WindowHeight - (Margin * 2) - topH;

        // Containers
        mCharacterInfoContainer = CreateContainer("CharacterInfoContainer", contentX, contentY, leftW, 150);
        mStatsContainer = CreateContainer("StatsContainer", contentX, contentY + 150, leftW, topH - 150);
        mEquipmentContainer = CreateContainer("EquipmentContainer", contentX + leftW, contentY, rightW, topH);
        mExtraBuffsContainer = CreateContainer("ExtraBuffsContainer", contentX, contentY + topH, WindowWidth - (Margin * 2), bottomH);

        BuildCharacterInfoSection();
        BuildEquipmentSection();
        BuildStatsSection();
        BuildExtraBuffsSection();

        UpdateExtraBuffs();
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    private void BuildCharacterInfoSection()
    {
        int y = 0;

        mCharacterName = new Label(mCharacterInfoContainer, "CharacterNameLabel")
        {
            FontName = TitleFont,
            FontSize = 18,
            TextColorOverride = Color.White,
            AutoSizeToContents = true
        };
        y = Stack(mCharacterName, 0, y, 2);

        mCharacterLevelAndClass = new Label(mCharacterInfoContainer, "ChatacterInfoLabel")
        {
            FontName = BodyFont,
            FontSize = 12,
            TextColorOverride = Color.White,
            AutoSizeToContents = true
        };
        y = Stack(mCharacterLevelAndClass, 0, y, 10);

        mFactionButton = new Button(mCharacterInfoContainer, "FactionInfoButton")
        {
            Width = 32,
            Height = 32
        };
        mFactionButton.SetPosition(mCharacterInfoContainer.Width - mFactionButton.Width, 0);
        mFactionButton.SetStateTexture(ComponentState.Normal, "factionicon.png");
        mFactionButton.SetStateTexture(ComponentState.Hovered, "factionicon_hovered.png");
        mFactionButton.SetToolTipText("Faction");
        mFactionButton.Clicked += (s, e) => Interface.GameUi.GameMenu?.ToggleFactionWindow();

        mCharacterContainer = new ImagePanel(mCharacterInfoContainer, "CharacterContainer");
        mCharacterContainer.SetSize(120, 120);
        mCharacterContainer.SetPosition(0, y);

        mCharacterPortrait = new ImagePanel(mCharacterContainer);
        mCharacterPortrait.SetSize(80, 80);
        mCharacterPortrait.SetPosition(
            (mCharacterContainer.Width - mCharacterPortrait.Width) / 2,
            (mCharacterContainer.Height - mCharacterPortrait.Height) / 2
        );

        PaperdollPanels = new ImagePanel[Options.Instance.Equipment.Slots.Count + 1];
        PaperdollTextures = new string[Options.Instance.Equipment.Slots.Count + 1];
        for (var i = 0; i <= Options.Instance.Equipment.Slots.Count; i++)
        {
            PaperdollPanels[i] = new ImagePanel(mCharacterContainer);
            PaperdollTextures[i] = string.Empty;
            PaperdollPanels[i].Hide();
        }
    }

    private void BuildEquipmentSection()
    {
        var header = CreateSectionTitle(mEquipmentContainer, "EquipmentHeader", 0, 0, "Equipo");

        var columns = 4;
        var slotW = 36;
        var slotH = 36;
        var spacingX = 8;
        var spacingY = 8;

        var startX = 0;
        var startY = header.Height + 8;

        int itemIndex = 0;
        var multiSlotTracker = new Dictionary<string, int>();

        for (int slotIndex = 0; slotIndex < Options.Instance.Equipment.EquipmentSlots.Count; slotIndex++)
        {
            var slot = Options.Instance.Equipment.EquipmentSlots[slotIndex];

            for (int j = 0; j < slot.MaxItems; j++)
            {
                var item = new EquipmentItem(slotIndex, j, this);
                Items.Add(item);

                var slotName = slot.Name;

                if (slot.MaxItems <= 1)
                {
                    item.Pnl = new ImagePanel(mEquipmentContainer, slotName);
                }
                else
                {
                    if (!multiSlotTracker.ContainsKey(slotName))
                        multiSlotTracker[slotName] = 0;

                    var currentIndex = multiSlotTracker[slotName];
                    item.Pnl = new ImagePanel(mEquipmentContainer, $"{slotName}_{currentIndex}");
                    multiSlotTracker[slotName]++;
                }

                int row = itemIndex / columns;
                int col = itemIndex % columns;

                item.Pnl.SetSize(slotW, slotH);
                item.Pnl.SetPosition(startX + col * (slotW + spacingX), startY + row * (slotH + spacingY));
                item.Setup();

                itemIndex++;
            }
        }
    }

    private void BuildStatsSection()
    {
        var header = CreateSectionTitle(mStatsContainer, "StatsHeader", 0, 0, "Atributos");

        var y = header.Height + 6;
        mStatsRows = new RowContainerComponent(mStatsContainer, "StatsRows");
        mStatsRows.SetPosition(0, y);
        mStatsRows.SetSize(mStatsContainer.Width - 48, mStatsContainer.Height - y);

        mAttackRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForStat(Stat.Attack), Color.White, Color.White);
        mAbilityPwrRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForStat(Stat.Intelligence), Color.White, Color.White);
        mDefenseRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForStat(Stat.Defense), Color.White, Color.White);
        mMagicRstRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForStat(Stat.Vitality), Color.White, Color.White);
        mAgilityRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForStat(Stat.Agility), Color.White, Color.White);
        mSpeedRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForStat(Stat.Speed), Color.White, Color.White);
        mDamageRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Damages), Color.White, Color.White);
        mCureRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Cures), Color.White, Color.White);
        mCritChanceRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CriticalChance), Color.White, Color.White);
        mPointsRow = mStatsRows.AddKeyValueRow(string.Empty, string.Empty, null, Color.White, Color.White);

        var buttonX = mStatsRows.X + mStatsRows.Width + 8;
        var buttonY = mStatsRows.Y;

        mAddAttackBtn = CreateStatButton(mStatsContainer, "IncreaseAttackButton", buttonX, buttonY);
        mAddAttackBtn.Clicked += _addAttackBtn_Clicked;
        buttonY += StatRowHeight;

        mAddAbilityPwrBtn = CreateStatButton(mStatsContainer, "IncreaseAbilityPowerButton", buttonX, buttonY);
        mAddAbilityPwrBtn.Clicked += _addAbilityPwrBtn_Clicked;
        buttonY += StatRowHeight;

        mAddDefenseBtn = CreateStatButton(mStatsContainer, "IncreaseDefenseButton", buttonX, buttonY);
        mAddDefenseBtn.Clicked += _addDefenseBtn_Clicked;
        buttonY += StatRowHeight;

        mAddMagicResistBtn = CreateStatButton(mStatsContainer, "IncreaseMagicResistButton", buttonX, buttonY);
        mAddMagicResistBtn.Clicked += _addMagicResistBtn_Clicked;
        buttonY += StatRowHeight;

        mAddAgilityBtn = CreateStatButton(mStatsContainer, "IncreaseSpeedButton", buttonX, buttonY);
        mAddAgilityBtn.Clicked += _addSpeedBtn_Clicked;

        mBasicAttackDamageLabel = CreateBodyLabel(
            mStatsContainer,
            "BasicAttackDamageLabel",
            mStatsRows.X,
            mStatsRows.Y + mStatsRows.Height + 4,
            mStatsContainer.Width,
            StatRowHeight + 4
        );
    }

    private void BuildExtraBuffsSection()
    {
        var header = CreateSectionTitle(mExtraBuffsContainer, "ExtraBuffsHeader", 0, 0, Strings.Character.ExtraBuffs);

        mExtraBuffsScroll = new ScrollControl(mExtraBuffsContainer, "ExtraBuffsScroll");
        mExtraBuffsScroll.SetPosition(0, header.Height + 6);
        mExtraBuffsScroll.SetSize(mExtraBuffsContainer.Width, mExtraBuffsContainer.Height - (header.Height + 6));
        mExtraBuffsScroll.AutoHideBars = true;

        mExtraBuffsList = new Base(mExtraBuffsScroll, "ExtraBuffsList");
        mExtraBuffsList.SetPosition(0, 0);
        mExtraBuffsList.SetSize(mExtraBuffsScroll.Width - 20, 10);

        mExtraBuffsRows = new RowContainerComponent(mExtraBuffsList, "ExtraBuffsRows");
        mExtraBuffsRows.SetPosition(0, 0);
        mExtraBuffsRows.SetSize(mExtraBuffsList.Width, mExtraBuffsList.Height);

        mHpRegenRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForVital(Vital.Health), Color.White, Color.White);
        mManaRegenRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForVital(Vital.Mana), Color.White, Color.White);
        mLifeStealRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Lifesteal), Color.White, Color.White);
        mAttackSpeedRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CooldownReduction), Color.White, Color.White);

        mSpeedBuffRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForStat(Stat.Speed), Color.White, Color.White);
        mDamageBuffRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Damages), Color.White, Color.White);
        mCureBuffRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Cures), Color.White, Color.White);

        mExtraExpRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.EXP), Color.White, Color.White);
        mLuckRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Luck), Color.White, Color.White);
        mTenacityRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Tenacity), Color.White, Color.White);
        mCooldownReductionRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CooldownReduction), Color.White, Color.White);
        mManaStealRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Manasteal), Color.White, Color.White);

        mAccuracyRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Accuracy), Color.White, Color.White);
        mEvasionRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Evasion), Color.White, Color.White);
        mCritBonusRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CriticalChance), Color.White, Color.White);
        mAntiCritRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.AntiCritChance), Color.White, Color.White);
        mArmorPenetrationRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.ArmorPenetration), Color.White, Color.White);
        mDamageReductionRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.DamageReduction), Color.White, Color.White);
        mDamageReflectRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.DamageReflect), Color.White, Color.White);

        mFlatDamageRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Damages), Color.White, Color.White);
        mFlatCuresRow = mExtraBuffsRows.AddKeyValueRow(string.Empty, string.Empty, StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Cures), Color.White, Color.White);

        mExtraBuffsRows.SizeToChildren(true, true);
        mExtraBuffsList.SetSize(mExtraBuffsList.Width, mExtraBuffsRows.Height + 4);
    }

    // -------------------------
    // Public
    // -------------------------

    public void SetPlayer(Player? player) => _player = player;

    //Update Button Event Handlers
    void _addMagicResistBtn_Clicked(Base sender, MouseButtonState arguments) =>
        PacketSender.SendUpgradeStat((int)Stat.Vitality);

    void _addAbilityPwrBtn_Clicked(Base sender, MouseButtonState arguments) =>
        PacketSender.SendUpgradeStat((int)Stat.Intelligence);

    void _addSpeedBtn_Clicked(Base sender, MouseButtonState arguments) =>
        PacketSender.SendUpgradeStat((int)Stat.Agility);

    void _addDefenseBtn_Clicked(Base sender, MouseButtonState arguments) =>
        PacketSender.SendUpgradeStat((int)Stat.Defense);

    void _addAttackBtn_Clicked(Base sender, MouseButtonState arguments) =>
        PacketSender.SendUpgradeStat((int)Stat.Attack);

    private IEnumerable<ItemDescriptor> GetEquippedDescriptors(Player player)
    {
        if (player == Globals.Me)
        {
            foreach (var (_, equipmentSlots) in player.MyEquipment)
            {
                foreach (var slotIndex in equipmentSlots)
                {
                    var invItem = player.Inventory.ElementAtOrDefault(slotIndex);
                    if (invItem?.ItemId == null || invItem.ItemId == Guid.Empty)
                        continue;

                    var descriptor = ItemDescriptor.Get(invItem.ItemId);
                    if (descriptor != null)
                        yield return descriptor;
                }
            }
        }
        else
        {
            foreach (var (_, equippedItems) in player.Equipment)
            {
                foreach (var id in equippedItems)
                {
                    if (id == Guid.Empty)
                        continue;

                    var descriptor = ItemDescriptor.Get(id);
                    if (descriptor != null)
                        yield return descriptor;
                }
            }
        }
    }

    private bool TryGetEquippedWeaponProfile(
        Player player,
        out int baseDamage,
        out Stat scalingStat,
        out int scalingPercent
    )
    {
        baseDamage = 0;
        scalingStat = Stat.Attack;
        scalingPercent = 0;

        var weaponSlotIndex = Options.Instance.Equipment.Slots.IndexOf("Weapon");
        if (weaponSlotIndex < 0)
        {
            weaponSlotIndex = Options.Instance.Equipment.Slots.IndexOf("MainHand");
        }

        if (weaponSlotIndex < 0)
        {
            return false;
        }

        Guid weaponId = Guid.Empty;

        if (player == Globals.Me)
        {
            if (player.MyEquipment.TryGetValue(weaponSlotIndex, out var list) && list.Count > 0)
            {
                var invIndex = list[0];
                if (invIndex >= 0 && invIndex < Options.Instance.Player.MaxInventory)
                {
                    var inventorySlot = player.Inventory.ElementAtOrDefault(invIndex);
                    if (inventorySlot != null)
                    {
                        weaponId = inventorySlot.ItemId;
                    }
                }
            }
        }
        else
        {
            if (player.Equipment.TryGetValue(weaponSlotIndex, out var list) && list.Count > 0)
            {
                weaponId = list[0];
            }
        }

        if (weaponId == Guid.Empty)
        {
            return false;
        }

        if (!ItemDescriptor.TryGet(weaponId, out var weapon))
        {
            return false;
        }

        baseDamage = weapon.Damage;
        scalingStat = (Stat)weapon.ScalingStat;
        scalingPercent = weapon.Scaling;

        return true;
    }

    // -------------------------
    // Update Loop
    // -------------------------

    public void Update()
    {
        var player = DisplayedPlayer;
        if (IsHidden || player is null)
            return;

        mCharacterName.Text = player.Name;
        mCharacterLevelAndClass.Text = Strings.Character.LevelAndClass.ToString(player.Level, ClassDescriptor.GetName(player.Class));

        // Portrait/paperdoll
        var entityTex = Globals.ContentManager.GetTexture(Framework.Content.TextureType.Entity, player.Sprite);

        if (!string.IsNullOrWhiteSpace(player.Sprite) && player.Sprite != mCurrentSprite && entityTex != null)
        {
            for (var z = 0; z < Options.Instance.Equipment.Paperdoll.Directions[1].Count; z++)
            {
                var paperdoll = string.Empty;
                var slotName = Options.Instance.Equipment.Paperdoll.Directions[1][z];
                var slotIndex = Options.Instance.Equipment.Slots.IndexOf(slotName);

                if (slotIndex > -1)
                {
                    var equipment = player.MyEquipment;

                    if (equipment.TryGetValue(slotIndex, out var equippedList) && equippedList.Count > 0)
                    {
                        var inventoryIndex = equippedList[0];
                        if (inventoryIndex >= 0 && inventoryIndex < Options.Instance.Player.MaxInventory)
                        {
                            var itemNum = player.Inventory[inventoryIndex].ItemId;

                            if (ItemDescriptor.TryGet(itemNum, out var itemDescriptor))
                            {
                                paperdoll = player.Gender == 0 ? itemDescriptor.MalePaperdoll : itemDescriptor.FemalePaperdoll;
                                PaperdollPanels[z].RenderColor = itemDescriptor.Color;
                            }
                        }
                    }
                }
                else if (slotName == "Player")
                {
                    PaperdollPanels[z].Show();
                    PaperdollPanels[z].Texture = entityTex;
                    PaperdollPanels[z].SetTextureRect(0, 0, entityTex.Width / Options.Instance.Sprites.NormalFrames, entityTex.Height / Options.Instance.Sprites.Directions);
                    PaperdollPanels[z].SizeToContents();
                    PaperdollPanels[z].RenderColor = player.Color;
                    Align.Center(PaperdollPanels[z]);
                }

                if (string.IsNullOrWhiteSpace(paperdoll) && !string.IsNullOrWhiteSpace(PaperdollTextures[z]) && slotName != "Player")
                {
                    PaperdollPanels[z].Texture = null;
                    PaperdollPanels[z].Hide();
                    PaperdollTextures[z] = string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(paperdoll) && paperdoll != PaperdollTextures[z])
                {
                    var paperdollTex = Globals.ContentManager.GetTexture(Framework.Content.TextureType.Paperdoll, paperdoll);

                    PaperdollPanels[z].Texture = paperdollTex;
                    if (paperdollTex != null)
                    {
                        PaperdollPanels[z].SetTextureRect(0, 0, paperdollTex.Width / Options.Instance.Sprites.NormalFrames, paperdollTex.Height / Options.Instance.Sprites.Directions);
                        PaperdollPanels[z].SetSize(paperdollTex.Width / Options.Instance.Sprites.NormalFrames, paperdollTex.Height / Options.Instance.Sprites.Directions);
                        PaperdollPanels[z].SetPosition(
                            mCharacterContainer.Width / 2 - PaperdollPanels[z].Width / 2,
                            mCharacterContainer.Height / 2 - PaperdollPanels[z].Height / 2
                        );
                    }

                    PaperdollPanels[z].Show();
                    PaperdollTextures[z] = paperdoll;
                }
            }
        }
        else if (player.Sprite != mCurrentSprite && player.Face != mCurrentSprite)
        {
            mCharacterPortrait.IsHidden = true;
            for (var i = 0; i < Options.Instance.Equipment.Slots.Count; i++)
                PaperdollPanels[i].Hide();
        }

        // Stats text
        mAttackRow.SetText(Strings.Combat.Stats[Stat.Attack], player.Stat[(int)Stat.Attack].ToString());
        mAbilityPwrRow.SetText(Strings.Combat.Stats[Stat.Intelligence], player.Stat[(int)Stat.Intelligence].ToString());
        mDefenseRow.SetText(Strings.Combat.Stats[Stat.Defense], player.Stat[(int)Stat.Defense].ToString());
        mMagicRstRow.SetText(Strings.Combat.Stats[Stat.Vitality], player.Stat[(int)Stat.Vitality].ToString());
        mSpeedRow.SetText(Strings.Combat.Stats[Stat.Speed], player.Stat[(int)Stat.Speed].ToString());
        mAgilityRow.SetText(Strings.Combat.Stats[Stat.Agility], player.Stat[(int)Stat.Agility].ToString());

        var critChance = player.CalculateCriticalChance(player.GetBaseCriticalChance());
        mCritChanceRow.SetValueText(Strings.Character.CriticalChance.ToString(critChance));

        mPointsRow.SetValueText(Strings.Character.Points.ToString(player.StatPoints));

        // Buttons visibility
        mAddAbilityPwrBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Intelligence] == Options.Instance.Player.MaxStat;
        mAddAttackBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Attack] == Options.Instance.Player.MaxStat;
        mAddDefenseBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Defense] == Options.Instance.Player.MaxStat;
        mAddMagicResistBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Vitality] == Options.Instance.Player.MaxStat;
        mAddAgilityBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Agility] == Options.Instance.Player.MaxStat;

        // Effects
        UpdateExtraBuffs();
        mDamageRow.SetValueText(Strings.Character.FlatDamage.ToString(FormatEffectValue(_flatDamage)));
        mCureRow.SetValueText(Strings.Character.FlatCures.ToString(FormatEffectValue(_flatCures)));

        UpdateEquippedItems(true);
    }

    private void UpdateEquippedItems(bool updateExtraBuffs = false)
    {
        var player = DisplayedPlayer;
        if (player is null)
            return;

        int itemIndex = 0;
        for (var slotIndex = 0; slotIndex < Options.Instance.Equipment.EquipmentSlots.Count; slotIndex++)
        {
            var slot = Options.Instance.Equipment.EquipmentSlots[slotIndex];

            if (player == Globals.Me)
            {
                var itemSlots = player.MyEquipment.GetValueOrDefault(slotIndex) ?? new List<int>();
                for (var i = 0; i < slot.MaxItems; i++)
                {
                    if (itemIndex >= Items.Count)
                        break;

                    var itemIds = new List<Guid>();
                    var props = new List<ItemProperties>();

                    if (i < itemSlots.Count && itemSlots[i] >= 0 && itemSlots[i] < Options.Instance.Player.MaxInventory)
                    {
                        var invItem = player.Inventory[itemSlots[i]];
                        if (invItem.ItemId != Guid.Empty)
                        {
                            itemIds.Add(invItem.ItemId);
                            props.Add(invItem.ItemProperties);
                        }
                    }

                    Items[itemIndex].Update(itemIds, props);
                    itemIndex++;
                }
            }
            else
            {
                var equippedIds = player.Equipment.GetValueOrDefault(slotIndex) ?? new List<Guid>();
                for (var i = 0; i < slot.MaxItems; i++)
                {
                    if (itemIndex >= Items.Count)
                        break;

                    var itemIds = new List<Guid>();
                    if (i < equippedIds.Count && equippedIds[i] != Guid.Empty)
                        itemIds.Add(equippedIds[i]);

                    Items[itemIndex].Update(itemIds, new List<ItemProperties>());
                    itemIndex++;
                }
            }
        }
    }

    public void UpdateExtraBuffs()
    {
        var player = DisplayedPlayer;
        mClassDescriptor = ClassDescriptor.Get(player?.Class ?? Guid.Empty);

        HpRegenAmount = mClassDescriptor?.VitalRegen[(int)Vital.Health] ?? 0;
        ManaRegenAmount = mClassDescriptor?.VitalRegen[(int)Vital.Mana] ?? 0;

        CooldownAmount = 0;
        LifeStealAmount = 0;
        TenacityAmount = 0;
        LuckAmount = 0;
        ExtraExpAmount = 0;
        ManaStealAmount = 0;

        _accuracy = default;
        _evasion = default;
        _critBonus = default;
        _antiCrit = default;
        _armorPenetration = default;
        _damageReduction = default;
        _damageReflect = default;
        _flatDamage = default;
        _flatCures = default;

        if (player != null)
        {
            foreach (var descriptor in GetEquippedDescriptors(player))
            {
                HpRegenAmount += descriptor.VitalsRegen[(int)Vital.Health];
                ManaRegenAmount += descriptor.VitalsRegen[(int)Vital.Mana];

                foreach (var effect in descriptor.Effects)
                {
                    var effectValue = effect.Percentage;
                    if (effectValue == 0)
                        continue;

                    switch (effect.Type)
                    {
                        case ItemEffect.CooldownReduction: CooldownAmount += effectValue; break;
                        case ItemEffect.Lifesteal: LifeStealAmount += effectValue; break;
                        case ItemEffect.Tenacity: TenacityAmount += effectValue; break;
                        case ItemEffect.Luck: LuckAmount += effectValue; break;
                        case ItemEffect.EXP: ExtraExpAmount += effectValue; break;
                        case ItemEffect.Manasteal: ManaStealAmount += effectValue; break;

                        case ItemEffect.Accuracy: _accuracy += effectValue; break;
                        case ItemEffect.Evasion: _evasion += effectValue; break;
                        case ItemEffect.CriticalChance: _critBonus += effectValue; break;
                        case ItemEffect.AntiCritChance: _antiCrit += effectValue; break;
                        case ItemEffect.ArmorPenetration: _armorPenetration += effectValue; break;
                        case ItemEffect.DamageReduction: _damageReduction += effectValue; break;
                        case ItemEffect.DamageReflect: _damageReflect += effectValue; break;
                        case ItemEffect.Damages: _flatDamage += effectValue; break;
                        case ItemEffect.Cures: _flatCures += effectValue; break;
                    }
                }
            }

            mAttackSpeedRow.SetValueText(Strings.Character.AttackSpeed.ToString(player.CalculateAttackTime() / 1000f));

            mSpeedBuffRow.SetText(Strings.Combat.Stats[Stat.Speed], player.Stat[(int)Stat.Speed].ToString());

            int sourceBaseDamage;
            Stat sourceScalingStat;
            int sourceScalingPercent;

            if (!TryGetEquippedWeaponProfile(player, out sourceBaseDamage, out sourceScalingStat, out sourceScalingPercent))
            {
                sourceBaseDamage = mClassDescriptor?.Damage ?? 0;
                sourceScalingStat = (Stat)(mClassDescriptor?.ScalingStat ?? (int)Stat.Attack);
                sourceScalingPercent = mClassDescriptor?.Scaling ?? 0;
            }

            var statValue = player.Stat[(int)sourceScalingStat];
            var scaledBase = sourceBaseDamage + statValue * (sourceScalingPercent / 100f);
            var afterBonuses = scaledBase * (1f + _flatDamage / 100f);

            var minTrueDamage = afterBonuses * 0.975f;
            var maxTrueDamage = afterBonuses * 1.025f;

            mBasicAttackDamageLabel.SetText(
                Strings.Character.BasicAttackDamage.ToString(
                    sourceBaseDamage,
                    (int)Math.Round(minTrueDamage),
                    (int)Math.Round(maxTrueDamage)
                )
            );
        }

        mHpRegenRow.SetValueText(Strings.Character.HealthRegen.ToString(HpRegenAmount));
        mManaRegenRow.SetValueText(Strings.Character.ManaRegen.ToString(ManaRegenAmount));
        mLifeStealRow.SetValueText(Strings.Character.Lifesteal.ToString(LifeStealAmount));
        mExtraExpRow.SetValueText(Strings.Character.ExtraExp.ToString(ExtraExpAmount));
        mLuckRow.SetValueText(Strings.Character.Luck.ToString(LuckAmount));
        mTenacityRow.SetValueText(Strings.Character.Tenacity.ToString(TenacityAmount));
        mCooldownReductionRow.SetValueText(Strings.Character.CooldownReduction.ToString(CooldownAmount));
        mManaStealRow.SetValueText(Strings.Character.Manasteal.ToString(ManaStealAmount));

        mDamageBuffRow.SetValueText(Strings.Character.FlatDamage.ToString(FormatEffectValue(_flatDamage)));
        mCureBuffRow.SetValueText(Strings.Character.FlatCures.ToString(FormatEffectValue(_flatCures)));

        mAccuracyRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.Accuracy], FormatEffectValue(_accuracy));
        mEvasionRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.Evasion], FormatEffectValue(_evasion));
        mCritBonusRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.CriticalChance], FormatEffectValue(_critBonus));
        mAntiCritRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.AntiCritChance], FormatEffectValue(_antiCrit));
        mArmorPenetrationRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.ArmorPenetration], FormatEffectValue(_armorPenetration));
        mDamageReductionRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.DamageReduction], FormatEffectValue(_damageReduction));
        mDamageReflectRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.DamageReflect], FormatEffectValue(_damageReflect));

        // Estos dos son “flat” pero los estás mostrando también acá
        mFlatDamageRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.Damages], FormatEffectValue(_flatDamage));
        mFlatCuresRow.SetText(Strings.ItemDescription.BonusEffects[(int)ItemEffect.Cures], FormatEffectValue(_flatCures));

        // Reajusta alto del listado por si cambiaste fuentes/escala
        if (mExtraBuffsList != null)
        {
            // “básico”: deja el height tal cual lo seteamos en BuildExtraBuffsSection()
            // Si luego haces hide/show dinámico, aquí recalculas el alto.
        }
    }

    /// <summary>
    /// Show the window
    /// </summary>
    public void Show() => IsHidden = false;

    public bool IsVisible() => !IsHidden;

    public void Hide() => IsHidden = true;

    protected override void EnsureInitialized()
    {
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }
}
