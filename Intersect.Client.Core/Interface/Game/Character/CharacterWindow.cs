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
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.PlayerClass;

namespace Intersect.Client.Interface.Game.Character;

public partial class CharacterWindow : Window
{
    private const int WindowWidth = 560;
    private const int WindowHeight = 520;
    private const int Margin = 16;

    private const int InfoHeight = 120;
    private const int StatsHeight = 220;

    private const int StatRowHeight = 24;
    private const int StatLabelWidth = 230;
    private const int EffectLabelWidth = 420; // más ancho porque ahora es 1 sola columna
    private const int EffectRowHeight = 20;

    private const string TitleFont = "sourcesansproblack";
    private const string BodyFont = "source-sans-pro";

    // Containers
    private Base mCharacterInfoContainer;
    private Base mStatsContainer;
    private Base mExtraBuffsContainer;

    private ScrollControl mExtraBuffsScroll;
    private Base mExtraBuffsList;

    // Character UI
    private Label mCharacterLevelAndClass;
    private Label mCharacterName;
    private Button mFactionButton;

    // Stats UI
    private Label mAttackLabel;
    private Label mAbilityPwrLabel;
    private Label mDefenseLabel;
    private Label mMagicRstLabel;
    private Label mSpeedLabel;
    private Label mAgilityLabel;
    private Label mDamageLabel;
    private Label mCureLabel;
    private Label mCritChanceLabel;
    private Label mBasicAttackDamageLabel;
    private Label mPointsLabel;

    private Button mAddAttackBtn;
    private Button mAddAbilityPwrBtn;
    private Button mAddDefenseBtn;
    private Button mAddMagicResistBtn;
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

    private EffectValue _accuracy = default;
    private EffectValue _evasion = default;
    private EffectValue _critBonus = default;
    private EffectValue _antiCrit = default;
    private EffectValue _armorPenetration = default;
    private EffectValue _damageReduction = default;
    private EffectValue _damageReflect = default;
    private EffectValue _flatDamage = default;
    private EffectValue _flatCures = default;

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
            Alignment = [Alignments.Left, Alignments.CenterV]
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

    private string FormatEffectValue(EffectValue value)
    {
        var segments = new List<string>();
        if (value.Percentage != 0)
        {
            segments.Add($"{value.Percentage}%");
        }

        if (value.Flat != 0)
        {
            segments.Add($"+{value.Flat}");
        }

        return segments.Count == 0 ? "0" : string.Join(" / ", segments);
    }

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

        var contentW = WindowWidth - (Margin * 2);
        var contentH = WindowHeight - (Margin * 2);

        // Containers
        mCharacterInfoContainer = CreateContainer("CharacterInfoContainer", contentX, contentY, contentW, InfoHeight);
        mStatsContainer = CreateContainer("StatsContainer", contentX, contentY + InfoHeight, contentW, StatsHeight);
        mExtraBuffsContainer = CreateContainer(
            "ExtraBuffsContainer",
            contentX,
            contentY + InfoHeight + StatsHeight,
            contentW,
            contentH - InfoHeight - StatsHeight
        );

