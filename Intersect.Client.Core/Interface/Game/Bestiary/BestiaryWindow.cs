using Intersect.Client.Controllers;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Utilities;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.GameObjects;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Core;
using Intersect.Client.Localization;
using Intersect.Framework.Core.GameObjects.Items;
using System.Collections.Generic;
using System.Drawing;
using Intersect;
using Intersect.Enums;
using System.Linq;
using System.Text.RegularExpressions;
using Intersect.Localization;
namespace Intersect.Client.Interface.Game.Bestiary;
public sealed class BestiaryWindow : Window
{
    private static readonly BestiaryUnlock[] SectionOrder =
    [
        BestiaryUnlock.Stats,
        BestiaryUnlock.Drops,
        BestiaryUnlock.Spells,
        BestiaryUnlock.Behavior,
        BestiaryUnlock.Lore,
    ];

    private readonly ScrollControl _tilesScroll;
    private readonly ScrollControl _detailsScroll;
    private readonly Base _detailsContent;
    private readonly TextBox _searchBox;
    private readonly List<BeastTile> _tiles = new();
    private Guid? _selectedNpcId;
    private readonly Label _npcTitleLabel;
    private readonly Dictionary<BestiaryUnlock, Label> _sectionTitleLabels = new();
    private readonly Dictionary<BestiaryUnlock, Label> _lockedRequirementLabels = new();
    private readonly Dictionary<BestiaryUnlock, Base> _sectionContents = new();
    private readonly Label _aggressiveLabel;
    private readonly Label _movementLabel;
    private readonly Label _fleeHpLabel;
    private readonly Label _swarmLabel;
    private readonly Label _loreLabel;
    private BestiaryStatsPanel _statsPanel;

    public BestiaryWindow(Canvas canvas)
        : base(canvas, Strings.Bestiary.Title, false, nameof(BestiaryWindow))
    {
        SetSize(720, 520);
        SetPosition(100, 100);
        IsResizable = false;
        IsClosable = true;

        _searchBox = new TextBox(this,$"Searcher")
        {
            PlaceholderText = Strings.Bestiary.SearchPlaceholder,
            Margin = new Margin(8, 8, 0, 0),
        };
        _searchBox.SetPosition(16, 8);
        _searchBox.SetSize(320, 40);
        _searchBox.TextChanged += (_, _) => RebuildTiles();
        Interface.FocusComponents.Add(_searchBox);
        _tilesScroll = new ScrollControl(this, $"MobsControl");
        _tilesScroll.EnableScroll(false, true);
        _tilesScroll.SetPosition(16, 50);
        _tilesScroll.SetSize(320, 440);

        _detailsScroll = new ScrollControl(this, "DetailsControl");
        _detailsScroll.SetPosition(352, 16);
        _detailsScroll.SetSize(340, 480);
        _detailsScroll.EnableScroll(horizontal: false, vertical: true);
        _detailsScroll.AutoHideBars = true;

        _detailsContent = new Base(_detailsScroll, "DetailsContent");
        _detailsContent.SetPosition(0, 0);
        _detailsContent.SetSize(1, 1);

        _npcTitleLabel = new Label(_detailsContent, "NpcTitle")
        {
            FontName = "sourcesansproblack",
            FontSize = 14,
            TextColorOverride = Color.White,
        };

        foreach (var unlock in SectionOrder)
        {
            var sectionTitle = new Label(_detailsContent, $"{unlock}SectionTitle")
            {
                FontName = "sourcesansproblack",
                FontSize = 11,
                TextColorOverride = Color.White,
                AutoSizeToContents = true,
            };

            var lockedLabel = new Label(_detailsContent, $"{unlock}LockedLabel");
            ConfigureDetailLabel(lockedLabel);
            lockedLabel.Hide();

            var sectionContent = new Base(_detailsContent, $"{unlock}SectionContent");
            sectionContent.SetSize(1, 1);
            sectionContent.Hide();

            _sectionTitleLabels.Add(unlock, sectionTitle);
            _lockedRequirementLabels.Add(unlock, lockedLabel);
            _sectionContents.Add(unlock, sectionContent);
        }

        var statsContainer = _sectionContents[BestiaryUnlock.Stats];
        _statsPanel = new BestiaryStatsPanel(statsContainer);
        _statsPanel.SetPosition(0, 0);

        var behaviorContainer = _sectionContents[BestiaryUnlock.Behavior];
        _aggressiveLabel = new Label(behaviorContainer, "AggressiveLabel");
        _movementLabel = new Label(behaviorContainer, "MovementLabel");
        _fleeHpLabel = new Label(behaviorContainer, "FleeHpLabel");
        _swarmLabel = new Label(behaviorContainer, "SwarmLabel");

        ConfigureDetailLabel(_aggressiveLabel);
        ConfigureDetailLabel(_movementLabel);
        ConfigureDetailLabel(_fleeHpLabel);
        ConfigureDetailLabel(_swarmLabel);

        var loreContainer = _sectionContents[BestiaryUnlock.Lore];
        _loreLabel = new Label(loreContainer, "LoreLabel");
        ConfigureDetailLabel(_loreLabel);
      
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

        BestiaryController.InitializeAllBeasts();
        BestiaryController.OnUnlockGained += OnUnlockGained;
        BuildTiles();
    }
    private void OnUnlockGained(Guid npcId, BestiaryUnlock _)
    {
        RefreshTilesState();
        if (_selectedNpcId == npcId)
        {
            ShowNpcDetails(npcId);
        }
    }

