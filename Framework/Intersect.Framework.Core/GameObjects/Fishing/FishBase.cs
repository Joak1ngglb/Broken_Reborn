using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Conditions;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Fishing;

public partial class FishBase : DatabaseObject<FishBase>, IFolderable
{
    public FishBase()
    {
        Name = "New Fish";
    }

    [JsonConstructor]
    public FishBase(Guid id) : base(id)
    {
        Name = "New Fish";
    }

    public string Folder { get; set; } = string.Empty;

    public int Chance { get; set; }

    public int Strength { get; set; }

    public int Speed { get; set; }

    [NotMapped]
    public List<Guid> Hooks { get; set; } = [];

    [Column(nameof(Hooks))]
    public string JsonHooks
    {
        get => JsonConvert.SerializeObject(Hooks);
        set => Hooks = JsonConvert.DeserializeObject<List<Guid>>(value ?? string.Empty) ?? [];
    }

    [NotMapped]
    public ConditionLists Requirements { get; set; } = new();

    [Column(nameof(Requirements))]
    [JsonIgnore]
    public string JsonRequirements
    {
        get => Requirements.Data();
        set => Requirements.Load(value);
    }
}