        BuildCharacterInfoSection();
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
    }

    private void BuildStatsSection()
    {
        var header = CreateSectionTitle(mStatsContainer, "StatsHeader", 0, 0, "Atributos");

        var x = 0;
        var y = header.Height + 6;

        // Attack
        mAttackLabel = CreateBodyLabel(mStatsContainer, "AttackLabel", x, y, StatLabelWidth);
        mAddAttackBtn = CreateStatButton(mStatsContainer, "IncreaseAttackButton", x + StatLabelWidth + 8, y);
        mAddAttackBtn.Clicked += _addAttackBtn_Clicked;
        y += StatRowHeight;

        // Ability Power
        mAbilityPwrLabel = CreateBodyLabel(mStatsContainer, "AbilityPowerLabel", x, y, StatLabelWidth);
        mAddAbilityPwrBtn = CreateStatButton(mStatsContainer, "IncreaseAbilityPowerButton", x + StatLabelWidth + 8, y);
        mAddAbilityPwrBtn.Clicked += _addAbilityPwrBtn_Clicked;
        y += StatRowHeight;

        // Defense
        mDefenseLabel = CreateBodyLabel(mStatsContainer, "DefenseLabel", x, y, StatLabelWidth);
        mAddDefenseBtn = CreateStatButton(mStatsContainer, "IncreaseDefenseButton", x + StatLabelWidth + 8, y);
        mAddDefenseBtn.Clicked += _addDefenseBtn_Clicked;
        y += StatRowHeight;

        // Vitality (Magic Resist label en tu UI, pero realmente es Vitality)
        mMagicRstLabel = CreateBodyLabel(mStatsContainer, "MagicResistLabel", x, y, StatLabelWidth);
        mAddMagicResistBtn = CreateStatButton(mStatsContainer, "IncreaseMagicResistButton", x + StatLabelWidth + 8, y);
        mAddMagicResistBtn.Clicked += _addMagicResistBtn_Clicked;
        y += StatRowHeight;

        // Speed
        mAgilityLabel = CreateBodyLabel(mStatsContainer, "AgilityLabel", x, y, StatLabelWidth);

        mAddAgilityBtn = CreateStatButton(mStatsContainer, "IncreaseSpeedButton", x + StatLabelWidth + 8, y);
        mAddAgilityBtn.Clicked += _addSpeedBtn_Clicked;
        y += StatRowHeight;

        // Read-only stats
        mSpeedLabel = CreateBodyLabel(mStatsContainer, "SpeedLabel", x, y, StatLabelWidth); y += StatRowHeight;
        mDamageLabel = CreateBodyLabel(mStatsContainer, "DamageLabel", x, y, StatLabelWidth); y += StatRowHeight;
        mCureLabel = CreateBodyLabel(mStatsContainer, "CureLabel", x, y, StatLabelWidth); y += StatRowHeight;
        mCritChanceLabel = CreateBodyLabel(mStatsContainer, "CritLabel", x, y, StatLabelWidth); y += StatRowHeight;
        mPointsLabel = CreateBodyLabel(mStatsContainer, "PointsLabel", x, y, StatLabelWidth + 40); y += StatRowHeight;

        mBasicAttackDamageLabel = CreateBodyLabel(
            mStatsContainer,
            "BasicAttackDamageLabel",
            x,
            y,
            StatLabelWidth + 120,
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

        int y = 0;

        // Apilado 1 por 1 (una sola columna)
        mHpRegen = CreateBodyLabel(mExtraBuffsList, "HpRegen", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mManaRegen = CreateBodyLabel(mExtraBuffsList, "ManaRegen", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mLifeSteal = CreateBodyLabel(mExtraBuffsList, "Lifesteal", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mAttackSpeed = CreateBodyLabel(mExtraBuffsList, "AttackSpeed", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;

        mSpeedBuff = CreateBodyLabel(mExtraBuffsList, "SpeedBuff", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mDamageBuff = CreateBodyLabel(mExtraBuffsList, "DamageBuff", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mCureBuff = CreateBodyLabel(mExtraBuffsList, "CureBuff", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;

        mExtraExp = CreateBodyLabel(mExtraBuffsList, "ExtraExp", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mLuck = CreateBodyLabel(mExtraBuffsList, "Luck", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mTenacity = CreateBodyLabel(mExtraBuffsList, "Tenacity", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mCooldownReduction = CreateBodyLabel(mExtraBuffsList, "CooldownReduction", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mManaSteal = CreateBodyLabel(mExtraBuffsList, "Manasteal", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;

        mAccuracy = CreateBodyLabel(mExtraBuffsList, "Accuracy", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mEvasion = CreateBodyLabel(mExtraBuffsList, "Evasion", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mCritBonus = CreateBodyLabel(mExtraBuffsList, "CriticalBonus", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mAntiCrit = CreateBodyLabel(mExtraBuffsList, "AntiCritical", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mArmorPenetration = CreateBodyLabel(mExtraBuffsList, "ArmorPenetration", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mDamageReduction = CreateBodyLabel(mExtraBuffsList, "DamageReduction", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mDamageReflect = CreateBodyLabel(mExtraBuffsList, "DamageReflect", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;

        mFlatDamage = CreateBodyLabel(mExtraBuffsList, "FlatDamage", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;
        mFlatCures = CreateBodyLabel(mExtraBuffsList, "FlatCures", 0, y, EffectLabelWidth, EffectRowHeight); y += EffectRowHeight;

        // Ajusta el alto del list para que el scroll funcione bien
        mExtraBuffsList.SetSize(mExtraBuffsList.Width, y + 4);
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

        // Stats text
        mAttackLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Attack], player.Stat[(int)Stat.Attack]));
        mAbilityPwrLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Intelligence], player.Stat[(int)Stat.Intelligence]));
        mDefenseLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Defense], player.Stat[(int)Stat.Defense]));
        mMagicRstLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Vitality], player.Stat[(int)Stat.Vitality]));
        mSpeedLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Speed], player.Stat[(int)Stat.Speed]));
        mAgilityLabel.SetText(Strings.Character.StatLabelValue.ToString(Strings.Combat.Stats[Stat.Agility], player.Stat[(int)Stat.Agility]));

        var critChance = player.CalculateCriticalChance(player.GetBaseCriticalChance());
        mCritChanceLabel.SetText(Strings.Character.CriticalChance.ToString(critChance));

        mPointsLabel.SetText(Strings.Character.Points.ToString(player.StatPoints));

        // Buttons visibility
        mAddAbilityPwrBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Intelligence] == Options.Instance.Player.MaxStat;
        mAddAttackBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Attack] == Options.Instance.Player.MaxStat;
        mAddDefenseBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Defense] == Options.Instance.Player.MaxStat;
        mAddMagicResistBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Vitality] == Options.Instance.Player.MaxStat;
        mAddAgilityBtn.IsHidden = player.StatPoints == 0 || player.Stat[(int)Stat.Agility] == Options.Instance.Player.MaxStat;

        // Effects
        UpdateExtraBuffs();
        mDamageLabel.SetText(Strings.Character.FlatDamage.ToString(FormatEffectValue(_flatDamage)));
        mCureLabel.SetText(Strings.Character.FlatCures.ToString(FormatEffectValue(_flatCures)));
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
                    var effectValue = effect.GetValues();
                    if (effectValue.GetPrimaryValue() == 0)
                        continue;

                    switch (effect.Type)
                    {
                        case ItemEffect.CooldownReduction: CooldownAmount += effectValue.GetPrimaryValue(); break;
                        case ItemEffect.Lifesteal: LifeStealAmount += effectValue.GetPrimaryValue(); break;
                        case ItemEffect.Tenacity: TenacityAmount += effectValue.GetPrimaryValue(); break;
                        case ItemEffect.Luck: LuckAmount += effectValue.GetPrimaryValue(); break;
                        case ItemEffect.EXP: ExtraExpAmount += effectValue.GetPrimaryValue(); break;
                        case ItemEffect.Manasteal: ManaStealAmount += effectValue.GetPrimaryValue(); break;

                        case ItemEffect.Accuracy: _accuracy = _accuracy.Add(effectValue); break;
                        case ItemEffect.Evasion: _evasion = _evasion.Add(effectValue); break;
                        case ItemEffect.CriticalChance: _critBonus = _critBonus.Add(effectValue); break;
                        case ItemEffect.AntiCritChance: _antiCrit = _antiCrit.Add(effectValue); break;
                        case ItemEffect.ArmorPenetration: _armorPenetration = _armorPenetration.Add(effectValue); break;
                        case ItemEffect.DamageReduction: _damageReduction = _damageReduction.Add(effectValue); break;
                        case ItemEffect.DamageReflect: _damageReflect = _damageReflect.Add(effectValue); break;
                        case ItemEffect.Damages: _flatDamage = _flatDamage.Add(effectValue); break;
                        case ItemEffect.Cures: _flatCures = _flatCures.Add(effectValue); break;
                    }
                }
            }

            mAttackSpeed.SetText(Strings.Character.AttackSpeed.ToString(player.CalculateAttackTime() / 1000f));

            mSpeedBuff.SetText(Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Speed],
                player.Stat[(int)Stat.Speed]
            ));

            var baseDamage = mClassDescriptor?.Damage ?? 0;
            var scalingStat = (Stat)(mClassDescriptor?.ScalingStat ?? (int)Stat.Attack);
            var scalingPercent = mClassDescriptor?.Scaling ?? 0;
            var critMultiplier = mClassDescriptor?.CritMultiplier ?? 1f;

            var scalingStatValue = player.Stat[(int)scalingStat];
            var scaledBase = baseDamage + scalingStatValue * (scalingPercent / 100f);
            var minTrueDamage = scaledBase * 0.975 * critMultiplier;
            var maxTrueDamage = scaledBase * 1.025 * critMultiplier;

            mBasicAttackDamageLabel.SetText(
                Strings.Character.BasicAttackDamage.ToString(
                    baseDamage,
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

        mDamageBuff.SetText(Strings.Character.FlatDamage.ToString(FormatEffectValue(_flatDamage)));
        mCureBuff.SetText(Strings.Character.FlatCures.ToString(FormatEffectValue(_flatCures)));

        mAccuracy.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Accuracy]} {FormatEffectValue(_accuracy)}");
        mEvasion.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Evasion]} {FormatEffectValue(_evasion)}");
        mCritBonus.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.CriticalChance]} {FormatEffectValue(_critBonus)}");
        mAntiCrit.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.AntiCritChance]} {FormatEffectValue(_antiCrit)}");
        mArmorPenetration.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.ArmorPenetration]} {FormatEffectValue(_armorPenetration)}");
        mDamageReduction.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.DamageReduction]} {FormatEffectValue(_damageReduction)}");
        mDamageReflect.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.DamageReflect]} {FormatEffectValue(_damageReflect)}");

        // Estos dos son “flat” pero los estás mostrando también acá
        mFlatDamage.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Damages]} {FormatEffectValue(_flatDamage)}");
        mFlatCures.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Cures]} {FormatEffectValue(_flatCures)}");

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