    private void BuildTiles()
    {
        foreach (var ch in _tiles) _tilesScroll.RemoveChild(ch, dispose: true);
        _tiles.Clear();

        var ids = BestiaryController.AllBeastNpcIds;
        IEnumerable<Guid> filtered = ids;

        if (!string.IsNullOrWhiteSpace(_searchBox.Text))
        {
            filtered = ids.Where(id =>
                NPCDescriptor.TryGet(id, out var d) &&
                SearchHelper.Matches(_searchBox.Text, d.Name));
        }

        var list = filtered
            .Select(id => (id, desc: NPCDescriptor.TryGet(id, out var d) ? d : null))
            .OrderBy(t => t.desc?.Level > 0 ? t.desc.Level : int.MaxValue)
            .ThenBy(t => t.desc?.Name ?? string.Empty)
            .Select(t => t.id)
            .ToList();
        const int tileW = 84, tileH = 104, pad = 6;
        var cols = Math.Max(1, (_tilesScroll.Width - _tilesScroll.VerticalScrollBar.Width) / (tileW + pad));

        for (int i = 0; i < list.Count; i++)
        {
            var tile = new BeastTile(_tilesScroll, list[i]);
            tile.ClickedNpc += OnSelectNpc;

            var col = i % cols;
            var row = i / cols;
            tile.SetBounds(col * (tileW + pad), row * (tileH + pad), tileW, tileH);

            _tiles.Add(tile);
        }
    }

    private void RebuildTiles()
    {
        const int tileW = 84, tileH = 104, pad = 6;
        var cols = Math.Max(1, (_tilesScroll.Width - _tilesScroll.VerticalScrollBar.Width) / (tileW + pad));

        var matches = new List<BeastTile>();

        foreach (var tile in _tiles)
        {
            var match = SearchHelper.Matches(
                _searchBox.Text,
                NPCDescriptor.TryGet(tile.NpcId, out var d) ? d.Name : string.Empty
            );

            tile.SetFilterMatch(match);
            tile.Update();

            if (match)
            {
                matches.Add(tile);
            }
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;
            matches[i].SetBounds(col * (tileW + pad), row * (tileH + pad), tileW, tileH);
        }
    }


    private void RefreshTilesState() => _tiles.ForEach(t => t.RefreshState());

    private void OnSelectNpc(Guid npcId)
    {
        _selectedNpcId = npcId;
        ShowNpcDetails(npcId);
    }

    private void ShowNpcDetails(Guid npcId)
    {
        if (!NPCDescriptor.TryGet(npcId, out var desc))
        {
            HideDetails();
            return;
        }

        int yOffset = 0;
        _npcTitleLabel.Text = desc.Name;
        _npcTitleLabel.SetPosition(10, yOffset);
        _npcTitleLabel.SetSize(300, 30);
        _npcTitleLabel.Show();
        yOffset += 35;

        // Secciones condicionales por desbloqueo
        foreach (var unlock in SectionOrder)
        {
            AddSection(npcId, unlock, GetSectionTitle(unlock), desc, ref yOffset);
        }

        _detailsContent.SetSize(_detailsScroll.Width - 20, yOffset);
    }

