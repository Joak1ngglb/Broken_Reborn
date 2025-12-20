using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Config;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Fishing;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Fishing;
using Intersect.Network.Packets.Server;
using Intersect.Network.Packets.Server.Fishing;
using Intersect.Server.Entities;
using Intersect.Server.Maps;
using Intersect.Server.Networking;
using Intersect.Utilities;

namespace Intersect.Server.Entities.Events;

public class FishEventServer
{
    #region Vars

    private readonly Player _player;

    private int[]? _fishingPosition;
    private Direction _fishingDirection;
    private Guid _fishingSpotId;
    private long _timerWaitFish;
    private Guid _currentFishId;
    private int _stage;

    private Guid _pendingSessionId;
    private Guid _pendingFishingSpotId;
    private int[]? _pendingFishingPosition;
    private Direction _pendingFishingDirection;
    private long _pendingBiteAt;

    #endregion

    public FishEventServer(Player player)
    {
        _player = player;
    }

    public void CastV2(Guid sessionId, Guid fishingSpotId)
    {
        if (!Options.Instance.Features.NewFishingV2)
        {
            return;
        }

        var spot = FishingSpotBase.Get(fishingSpotId);
        if (spot == null || !Conditions.MeetsConditionLists(spot.FishingRequirements, _player, null))
        {
            _player.SendPacket(new FishingCastResult(sessionId, false, "Invalid fishing spot"));
            return;
        }

        _pendingSessionId = sessionId;
        _pendingFishingSpotId = fishingSpotId;
        _pendingFishingPosition = new[] { _player.X, _player.Y };
        _pendingFishingDirection = _player.Dir;
        _pendingBiteAt = Timing.Global.Milliseconds + Randomization.Next(spot.FishingTimeMin, spot.FishingTimeMax);

        _player.SendPacket(new FishingCastResult(sessionId, true));
    }

    public void CancelV2(Guid sessionId, string? reason = null)
    {
        if (!Options.Instance.Features.NewFishingV2)
        {
            return;
        }

        if (sessionId != Guid.Empty && _player.FishingSession?.SessionId != sessionId && _pendingSessionId != sessionId)
        {
            return;
        }

        _pendingSessionId = Guid.Empty;
        _pendingFishingSpotId = Guid.Empty;
        _pendingFishingPosition = null;
        _pendingBiteAt = 0;

        if (_player.FishingSession != null && (sessionId == Guid.Empty || _player.FishingSession.SessionId == sessionId))
        {
            ResolveCatchV2(true, reason);
        }
        else
        {
            _player.ClearFishingSession();
            _player.SendPacket(new StopFishingPacket(true));
        }

        _player.SendPacket(new FishingCanceled(sessionId, reason));
    }

    public void UpdateV2()
    {
        if (!Options.Instance.Features.NewFishingV2)
        {
            return;
        }

        TryStartBiteV2();
        MaybeSendSnapshotV2();
    }

    #region Stage 0 — player casts or retrieves the fishing rod

    public void ServerCastFishingRod(Guid fishingSpotId) //Player cast the fishing rod
    {
        var spot = FishingSpotBase.Get(fishingSpotId);
        if (spot != null && Conditions.MeetsConditionLists(spot.FishingRequirements, _player, null))
        {
            _fishingPosition = new[] { _player.X, _player.Y };
            _fishingDirection = _player.Dir;
            _fishingSpotId = fishingSpotId;
            _stage = 1;

            var timer = Randomization.Next(spot.FishingTimeMin, spot.FishingTimeMax);
            _timerWaitFish = Timing.Global.MillisecondsUtc + timer;
            PacketSender.SendClientResultCastFishingRod(_player, true);
        }
        else
        {
            PacketSender.SendClientResultCastFishingRod(_player, false);
        }
    }

    public void ServerReturnFishingRod() //Player retrieved the fishing rod
    {
        _stage = 0;
        _fishingSpotId = Guid.Empty;
        _timerWaitFish = 0;
    }

    #endregion

    #region Stage 1 After some time a random fish is sent to the player

    private bool WaitingCatchFish() //Waiting for a fish
    {
        if (_fishingPosition != null && (_player.X != _fishingPosition[0] || _player.Y != _fishingPosition[1] ||
                                         _fishingDirection != _player.Dir))
        {
            //Cancel the attempt if the player has moved or turned
            _stage = 0;
            _fishingSpotId = Guid.Empty;
            _timerWaitFish = 0;
            return false;
        }

        return Timing.Global.MillisecondsUtc >= _timerWaitFish;
    }

