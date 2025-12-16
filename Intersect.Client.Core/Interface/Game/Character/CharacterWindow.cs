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


public partial class CharacterWindow:Window
{

    private const int WindowWidth = 700;

    private const int WindowHeight = 520;

    private const int Margin = 16;

    private const int StatRowHeight = 24;

    private const int StatLabelWidth = 230;

    private const int EffectLabelWidth = 220;

    private const string TitleFont = "sourcesansproblack";

    private const string BodyFont = "source-sans-pro";

    //Equipment List
    public List<EquipmentItem> Items = new List<EquipmentItem>();

    Label mAbilityPwrLabel;

    Button mAddAbilityPwrBtn;

    Button mAddAttackBtn;

    Button mAddDefenseBtn;

    Button mAddMagicResistBtn;

    Button mAddAgilityBtn;
    Button mFactionButton;

    //Stats
    Label mAttackLabel;

    private ImagePanel mCharacterContainer;

    private Label mCharacterLevelAndClass;

    private Label mCharacterName;

    private ImagePanel mCharacterPortrait;

    private string mCharacterPortraitImg = string.Empty;

    private string mCurrentSprite = string.Empty;

    Label mDefenseLabel;

    private ItemProperties mItemProperties = null;

    Label mMagicRstLabel;

    Label mPointsLabel;

    Label mSpeedLabel;
    Label mAgilityLabel;
    Label mDamageLabel;
    Label mCureLabel;
    Label mCritChanceLabel;
    Label mBasicAttackDamageLabel;
    public ImagePanel[] PaperdollPanels;

    public string[] PaperdollTextures;

    //Location
    public int X;

    public int Y;

    //Extra Buffs
    Label mHpRegen;
    Label mManaRegen;
    Label mLifeSteal;
    Label mAttackSpeed;
    Label mExtraExp;
    Label mLuck;
    Label mTenacity;
    Label mCooldownReduction;
    Label mManaSteal;
    Label mSpeedBuff;
    Label mDamageBuff;
    Label mCureBuff;
    Label mAccuracy;
    Label mEvasion;
    Label mCritBonus;
    Label mAntiCrit;
    Label mArmorPenetration;
    Label mDamageReduction;
    Label mDamageReflect;
    Label mFlatDamage;
    Label mFlatCures;
    
    private Player? _player;

    ClassDescriptor mClassDescriptor;

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

