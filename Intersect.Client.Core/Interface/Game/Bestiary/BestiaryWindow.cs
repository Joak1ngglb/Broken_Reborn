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
using System.Drawing;
using Intersect;
using Intersect.Enums;
using System.Linq;
using System.Text.RegularExpressions;
namespace Intersect.Client.Interface.Game.Bestiary;
public sealed class BestiaryWindow : Window
{
    private readonly ScrollControl _tilesScroll;
    private readonly ScrollControl _detailsScroll;
    private readonly Base _detailsContent;
    private readonly TextBox _searchBox;
    private readonly List<BeastTile> _tiles = new();
    private Guid? _selectedNpcId;
    private Label statLbl;
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

        _detailsContent = new Base(_detailsScroll);
        _detailsContent.SetPosition(0, 0);
        _detailsContent.SetSize(1, 1);
      
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
        _detailsContent.DeleteAllChildren();
        _statsPanel = null;

        if (!NPCDescriptor.TryGet(npcId, out var desc))
        {
            return;
        }

        int yOffset = 0;

        var title = new Label(_detailsContent, "NpcTitle")
        {
            Text = desc.Name,
            FontName = "sourcesansproblack",
            FontSize = 14,
            TextColorOverride = Color.White,
        };
        title.SetPosition(10, yOffset);
        title.SetSize(300, 30);
        yOffset += 35;

        // Secciones condicionales por desbloqueo
        AddSection(npcId, BestiaryUnlock.Stats, Strings.Bestiary.SectionStats, desc, ref yOffset);
        AddSection(npcId, BestiaryUnlock.Drops, Strings.Bestiary.SectionDrops, desc, ref yOffset);
        AddSection(npcId, BestiaryUnlock.Spells, Strings.Bestiary.SectionSpells, desc, ref yOffset);
        AddSection(npcId, BestiaryUnlock.Behavior, Strings.Bestiary.SectionBehavior, desc, ref yOffset);
        AddSection(npcId, BestiaryUnlock.Lore, Strings.Bestiary.SectionLore, desc, ref yOffset);

        _detailsContent.SetSize(_detailsScroll.Width - 20, yOffset);
    }

    private void AddSection(Guid npcId, BestiaryUnlock unlock, LocalizedString title, NPCDescriptor desc, ref int yOffset)
    {
        var unlocked = BestiaryController.HasUnlock(npcId, unlock);

        var sectionTitle = new Label(_detailsContent, $"{unlock}SectionTitle")
        {
            Text = title,
            FontName = "sourcesansproblack",
            FontSize = 11,
            TextColorOverride = Color.White,
            AutoSizeToContents = true,
        };
        sectionTitle.SetPosition(10, yOffset);
        sectionTitle.SetSize(300, 22);
        yOffset += 22;

        if (!unlocked)
        {
            // Si la sección es de estadísticas y no está desbloqueada, oculta
            // el panel de estadísticas para evitar mostrar datos stale.
            if (unlock == BestiaryUnlock.Stats && _statsPanel != null)
            {
                _statsPanel.Hide();
            }

            var killsReq = desc.BestiaryRequirements.TryGetValue(unlock, out var req) ? req : 0;
            var currentKills = BestiaryController.GetKillCount(npcId);
            var lockedText = killsReq > 0
                ? Strings.Bestiary.LockedKills.ToString(currentKills, killsReq)
                : Strings.Bestiary.LockedInfo.ToString();

            var label = new Label(_detailsContent, $"{unlock}LockedLabel")
            {
                Text = lockedText,
                FontSize = 10,
                TextColorOverride = Color.White,
                AutoSizeToContents = true,
            };
            label.SetPosition(20, yOffset);
            label.SetSize(300, 22);
            yOffset += 24;
            return;
        }

        // Contenido real por sección
        switch (unlock)
        {
            case BestiaryUnlock.Stats:
                if (_statsPanel == null)
                {
                    _statsPanel = new BestiaryStatsPanel(_detailsContent);
                }

                _statsPanel.SetPosition(20, yOffset);
                _statsPanel.Show();
                _statsPanel.UpdateData(desc);
                yOffset += _statsPanel.Height + 8;
                break;


            case BestiaryUnlock.Drops:
                
                    const int maxPerRow = 6;
                    const int iconSize = 40;
                    const int spacing = 6;
                    int index = 0;

                    foreach (var drop in desc.Drops)
                    {
                        if (!ItemDescriptor.TryGet(drop.ItemId, out _)) continue;

                        var col = index % maxPerRow;
                        var row = index / maxPerRow;

                        var dropDisplay = new BestiaryItemDisplay(_detailsContent, drop.ItemId, drop.Chance);
                        int x = 20 + col * (iconSize + spacing);
                        int y = yOffset + row * (iconSize + spacing);
                        dropDisplay.SetPosition(x, y);

                        index++;
                    }

                    // Aumenta yOffset según las filas usadas
                    int totalRows = (index + maxPerRow - 1) / maxPerRow;
                    yOffset += totalRows * (iconSize + spacing);
                
                    break;           

            case BestiaryUnlock.Spells:

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

                    var spellDisplay = new BestiarySpellDisplay(_detailsContent, spell);
                    int x = 20 + col * (spellIconSize + spellSpacing);
                    int y = yOffset + row * (spellIconSize + spellSpacing);
                    spellDisplay.SetPosition(x, y);

                    spellIndex++;
                }

                int spellRows = (spellIndex + spellMaxPerRow - 1) / spellMaxPerRow;
                yOffset += spellRows * (spellIconSize + spellSpacing);


                break;

            case BestiaryUnlock.Behavior:
                var aggressiveText = desc.Aggressive ? Strings.Bestiary.Yes : Strings.Bestiary.No;
                var swarmText = desc.Swarm ? Strings.Bestiary.Yes : Strings.Bestiary.No;
                AddText(Strings.Bestiary.AggressiveLabel.ToString(aggressiveText), ref yOffset, "AggressiveLabel");
                AddText(Strings.Bestiary.MovementLabel.ToString(desc.Movement), ref yOffset, "MovementLabel");
                AddText(
                    Strings.Bestiary.FleeHpLabel.ToString(desc.FleeHealthPercentage),
                    ref yOffset,
                    "FleeHpLabel"
                );
                AddText(Strings.Bestiary.SwarmLabel.ToString(swarmText), ref yOffset, "SwarmLabel");
                break;

            case BestiaryUnlock.Lore:
                AddText(Strings.Bestiary.LorePlaceholder, ref yOffset, "LoreLabel");
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

    private void AddText(string content, ref int yOffset, string name)
    {
        var (posOverride, sizeOverride, cleanText) = ParseFormatting(content);

        var lbl = new Label(_detailsContent, name)
        {
            Text = cleanText,
            FontSize = 10,
            TextColorOverride = Color.White,
            AutoSizeToContents = true,
        };

        // Tamaño
        if (sizeOverride.HasValue)
        {
            lbl.SetSize(sizeOverride.Value.Width, sizeOverride.Value.Height);
        }
        else
        {
            // Tamaño por defecto similar a tu versión actual
            lbl.SetSize(300, 30);
            lbl.SizeToContents();
        }

        // Posición
        if (posOverride.HasValue)
        {
            // Posición absoluta (no altera yOffset)
            lbl.SetPosition(posOverride.Value.X, posOverride.Value.Y);
        }
        else
        {
            // Flujo normal
            lbl.SetPosition(20, yOffset);
            yOffset += lbl.Height + 4;
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
