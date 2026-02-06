using System;

namespace Intersect.Framework.Core.Localization;

public static class EventFieldKey
{
    public static string PageDescription(int pageIndex) =>
        $"Page:{pageIndex}:Description";

    public static string ShowText(int pageIndex, Guid listId, int commandIndex) =>
        $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}:ShowText";

    public static string ChatboxText(int pageIndex, Guid listId, int commandIndex) =>
        $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}:ChatboxText";

    public static string OptionsText(int pageIndex, Guid listId, int commandIndex) =>
        $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}:OptionsText";

    public static string Option(int pageIndex, Guid listId, int commandIndex, int optionIndex) =>
        $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}:Option:{optionIndex}";

    public static string InputTitle(int pageIndex, Guid listId, int commandIndex) =>
        $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}:InputTitle";

    public static string InputText(int pageIndex, Guid listId, int commandIndex) =>
        $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}:InputText";

    public static string PlayerLabel(int pageIndex, Guid listId, int commandIndex) =>
        $"Page:{pageIndex}:List:{listId}:Command:{commandIndex}:PlayerLabel";
}
