using System.Collections.Generic;
using System.Linq;
using Intersect.Editor.Localization;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Events.Commands;

namespace Intersect.Editor.Forms.Editors.Events.Event_Commands;

public partial class EventCommandCompleteAchievement : UserControl
{
    private readonly FrmEvent mEventEditor;
    private readonly List<Guid> _achievementIds = [];
    private CompleteAchievementCommand mMyCommand;

    public EventCommandCompleteAchievement(CompleteAchievementCommand refCommand, FrmEvent editor)
    {
        InitializeComponent();
        mMyCommand = refCommand;
        mEventEditor = editor;
        InitLocalization();
        LoadAchievements();
    }

    private void InitLocalization()
    {
        grpCompleteAchievement.Text = Strings.EventCompleteAchievement.title;
        lblAchievement.Text = Strings.EventCompleteAchievement.achievement;
        btnSave.Text = Strings.EventCompleteAchievement.okay;
        btnCancel.Text = Strings.EventCompleteAchievement.cancel;
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

    private void btnCancel_Click(object sender, EventArgs e)
    {
        mEventEditor.CancelCommandEdit();
    }
}
