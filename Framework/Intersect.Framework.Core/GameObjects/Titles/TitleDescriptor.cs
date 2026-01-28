using Intersect.Collections;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Titles;

public sealed partial class TitleDescriptor : DatabaseObject<TitleDescriptor>, IFolderable
{
    public TitleDescriptor()
    {
        Name = "New Title";
    }

    [JsonConstructor]
    public TitleDescriptor(Guid id) : base(id)
    {
        Name = "New Title";
    }

    public string Description { get; set; } = string.Empty;

    public string? Folder { get; set; } = string.Empty;

    public static DatabaseObjectLookup TitleLookup => Lookup;
}
