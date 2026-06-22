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
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.Combat;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using System.Globalization;


namespace Intersect.Client.Interface.Game.Character;

public partial class CharacterWindow : Window
{
    private const int WindowWidth = 700;
    private const int WindowHeight = 520;
    private const int Margin = 16;
    private const int CharacterInfoHeight = 150;
    private const int TopSectionHeight = 308;

    private const int StatRowHeight = 22;
    private const int StatIconSize = 18;
    private const int EffectRowHeight = 20;
    private const int EffectIconSize = 16;
    private const int IconSpacing = 8;

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
    private Label mAttackLabel;
    private Label mAbilityPwrLabel;
    private Label mDefenseLabel;
    private Label mVitalityLabel;
    private Label mSpeedLabel;
    private Label mAgilityLabel;
    private Label mWillpowerLabel;
    private Label mDamageLabel;
    private Label mCureLabel;
    private Label mCritChanceLabel;
    private Label mBasicAttackDamageLabel;
    private Label mPointsLabel;

    private Button mAddAttackBtn;
    private Button mAddAbilityPwrBtn;
    private Button mAddDefenseBtn;
    private Button mAddVitalityBtn;
    private Button mAddWillpowerBtn;
    private Button mAddAgilityBtn; // (ojo: en tu código estaba “IncreaseSpeedButton” pero variable “AgilityBtn”)

    // Extra Buffs UI
    private Label mHpRegen;
    private Label mManaRegen;
    private Label mLifeSteal;
    private Label mAttackSpeed;
    private Label mExtraExp;
    private Label mLuck;
    private Label mTenacity;
    private Label mCooldownReduction;
    private Label mManaSteal;
    private Label mSpeedBuff;
    private Label mDamageBuff;
    private Label mCureBuff;

    private Label mAccuracy;
    private Label mEvasion;
    private Label mCritBonus;
    private Label mAntiCrit;
    private Label mArmorPenetration;
    private Label mDamageReduction;
    private Label mDamageReflect;
    private Label mCriticalReduction;
    private Label mFlatDamage;
    private Label mFlatCures;

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
    private int _criticalReduction = default;
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

    private ImagePanel CreateIconPanel(
        Base parent,
        string name,
        int x,
        int y,
        string? iconName,
        int rowHeight = StatRowHeight,
        int iconSize = StatIconSize
    )
    {
        var icon = new ImagePanel(parent, name);
        icon.SetSize(iconSize, iconSize);
        icon.SetPosition(x, y + Math.Max(0, (rowHeight - iconSize) / 2));
        icon.Texture = StatEffectIconProvider.GetIconTexture(iconName);
        icon.IsHidden = icon.Texture == null;

        return icon;
    }

    private Label CreateLabelWithIcon(
        Base parent,
        string name,
        string? iconName,
        int x,
        int y,
        int width,
        int rowHeight = StatRowHeight,
        int iconSize = StatIconSize
    )
    {
        if (!string.IsNullOrWhiteSpace(iconName))
        {
            CreateIconPanel(parent, $"{name}Icon", x, y, iconName, rowHeight, iconSize);
            x += iconSize + IconSpacing;
        }

        return CreateBodyLabel(parent, name, x, y, width, rowHeight);
    }

    private int Stack(Base ctrl, int x, int y, int spacing = 4)
    {
        ctrl.SetPosition(x, y);
        return y + ctrl.Height + spacing;
    }

    private static string FormatPercentBonus(int value) => value == 0 ? "0%" : $"{(value > 0 ? "+" : string.Empty)}{value}%";

    private static string FormatRatingValue(double value) => FormatScore(value);

    private static string FormatRatingBonus(int value) => value == 0 ? "0" : $"{(value > 0 ? "+" : string.Empty)}{value}";

