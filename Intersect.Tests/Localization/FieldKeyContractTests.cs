using System;
using Intersect.Framework.Core.Localization;
using NUnit.Framework;

namespace Intersect.Tests.Localization;

[TestFixture]
public class FieldKeyContractTests
{
    [Test]
    public void QuestFieldKeyConstantsAreStable()
    {
        Assert.That(QuestFieldKey.Name, Is.EqualTo(QuestFieldKey.Name));
        Assert.That(QuestFieldKey.StartDescription, Is.EqualTo(QuestFieldKey.StartDescription));
        Assert.That(QuestFieldKey.BeforeDescription, Is.EqualTo(QuestFieldKey.BeforeDescription));
        Assert.That(QuestFieldKey.InProgressDescription, Is.EqualTo(QuestFieldKey.InProgressDescription));
        Assert.That(QuestFieldKey.EndDescription, Is.EqualTo(QuestFieldKey.EndDescription));
    }

    [Test]
    public void QuestFieldKeyTaskDescriptionWithSameInputReturnsSameKey()
    {
        var taskId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var first = QuestFieldKey.TaskDescription(taskId);
        var second = QuestFieldKey.TaskDescription(taskId);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void EventFieldKeyMethodsWithSameInputReturnSameKey()
    {
        var listId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        Assert.That(EventFieldKey.PageDescription(2), Is.EqualTo(EventFieldKey.PageDescription(2)));
        Assert.That(EventFieldKey.ShowText(1, listId, 3), Is.EqualTo(EventFieldKey.ShowText(1, listId, 3)));
        Assert.That(EventFieldKey.ChatboxText(1, listId, 3), Is.EqualTo(EventFieldKey.ChatboxText(1, listId, 3)));
        Assert.That(EventFieldKey.OptionsText(1, listId, 3), Is.EqualTo(EventFieldKey.OptionsText(1, listId, 3)));
        Assert.That(EventFieldKey.Option(1, listId, 3, 4), Is.EqualTo(EventFieldKey.Option(1, listId, 3, 4)));
        Assert.That(EventFieldKey.InputTitle(1, listId, 3), Is.EqualTo(EventFieldKey.InputTitle(1, listId, 3)));
        Assert.That(EventFieldKey.InputText(1, listId, 3), Is.EqualTo(EventFieldKey.InputText(1, listId, 3)));
        Assert.That(EventFieldKey.PlayerLabel(1, listId, 3), Is.EqualTo(EventFieldKey.PlayerLabel(1, listId, 3)));
    }
}
