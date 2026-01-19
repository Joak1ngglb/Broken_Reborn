using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Interface.Game.DescriptionWindows;
using Intersect.Client.Interface.Game.DescriptionWindows.Components;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.GameObjects;
using System;
using System.Drawing;

namespace Intersect.Client.Interface.Game.Bestiary;

public class BestiaryStatsPanel : Base
{
    private const string DamageIconName = "damage.png";
    private readonly RowContainerComponent _rows;

    public BestiaryStatsPanel(Base parent) : base(parent, nameof(BestiaryStatsPanel))
    {
        // Puedes usar LoadJsonUi si defines el layout visual en JSON
        SetSize(300, 300);

        _rows = new RowContainerComponent(this, "BestiaryStatsRows");
        _rows.SetPosition(0, 0);
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    public void UpdateData(NPCDescriptor? desc)
    {
        if (desc == null)
        {
            Hide();
            return;
        }

        Show();
        _rows.ClearRows();

        for (var i = 0; i < Enum.GetValues<Stat>().Length; i++)
        {
            var stat = (Stat)i;
            var value = desc.StatsLookup.TryGetValue(stat, out var v) ? v : 0;
            if (value <= 0)
            {
                continue;
            }

            var statIconName = StatEffectIconProvider.GetIconForStat(stat);
            _rows.AddKeyValueRow(
                Strings.ItemDescription.StatCounts[i],
                value.ToString(),
                statIconName,
                Color.White,
                Color.White
            );
        }

        _rows.AddKeyValueRow("Nivel", desc.Level.ToString(), null, Color.White, Color.White);

        var health = desc.MaxVitalsLookup.TryGetValue(Vital.Health, out var h) ? h : 0;
        _rows.AddKeyValueRow(
            "Vida",
            health.ToString(),
            StatEffectIconProvider.GetIconForVital(Vital.Health),
            Color.White,
            Color.White
        );

        var mana = desc.MaxVitalsLookup.TryGetValue(Vital.Mana, out var m) ? m : 0;
        _rows.AddKeyValueRow(
            "Mana",
            mana.ToString(),
            StatEffectIconProvider.GetIconForVital(Vital.Mana),
            Color.White,
            Color.White
        );

        var scalingValue = desc.Stats.Length > desc.ScalingStat ? desc.Stats[desc.ScalingStat] : 0;
        var baseDamage = desc.Damage + scalingValue * (desc.Scaling / 100f);
        var minAttack = (int)Math.Round(baseDamage * 0.975f);
        var maxAttack = (int)Math.Round(baseDamage * 1.025f);

        _rows.AddKeyValueRow("Daño Mínimo", minAttack.ToString(), DamageIconName, Color.White, Color.White);
        _rows.AddKeyValueRow("Daño Máximo", maxAttack.ToString(), DamageIconName, Color.White, Color.White);

        _rows.SizeToChildren(true, true);
        SetSize(300, _rows.Height);
    }

}