    private static string FormatScore(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

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
        var bottomH = WindowHeight - (Margin * 2) - TopSectionHeight;

        // Containers
        mCharacterInfoContainer = CreateContainer("CharacterInfoContainer", contentX, contentY, leftW, CharacterInfoHeight);
        mStatsContainer = CreateContainer("StatsContainer", contentX, contentY + CharacterInfoHeight, leftW, TopSectionHeight - CharacterInfoHeight);
        mEquipmentContainer = CreateContainer("EquipmentContainer", contentX + leftW, contentY, rightW, TopSectionHeight);
        mExtraBuffsContainer = CreateContainer("ExtraBuffsContainer", contentX, contentY + TopSectionHeight, WindowWidth - (Margin * 2), bottomH);

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

        var startY = header.Height + 6;
        var leftX = 0;
        var rightX = (mStatsContainer.Width / 2) + 8;
        var leftLabelWidth = 152;
        var rightLabelWidth = 152;
        var buttonX = leftX + StatIconSize + IconSpacing + leftLabelWidth + 8;
        var rightButtonX = rightX + StatIconSize + IconSpacing + rightLabelWidth + 8;

        var leftY = startY;
        mAttackLabel = CreateLabelWithIcon(
            mStatsContainer,
            "AttackLabel",
            StatEffectIconProvider.GetIconForStat(Stat.Attack),
            leftX,
            leftY,
            leftLabelWidth
        );
        mAddAttackBtn = CreateStatButton(mStatsContainer, "IncreaseAttackButton", buttonX, leftY);
        mAddAttackBtn.Clicked += _addAttackBtn_Clicked;
        leftY += StatRowHeight;

        mAbilityPwrLabel = CreateLabelWithIcon(
            mStatsContainer,
            "AbilityPowerLabel",
            StatEffectIconProvider.GetIconForStat(Stat.Intelligence),
            leftX,
            leftY,
            leftLabelWidth
        );
        mAddAbilityPwrBtn = CreateStatButton(mStatsContainer, "IncreaseAbilityPowerButton", buttonX, leftY);
        mAddAbilityPwrBtn.Clicked += _addAbilityPwrBtn_Clicked;
        leftY += StatRowHeight;

        mDefenseLabel = CreateLabelWithIcon(
            mStatsContainer,
            "DefenseLabel",
            StatEffectIconProvider.GetIconForStat(Stat.Defense),
            leftX,
            leftY,
            leftLabelWidth
        );
        mAddDefenseBtn = CreateStatButton(mStatsContainer, "IncreaseDefenseButton", buttonX, leftY);
        mAddDefenseBtn.Clicked += _addDefenseBtn_Clicked;
        leftY += StatRowHeight;

        mVitalityLabel = CreateLabelWithIcon(
            mStatsContainer,
            "VitalityLabel",
            StatEffectIconProvider.GetIconForStat(Stat.Vitality),
            leftX,
            leftY,
            leftLabelWidth
        );
        mAddVitalityBtn = CreateStatButton(mStatsContainer, "IncreaseVitalityButton", buttonX, leftY);
        mAddVitalityBtn.Clicked += _addVitalityBtn_Clicked;
        leftY += StatRowHeight;

        mAgilityLabel = CreateLabelWithIcon(
            mStatsContainer,
            "AgilityLabel",
            StatEffectIconProvider.GetIconForStat(Stat.Agility),
            leftX,
            leftY,
            leftLabelWidth
        );
        mAddAgilityBtn = CreateStatButton(mStatsContainer, "IncreaseSpeedButton", buttonX, leftY);
        mAddAgilityBtn.Clicked += _addSpeedBtn_Clicked;

        var rightY = startY;
        mSpeedLabel = CreateLabelWithIcon(
            mStatsContainer,
            "SpeedLabel",
            StatEffectIconProvider.GetIconForStat(Stat.Speed),
            rightX,
            rightY,
            rightLabelWidth
        );
        rightY += StatRowHeight;

        mWillpowerLabel = CreateLabelWithIcon(
            mStatsContainer,
            "WillpowerLabel",
            StatEffectIconProvider.GetIconForStat(Stat.Willpower),
            rightX,
            rightY,
            rightLabelWidth
        );
        mAddWillpowerBtn = CreateStatButton(mStatsContainer, "IncreaseWillpowerButton", rightButtonX, rightY);
        mAddWillpowerBtn.Clicked += _addWillpowerBtn_Clicked;
        rightY += StatRowHeight;

        mDamageLabel = CreateLabelWithIcon(
            mStatsContainer,
            "DamageLabel",
            StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Damages),
            rightX,
            rightY,
            rightLabelWidth
        );
        rightY += StatRowHeight;

