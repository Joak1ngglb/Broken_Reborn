using System;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Enums;
using Intersect.Fishing;
using Intersect.Framework.Core;
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
    public FishingSession? FishingSession { get; private set; }

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

    public void StartFishingSession(FishingSession session)
    {
        FishingSession = session;

        FishingFishId = session.FishId;
        FishingStageTimer = Timing.Global.Milliseconds + session.StageTimer;
        FishingResolveTimer = Timing.Global.Milliseconds + session.ResolveTimer;
        FishingStage = session.Stage;
        FishingCancelRequested = session.CancelRequested;
        FishingCanceled = session.Canceled;
    }

    public void ClearFishingSession()
    {
        FishingSession = null;
        FishingFishId = null;
        FishingStage = FishingStage.None;
        FishingStageTimer = 0;
        FishingResolveTimer = 0;
        FishingCancelRequested = false;
        FishingCanceled = false;
    }


}

public sealed class FishingSession
{
    public Guid SessionId { get; init; }

    public Guid FishingSpotId { get; init; }

    public Guid FishId { get; set; }

    public FishingStage Stage { get; set; }

    public long StageTimer { get; set; }

    public long ResolveTimer { get; set; }

    public bool CancelRequested { get; set; }

    public bool Canceled { get; set; }

    public FishingSimConfig? Config { get; set; }

    public FishingSimState? State { get; set; }

    public FishingRng? Rng { get; set; }

    public long NextSnapshotAt { get; set; }

    public long NextInputAt { get; set; }
}
