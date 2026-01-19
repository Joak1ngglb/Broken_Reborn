using System;
using System.Linq;
using Intersect.Server.Core.CommandParsing;
using Intersect.Server.Localization;

namespace Intersect.Server.Core.Commands;

internal sealed partial class LocalizationStatusCommand : ServerCommand
{
    public LocalizationStatusCommand() : base(Strings.Commands.LocalizationStatus)
    {
    }

    protected override void HandleValue(ServerContext context, ParserResult result)
    {
        var entries = LocalizationRepository.Default.GetMissingAndNeedsReviewCounts();
        if (entries.Count == 0)
        {
            Console.WriteLine(Strings.Commandoutput.LocalizationStatusEmpty);
            return;
        }

        var langWidth = Math.Max("Lang".Length, entries.Max(entry => entry.Language.Length));
        var entityTypeWidth = Math.Max("EntityType".Length, entries.Max(entry => entry.EntityType.Length));
        var fieldWidth = Math.Max("Field".Length, entries.Max(entry => entry.Field.Length));

        var header =
            $"{ "Lang".PadRight(langWidth) } { "EntityType".PadRight(entityTypeWidth) } { "Field".PadRight(fieldWidth) } {"Count",5}";
        Console.WriteLine(header);
        foreach (var entry in entries.OrderBy(entry => entry.Language).ThenBy(entry => entry.EntityType).ThenBy(entry => entry.Field))
        {
            Console.WriteLine(
                $"{entry.Language.PadRight(langWidth)} {entry.EntityType.PadRight(entityTypeWidth)} {entry.Field.PadRight(fieldWidth)} {entry.Count,5}"
            );
        }
    }
}