    private Guid GetRandomFish() //Choose a fish to give the player
    {
        var fishingSpot = FishingSpotBase.Get(_fishingSpotId);
        var fishes = new List<FishBase>();

        //Sort fish by rarity in increasing order
        var fishesSort = fishingSpot?.SortingFishByRarity(FishingSpotBase.SortByType.INCREASING).ToArray();

        //Check each fish against fishing requirements
        if (fishesSort != null)
        {
            foreach (var fishGuid in fishesSort)
            {
                var fish = FishBase.Get(fishGuid);
                if (fish != null && Conditions.MeetsConditionLists(fish.FishingRequirements, _player, null))
                {
                    fishes.Add(fish);
                }
            }
        }

        if (fishes.Count == 0)
        {
            return Guid.Empty;
        }

        var totalChance = fishes.Sum(fish => Math.Max(0, fish.chance));

        // If all chances are zero or negative, give each fish an even weight to avoid dead ends
        if (totalChance <= 0)
        {
            totalChance = fishes.Count;
        }

        var roll = Randomization.Next(1, totalChance + 1);
        var accumulated = 0;

        foreach (var fish in fishes)
        {
            var weight = Math.Max(0, fish.chance);
            weight = weight == 0 ? 1 : weight;

            accumulated += weight;

            if (roll <= accumulated)
            {
                return fish.Id;
            }
        }

        return fishes.Last().Id;
    }

    #endregion

    private long _packetTime;
    private bool _isFishingVisual;
    private int _stageVisual;
    private bool _isPressed;
    private bool _oldFishingVisual;
    private int _oldStageVisual;
    private bool _oldPressed;

    public void UpdateVisual(bool isFishingVisual, int stageVisual, bool isPressed)
    {
        _isFishingVisual = isFishingVisual;
        _stageVisual = stageVisual;
        _isPressed = isPressed;

        _player.IsFishing = _isFishingVisual;
        _player.FishingStageIndex = _stageVisual;
        _player.IsFishingRodPressed = _isPressed;
        _player.FishingStageTimer = Timing.Global.Milliseconds;
        _player.FishingStageDuration = Options.Instance.Sprites.IdleFrameDuration;
        //Console.Write($"Updating player {_player.Name}\n");
    }

    public void FishingUpdate()
    {
        if (Timing.Global.Milliseconds > _packetTime)
        {
            if (_isFishingVisual != _oldFishingVisual ||
                _stageVisual != _oldStageVisual ||
                _isPressed != _oldPressed)
            {
                PacketSender.SendEntityFishing(_player, _isFishingVisual, _stageVisual, _isPressed);
                _oldFishingVisual = _isFishingVisual;
                _oldStageVisual = _stageVisual;
                _oldPressed = _isPressed;
            }

            _packetTime = Timing.Global.Milliseconds + 100;
        }

        switch (_stage)
        {
            case 1:
                if (WaitingCatchFish())
                {
                    _currentFishId = GetRandomFish();
                    if (_currentFishId != Guid.Empty)
                    {
                        PacketSender.SendClientFish(_player, _currentFishId);
                        _stage = 2;
                    }
                    else
                    {
                        _stage = 0;
                        _fishingSpotId = Guid.Empty;
                        _timerWaitFish = 0;
                        _currentFishId = Guid.Empty;
                    }
                }

                break;
        }
    }

    public void SimulateV2(Guid sessionId, uint tickMs, FishingInputFlags flags)
    {
        if (!Options.Instance.Features.NewFishingV2)
        {
            return;
        }

        var session = _player.FishingSession;
        if (session == null || session.SessionId != sessionId || session.State == null || session.Config == null || session.Rng == null)
        {
            return;
        }

        var now = Timing.Global.Milliseconds;
        if (session.NextInputAt > now)
        {
            return;
        }

        var deltaMs = (long)tickMs - session.State.TickMs;
        if (deltaMs <= 0 && flags == FishingInputFlags.None)
        {
            return;
        }

        session.NextInputAt = now + 50;

        var inputFrame = new FishingInputFrame
        {
            TickMs = tickMs,
            Flags = flags,
        };

        var result = FishingSimulator.Step(
            ref session.State,
            in session.Config,
            inputFrame,
            (uint)Math.Max(0, deltaMs),
            ref session.Rng
        );
        session.Stage = FishingStage.Hooked;
        session.CancelRequested = false;

        switch (result)
        {
            case FishingSimResult.Success:
                ResolveCatchV2(false);
                break;
            case FishingSimResult.Failed:
                ResolveCatchV2(true);
                break;
        }
    }