        mCureLabel = CreateLabelWithIcon(
            mStatsContainer,
            "CureLabel",
            StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Cures),
            rightX,
            rightY,
            rightLabelWidth
        );
        rightY += StatRowHeight;

        mCritChanceLabel = CreateLabelWithIcon(
            mStatsContainer,
            "CritLabel",
            StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CriticalChance),
            rightX,
            rightY,
            rightLabelWidth
        );
        rightY += StatRowHeight;

        mPointsLabel = CreateBodyLabel(mStatsContainer, "PointsLabel", rightX, rightY, rightLabelWidth, StatRowHeight);

        mBasicAttackDamageLabel = CreateBodyLabel(
            mStatsContainer,
            "BasicAttackDamageLabel",
            0,
            startY + (StatRowHeight * 6) + 8,
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

        var labelWidth = mExtraBuffsList.Width - EffectIconSize - IconSpacing;
        var y = 0;

        mHpRegen = CreateLabelWithIcon(mExtraBuffsList, "HpRegen", StatEffectIconProvider.GetIconForVital(Vital.Health), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mManaRegen = CreateLabelWithIcon(mExtraBuffsList, "ManaRegen", StatEffectIconProvider.GetIconForVital(Vital.Mana), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mLifeSteal = CreateLabelWithIcon(mExtraBuffsList, "Lifesteal", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Lifesteal), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mAttackSpeed = CreateLabelWithIcon(mExtraBuffsList, "AttackSpeed", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CooldownReduction), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;

        mSpeedBuff = CreateLabelWithIcon(mExtraBuffsList, "SpeedBuff", StatEffectIconProvider.GetIconForStat(Stat.Speed), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mDamageBuff = CreateLabelWithIcon(mExtraBuffsList, "DamageBuff", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Damages), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mCureBuff = CreateLabelWithIcon(mExtraBuffsList, "CureBuff", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Cures), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;

        mExtraExp = CreateLabelWithIcon(mExtraBuffsList, "ExtraExp", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.EXP), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mLuck = CreateLabelWithIcon(mExtraBuffsList, "Luck", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Luck), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mTenacity = CreateLabelWithIcon(mExtraBuffsList, "Tenacity", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Tenacity), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mCooldownReduction = CreateLabelWithIcon(mExtraBuffsList, "CooldownReduction", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CooldownReduction), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mManaSteal = CreateLabelWithIcon(mExtraBuffsList, "Manasteal", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Manasteal), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;

        mAccuracy = CreateLabelWithIcon(mExtraBuffsList, "Accuracy", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Accuracy), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mEvasion = CreateLabelWithIcon(mExtraBuffsList, "Evasion", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Evasion), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mCritBonus = CreateLabelWithIcon(mExtraBuffsList, "CriticalBonus", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CriticalChance), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mAntiCrit = CreateLabelWithIcon(mExtraBuffsList, "AntiCritical", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.AntiCritChance), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mArmorPenetration = CreateLabelWithIcon(mExtraBuffsList, "ArmorPenetration", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.ArmorPenetration), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mDamageReduction = CreateLabelWithIcon(mExtraBuffsList, "DamageReduction", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.DamageReduction), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mDamageReflect = CreateLabelWithIcon(mExtraBuffsList, "DamageReflect", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.DamageReflect), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mCriticalReduction = CreateLabelWithIcon(mExtraBuffsList, "CriticalReduction", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.CriticalReduction), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;

        mFlatDamage = CreateLabelWithIcon(mExtraBuffsList, "FlatDamage", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Damages), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;
        mFlatCures = CreateLabelWithIcon(mExtraBuffsList, "FlatCures", StatEffectIconProvider.GetIconForItemEffect(ItemEffect.Cures), 0, y, labelWidth, EffectRowHeight, EffectIconSize); y += EffectRowHeight;

        mExtraBuffsList.SetSize(mExtraBuffsList.Width, y + 4);
    }

    // -------------------------
    // Public
    // -------------------------

    public void SetPlayer(Player? player) => _player = player;

    //Update Button Event Handlers
    void _addVitalityBtn_Clicked(Base sender, MouseButtonState arguments) =>
        PacketSender.SendUpgradeStat((int)Stat.Vitality);

    void _addWillpowerBtn_Clicked(Base sender, MouseButtonState arguments) =>
        PacketSender.SendUpgradeStat((int)Stat.Willpower);

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
        var entity = (Entity)player;

        if (player == Globals.Me)
        {
            foreach (var equipmentEntry in entity.MyEquipment)
            {
                var equipmentSlots = equipmentEntry.Value;
                foreach (var slotIndex in equipmentSlots)
                {
                    var invItem = entity.Inventory.ElementAtOrDefault(slotIndex);
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
            foreach (var equipmentEntry in entity.Equipment)
            {
                var equippedItems = equipmentEntry.Value;
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
        var entity = (Entity)player;

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
            if (entity.MyEquipment.TryGetValue(weaponSlotIndex, out var list) && list.Count > 0)
            {
                var invIndex = list[0];
                if (invIndex >= 0 && invIndex < Options.Instance.Player.MaxInventory)
                {
                    var inventorySlot = entity.Inventory.ElementAtOrDefault(invIndex);
                    if (inventorySlot != null)
                    {
                        weaponId = inventorySlot.ItemId;
                    }
                }
            }
        }
        else
        {
            if (entity.Equipment.TryGetValue(weaponSlotIndex, out var list) && list.Count > 0)
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
                            var equippedItem = player.Inventory[inventoryIndex];
                            if (equippedItem != null && ItemDescriptor.TryGet(equippedItem.ItemId, out var itemDescriptor))
                            {
                                paperdoll = player.Gender == Gender.Male ? itemDescriptor.MalePaperdoll : itemDescriptor.FemalePaperdoll;
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
        mAttackLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Attack], player.Stat[(int)Stat.Attack]));
        mAbilityPwrLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Intelligence], player.Stat[(int)Stat.Intelligence]));
        mDefenseLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Defense], player.Stat[(int)Stat.Defense]));
        mVitalityLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Vitality], player.Stat[(int)Stat.Vitality]));
        mSpeedLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Speed], player.Stat[(int)Stat.Speed]));
        mAgilityLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Agility], player.Stat[(int)Stat.Agility]));
        mWillpowerLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Willpower], player.Stat[(int)Stat.Willpower]));

        var critChance = player.CalculateCriticalChance(player.GetBaseCriticalChance());
        mCritChanceLabel.SetText(Strings.Character.CriticalChance.ToString(critChance));

        mPointsLabel.SetText(Strings.Character.Points.ToString(player.StatPoints));

        // Buttons visibility
        mAddAbilityPwrBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Intelligence] == Options.Instance.Player.MaxStat;
        mAddAttackBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Attack] == Options.Instance.Player.MaxStat;
        mAddDefenseBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Defense] == Options.Instance.Player.MaxStat;
        mAddVitalityBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Vitality] == Options.Instance.Player.MaxStat;
        mAddAgilityBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Agility] == Options.Instance.Player.MaxStat;
        mAddWillpowerBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Willpower] == Options.Instance.Player.MaxStat;

        // Effects
        UpdateExtraBuffs();
        mDamageLabel.SetText(Strings.Character.FlatDamage.ToString(FormatPercentBonus(_flatDamage)));
        mCureLabel.SetText(Strings.Character.FlatCures.ToString(FormatPercentBonus(_flatCures)));

        UpdateEquippedItems(true);
    }

    private void UpdateEquippedItems(bool updateExtraBuffs = false)
    {
        var player = DisplayedPlayer;
        if (player is null)
            return;

        var entity = (Entity)player;
        int itemIndex = 0;
        for (var slotIndex = 0; slotIndex < Options.Instance.Equipment.EquipmentSlots.Count; slotIndex++)
        {
            var slot = Options.Instance.Equipment.EquipmentSlots[slotIndex];

            if (player == Globals.Me)
            {
                var itemSlots = entity.MyEquipment.GetValueOrDefault(slotIndex) ?? new List<int>();
                for (var i = 0; i < slot.MaxItems; i++)
                {
                    if (itemIndex >= Items.Count)
                        break;

                    var itemIds = new List<Guid>();
                    var props = new List<ItemProperties>();

                    if (i < itemSlots.Count && itemSlots[i] >= 0 && itemSlots[i] < Options.Instance.Player.MaxInventory)
                    {
                        var invItem = entity.Inventory[itemSlots[i]];
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
                var equippedIds = entity.Equipment.GetValueOrDefault(slotIndex) ?? new List<Guid>();
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
        var entity = player as Entity;

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
        _criticalReduction = default;
        _flatDamage = default;
        _flatCures = default;

        mAttackSpeed.SetText(Strings.Character.AttackSpeed.ToString(0f));
        mSpeedBuff.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Speed], 0));
        mBasicAttackDamageLabel.SetText(Strings.Character.BasicAttackDamage.ToString(0, 0, 0));

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
                        case ItemEffect.CriticalReduction: _criticalReduction += effectValue; break;
                        case ItemEffect.Damages: _flatDamage += effectValue; break;
                        case ItemEffect.Cures: _flatCures += effectValue; break;
                    }
                }
            }

            mAttackSpeed.SetText(Strings.Character.AttackSpeed.ToString(player.CalculateAttackTime() / 1000f));

            mSpeedBuff.SetText(Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Speed],
                entity?.Stat[(int)Stat.Speed] ?? 0
            ));

            int sourceBaseDamage;
            Stat sourceScalingStat;
            int sourceScalingPercent;

            if (!TryGetEquippedWeaponProfile(player, out sourceBaseDamage, out sourceScalingStat, out sourceScalingPercent))
            {
                sourceBaseDamage = mClassDescriptor?.Damage ?? 0;
                sourceScalingStat = (Stat)(mClassDescriptor?.ScalingStat ?? (int)Stat.Attack);
                sourceScalingPercent = mClassDescriptor?.Scaling ?? 0;
            }

            var statValue = entity?.Stat[(int)sourceScalingStat] ?? 0;
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

        mHpRegen.SetText(Strings.Character.HealthRegen.ToString(HpRegenAmount));
        mManaRegen.SetText(Strings.Character.ManaRegen.ToString(ManaRegenAmount));
        mLifeSteal.SetText(Strings.Character.Lifesteal.ToString(LifeStealAmount));
        mExtraExp.SetText(Strings.Character.ExtraExp.ToString(ExtraExpAmount));
        mLuck.SetText(Strings.Character.Luck.ToString(LuckAmount));
        mTenacity.SetText(Strings.Character.Tenacity.ToString(TenacityAmount));
        mCooldownReduction.SetText(Strings.Character.CooldownReduction.ToString(CooldownAmount));
        mManaSteal.SetText(Strings.Character.Manasteal.ToString(ManaStealAmount));

        mDamageBuff.SetText(Strings.Character.FlatDamage.ToString(FormatPercentBonus(_flatDamage)));
        mCureBuff.SetText(Strings.Character.FlatCures.ToString(FormatPercentBonus(_flatCures)));

        var agility = entity?.Stat[(int)Stat.Agility] ?? 0;
        var attack = entity?.Stat[(int)Stat.Attack] ?? 0;
        var defense = entity?.Stat[(int)Stat.Defense] ?? 0;

        var accuracyScore = CombatFormulaCalculator.CalculateAccuracyScore(agility, attack, _accuracy);
        var evasionScore = CombatFormulaCalculator.CalculateEvasionScore(agility, defense, _evasion);

        mAccuracy.SetText(
            $"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Accuracy]} rating final {FormatRatingValue(accuracyScore)} (Agi {FormatScore(agility * 0.5d)} + Atk {FormatScore(attack * 0.3d)} + Eq {FormatRatingBonus(_accuracy)})"
        );
        mEvasion.SetText(
            $"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Evasion]} rating final {FormatRatingValue(evasionScore)} (Agi {FormatScore(agility * 0.7d)} + Def {FormatScore(defense * 0.2d)} + Eq {FormatRatingBonus(_evasion)})"
        );
        mCritBonus.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.CriticalChance]} {FormatPercentBonus(_critBonus)}");
        mAntiCrit.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.AntiCritChance]} {FormatPercentBonus(_antiCrit)}");
        mArmorPenetration.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.ArmorPenetration]} {FormatPercentBonus(_armorPenetration)}");
        mDamageReduction.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.DamageReduction]} {FormatPercentBonus(_damageReduction)}");
        mDamageReflect.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.DamageReflect]} {FormatPercentBonus(_damageReflect)}");
        mCriticalReduction.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.CriticalReduction]} {FormatPercentBonus(_criticalReduction)}");

        // Estos dos son “flat” pero los estás mostrando también acá
        mFlatDamage.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Damages]} {FormatPercentBonus(_flatDamage)}");
        mFlatCures.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Cures]} {FormatPercentBonus(_flatCures)}");

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