    private void AddSection(Guid npcId, BestiaryUnlock unlock, LocalizedString title, NPCDescriptor desc, ref int yOffset)
    {
        var unlocked = BestiaryController.HasUnlock(npcId, unlock);

        var sectionTitle = _sectionTitleLabels[unlock];
        sectionTitle.Text = title;
        sectionTitle.SetPosition(10, yOffset);
        sectionTitle.SetSize(300, 22);
        sectionTitle.Show();
        yOffset += 22;

        if (!unlocked)
        {
            // Si la sección es de estadísticas y no está desbloqueada, oculta
            // el panel de estadísticas para evitar mostrar datos stale.
            var killsReq = desc.BestiaryRequirements.TryGetValue(unlock, out var req) ? req : 0;
            var currentKills = BestiaryController.GetKillCount(npcId);
            var lockedText = killsReq > 0
                ? Strings.Bestiary.LockedKills.ToString(currentKills, killsReq)
                : Strings.Bestiary.LockedInfo.ToString();

            var lockedLabel = _lockedRequirementLabels[unlock];
            ApplyFormattedText(lockedLabel, lockedText, ref yOffset, 20, 300);
            lockedLabel.Show();
            _sectionContents[unlock].Hide();
            return;
        }

        // Contenido real por sección
        switch (unlock)
        {
            case BestiaryUnlock.Stats:
                _lockedRequirementLabels[unlock].Hide();
                _sectionContents[unlock].Show();
                _sectionContents[unlock].SetPosition(20, yOffset);
                _statsPanel.SetPosition(0, 0);
                _statsPanel.Show();
                _statsPanel.UpdateData(desc);
                _sectionContents[unlock].SetSize(_statsPanel.Width, _statsPanel.Height);
                yOffset += _statsPanel.Height + 8;
                break;


            case BestiaryUnlock.Drops:
                _lockedRequirementLabels[unlock].Hide();
                _sectionContents[unlock].Show();
                _sectionContents[unlock].DeleteAllChildren();
                _sectionContents[unlock].SetPosition(20, yOffset);

                const int maxPerRow = 6;
                const int iconSize = 40;
                const int spacing = 6;
                int index = 0;

                foreach (var drop in desc.Drops)
                {
                    if (!ItemDescriptor.TryGet(drop.ItemId, out _))
                    {
                        continue;
                    }

                    var col = index % maxPerRow;
                    var row = index / maxPerRow;

                    var dropDisplay = new BestiaryItemDisplay(_sectionContents[unlock], drop.ItemId, drop.Chance);
                    int x = col * (iconSize + spacing);
                    int y = row * (iconSize + spacing);
                    dropDisplay.SetPosition(x, y);

                    index++;
                }

                int totalRows = (index + maxPerRow - 1) / maxPerRow;
                var dropsHeight = totalRows * (iconSize + spacing);
                _sectionContents[unlock].SetSize(300, dropsHeight);
                yOffset += dropsHeight;
                break;           

            case BestiaryUnlock.Spells:
                _lockedRequirementLabels[unlock].Hide();
                _sectionContents[unlock].Show();
                _sectionContents[unlock].DeleteAllChildren();
                _sectionContents[unlock].SetPosition(20, yOffset);

                const int spellMaxPerRow = 6;
                const int spellIconSize = 40;
                const int spellSpacing = 6;
                int spellIndex = 0;


                foreach (var spellId in desc.Spells)
                {
                    var spell = SpellDescriptor.Get(spellId);
                    if (spell == null) continue;


                    var col = spellIndex % spellMaxPerRow;
                    var row = spellIndex / spellMaxPerRow;

                    var spellDisplay = new BestiarySpellDisplay(_sectionContents[unlock], spell);
                    int x = col * (spellIconSize + spellSpacing);
                    int y = row * (spellIconSize + spellSpacing);
                    spellDisplay.SetPosition(x, y);

                    spellIndex++;
                }

                int spellRows = (spellIndex + spellMaxPerRow - 1) / spellMaxPerRow;
                var spellsHeight = spellRows * (spellIconSize + spellSpacing);
                _sectionContents[unlock].SetSize(300, spellsHeight);
                yOffset += spellsHeight;


                break;

            case BestiaryUnlock.Behavior:
                var aggressiveText = desc.Aggressive ? Strings.Bestiary.Yes : Strings.Bestiary.No;
                var swarmText = desc.Swarm ? Strings.Bestiary.Yes : Strings.Bestiary.No;
                _lockedRequirementLabels[unlock].Hide();
                _sectionContents[unlock].Show();
                _sectionContents[unlock].SetPosition(20, yOffset);

                int behaviorOffset = 0;
                ApplyFormattedText(
                    _aggressiveLabel,
                    Strings.Bestiary.AggressiveLabel.ToString(aggressiveText),
                    ref behaviorOffset,
                    0,
                    300
                );
                ApplyFormattedText(
                    _movementLabel,
                    Strings.Bestiary.MovementLabel.ToString(desc.Movement),
                    ref behaviorOffset,
                    0,
                    300
                );
                ApplyFormattedText(
                    _fleeHpLabel,
                    Strings.Bestiary.FleeHpLabel.ToString(desc.FleeHealthPercentage),
                    ref behaviorOffset,
                    0,
                    300
                );
                ApplyFormattedText(
                    _swarmLabel,
                    Strings.Bestiary.SwarmLabel.ToString(swarmText),
                    ref behaviorOffset,
                    0,
                    300
                );

                _sectionContents[unlock].SetSize(300, behaviorOffset);
                yOffset += behaviorOffset;
                break;

            case BestiaryUnlock.Lore:
                _lockedRequirementLabels[unlock].Hide();
                _sectionContents[unlock].Show();
                _sectionContents[unlock].SetPosition(20, yOffset);

                int loreOffset = 0;
                ApplyFormattedText(_loreLabel, Strings.Bestiary.LorePlaceholder, ref loreOffset, 0, 300);
                _sectionContents[unlock].SetSize(300, loreOffset);
                yOffset += loreOffset;
                break;
        }

        yOffset += 4;
    }