    private void MaybeSendSnapshotV2()
    {
        var session = _player.FishingSession;
        if (session?.State == null)
        {
            return;
        }

        var now = Timing.Global.Milliseconds;
        if (session.NextSnapshotAt > now)
        {
            return;
        }

        session.NextSnapshotAt = now + 250;
        _player.SendPacket(
            new FishingStateSnapshot(
                session.SessionId,
                FishingSimStateDto.FromState(session.State),
                session.Rng?.State ?? 0
            )
        );
    }

    private void TryStartBiteV2()
    {
        if (_pendingSessionId == Guid.Empty || _pendingFishingSpotId == Guid.Empty)
        {
            return;
        }

        if (_pendingFishingPosition != null && (_player.X != _pendingFishingPosition[0] || _player.Y != _pendingFishingPosition[1] ||
                                                _pendingFishingDirection != _player.Dir))
        {
            CancelV2(_pendingSessionId);
            return;
        }

        if (Timing.Global.Milliseconds < _pendingBiteAt)
        {
            return;
        }

        _fishingSpotId = _pendingFishingSpotId;
        var fishId = GetRandomFish();
        if (fishId == Guid.Empty)
        {
            CancelV2(_pendingSessionId);
            return;
        }

        var fish = FishBase.Get(fishId);
        if (fish == null)
        {
            CancelV2(_pendingSessionId);
            return;
        }

        var session = BuildSession(_pendingSessionId, _pendingFishingSpotId, fish);
        _player.StartFishingSession(session);

        _player.SendPacket(
            new FishingBiteStart(
                session.SessionId,
                session.FishId,
                FishingSimConfigDto.FromConfig(session.Config!),
                FishingSimStateDto.FromState(session.State!),
                session.Rng?.State ?? 0,
                session.StageTimer,
                session.ResolveTimer
            )
        );

        _player.SendPacket(
            new FishingSessionConfigPacket(
                session.SessionId,
                FishingSimConfigDto.FromConfig(session.Config!),
                FishingSimStateDto.FromState(session.State!)
            )
        );

        PacketSender.SendEntityFishing(_player, true, 0, false);
        _player.SendPacket(new StartFishingPacket(session.FishId, 1000, session.ResolveTimer, FishingStage.Hooked, false));

        _pendingSessionId = Guid.Empty;
        _pendingFishingSpotId = Guid.Empty;
        _pendingFishingPosition = null;
        _pendingBiteAt = 0;
    }

    private FishingSession BuildSession(Guid sessionId, Guid fishingSpotId, FishBase fish)
    {
        var config = BuildSimConfig(fish);
        var state = new FishingSimState
        {
            CurrentValue = config.BeginValue,
            FishPosition = config.FishInitialPosition,
            FishMoveSpeed = config.FishBaseSpeed,
            RangeSize = config.FishRangeBaseSize,
            TargetRangeSize = config.FishRangeBaseSize,
            PlayerPosition = config.FishInitialPosition,
            PullMeter = 0f,
            NextSpeedChangeAtMs = config.TimeChangeSpeedMs,
            NextRangeChangeAtMs = config.TimeChangeRangeMs,
            TickMs = 0,
        };

        return new FishingSession
        {
            SessionId = sessionId,
            FishingSpotId = fishingSpotId,
            FishId = fish.Id,
            Stage = FishingStage.Hooked,
            StageTimer = 1000,
            ResolveTimer = 60000,
            Config = config,
            State = state,
            Rng = new FishingRng(DeriveSeed(sessionId, fish.Id)),
            NextSnapshotAt = Timing.Global.Milliseconds + 250,
            NextInputAt = Timing.Global.Milliseconds,
        };
    }

    private static FishingSimConfig BuildSimConfig(FishBase fish)
    {
        return new FishingSimConfig
        {
            BeginValue = 0.5f,
            PlayerStrength = 0.15f,
            HookSize = 0.15f,
            FishInitialPosition = fish.position / 100f,
            FishBaseSpeed = fish.speedMove / 100f,
            FishRangeBaseSize = fish.rangeSize / 100f,
            FishRangeChangeSpeed = fish.speedChangeRangeSize / 100f,
            FishWeight = fish.weight / 100f,
            FishStrengthDrain = fish.strength / 100f,
            FishPushStrength = fish.pushStrength / 100f,
            TimeChangeSpeedMs = fish.timeChangeSpeed,
            TimeChangeRangeMs = fish.timeChangeRangeSize,
            Unpredictability = fish.coeffUnpredictability,
        };
    }

