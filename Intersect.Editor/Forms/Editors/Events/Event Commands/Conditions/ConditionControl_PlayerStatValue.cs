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
    private readonly List<VariableComparator> _comparators = new();
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
        _comparators.Clear();
        foreach (var comparator in Enum.GetValues<VariableComparator>())
        {
            if (comparator == VariableComparator.Between)
            {
                continue;
            }

            _comparators.Add(comparator);
            if (Strings.EventConditional.comparators.TryGetValue(comparator, out var label))
            {
                cmbComparator.Items.Add(label);
            }
            else
            {
                cmbComparator.Items.Add(comparator.ToString());
            }
        }

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
        var comparatorIndex = _comparators.IndexOf(condition.Comparator);
        cmbComparator.SelectedIndex = comparatorIndex >= 0 ? comparatorIndex : 0;
        nudValue.Value = condition.Value;

        var index = _statTypes.IndexOf(condition.Stat);
        cmbStat.SelectedIndex = index >= 0 ? index : 0;
    }

    public void SaveFormValues(PlayerStatCondition condition)
    {
        if (cmbComparator.SelectedIndex >= 0 && cmbComparator.SelectedIndex < _comparators.Count)
        {
            condition.Comparator = _comparators[cmbComparator.SelectedIndex];
        }
        else
        {
            condition.Comparator = VariableComparator.Equal;
        }

        condition.Value = (int)nudValue.Value;

        if (cmbStat.SelectedIndex >= 0 && cmbStat.SelectedIndex < _statTypes.Count)
        {
            condition.Stat = _statTypes[cmbStat.SelectedIndex];
        }
    }
}