    private static readonly Regex PosTag = new(@"\[pos\s*=\s*(\d+)\s*,\s*(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SizeTag = new(@"\[size\s*=\s*(\d+)\s*,\s*(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private (Point? pos, Size? size, string clean) ParseFormatting(string content)
    {
        Point? pos = null;
        Size? size = null;

        // Pos
        var mPos = PosTag.Match(content);
        if (mPos.Success && int.TryParse(mPos.Groups[1].Value, out var px) && int.TryParse(mPos.Groups[2].Value, out var py))
        {
            pos = new Point(px, py);
            content = PosTag.Replace(content, string.Empty);
        }

        // Size
        var mSize = SizeTag.Match(content);
        if (mSize.Success && int.TryParse(mSize.Groups[1].Value, out var w) && int.TryParse(mSize.Groups[2].Value, out var h))
        {
            size = new Size(w, h);
            content = SizeTag.Replace(content, string.Empty);
        }

        // Limpia espacios residuales
        content = content.Trim();

        return (pos, size, content);
    }

    private void ApplyFormattedText(Label label, string content, ref int yOffset, int baseX, int defaultWidth)
    {
        var (posOverride, sizeOverride, cleanText) = ParseFormatting(content);

        label.Text = cleanText;

        // Tamaño
        if (sizeOverride.HasValue)
        {
            label.SetSize(sizeOverride.Value.Width, sizeOverride.Value.Height);
        }
        else
        {
            // Tamaño por defecto similar a tu versión actual
            label.SetSize(defaultWidth, 30);
            label.SizeToContents();
        }

        // Posición
        if (posOverride.HasValue)
        {
            // Posición absoluta (no altera yOffset)
            label.SetPosition(posOverride.Value.X, posOverride.Value.Y);
        }
        else
        {
            // Flujo normal
            label.SetPosition(baseX, yOffset);
            yOffset += label.Height + 4;
        }

        label.Show();
    }

    private static LocalizedString GetSectionTitle(BestiaryUnlock unlock)
    {
        return unlock switch
        {
            BestiaryUnlock.Stats => Strings.Bestiary.SectionStats,
            BestiaryUnlock.Drops => Strings.Bestiary.SectionDrops,
            BestiaryUnlock.Spells => Strings.Bestiary.SectionSpells,
            BestiaryUnlock.Behavior => Strings.Bestiary.SectionBehavior,
            BestiaryUnlock.Lore => Strings.Bestiary.SectionLore,
            _ => Strings.Bestiary.SectionStats,
        };
    }

    private static void ConfigureDetailLabel(Label label)
    {
        label.FontSize = 10;
        label.TextColorOverride = Color.White;
        label.AutoSizeToContents = true;
    }

    private void HideDetails()
    {
        _npcTitleLabel.Hide();
        foreach (var label in _sectionTitleLabels.Values)
        {
            label.Hide();
        }

        foreach (var label in _lockedRequirementLabels.Values)
        {
            label.Hide();
        }

        foreach (var container in _sectionContents.Values)
        {
            container.Hide();
        }
    }


    internal void Update()
    {
        if (!IsVisibleInParent) return;
        foreach (var tile in _tiles) tile.Update();
    }
    protected override void Dispose(bool disposing)
    {
        BestiaryController.OnUnlockGained -= OnUnlockGained;
        base.Dispose(disposing);
    }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }
}