    private static uint DeriveSeed(Guid sessionId, Guid fishId)
    {
        unchecked
        {
            var hash = 17u;
            foreach (var b in sessionId.ToByteArray())
            {
                hash = hash * 31u + b;
            }

            foreach (var b in fishId.ToByteArray())
            {
                hash = hash * 31u + b;
            }

            return hash;
        }
    }

    private void ResolveCatchV2(bool canceled, string? reason = null)
    {
        var session = _player.FishingSession;
        if (session == null)
        {
            return;
        }

        session.Canceled = canceled;

        var payout = BuildPayout(session, canceled);

        _player.SendPacket(
            new ResolveFishingPacket(
                session.FishId,
                1000,
                session.CancelRequested,
                canceled
            )
        );

        _player.SendPacket(
            new FishingResolve(
                session.SessionId,
                session.FishId,
                payout,
                !canceled,
                canceled
            )
        );

        if (!canceled)
        {
            ResolvePayout(session.FishId, session.FishingSpotId);
        }
        else
        {
            PacketSender.SendChatBubble(
                _player.Id,
                _player.MapInstanceId,
                (int)EntityType.GlobalEntity,
                "The fish got away..",
                _player.MapId
            );
        }

        _player.SendPacket(new StopFishingPacket(canceled));
        _player.ClearFishingSession();
    }

    private FishingSessionPayoutDto? BuildPayout(FishingSession session, bool canceled)
    {
        if (canceled)
        {
            return null;
        }

        var fish = FishBase.Get(session.FishId);
        var fishingSpot = FishingSpotBase.Get(session.FishingSpotId);

        if (fish == null)
        {
            return null;
        }

        var payout = new FishingSessionPayoutDto
        {
            Experience = fishingSpot?.FishingJobExperience ?? 0,
            Currency = 0,
        };

        payout.Items[fish.ItemId] = payout.Items.TryGetValue(fish.ItemId, out var count) ? count + 1 : 1;

        return payout;
    }

    private void ResolvePayout(Guid fishId, Guid fishingSpotId)
    {
        var fish = FishBase.Get(fishId);
        if (fish == null)
        {
            return;
        }

        var fishingSpot = FishingSpotBase.Get(fishingSpotId);

        if (!_player.TryGiveItem(fish.ItemId, 1))
        {
            if (MapController.TryGetInstanceFromMap(_player.MapId, _player.MapInstanceId, out var instance))
            {
                var item = new Database.Item(fish.ItemId, 1);
                instance.SpawnItem((Framework.Items.IItemSource)_player, _player.X, _player.Y, item, 1, _player.Id);
            }
        }

        if (fish.Event != default)
        {
            _player.EnqueueStartCommonEvent(fish.Event);
        }

        if (fishingSpot != null && fishingSpot.FishingJobExperience > 0)
        {
            _player.GiveJobExperience(JobType.Fishing, fishingSpot.FishingJobExperience);
        }
    }

    #region Stage 3 Fishing result

    public void FishingSuccess()
    {
        var fish = FishBase.Get(_currentFishId);
        if (fish == null)
        {
            return;
        }

        var fishingSpot = FishingSpotBase.Get(_fishingSpotId);

        if (!_player.TryGiveItem(fish.ItemId, 1))
        {
            if (MapController.TryGetInstanceFromMap(_player.MapId, _player.MapInstanceId, out var instance))
            {
                var item = new Database.Item(fish.ItemId, 1);
                instance.SpawnItem((Framework.Items.IItemSource)_player, _player.X, _player.Y, item, 1, _player.Id);
            }
        }

        if (fish.Event != default)
        {
            _player.EnqueueStartCommonEvent(fish.Event);
        }

        if (fishingSpot != null && fishingSpot.FishingJobExperience > 0)
        {
            _player.GiveJobExperience(JobType.Fishing, fishingSpot.FishingJobExperience);
        }

        //Console.Write($"{_player.Name} caught {FishBase.Get(_currentFishId).Name} with a fishing rod.\n");
        _stage = 0;
        _fishingSpotId = Guid.Empty;
        _timerWaitFish = 0;
        _currentFishId = Guid.Empty;
    }

    public void FishingFailed()
    {
        string[] strings =
        {
            "Got away..", "Darn..", "No luck.."
        };
        var rand = Randomization.Next(0, strings.Length);
        PacketSender.SendChatBubble(_player.Id, _player.MapInstanceId, (int)EntityType.GlobalEntity, strings[rand],
            _player.MapId);
        _stage = 0;
        _fishingSpotId = Guid.Empty;
        _timerWaitFish = 0;
        _currentFishId = Guid.Empty;
        //Console.Write($"{_player.Name} did not catch a fish.\n");
    }

    #endregion
}
