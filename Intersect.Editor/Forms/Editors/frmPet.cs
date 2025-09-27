using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DarkUI.Controls;
using DarkUI.Forms;
using Intersect.Editor.Content;
using Intersect.Editor.Core;
using Intersect.Editor.General;
using Intersect.Editor.Localization;
using Intersect.Editor.Networking;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Pets;
using Intersect.Framework.Core.GameObjects.Spells;
using Intersect.GameObjects;
using Intersect.Utilities;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Intersect.Editor.Forms.Editors;

public partial class FrmPet : EditorForm
{
    private readonly BindingList<string> _spellNames = new();
    private readonly Dictionary<SpellEffect, DarkCheckBox> _immunityCheckboxes = new();
    private readonly Dictionary<Stat, DarkNumericUpDown> _statControls = new();
    private readonly Dictionary<Vital, DarkNumericUpDown> _vitalControls = new();
    private readonly Dictionary<Vital, DarkNumericUpDown> _vitalRegenControls = new();

    private readonly List<PetDescriptor> _changed = new();
    private readonly List<string> _knownFolders = new();

    private string? _copiedItem;
    private PetDescriptor? _editorItem;
    private bool _isClosing;
    private bool _suppressPreviewUpdate;

    public FrmPet()
    {
        ApplyHooks();
        InitializeComponent();
        Icon = Program.Icon;
        _btnSave = btnSave;
        _btnCancel = btnCancel;

        lstGameObjects.Init(
            UpdateToolStripItems,
            AssignEditorItem,
            toolStripItemNew_Click,
            toolStripItemCopy_Click,
            toolStripItemUndo_Click,
            toolStripItemPaste_Click,
            toolStripItemDelete_Click
        );

        InitControlMappings();

        lstSpells.DataSource = _spellNames;
        pnlContainer.Hide();
        UpdateToolStripItems();
        UpdateEditorButtons(false);
    }

    private void InitControlMappings()
    {
        _statControls.Clear();
        _vitalControls.Clear();
        _vitalRegenControls.Clear();
        _immunityCheckboxes.Clear();

        if (nudStr != null)
        {
            _statControls[Stat.Attack] = nudStr;
        }

        if (nudMag != null)
        {
            _statControls[Stat.Intelligence] = nudMag;
        }

        if (nudDef != null)
        {
            _statControls[Stat.Defense] = nudDef;
        }

        if (nudMR != null)
        {
            _statControls[Stat.Vitality] = nudMR;
        }

        if (nudSpd != null)
        {
            _statControls[Stat.Speed] = nudSpd;
        }

        if (nudAgi != null)
        {
            _statControls[Stat.Agility] = nudAgi;
        }

        if (nudDamages != null)
        {
            _statControls[Stat.Damages] = nudDamages;
        }

        if (nudCures != null)
        {
            _statControls[Stat.Cures] = nudCures;
        }

        foreach (var (stat, control) in _statControls)
        {
            control.Minimum = 0;
            control.Maximum = Options.Instance.Player.MaxStat;
            var statKey = stat;
            var statControl = control;
            statControl.ValueChanged += (_, _) => UpdateStat(statKey, statControl);
        }

        if (nudHp != null)
        {
            _vitalControls[Vital.Health] = nudHp;
        }

        if (nudMana != null)
        {
            _vitalControls[Vital.Mana] = nudMana;
        }

        foreach (var (vital, control) in _vitalControls)
        {
            control.Minimum = 0;
            control.Maximum = int.MaxValue;
            var vitalKey = vital;
            var vitalControl = control;
            vitalControl.ValueChanged += (_, _) => UpdateVital(vitalKey, vitalControl);
        }

        if (nudHpRegen != null)
        {
            _vitalRegenControls[Vital.Health] = nudHpRegen;
        }

        if (nudMpRegen != null)
        {
            _vitalRegenControls[Vital.Mana] = nudMpRegen;
        }

        foreach (var (vital, control) in _vitalRegenControls)
        {
            control.Minimum = 0;
            control.Maximum = int.MaxValue;
            var vitalKey = vital;
            var vitalControl = control;
            vitalControl.ValueChanged += (_, _) => UpdateVitalRegen(vitalKey, vitalControl);
        }

        if (chkKnockback != null)
        {
            _immunityCheckboxes[SpellEffect.Knockback] = chkKnockback;
        }

        if (chkSilence != null)
        {
            _immunityCheckboxes[SpellEffect.Silence] = chkSilence;
        }

        if (chkStun != null)
        {
            _immunityCheckboxes[SpellEffect.Stun] = chkStun;
        }

        if (chkSnare != null)
        {
            _immunityCheckboxes[SpellEffect.Snare] = chkSnare;
        }

        if (chkBlind != null)
        {
            _immunityCheckboxes[SpellEffect.Blind] = chkBlind;
        }

        if (chkTransform != null)
        {
            _immunityCheckboxes[SpellEffect.Transform] = chkTransform;
        }

        if (chkSleep != null)
        {
            _immunityCheckboxes[SpellEffect.Sleep] = chkSleep;
        }

        if (chkTaunt != null)
        {
            _immunityCheckboxes[SpellEffect.Taunt] = chkTaunt;
        }

        foreach (var (effect, checkbox) in _immunityCheckboxes)
        {
            var effectKey = effect;
            var effectCheckbox = checkbox;
            effectCheckbox.CheckedChanged += (_, _) => UpdateImmunity(effectKey, effectCheckbox.Checked);
        }

        if (nudTenacity != null)
        {
            nudTenacity.Minimum = 0;
            nudTenacity.Maximum = 100;
            nudTenacity.DecimalPlaces = 2;
            nudTenacity.ValueChanged += (_, _) => UpdateTenacity();
        }

    }

