using System;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Enums;
using Intersect.Network.Packets.Server;
using Intersect.Server.Entities.Events;
using Intersect.Server.Networking;
using Newtonsoft.Json;

namespace Intersect.Server.Entities;

public partial class Player
{
    [NotMapped, JsonIgnore]
    private FishEventServer? _fishEvent;

    [NotMapped, JsonIgnore]
    public FishEventServer FishEvent => _fishEvent ??= new FishEventServer(this);

    [NotMapped, JsonIgnore]
    public Guid? FishingFishId { get;  set; }

    [NotMapped, JsonIgnore]
    public long FishingStageTimer { get;  set; }

    [NotMapped, JsonIgnore]
    public long FishingResolveTimer { get;  set; }

    [NotMapped, JsonIgnore]
    public FishingStage FishingStage { get; set; } = FishingStage.None;

    [NotMapped, JsonIgnore]
    public bool FishingCancelRequested { get; set; }

    [NotMapped, JsonIgnore]
    public bool FishingCanceled { get; set; }

  
}
