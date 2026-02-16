using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Events.Commands;

namespace Intersect.Server.Entities.Events;


public partial class CommandInstance
{

    public enum EventResponse
    {

        None = 0,

        Dialogue,

        Shop,

        Bank,

        Crafting,

        Quest,

        Timer,

        Picture,

        Fade

    }

    public Guid[]
        BranchIds = null; //Potential Branches for Commands that require responses such as ShowingOptions or Offering a Quest

    public EventCommand Command;

    private int commandIndex;

    public List<EventCommand> CommandList;

    public Guid CommandListId;

    public EventPage Page;

    public int PageIndex;

    public EventResponse WaitingForResponse = EventResponse.None;

    public Guid WaitingForRoute;

    public Guid WaitingForRouteMap;

    public EventCommand WaitingOnCommand = null;

    public CommandInstance(EventPage page, int listIndex = 0, int pageIndex = -1)
    {
        Page = page;
        PageIndex = pageIndex;

        if (page.CommandLists?.Any() == true)
        {
            var firstList = page.CommandLists.First();
            CommandListId = firstList.Key;
            CommandList = firstList.Value;
        }

        CommandIndex = listIndex;
    }

    public CommandInstance(
        EventPage page,
        List<EventCommand> commandList,
        int listIndex = 0,
        int pageIndex = -1,
        Guid commandListId = default
    )
    {
        Page = page;
        PageIndex = pageIndex;
        CommandList = commandList;
        CommandListId = commandListId;

        if (CommandListId == Guid.Empty && commandList != null && page.CommandLists != null)
        {
            foreach (var (listId, commands) in page.CommandLists)
            {
                if (commands == commandList)
                {
                    CommandListId = listId;
                    break;
                }
            }
        }

        CommandIndex = listIndex;
    }

    public CommandInstance(EventPage page, Guid commandListId, int listIndex = 0, int pageIndex = -1)
    {
        Page = page;
        PageIndex = pageIndex;
        CommandListId = commandListId;

        if (page.CommandLists != null && page.CommandLists.TryGetValue(commandListId, out var commandList))
        {
            CommandList = commandList;
        }

        CommandIndex = listIndex;
    }

    public int CommandIndex
    {
        get => commandIndex;
        set
        {
            commandIndex = value;
            Command = CommandList != null && commandIndex >= 0 && commandIndex < CommandList.Count
                ? CommandList[commandIndex]
                : null;
        }
    }

}
