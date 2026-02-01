using System.Collections.Generic;
using System.Linq;
using Intersect.Editor.Localization;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Events.Commands;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands;

public partial class EventCommandCompleteAchievementTask : UserControl
{
    private readonly FrmEvent mEventEditor;
    private readonly List<Guid> _achievementIds = [];
    private CompleteAchievementTaskCommand mMyCommand;

    public EventCommandCompleteAchievementTask(CompleteAchievementTaskCommand refCommand, FrmEvent editor)
    {
        InitializeComponent();
        mMyCommand = refCommand;
        mEventEditor = editor;
        InitLocalization();
        LoadAchievements();
    }

    private void InitLocalization()
    {
        grpCompleteAchievement.Text = Strings.EventCompleteAchievementTask.title;
        lblAchievement.Text = Strings.EventCompleteAchievementTask.achievement;
        lblTask.Text = Strings.EventCompleteAchievementTask.task;
        btnSave.Text = Strings.EventCompleteAchievementTask.okay;
        btnCancel.Text = Strings.EventCompleteAchievementTask.cancel;
    }

    private void LoadAchievements()
    {
        cmbAchievements.Items.Clear();
        _achievementIds.Clear();
        foreach (var achievement in AchievementDescriptor.Lookup.Values.OfType<AchievementDescriptor>().OrderBy(a => a.Name))
        {
            _achievementIds.Add(achievement.Id);
            cmbAchievements.Items.Add(achievement.Name);
        }

        var selectedIndex = _achievementIds.IndexOf(mMyCommand.AchievementId);
        if (selectedIndex >= 0)
        {
            cmbAchievements.SelectedIndex = selectedIndex;
        }
        else if (cmbAchievements.Items.Count > 0)
        {
            cmbAchievements.SelectedIndex = 0;
        }
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        mMyCommand.AchievementId = GetSelectedAchievementId();
        mMyCommand.ObjectiveIndex = GetSelectedObjectiveIndex();
        mEventEditor.FinishCommandEdit();
    }

    private Guid GetSelectedAchievementId()
    {
        if (cmbAchievements.SelectedIndex < 0 || cmbAchievements.SelectedIndex >= _achievementIds.Count)
        {
            return Guid.Empty;
        }

        return _achievementIds[cmbAchievements.SelectedIndex];
    }

    private int GetSelectedObjectiveIndex()
    {
        if (!cmbAchievementTask.Enabled || cmbAchievementTask.SelectedIndex < 0)
        {
            return -1;
        }

        return cmbAchievementTask.SelectedIndex;
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        mEventEditor.CancelCommandEdit();
    }

    private void cmbAchievements_SelectedIndexChanged(object sender, EventArgs e)
    {
        LoadObjectives();
    }

    private void LoadObjectives()
    {
        cmbAchievementTask.Items.Clear();
        var achievement = GetSelectedAchievement();
        if (achievement == null)
        {
            cmbAchievementTask.Enabled = false;
            lblTask.Enabled = false;
            return;
        }

        var objectives = GetObjectiveDescriptions(achievement).ToList();
        if (objectives.Count == 0)
        {
            cmbAchievementTask.Items.Add(Strings.EventCompleteAchievementTask.noobjectives);
            cmbAchievementTask.SelectedIndex = 0;
            cmbAchievementTask.Enabled = false;
            lblTask.Enabled = false;
            return;
        }

        cmbAchievementTask.Items.AddRange(objectives.ToArray());
        cmbAchievementTask.Enabled = true;
        lblTask.Enabled = true;
        cmbAchievementTask.SelectedIndex = mMyCommand.ObjectiveIndex >= 0 &&
                                           mMyCommand.ObjectiveIndex < objectives.Count
            ? mMyCommand.ObjectiveIndex
            : 0;
    }

    private AchievementDescriptor? GetSelectedAchievement()
    {
        var achievementId = GetSelectedAchievementId();
        return achievementId == Guid.Empty ? null : AchievementDescriptor.Get(achievementId);
    }

    private static IEnumerable<string> GetObjectiveDescriptions(AchievementDescriptor achievement)
    {
        if (achievement.MetaAchievementIds.Count > 0)
        {
            foreach (var metaAchievementId in achievement.MetaAchievementIds)
            {
                var metaAchievement = AchievementDescriptor.Get(metaAchievementId);
                var name = metaAchievement?.Name ?? Strings.EventCompleteAchievementTask.achievementundefined;
                yield return Strings.EventCompleteAchievementTask.metaachievement.ToString(name);
            }

            yield break;
        }

        foreach (var condition in achievement.Requirements.Lists.SelectMany(list => list.Conditions))
        {
            yield return Strings.GetEventConditionalDesc((dynamic)condition);
        }
    }
}
