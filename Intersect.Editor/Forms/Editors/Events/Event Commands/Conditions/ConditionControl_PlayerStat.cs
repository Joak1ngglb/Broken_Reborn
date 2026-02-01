using System;
using System.Collections.Generic;
using Intersect.Editor.Localization;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands.Conditions;

public partial class ConditionControl_PlayerStat : UserControl
{
    private readonly List<VariableComparator> _comparators = new();

    public ConditionControl_PlayerStat()
    {
        InitializeComponent();
        InitLocalization();
    }

    public void InitLocalization()
    {
        grpLevelStat.Text = Strings.EventConditional.levelorstat;
        lblLvlStatValue.Text = Strings.EventConditional.levelstatvalue;
        lblLevelComparator.Text = Strings.EventConditional.comparator;
        lblLevelOrStat.Text = Strings.EventConditional.levelstatitem;
        cmbLevelStat.Items.Clear();
        cmbLevelStat.Items.Add(Strings.EventConditional.level);
        for (var i = 0; i < Enum.GetValues<Stat>().Length; i++)
        {
            cmbLevelStat.Items.Add(Strings.Combat.stats[i]);
        }

        cmbLevelComparator.Items.Clear();
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
                cmbLevelComparator.Items.Add(label);
            }
            else
            {
                cmbLevelComparator.Items.Add(comparator.ToString());
            }
        }

        chkStatIgnoreBuffs.Text = Strings.EventConditional.ignorestatbuffs;
    }

    public void SetupFormValues(LevelOrStatCondition condition)
    {
        var comparatorIndex = _comparators.IndexOf(condition.Comparator);
        cmbLevelComparator.SelectedIndex = comparatorIndex >= 0 ? comparatorIndex : 0;
        nudLevelStatValue.Value = condition.Value;
        cmbLevelStat.SelectedIndex = condition.ComparingLevel ? 0 : (int)condition.Stat + 1;
        chkStatIgnoreBuffs.Checked = condition.IgnoreBuffs;
    }

    public void SaveFormValues(LevelOrStatCondition condition)
    {
        if (cmbLevelComparator.SelectedIndex >= 0 && cmbLevelComparator.SelectedIndex < _comparators.Count)
        {
            condition.Comparator = _comparators[cmbLevelComparator.SelectedIndex];
        }
        else
        {
            condition.Comparator = VariableComparator.Equal;
        }

        condition.Value = (int)nudLevelStatValue.Value;
        condition.ComparingLevel = cmbLevelStat.SelectedIndex == 0;
        condition.IgnoreBuffs = chkStatIgnoreBuffs.Checked;

        if (!condition.ComparingLevel)
        {
            condition.Stat = (Stat)(cmbLevelStat.SelectedIndex - 1);
        }
    }
}