    private void UpdateStat(Stat stat, DarkNumericUpDown control)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.Stats[(int)stat] = (int)control.Value;
    }

    private void UpdateVital(Vital vital, DarkNumericUpDown control)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.MaxVitals[(int)vital] = (long)control.Value;
    }

    private void UpdateVitalRegen(Vital vital, DarkNumericUpDown control)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.VitalRegen[(int)vital] = (long)control.Value;
    }

    private void UpdateImmunity(SpellEffect effect, bool isChecked)
    {
        if (_editorItem == null)
        {
            return;
        }

        if (isChecked)
        {
            if (!_editorItem.Immunities.Contains(effect))
            {
                _editorItem.Immunities.Add(effect);
            }
        }
        else
        {
            _editorItem.Immunities.Remove(effect);
        }
    }

    private void UpdateTenacity()
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.Tenacity = (double)nudTenacity.Value;
    }


    private void frmPet_Load(object sender, EventArgs e)
    {
        var entitySprites = GameContentManager.GetSmartSortedTextureNames(
            GameContentManager.TextureType.Entity
        );

        InitializeSpriteCombo(cmbMaleSprite, entitySprites);
        InitializeSpriteCombo(cmbFemaleSprite, entitySprites);

        cmbFeedingItem.Items.Clear();
        cmbFeedingItem.Items.Add(Strings.General.None);
        cmbFeedingItem.Items.AddRange(ItemDescriptor.Names);
        if (cmbFeedingItem.Items.Count > 0)
        {
            cmbFeedingItem.SelectedIndex = 0;
        }

        cmbAttackAnimation.Items.Clear();
        cmbAttackAnimation.Items.Add(Strings.General.None);
        cmbAttackAnimation.Items.AddRange(AnimationDescriptor.Names);

        cmbDeathAnimation.Items.Clear();
        cmbDeathAnimation.Items.Add(Strings.General.None);
        cmbDeathAnimation.Items.AddRange(AnimationDescriptor.Names);

        cmbSpell.Items.Clear();
        cmbSpell.Items.AddRange(SpellDescriptor.Names);
        if (cmbSpell.Items.Count > 0)
        {
            cmbSpell.SelectedIndex = 0;
        }

        cmbDamageType.Items.Clear();
        for (var i = 0; i < Strings.Combat.damagetypes.Count; i++)
        {
            cmbDamageType.Items.Add(Strings.Combat.damagetypes[i]);
        }
        if (cmbDamageType.Items.Count > 0)
        {
            cmbDamageType.SelectedIndex = 0;
        }

        cmbAttackSpeedModifier.Items.Clear();
        foreach (var modifier in Strings.NpcEditor.attackspeedmodifiers)
        {
            cmbAttackSpeedModifier.Items.Add(modifier.Value.ToString());
        }
        if (cmbAttackSpeedModifier.Items.Count > 0)
        {
            cmbAttackSpeedModifier.SelectedIndex = 0;
        }

        cmbScalingStat.Items.Clear();
        for (var i = 0; i < Enum.GetValues<Stat>().Length; i++)
        {
            cmbScalingStat.Items.Add(Globals.GetStatName(i));
        }
        if (cmbScalingStat.Items.Count > 0)
        {
            cmbScalingStat.SelectedIndex = 0;
        }

        toolStripItemNew.ToolTipText = Strings.NpcEditor.New;
        toolStripItemDelete.ToolTipText = Strings.NpcEditor.delete;
        toolStripItemCopy.ToolTipText = Strings.NpcEditor.copy;
        toolStripItemPaste.ToolTipText = Strings.NpcEditor.paste;
        toolStripItemUndo.ToolTipText = Strings.NpcEditor.undo;
        btnAlphabetical.ToolTipText = Strings.NpcEditor.sortalphabetically;

        nudDamage.Maximum = Options.Instance.Player.MaxStat;
        nudScaling.Maximum = 1000;
        nudCritChance.Maximum = 100;
        nudAttackSpeedValue.Maximum = 100000;

        InitLocalization();
        InitEditor();
    }

    private void InitLocalization()
    {
        Text = Strings.Pets.title;
        grpPets.Text = Strings.Pets.petlist;
        btnClearSearch.Text = Strings.Pets.clearsearch;
        txtSearch.Text = Strings.Pets.searchplaceholder;
        grpGeneral.Text = Strings.Pets.general;
        lblName.Text = Strings.Pets.name;
        lblFolder.Text = Strings.Pets.folderlabel;
        btnAddFolder.Text = Strings.Pets.addfolder;
        lblBaseEnergy.Text = Strings.Pets.baseenergy;
        lblBaseMood.Text = Strings.Pets.basemood;
        lblBaseMaturity.Text = Strings.Pets.basematurity;
        lblFeedingItem.Text = Strings.Pets.feedingitem;
        lblMaleSprite.Text = Strings.Pets.malesprite;
        lblFemaleSprite.Text = Strings.Pets.femalesprite;
        rdoPreviewMale.Text = Strings.Pets.previewmale;
        rdoPreviewFemale.Text = Strings.Pets.previewfemale;
        lblHP.Text = $"{Strings.Combat.vitals[(int)Vital.Health]}:";
        lblMana.Text = $"{Strings.Combat.vitals[(int)Vital.Mana]}:";
        lblStr.Text = $"{Strings.Combat.stats[(int)Stat.Attack]}:";
        lblMag.Text = $"{Strings.Combat.stats[(int)Stat.Intelligence]}:";
        lblDef.Text = $"{Strings.Combat.stats[(int)Stat.Defense]}:";
        lblMR.Text = $"{Strings.Combat.stats[(int)Stat.Vitality]}:";
        lblSpd.Text = $"{Strings.Combat.stats[(int)Stat.Speed]}:";
        lblAgility.Text = $"{Strings.Combat.stats[(int)Stat.Agility]}:";
        lblDamages.Text = $"{Strings.Combat.stats[(int)Stat.Damages]}:";
        lblCures.Text = $"{Strings.Combat.stats[(int)Stat.Cures]}:";

        grpStats.Text = Strings.Pets.stats;
        grpRegen.Text = Strings.Pets.vitalregen;
        grpCombat.Text = Strings.Pets.combat;
        lblAttackAnimation.Text = Strings.Pets.attackanimation;
        lblDeathAnimation.Text = Strings.Pets.deathanimation;
        lblDamageType.Text = Strings.Pets.damagetype;
        lblScalingStat.Text = Strings.Pets.scalingstat;
        lblScaling.Text = Strings.Pets.scalingamount;
        lblDamage.Text = Strings.Pets.damage;
        lblCritChance.Text = Strings.Pets.critchance;
        lblCritMultiplier.Text = Strings.Pets.critmultiplier;
        lblAttackSpeedModifier.Text = Strings.Pets.attackspeedmodifier;
        lblAttackSpeedValue.Text = Strings.Pets.attackspeedvalue;
        grpImmunities.Text = Strings.Pets.immunities;
        lblTenacity.Text = Strings.Pets.tenacity;
        grpSpells.Text = Strings.Pets.spells;
        btnAdd.Text = Strings.Pets.addspell;
        btnRemove.Text = Strings.Pets.removespell;
        btnSave.Text = Strings.Pets.save;
        btnCancel.Text = Strings.Pets.cancel;
    }

    private static void InitializeSpriteCombo(DarkComboBox combo, string[] sprites)
    {
        combo.Items.Clear();
        combo.Items.Add(Strings.General.None);
        combo.Items.AddRange(sprites);

        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private static void SetSpriteSelection(DarkComboBox combo, string value)
    {
        var index = combo.FindString(value);
        combo.SelectedIndex = index >= 0 ? index : 0;
    }

    private void UpdateDescriptorFallbackSprite()
    {
        if (_editorItem == null)
        {
            return;
        }

        var fallback = _editorItem.MaleSprite;
        if (string.IsNullOrWhiteSpace(fallback))
        {
            fallback = _editorItem.FemaleSprite;
        }

        _editorItem.Sprite = fallback ?? string.Empty;
    }

    private string GetPreviewSpriteName()
    {
        return rdoPreviewFemale.Checked
            ? TextUtils.SanitizeNone(cmbFemaleSprite.Text)
            : TextUtils.SanitizeNone(cmbMaleSprite.Text);
    }

    private void UpdatePreview()
    {
        if (_suppressPreviewUpdate || _editorItem == null)
        {
            return;
        }

        picPet.BackgroundImage?.Dispose();
        picPet.BackgroundImage = null;

        var spriteName = GetPreviewSpriteName();
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return;
        }

        var texture = GameContentManager.GetTexture(GameContentManager.TextureType.Entity, spriteName);
        if (texture == null)
        {
            return;
        }

        var frames = Math.Max(1, Options.Instance.Sprites.NormalFrames);
        var directions = Math.Max(1, Options.Instance.Sprites.Directions);
        var frameWidth = texture.Width / frames;
        var frameHeight = texture.Height / directions;

        if (frameWidth <= 0 || frameHeight <= 0)
        {
            return;
        }

        var colors = new XnaColor[frameWidth * frameHeight];
        var sourceRectangle = new XnaRectangle(0, 0, frameWidth, frameHeight);
        texture.GetData(0, sourceRectangle, colors, 0, colors.Length);

        var pixelData = new byte[colors.Length * 4];
        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            var offset = i * 4;
            pixelData[offset] = color.B;
            pixelData[offset + 1] = color.G;
            pixelData[offset + 2] = color.R;
            pixelData[offset + 3] = color.A;
        }

        using var frameBitmap = new Bitmap(frameWidth, frameHeight, PixelFormat.Format32bppArgb);
        var bitmapData = frameBitmap.LockBits(
            new Rectangle(0, 0, frameWidth, frameHeight),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb
        );
        Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
        frameBitmap.UnlockBits(bitmapData);

        var previewBitmap = new Bitmap(picPet.Width, picPet.Height);
        using (var graphics = Graphics.FromImage(previewBitmap))
        {
            graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, previewBitmap.Width, previewBitmap.Height));
            var destination = new Rectangle(
                (previewBitmap.Width - frameBitmap.Width) / 2,
                (previewBitmap.Height - frameBitmap.Height) / 2,
                frameBitmap.Width,
                frameBitmap.Height
            );
            graphics.DrawImage(frameBitmap, destination);
        }

        picPet.BackgroundImage = previewBitmap;
    }

    private void AssignEditorItem(Guid id)
    {
        _editorItem = PetDescriptor.Get(id);
        UpdateEditor();
    }

    protected override void GameObjectUpdatedDelegate(GameObjectType type)
    {
        if (type == GameObjectType.Pet)
        {
            InitEditor();
            if (_editorItem != null && !PetDescriptor.Lookup.Values.Contains(_editorItem))
            {
                _editorItem = null;
                UpdateEditor();
            }
        }
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        foreach (var item in _changed)
        {
            item.Immunities.Sort();
            PacketSender.SendSaveObject(item);
            item.DeleteBackup();
        }

        Hide();
        Globals.CurrentEditor = -1;
        Dispose();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        foreach (var item in _changed)
        {
            item.RestoreBackup();
            item.DeleteBackup();
        }

        Hide();
        Globals.CurrentEditor = -1;
        Dispose();
    }

    private void frmPet_FormClosed(object sender, FormClosedEventArgs e)
    {
        btnCancel_Click(null, EventArgs.Empty);
    }

    private void UpdateEditor()
    {
        if (_editorItem != null)
        {
            pnlContainer.Show();
            _suppressPreviewUpdate = true;
            try
            {
                SetSpriteSelection(cmbMaleSprite, TextUtils.NullToNone(_editorItem.MaleSprite));
                SetSpriteSelection(cmbFemaleSprite, TextUtils.NullToNone(_editorItem.FemaleSprite));

                var useMalePreview = !string.IsNullOrWhiteSpace(_editorItem.MaleSprite)
                                     || string.IsNullOrWhiteSpace(_editorItem.FemaleSprite);
                rdoPreviewMale.Checked = useMalePreview;
                rdoPreviewFemale.Checked = !useMalePreview;
            }
            finally
            {
                _suppressPreviewUpdate = false;
            }

            txtName.Text = _editorItem.Name;
            cmbFolder.Text = _editorItem.Folder;
            nudBaseEnergy.Value = Math.Max(nudBaseEnergy.Minimum, Math.Min(nudBaseEnergy.Maximum, _editorItem.BaseEnergy));
            nudBaseMood.Value = Math.Max(nudBaseMood.Minimum, Math.Min(nudBaseMood.Maximum, _editorItem.BaseMood));
            nudBaseMaturity.Value = Math.Max(nudBaseMaturity.Minimum, Math.Min(nudBaseMaturity.Maximum, _editorItem.BaseMaturity));
            SetComboIndex(cmbFeedingItem, ItemDescriptor.ListIndex(_editorItem.FeedingItemId) + 1, 0);
            SetComboIndex(cmbAttackAnimation, AnimationDescriptor.ListIndex(_editorItem.AttackAnimationId) + 1, 0);
            SetComboIndex(cmbDeathAnimation, AnimationDescriptor.ListIndex(_editorItem.DeathAnimationId) + 1, 0);
            SetComboIndex(cmbDamageType, _editorItem.DamageType);
            SetComboIndex(cmbScalingStat, _editorItem.ScalingStat);
            nudScaling.Value = Math.Max(nudScaling.Minimum, Math.Min(nudScaling.Maximum, _editorItem.Scaling));
            nudDamage.Value = Math.Max(nudDamage.Minimum, Math.Min(nudDamage.Maximum, _editorItem.Damage));
            nudCritChance.Value = Math.Max(nudCritChance.Minimum, Math.Min(nudCritChance.Maximum, _editorItem.CritChance));
            nudCritMultiplier.Value = Math.Max(nudCritMultiplier.Minimum, Math.Min(nudCritMultiplier.Maximum, (decimal)_editorItem.CritMultiplier));
            SetComboIndex(cmbAttackSpeedModifier, _editorItem.AttackSpeedModifier);
            nudAttackSpeedValue.Value = Math.Max(nudAttackSpeedValue.Minimum, Math.Min(nudAttackSpeedValue.Maximum, _editorItem.AttackSpeedValue));
            nudTenacity.Value = (decimal)_editorItem.Tenacity;
            optLevel.Checked = _editorItem.LevelingMode == PetLevelingMode.Experience;
            optDoNotLevel.Checked = _editorItem.LevelingMode == PetLevelingMode.Disabled;
            pnlPetlevel.Visible = _editorItem.LevelingMode == PetLevelingMode.Experience;
            nudPetExp.Value = Math.Max(nudPetExp.Minimum, Math.Min(nudPetExp.Maximum, _editorItem.ExperienceRate));
            nudPetPnts.Value = Math.Max(nudPetPnts.Minimum, Math.Min(nudPetPnts.Maximum, _editorItem.StatPointsPerLevel));
            nudMaxLevel.Value = Math.Max(nudMaxLevel.Minimum, Math.Min(nudMaxLevel.Maximum, _editorItem.MaxLevel));
            foreach (var (stat, control) in _statControls)
            {
                control.Value = Math.Max(control.Minimum, Math.Min(control.Maximum, _editorItem.Stats[(int)stat]));
            }

            foreach (var (vital, control) in _vitalControls)
            {
                control.Value = Math.Max(control.Minimum, Math.Min(control.Maximum, _editorItem.MaxVitals[(int)vital]));
            }

            foreach (var (vital, control) in _vitalRegenControls)
            {
                control.Value = Math.Max(control.Minimum, Math.Min(control.Maximum, _editorItem.VitalRegen[(int)vital]));
            }

            foreach (var (effect, checkbox) in _immunityCheckboxes)
            {
                checkbox.Checked = _editorItem.Immunities.Contains(effect);
            }

            _spellNames.RaiseListChangedEvents = false;
            _spellNames.Clear();
            foreach (var spellId in _editorItem.Spells)
            {
                _spellNames.Add(spellId != Guid.Empty ? SpellDescriptor.GetName(spellId) : Strings.General.None);
            }

            _spellNames.RaiseListChangedEvents = true;
            _spellNames.ResetBindings();

            if (_editorItem.Spells.Count > 0)
            {
                lstSpells.SelectedIndex = 0;
                cmbSpell.SelectedIndex = SpellDescriptor.ListIndex(_editorItem.Spells[0]);
            }
            else if (cmbSpell.Items.Count > 0)
            {
                cmbSpell.SelectedIndex = 0;
            }

            if (!_changed.Contains(_editorItem))
            {
                _changed.Add(_editorItem);
                _editorItem.MakeBackup();
            }

            UpdateDescriptorFallbackSprite();
            UpdatePreview();
        }
        else
        {
            pnlContainer.Hide();
            picPet.BackgroundImage?.Dispose();
            picPet.BackgroundImage = null;
        }

        var hasItem = _editorItem != null;
        UpdateEditorButtons(hasItem);
        UpdateToolStripItems();
    }

    private void txtName_TextChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.Name = txtName.Text;
        lstGameObjects.UpdateText(txtName.Text);
    }

    private void cmbMaleSprite_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || cmbMaleSprite.SelectedIndex < 0)
        {
            return;
        }

        _editorItem.MaleSprite = TextUtils.SanitizeNone(cmbMaleSprite.Text);
        UpdateDescriptorFallbackSprite();

        if (!_suppressPreviewUpdate)
        {
            UpdatePreview();
        }
    }

    private void cmbFemaleSprite_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || cmbFemaleSprite.SelectedIndex < 0)
        {
            return;
        }

        _editorItem.FemaleSprite = TextUtils.SanitizeNone(cmbFemaleSprite.Text);
        UpdateDescriptorFallbackSprite();

        if (!_suppressPreviewUpdate)
        {
            UpdatePreview();
        }
    }

    private void rdoPreviewMale_CheckedChanged(object sender, EventArgs e)
    {
        if (!_suppressPreviewUpdate && rdoPreviewMale.Checked)
        {
            UpdatePreview();
        }
    }

    private void rdoPreviewFemale_CheckedChanged(object sender, EventArgs e)
    {
        if (!_suppressPreviewUpdate && rdoPreviewFemale.Checked)
        {
            UpdatePreview();
        }
    }

    private void cmbFeedingItem_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || cmbFeedingItem.SelectedIndex < 0)
        {
            return;
        }

        var selectedIndex = cmbFeedingItem.SelectedIndex;
        _editorItem.FeedingItemId = selectedIndex <= 0
            ? Guid.Empty
            : ItemDescriptor.IdFromList(selectedIndex - 1);
    }

    private void nudHp_ValueChanged(object sender, EventArgs e) => UpdateVital(Vital.Health, nudHp);

    private void nudMana_ValueChanged(object sender, EventArgs e) => UpdateVital(Vital.Mana, nudMana);

    private void nudBaseEnergy_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.BaseEnergy = (int)nudBaseEnergy.Value;
    }

    private void nudBaseMood_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.BaseMood = (int)nudBaseMood.Value;
    }

    private void nudBaseMaturity_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.BaseMaturity = (int)nudBaseMaturity.Value;
    }

    private void nudHpRegen_ValueChanged(object sender, EventArgs e) => UpdateVitalRegen(Vital.Health, nudHpRegen);

    private void nudMpRegen_ValueChanged(object sender, EventArgs e) => UpdateVitalRegen(Vital.Mana, nudMpRegen);

    private void nudStr_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Attack, nudStr);

    private void nudMag_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Intelligence, nudMag);

    private void nudDef_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Defense, nudDef);

    private void nudMR_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Vitality, nudMR);

    private void nudSpd_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Speed, nudSpd);

    private void nudAgi_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Agility, nudAgi);

    private void nudDamages_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Damages, nudDamages);

    private void nudCures_ValueChanged(object sender, EventArgs e) => UpdateStat(Stat.Cures, nudCures);

    private void cmbAttackAnimation_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.AttackAnimation = AnimationDescriptor.Get(AnimationDescriptor.IdFromList(cmbAttackAnimation.SelectedIndex - 1));
    }

    private void cmbDeathAnimation_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.DeathAnimation = AnimationDescriptor.Get(AnimationDescriptor.IdFromList(cmbDeathAnimation.SelectedIndex - 1));
    }

    private void cmbDamageType_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.DamageType = cmbDamageType.SelectedIndex;
    }

    private void cmbScalingStat_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.ScalingStat = cmbScalingStat.SelectedIndex;
    }

    private void nudScaling_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.Scaling = (int)nudScaling.Value;
    }

    private void nudDamage_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.Damage = (int)nudDamage.Value;
    }

    private void nudCritChance_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.CritChance = (int)nudCritChance.Value;
    }

    private void nudCritMultiplier_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.CritMultiplier = (double)nudCritMultiplier.Value;
    }

    private void cmbAttackSpeedModifier_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.AttackSpeedModifier = cmbAttackSpeedModifier.SelectedIndex;
    }

    private void nudAttackSpeedValue_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.AttackSpeedValue = (int)nudAttackSpeedValue.Value;
    }

    private void nudTenacity_ValueChanged(object sender, EventArgs e) => UpdateTenacity();

    private void optLevel_CheckedChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || !optLevel.Checked)
        {
            return;
        }

        _editorItem.LevelingMode = PetLevelingMode.Experience;
        pnlPetlevel.Visible = true;
    }

    private void optDoNotLevel_CheckedChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || !optDoNotLevel.Checked)
        {
            return;
        }

        _editorItem.LevelingMode = PetLevelingMode.Disabled;
        pnlPetlevel.Visible = false;
    }

    private void nudPetExp_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.ExperienceRate = (int)nudPetExp.Value;
    }

    private void nudPetPnts_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.StatPointsPerLevel = (int)nudPetPnts.Value;
    }

    private void nudMaxLevel_ValueChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.MaxLevel = (int)nudMaxLevel.Value;
    }

    private void chkKnockback_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Knockback, chkKnockback.Checked);

    private void chkSilence_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Silence, chkSilence.Checked);

    private void chkStun_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Stun, chkStun.Checked);

    private void chkSnare_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Snare, chkSnare.Checked);

    private void chkBlind_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Blind, chkBlind.Checked);

    private void chkTransform_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Transform, chkTransform.Checked);

    private void chkSleep_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Sleep, chkSleep.Checked);

    private void chkTaunt_CheckedChanged(object sender, EventArgs e) => UpdateImmunity(SpellEffect.Taunt, chkTaunt.Checked);

    private void cmbSpell_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null || lstSpells.SelectedIndex < 0)
        {
            return;
        }

        if (lstSpells.SelectedIndex >= _editorItem.Spells.Count)
        {
            return;
        }

        var selectedSpell = SpellDescriptor.IdFromList(cmbSpell.SelectedIndex);
        _editorItem.Spells[lstSpells.SelectedIndex] = selectedSpell;
        var displayName = selectedSpell != Guid.Empty ? SpellDescriptor.GetName(selectedSpell) : Strings.General.None.ToString();
        _spellNames[lstSpells.SelectedIndex] = displayName;
        _spellNames.ResetItem(lstSpells.SelectedIndex);
    }

    private void lstSpells_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        if (lstSpells.SelectedIndex < 0 || lstSpells.SelectedIndex >= _editorItem.Spells.Count)
        {
            return;
        }

        cmbSpell.SelectedIndex = SpellDescriptor.ListIndex(_editorItem.Spells[lstSpells.SelectedIndex]);
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        if (cmbSpell.SelectedIndex < 0)
        {
            return;
        }

        var spellId = SpellDescriptor.IdFromList(cmbSpell.SelectedIndex);
        _editorItem.Spells.Add(spellId);
        _spellNames.Add(spellId != Guid.Empty ? SpellDescriptor.GetName(spellId) : Strings.General.None);
    }

    private void btnRemove_Click(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        if (lstSpells.SelectedIndex < 0 || lstSpells.SelectedIndex >= _editorItem.Spells.Count)
        {
            return;
        }

        var index = lstSpells.SelectedIndex;
        _editorItem.Spells.RemoveAt(index);
        _spellNames.RemoveAt(index);
    }

    private void cmbFolder_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        _editorItem.Folder = cmbFolder.Text;
        InitEditor();
    }

    private void btnAddFolder_Click(object sender, EventArgs e)
    {
        if (_editorItem == null)
        {
            return;
        }

        var folderName = string.Empty;
        var result = DarkInputBox.ShowInformation(
            Strings.Pets.folderprompt,
            Strings.Pets.foldertitle,
            ref folderName,
            DarkDialogButton.OkCancel
        );

        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderName))
        {
            if (!cmbFolder.Items.Contains(folderName))
            {
                _editorItem.Folder = folderName;
                lstGameObjects.UpdateText(folderName);
                InitEditor();
                cmbFolder.Text = folderName;
            }
        }
    }

    private void btnAlphabetical_Click(object sender, EventArgs e)
    {
        btnAlphabetical.Checked = !btnAlphabetical.Checked;
        InitEditor();
    }

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        InitEditor();
    }

    private void txtSearch_Leave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSearch.Text))
        {
            txtSearch.Text = Strings.Pets.searchplaceholder;
        }
    }

    private void txtSearch_Enter(object sender, EventArgs e)
    {
        txtSearch.SelectAll();
    }

    private void btnClearSearch_Click(object sender, EventArgs e)
    {
        txtSearch.Text = Strings.Pets.searchplaceholder;
        InitEditor();
    }

    private bool CustomSearch()
    {
        return !string.IsNullOrWhiteSpace(txtSearch.Text) && txtSearch.Text != Strings.Pets.searchplaceholder;
    }

    public void InitEditor()
    {
        var folders = new List<string>();
        foreach (var descriptor in PetDescriptor.Lookup)
        {
            if (descriptor.Value is not PetDescriptor pet)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(pet.Folder) && !folders.Contains(pet.Folder))
            {
                folders.Add(pet.Folder);
                if (!_knownFolders.Contains(pet.Folder))
                {
                    _knownFolders.Add(pet.Folder);
                }
            }
        }

        folders.Sort();
        _knownFolders.Sort();

        cmbFolder.Items.Clear();
        cmbFolder.Items.Add(string.Empty);
        cmbFolder.Items.AddRange(_knownFolders.Cast<object>().ToArray());

        var items = PetDescriptor.Lookup
            .OrderBy(p => p.Value?.Name)
            .Select(
                pair => new KeyValuePair<Guid, KeyValuePair<string, string>>(
                    pair.Key,
                    new KeyValuePair<string, string>(
                        ((PetDescriptor)pair.Value)?.Name ?? Models.DatabaseObject<PetDescriptor>.Deleted,
                        ((PetDescriptor)pair.Value)?.Folder ?? string.Empty
                    )
                )
            )
            .ToArray();

        lstGameObjects.Repopulate(items, folders, btnAlphabetical.Checked, CustomSearch(), txtSearch.Text);
    }

    private void toolStripItemNew_Click(object sender, EventArgs e)
    {
        PacketSender.SendCreateObject(GameObjectType.Pet);
    }

    private void toolStripItemDelete_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || !lstGameObjects.Focused)
        {
            return;
        }

        if (DarkMessageBox.ShowWarning(
                Strings.Pets.deleteprompt,
                Strings.Pets.deletetitle,
                DarkDialogButton.YesNo,
                Icon
            ) == DialogResult.Yes)
        {
            PacketSender.SendDeleteObject(_editorItem);
        }
    }

    private void toolStripItemCopy_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || !lstGameObjects.Focused)
        {
            return;
        }

        _copiedItem = _editorItem.JsonData;
        toolStripItemPaste.Enabled = true;
    }

    private void toolStripItemPaste_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || _copiedItem == null || !lstGameObjects.Focused)
        {
            return;
        }

        _editorItem.Load(_copiedItem, true);
        UpdateEditor();
    }

    private void toolStripItemUndo_Click(object sender, EventArgs e)
    {
        if (_editorItem == null || !_changed.Contains(_editorItem))
        {
            return;
        }

        if (DarkMessageBox.ShowWarning(
                Strings.Pets.undoprompt,
                Strings.Pets.undotitle,
                DarkDialogButton.YesNo,
                Icon
            ) == DialogResult.Yes)
        {
            _editorItem.RestoreBackup();
            UpdateEditor();
        }
    }

    private void UpdateToolStripItems()
    {
        toolStripItemCopy.Enabled = _editorItem != null && lstGameObjects.Focused;
        toolStripItemPaste.Enabled = _editorItem != null && _copiedItem != null && lstGameObjects.Focused;
        toolStripItemDelete.Enabled = _editorItem != null && lstGameObjects.Focused;
        toolStripItemUndo.Enabled = _editorItem != null && lstGameObjects.Focused;
    }

    private void form_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.N)
        {
            toolStripItemNew_Click(sender, e);
        }
    }

    private static void SetComboIndex(DarkComboBox combo, int index, int fallbackIndex = 0)
    {
        if (combo.Items.Count == 0)
        {
            combo.SelectedIndex = -1;
            return;
        }

        if (index < 0 || index >= combo.Items.Count)
        {
            index = Math.Min(Math.Max(fallbackIndex, 0), combo.Items.Count - 1);
        }

        combo.SelectedIndex = index;
    }

    private void grpRegen_Enter(object sender, EventArgs e)
    {

    }
}