    private Label CreateSectionTitle(string name, int x, int y, string text)
    {
        var label = new Label(this, name)
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

    private Label CreateBodyLabel(string name, int x, int y, int width, int height = StatRowHeight)
    {
        var label = new Label(this, name)
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

    private Button CreateStatButton(string name, int x, int y)
    {
        var button = new Button(this, name)
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

    //Init
    public CharacterWindow(Canvas gameCanvas) : base(gameCanvas, Strings.Character.Title, false, nameof(CharacterWindow))
    {
       
        SetSize(WindowWidth, WindowHeight);

        IsResizable = false;

        TitleLabel.FontName = TitleFont;
        TitleLabel.FontSize = 14;
        TitleLabel.TextColorOverride = Color.White;

        var headerX = Margin;
        var headerY = Margin;

        mCharacterName = new Label(this, "CharacterNameLabel")
        {
            FontName = TitleFont,
            FontSize = 18,
            TextColorOverride = Color.White,
            AutoSizeToContents = true
        };

        mCharacterName.SetPosition(headerX, headerY);

        mCharacterLevelAndClass = new Label(this, "ChatacterInfoLabel")
        {
            FontName = BodyFont,
            FontSize = 12,
            TextColorOverride = Color.White,
            AutoSizeToContents = true
        };

        mCharacterLevelAndClass.SetPosition(headerX, headerY + 24);

        mFactionButton = new Button(this, "FactionInfoButton")
        {
            Width = 32,
            Height = 32
        };

        mFactionButton.SetPosition(WindowWidth - Margin - mFactionButton.Width, headerY);
        mFactionButton.SetStateTexture(ComponentState.Normal, "factionicon.png");
        mFactionButton.SetStateTexture(ComponentState.Hovered, "factionicon_hovered.png");
        mFactionButton.SetToolTipText("Faction");
        mFactionButton.Clicked += (s, e) => Interface.GameUi.GameMenu?.ToggleFactionWindow();

        mCharacterContainer = new ImagePanel(this, "CharacterContainer");
        mCharacterContainer.SetSize(120, 120);
        mCharacterContainer.SetPosition(headerX, headerY + 48);

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

        var columns = 4;
        var slotW = 36;
        var slotH = 36;
        var spacingX = 8;
        var spacingY = 8;
        var equipmentHeaderX = WindowWidth - Margin - (columns * slotW + spacingX * (columns - 1));
        var equipmentHeader = CreateSectionTitle("EquipmentHeader", equipmentHeaderX, headerY, "Equipo");
        var startX = equipmentHeader.X;
        var startY = equipmentHeader.Y + equipmentHeader.Height + 8;

        int itemIndex = 0;
        var multiSlotTracker = new Dictionary<string, int>();

        for (int slotIndex = 0; slotIndex < Options.Instance.Equipment.EquipmentSlots.Count; slotIndex++)
        {
            var slot = Options.Instance.Equipment.EquipmentSlots[slotIndex];

            for (int j = 0; j < slot.MaxItems; j++)
            {
                var item = new EquipmentItem(slotIndex, this);
                Items.Add(item);

                var slotName = slot.Name;

                if (slot.MaxItems <= 1)
                {
                    item.Pnl = new ImagePanel(this, slotName);
                }
                else
                {
                    if (!multiSlotTracker.ContainsKey(slotName))
                        multiSlotTracker[slotName] = 0;

                    var currentIndex = multiSlotTracker[slotName];
                    item.Pnl = new ImagePanel(this, $"{slotName}_{currentIndex}");
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


        var statsHeader = CreateSectionTitle("StatsHeader", mCharacterContainer.X + mCharacterContainer.Width + 20, headerY, "Atributos");
        var statsX = statsHeader.X;
        var statsY = statsHeader.Y + statsHeader.Height + 6;

        mAttackLabel = CreateBodyLabel("AttackLabel", statsX, statsY, StatLabelWidth);
        mAddAttackBtn = CreateStatButton("IncreaseAttackButton", statsX + StatLabelWidth + 8, statsY);
        mAddAttackBtn.Clicked += _addAttackBtn_Clicked;

        mAbilityPwrLabel = CreateBodyLabel("AbilityPowerLabel", statsX, statsY + StatRowHeight, StatLabelWidth);
        mAddAbilityPwrBtn = CreateStatButton("IncreaseAbilityPowerButton", statsX + StatLabelWidth + 8, statsY + StatRowHeight);
        mAddAbilityPwrBtn.Clicked += _addAbilityPwrBtn_Clicked;

        mDefenseLabel = CreateBodyLabel("DefenseLabel", statsX, statsY + StatRowHeight * 2, StatLabelWidth);
        mAddDefenseBtn = CreateStatButton("IncreaseDefenseButton", statsX + StatLabelWidth + 8, statsY + StatRowHeight * 2);
        mAddDefenseBtn.Clicked += _addDefenseBtn_Clicked;

        mMagicRstLabel = CreateBodyLabel("MagicResistLabel", statsX, statsY + StatRowHeight * 3, StatLabelWidth);
        mAddMagicResistBtn = CreateStatButton("IncreaseMagicResistButton", statsX + StatLabelWidth + 8, statsY + StatRowHeight * 3);
        mAddMagicResistBtn.Clicked += _addMagicResistBtn_Clicked;

        mSpeedLabel = CreateBodyLabel("SpeedLabel", statsX, statsY + StatRowHeight * 4, StatLabelWidth);
        mAddAgilityBtn = CreateStatButton("IncreaseSpeedButton", statsX + StatLabelWidth + 8, statsY + StatRowHeight * 4);
        mAddAgilityBtn.Clicked += _addSpeedBtn_Clicked;

        mAgilityLabel = CreateBodyLabel("AgilityLabel", statsX, statsY + StatRowHeight * 5, StatLabelWidth);
        mDamageLabel = CreateBodyLabel("DamageLabel", statsX, statsY + StatRowHeight * 6, StatLabelWidth);
        mCureLabel = CreateBodyLabel("CureLabel", statsX, statsY + StatRowHeight * 7, StatLabelWidth);
        mCritChanceLabel = CreateBodyLabel("CritLabel", statsX, statsY + StatRowHeight * 8, StatLabelWidth);
        mPointsLabel = CreateBodyLabel("PointsLabel", statsX, statsY + StatRowHeight * 9, StatLabelWidth + 40);

        mBasicAttackDamageLabel = CreateBodyLabel(
            "BasicAttackDamageLabel",
            statsX,
            statsY + StatRowHeight * 10,
            StatLabelWidth + 60,
            StatRowHeight + 4
        );

        var effectsHeader = CreateSectionTitle(
            "ExtraBuffsLabel",
            statsX,
            statsY + StatRowHeight * 10 + mBasicAttackDamageLabel.Height + 12,
            Strings.Character.ExtraBuffs
        );
        var effectsY = effectsHeader.Y + effectsHeader.Height + 4;
        var effectsX = effectsHeader.X;
        var secondColumnX = effectsX + EffectLabelWidth + 20;
        var thirdColumnX = secondColumnX + EffectLabelWidth + 20;
        const int effectSpacing = 20;

        mHpRegen = CreateBodyLabel("HpRegen", effectsX, effectsY, EffectLabelWidth, effectSpacing);
        mManaRegen = CreateBodyLabel("ManaRegen", effectsX, effectsY + effectSpacing, EffectLabelWidth, effectSpacing);
        mLifeSteal = CreateBodyLabel("Lifesteal", effectsX, effectsY + effectSpacing * 2, EffectLabelWidth, effectSpacing);
        mAttackSpeed = CreateBodyLabel("AttackSpeed", effectsX, effectsY + effectSpacing * 3, EffectLabelWidth, effectSpacing);
        mSpeedBuff = CreateBodyLabel("SpeedBuff", effectsX, effectsY + effectSpacing * 4, EffectLabelWidth, effectSpacing);
        mDamageBuff = CreateBodyLabel("DamageBuff", effectsX, effectsY + effectSpacing * 5, EffectLabelWidth, effectSpacing);
        mAccuracy = CreateBodyLabel("Accuracy", effectsX, effectsY + effectSpacing * 6, EffectLabelWidth, effectSpacing);
        mEvasion = CreateBodyLabel("Evasion", effectsX, effectsY + effectSpacing * 7, EffectLabelWidth, effectSpacing);
        mArmorPenetration = CreateBodyLabel(
            "ArmorPenetration",
            effectsX,
            effectsY + effectSpacing * 8,
            EffectLabelWidth,
            effectSpacing
        );

        mCureBuff = CreateBodyLabel("CureBuff", secondColumnX, effectsY, EffectLabelWidth, effectSpacing);
        mExtraExp = CreateBodyLabel("ExtraExp", secondColumnX, effectsY + effectSpacing, EffectLabelWidth, effectSpacing);
        mLuck = CreateBodyLabel("Luck", secondColumnX, effectsY + effectSpacing * 2, EffectLabelWidth, effectSpacing);
        mTenacity = CreateBodyLabel("Tenacity", secondColumnX, effectsY + effectSpacing * 3, EffectLabelWidth, effectSpacing);
        mCooldownReduction = CreateBodyLabel(
            "CooldownReduction",
            secondColumnX,
            effectsY + effectSpacing * 4,
            EffectLabelWidth,
            effectSpacing
        );
        mManaSteal = CreateBodyLabel("Manasteal", secondColumnX, effectsY + effectSpacing * 5, EffectLabelWidth, effectSpacing);
        mCritBonus = CreateBodyLabel("CriticalBonus", secondColumnX, effectsY + effectSpacing * 6, EffectLabelWidth, effectSpacing);
        mAntiCrit = CreateBodyLabel("AntiCritical", secondColumnX, effectsY + effectSpacing * 7, EffectLabelWidth, effectSpacing);
        mDamageReduction = CreateBodyLabel(
            "DamageReduction",
            secondColumnX,
            effectsY + effectSpacing * 8,
            EffectLabelWidth,
            effectSpacing
        );

        mDamageReflect = CreateBodyLabel("DamageReflect", thirdColumnX, effectsY, EffectLabelWidth, effectSpacing);
        mFlatDamage = CreateBodyLabel("FlatDamage", thirdColumnX, effectsY + effectSpacing, EffectLabelWidth, effectSpacing);
        mFlatCures = CreateBodyLabel("FlatCures", thirdColumnX, effectsY + effectSpacing * 2, EffectLabelWidth, effectSpacing);

        UpdateExtraBuffs();

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    public void SetPlayer(Player? player)
    {
        _player = player;
    }

    //Update Button Event Handlers
    void _addMagicResistBtn_Clicked(Base sender, MouseButtonState arguments)
    {
        PacketSender.SendUpgradeStat((int) Stat.Vitality);
    }

    void _addAbilityPwrBtn_Clicked(Base sender, MouseButtonState arguments)
    {
        PacketSender.SendUpgradeStat((int) Stat.Intelligence);
    }

    void _addSpeedBtn_Clicked(Base sender, MouseButtonState arguments)
    {
        PacketSender.SendUpgradeStat((int) Stat.Agility);
    }

    void _addDefenseBtn_Clicked(Base sender, MouseButtonState arguments)
    {
        PacketSender.SendUpgradeStat((int) Stat.Defense);
    }

    void _addAttackBtn_Clicked(Base sender, MouseButtonState arguments)
    {
        PacketSender.SendUpgradeStat((int) Stat.Attack);
    }

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
                    {
                        continue;
                    }

                    var descriptor = ItemDescriptor.Get(invItem.ItemId);
                    if (descriptor != null)
                    {
                        yield return descriptor;
                    }
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
                    {
                        continue;
                    }

                    var descriptor = ItemDescriptor.Get(id);
                    if (descriptor != null)
                    {
                        yield return descriptor;
                    }
                }
            }
        }
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

    //Methods
    public void Update()
    {
        var player = DisplayedPlayer;
        if (IsHidden || player is null)
        {
            return;
        }

        mCharacterName.Text = player.Name;
        mCharacterLevelAndClass.Text = Strings.Character.LevelAndClass.ToString(
            player.Level, ClassDescriptor.GetName(player.Class)
        );

        //Load Portrait
        //UNCOMMENT THIS LINE IF YOU'D RATHER HAVE A FACE HERE IGameTexture faceTex = Globals.ContentManager.GetTexture(Framework.Content.TextureType.Face, player.Face);
        var entityTex = Globals.ContentManager.GetTexture(
            Framework.Content.TextureType.Entity, player.Sprite
        );

        /* UNCOMMENT THIS BLOCK IF YOU"D RATHER HAVE A FACE HERE if (player.Face != "" && player.Face != _currentSprite && faceTex != null)
         {
             _characterPortrait.Texture = faceTex;
             _characterPortrait.SetTextureRect(0, 0, faceTex.GetWidth(), faceTex.GetHeight());
             _characterPortrait.SizeToContents();
             Align.Center(_characterPortrait);
             _characterPortrait.IsHidden = false;
             for (int i = 0; i < Options.Instance.Equipment.Slots.Count; i++)
             {
                 _paperdollPanels[i].Hide();
             }
         }
         else */
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

                    // Intentar obtener la lista de ítems equipados en este slot
                    if (equipment.TryGetValue(slotIndex, out var equippedList) && equippedList.Count > 0)
                    {
                        var inventoryIndex = equippedList[0]; // Tomamos el primero para mostrar
                        if (inventoryIndex >= 0 && inventoryIndex < Options.Instance.Player.MaxInventory)
                        {
                            var itemNum = player.Inventory[inventoryIndex].ItemId;

                            if (ItemDescriptor.TryGet(itemNum, out var itemDescriptor))
                            {
                                paperdoll = player.Gender == 0
                                    ? itemDescriptor.MalePaperdoll
                                    : itemDescriptor.FemalePaperdoll;

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
                        PaperdollPanels[z].SetPosition(mCharacterContainer.Width / 2 - PaperdollPanels[z].Width / 2, mCharacterContainer.Height / 2 - PaperdollPanels[z].Height / 2);
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
            {
                PaperdollPanels[i].Hide();
            }
        }

        mAttackLabel.SetText(
            Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Attack],
                player.Stat[(int)Stat.Attack]
            )
        );

        mAbilityPwrLabel.SetText(
            Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Intelligence],
                player.Stat[(int)Stat.Intelligence]
            )
        );

        mDefenseLabel.SetText(
            Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Defense],
                player.Stat[(int)Stat.Defense]
            )
        );

        mMagicRstLabel.SetText(
            Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Vitality],
                player.Stat[(int)Stat.Vitality]
            )
        );

        mSpeedLabel.SetText(
            Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Speed],
                player.Stat[(int)Stat.Speed]
            )
        );
        mAgilityLabel.SetText(
            Strings.Character.StatLabelValue.ToString(
                Strings.Combat.Stats[Stat.Agility],
                player.Stat[(int)Stat.Agility]
            )
        );
        var critChance = player.CalculateCriticalChance(player.GetBaseCriticalChance());
        mCritChanceLabel.SetText(Strings.Character.CriticalChance.ToString(critChance));
        mPointsLabel.SetText(Strings.Character.Points.ToString(player.StatPoints));
        mAddAbilityPwrBtn.IsHidden = player.StatPoints == 0 ||
                                     player.Stat[(int) Stat.Intelligence] == Options.Instance.Player.MaxStat;

        mAddAttackBtn.IsHidden =
            player.StatPoints == 0 || player.Stat[(int) Stat.Attack] == Options.Instance.Player.MaxStat;

        mAddDefenseBtn.IsHidden = player.StatPoints == 0 ||
                                  player.Stat[(int) Stat.Defense] == Options.Instance.Player.MaxStat;

        mAddMagicResistBtn.IsHidden = player.StatPoints == 0 ||
                                      player.Stat[(int) Stat.Vitality] == Options.Instance.Player.MaxStat;

        mAddAgilityBtn.IsHidden =
            player.StatPoints == 0 || player.Stat[(int) Stat.Agility] == Options.Instance.Player.MaxStat;

        UpdateExtraBuffs();
        mDamageLabel.SetText(Strings.Character.FlatDamage.ToString(FormatEffectValue(_flatDamage)));
        mCureLabel.SetText(Strings.Character.FlatCures.ToString(FormatEffectValue(_flatCures)));
        UpdateEquippedItems(true);
    }

    private void UpdateEquippedItems(bool updateExtraBuffs = false)
    {
        var player = DisplayedPlayer;
        if (player is null)
        {
            return;
        }

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
                    {
                        itemIds.Add(equippedIds[i]);
                    }

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
                    var effectValue = effect.GetValues();
                    if (effectValue.GetPrimaryValue() == 0)
                    {
                        continue;
                    }

                    switch (effect.Type)
                    {
                        case ItemEffect.CooldownReduction:
                            CooldownAmount += effectValue.GetPrimaryValue();
                            break;
                        case ItemEffect.Lifesteal:
                            LifeStealAmount += effectValue.GetPrimaryValue();
                            break;
                        case ItemEffect.Tenacity:
                            TenacityAmount += effectValue.GetPrimaryValue();
                            break;
                        case ItemEffect.Luck:
                            LuckAmount += effectValue.GetPrimaryValue();
                            break;
                        case ItemEffect.EXP:
                            ExtraExpAmount += effectValue.GetPrimaryValue();
                            break;
                        case ItemEffect.Manasteal:
                            ManaStealAmount += effectValue.GetPrimaryValue();
                            break;
                        case ItemEffect.Accuracy:
                            _accuracy = _accuracy.Add(effectValue);
                            break;
                        case ItemEffect.Evasion:
                            _evasion = _evasion.Add(effectValue);
                            break;
                        case ItemEffect.CriticalChance:
                            _critBonus = _critBonus.Add(effectValue);
                            break;
                        case ItemEffect.AntiCritChance:
                            _antiCrit = _antiCrit.Add(effectValue);
                            break;
                        case ItemEffect.ArmorPenetration:
                            _armorPenetration = _armorPenetration.Add(effectValue);
                            break;
                        case ItemEffect.DamageReduction:
                            _damageReduction = _damageReduction.Add(effectValue);
                            break;
                        case ItemEffect.DamageReflect:
                            _damageReflect = _damageReflect.Add(effectValue);
                            break;
                        case ItemEffect.Damages:
                            _flatDamage = _flatDamage.Add(effectValue);
                            break;
                        case ItemEffect.Cures:
                            _flatCures = _flatCures.Add(effectValue);
                            break;
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
        mFlatDamage.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Damages]} {FormatEffectValue(_flatDamage)}");
        mFlatCures.SetText($"{Strings.ItemDescription.BonusEffects[(int)ItemEffect.Cures]} {FormatEffectValue(_flatCures)}");
    }

    /// <summary>
    /// Update Extra Buffs Effects like hp/mana regen and items effect types
    /// </summary>
    /// <param name="itemId">Id of item to update extra buffs</param>
    private void UpdateExtraBuffs(Guid itemId)
    {
        var item = ItemDescriptor.Get(itemId);

        if (item == null)
        {
            return;
        }

        //Getting HP and Mana Regen from items
        if (item.VitalsRegen[(int)Vital.Health] != 0)
        {
            HpRegenAmount += item.VitalsRegen[(int)Vital.Health];
        }

        if (item.VitalsRegen[(int)Vital.Mana] != 0)
        {
            ManaRegenAmount += item.VitalsRegen[(int)Vital.Mana];
        }

        //Getting extra buffs from items
        if (item.Effects.Find(effect => effect.Type != ItemEffect.None && effect.GetValue() > 0) != default)
        {
            foreach (var effect in item.Effects)
            {
                var effectAmount = effect.GetValue();
                if (effectAmount <= 0)
                {
                    continue;
                }

                switch (effect.Type)
                {
                    case ItemEffect.CooldownReduction:
                        CooldownAmount += effectAmount;
                        break;
                    case ItemEffect.Lifesteal:
                        LifeStealAmount += effectAmount;
                        break;
                    case ItemEffect.Tenacity:
                        TenacityAmount += effectAmount;
                        break;
                    case ItemEffect.Luck:
                        LuckAmount += effectAmount;
                        break;
                    case ItemEffect.EXP:
                        ExtraExpAmount += effectAmount;
                        break;
                    case ItemEffect.Manasteal:
                        ManaStealAmount += effectAmount;
                        break;
                }
            }
        }
    }


    /// <summary>
    /// Show the window
    /// </summary>
    public void Show()
    {
        this.IsHidden = false;
    }

    /// <summary>
    /// </summary>
    /// <returns>Returns if window is visible</returns>
    public bool IsVisible()
    {
        return !this.IsHidden;
    }

    /// <summary>
    /// Hide the window
    /// </summary>
    public void Hide()
    {
        this.IsHidden = true;
    }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

}
