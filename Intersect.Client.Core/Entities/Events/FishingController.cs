using System;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Interface.Game.Fishing;
using Intersect.Client.Networking;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Fishing;
using Intersect.Framework.Core.GameObjects.Fishing;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.GameObjects;
using Intersect.Network.Packets.Server.Fishing;
using Intersect.Utilities;

namespace Intersect.Client.Entities.Events;

public sealed class FishingController
{
    private readonly Player _player;
    private Guid _sessionId;
    private Guid _fishId;
    private FishingSimConfig? _config;
    private FishingSimState? _predictedState;
    private FishingRng _rng;
    private long _sessionStartMs;
    private uint _lastSentTick;
    private (bool Success, ItemDescriptor? Item)? _pendingOutcome;

    public FishingController(Player player)
    {
        _player = player;
    }

    private bool IsActive => _sessionId != Guid.Empty && _predictedState != null && _config != null;

    public void HandleCastResult(FishingCastResult packet)
    {
        if (!packet.Success)
        {
            ResetSession();
            return;
        }

        _sessionId = packet.SessionId;
    }

    public void HandleBiteStart(FishingBiteStart packet)
    {
        if (packet.SessionId != _sessionId && _sessionId != Guid.Empty)
        {
            return;
        }

        _sessionId = packet.SessionId;
        _fishId = packet.FishId;
        _config = packet.Config.ToConfig();
        _predictedState = packet.State.ToState();
        _rng = new FishingRng(packet.RngState);
        _sessionStartMs = Timing.Global.Milliseconds - _predictedState.TickMs;
        _lastSentTick = _predictedState.TickMs;
        _pendingOutcome = null;

        Globals.InFishing = true;

        _player.StartFishing(
            packet.FishId,
            Timing.Global.Milliseconds + packet.StageTimer,
            Timing.Global.Milliseconds + packet.ResolveTimer,
            FishingStage.Hooked,
            false
        );
    }

    public void HandleSnapshot(FishingStateSnapshot packet)
    {
        if (!IsActive || packet.SessionId != _sessionId || _predictedState == null)
        {
            return;
        }

        var serverState = packet.State.ToState();
        var driftCurrent = Math.Abs(serverState.CurrentValue - _predictedState.CurrentValue);
        var driftFish = Math.Abs(serverState.FishPosition - _predictedState.FishPosition);
        var driftPlayer = Math.Abs(serverState.PlayerPosition - _predictedState.PlayerPosition);
        var maxDrift = Math.Max(driftCurrent, Math.Max(driftFish, driftPlayer));

        _rng = new FishingRng(packet.RngState);

        if (maxDrift > 0.15f)
        {
            _predictedState = serverState;
            _sessionStartMs = Timing.Global.Milliseconds - _predictedState.TickMs;
            _lastSentTick = _predictedState.TickMs;
            return;
        }

        _predictedState.CurrentValue = Lerp(_predictedState.CurrentValue, serverState.CurrentValue, 0.5f);
        _predictedState.FishPosition = Lerp(_predictedState.FishPosition, serverState.FishPosition, 0.5f);
        _predictedState.PlayerPosition = Lerp(_predictedState.PlayerPosition, serverState.PlayerPosition, 0.5f);
        _predictedState.RangeSize = serverState.RangeSize;
        _predictedState.TargetRangeSize = serverState.TargetRangeSize;
        _predictedState.TickMs = serverState.TickMs;
        _sessionStartMs = Timing.Global.Milliseconds - _predictedState.TickMs;
    }

    public void HandleResolve(FishingResolve packet)
    {
        if (packet.SessionId != _sessionId)
        {
            return;
        }

        Globals.InFishing = false;
        _player.StopFishing(packet.Canceled);

        ItemDescriptor? itemDescriptor = null;
        if (packet.Payout?.Items.Count > 0)
        {
            var itemEntry = packet.Payout.Items.FirstOrDefault();
            if (itemEntry.Value > 0 && ItemDescriptor.TryGet(itemEntry.Key, out var descriptor))
            {
                itemDescriptor = descriptor;
            }
        }
        else if (_fishId != Guid.Empty)
        {
            var fish = FishBase.Get(_fishId);
            if (fish != null && ItemDescriptor.TryGet(fish.ItemId, out var descriptor))
            {
                itemDescriptor = descriptor;
            }
        }

        _pendingOutcome = (packet.Success && !packet.Canceled, itemDescriptor);
        ResetSession();
    }

    public void HandleCanceled(FishingCanceled packet)
    {
        if (_sessionId != Guid.Empty && packet.SessionId != Guid.Empty && _sessionId != packet.SessionId)
        {
            return;
        }

        Globals.InFishing = false;
        _pendingOutcome = (false, null);
        _player.StopFishing(true);
        ResetSession();
    }

    public void Update()
    {
        if (!Options.Instance.Features.NewFishingV2 || !IsActive || _predictedState == null || _config == null)
        {
            return;
        }

        var now = Timing.Global.Milliseconds;
        var targetTick = (uint)Math.Max(0, now - _sessionStartMs);
        var flags = Controls.IsControlJustPressed(Control.AttackInteract)
            ? FishingInputFlags.Tap
            : FishingInputFlags.None;

        if (targetTick <= _lastSentTick && flags == FishingInputFlags.None)
        {
            return;
        }

        var delta = (uint)Math.Max(0, targetTick - _predictedState.TickMs);
        var inputFrame = new FishingInputFrame
        {
            TickMs = targetTick,
            Flags = flags,
        };

        var result = FishingSimulator.Step(
            ref _predictedState,
            in _config,
            inputFrame,
            delta,
            ref _rng
        );

        _lastSentTick = targetTick;

        PacketSender.SendFishingInput(_sessionId, targetTick, flags);

        if (result != FishingSimResult.InProgress)
        {
            Globals.InFishing = false;
        }
    }

    public void SyncWindow(FishingWindow window)
    {
        if (_pendingOutcome.HasValue)
        {
            window.Show();
            window.HideBars();
            if (_pendingOutcome.Value.Success)
            {
                window.HideFailed();
                window.ShowSuccess();
                if (_pendingOutcome.Value.Item != null)
                {
                    window.RedrawIcon(_pendingOutcome.Value.Item);
                }
            }
            else
            {
                window.HideSuccess();
                window.ShowFailed();
            }

            _pendingOutcome = null;
            return;
        }

        if (!IsActive || _predictedState == null || _config == null)
        {
            window.Hide();
            return;
        }

        window.Show();
        window.HideSuccess();
        window.HideFailed();
        window.RangeMoveResize(_predictedState.FishPosition, _predictedState.RangeSize);
        window.PlayerForceMoved(_predictedState.PlayerPosition, _config.HookSize);
        window.Progress(_predictedState.CurrentValue);
    }

    private void ResetSession()
    {
        _sessionId = Guid.Empty;
        _fishId = Guid.Empty;
        _config = null;
        _predictedState = null;
        _lastSentTick = 0;
        _pendingOutcome = null;
        Globals.InFishing = false;
    }

    private static float Lerp(float from, float to, float t)
    {
        return from + (to - from) * t;
    }
}
