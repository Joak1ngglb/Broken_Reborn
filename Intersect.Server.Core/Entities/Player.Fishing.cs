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
    public Guid? FishingFishId { get; private set; }

    [NotMapped, JsonIgnore]
    public long FishingStageTimer { get; private set; }

    [NotMapped, JsonIgnore]
    public long FishingResolveTimer { get; private set; }

    [NotMapped, JsonIgnore]
    public FishingStage FishingStage { get; private set; } = FishingStage.None;

    [NotMapped, JsonIgnore]
    public bool FishingCancelRequested { get; private set; }

    [NotMapped, JsonIgnore]
    public bool FishingCanceled { get; private set; }

    public void StartFishing(Guid fishId, long stageTimer, long resolveTimer, FishingStage stage, bool cancelRequested)
    {
        FishingFishId = fishId;
        FishingStageTimer = stageTimer;
        FishingResolveTimer = resolveTimer;
        FishingStage = stage;
        FishingCancelRequested = cancelRequested;
        FishingCanceled = false;

        PacketSender.SendStartFishing(this, fishId, stageTimer, resolveTimer, stage, cancelRequested);
    }

    public void ResolveFishing(Guid fishId, long resolveTimer, bool cancelRequested, bool canceled)
    {
        FishingFishId = fishId;
        FishingResolveTimer = resolveTimer;
        FishingStage = FishingStage.Resolving;
        FishingCancelRequested = cancelRequested;
        FishingCanceled = canceled;

        PacketSender.SendResolveFishing(this, fishId, resolveTimer, cancelRequested, canceled);
    }

    public void StopFishing(bool canceled)
    {
        FishingCanceled = canceled;
        FishingStage = FishingStage.None;
        FishingFishId = null;
        FishingStageTimer = 0;
        FishingResolveTimer = 0;
        FishingCancelRequested = false;

        PacketSender.SendStopFishing(this, canceled);
    }
}
