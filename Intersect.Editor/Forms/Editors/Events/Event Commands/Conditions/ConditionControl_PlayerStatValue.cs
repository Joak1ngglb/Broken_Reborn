using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Editor.Localization;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands.Conditions;

public partial class ConditionControl_PlayerStatValue : UserControl
{
    private readonly List<PlayerStatType> _statTypes = new();

    public ConditionControl_PlayerStatValue()
    {
        InitializeComponent();
        InitLocalization();
    }

    public void InitLocalization()
    {
        grpPlayerStat.Text = Strings.EventConditional.playerstat;
        lblStat.Text = Strings.EventConditional.playerstatlabel;
        lblComparator.Text = Strings.EventConditional.comparator;
        lblValue.Text = Strings.EventConditional.playerstatvalue;

        cmbComparator.Items.Clear();
        cmbComparator.Items.AddRange(Strings.EventConditional.comparators.Values.ToArray());

        cmbStat.Items.Clear();
        _statTypes.Clear();
        foreach (var stat in Enum.GetValues<PlayerStatType>())
        {
            _statTypes.Add(stat);
            if (Strings.EventConditional.playerstats.TryGetValue(stat, out var label))
            {
                cmbStat.Items.Add(label);
            }
            else
            {
                cmbStat.Items.Add(stat.ToString());
            }
        }

        if (cmbStat.Items.Count > 0 && cmbStat.SelectedIndex < 0)
        {
            cmbStat.SelectedIndex = 0;
        }

        if (cmbComparator.Items.Count > 0 && cmbComparator.SelectedIndex < 0)
        {
            cmbComparator.SelectedIndex = 0;
        }
    }

    public void SetupFormValues(PlayerStatCondition condition)
    {
        cmbComparator.SelectedIndex = (int)condition.Comparator;
        nudValue.Value = condition.Value;

        var index = _statTypes.IndexOf(condition.Stat);
        cmbStat.SelectedIndex = index >= 0 ? index : 0;
    }

    public void SaveFormValues(PlayerStatCondition condition)
    {
        condition.Comparator = (VariableComparator)cmbComparator.SelectedIndex;
        condition.Value = (int)nudValue.Value;

        if (cmbStat.SelectedIndex >= 0 && cmbStat.SelectedIndex < _statTypes.Count)
        {
            condition.Stat = _statTypes[cmbStat.SelectedIndex];
        }
    }
}
