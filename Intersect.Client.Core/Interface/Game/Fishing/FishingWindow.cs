using System;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;

namespace Intersect.Client.Interface.Game.Fishing;

public class FishingWindow : Window
{
    private readonly Label _stageLabel;
    private readonly Label _stageTimerLabel;
    private readonly Label _resolveTimerLabel;
    private readonly Button _hookButton;
    private readonly Button _cancelButton;

    private FishingStage _lastStage = FishingStage.None;

    private const string BiteSound = "fishing_bite.ogg";
    private const string ResolveSound = "fishing_resolve.ogg";

    public FishingWindow(Canvas parent) : base(parent, Strings.Fishing.WindowTitle, false, nameof(FishingWindow))
    {
        IsClosable = true;
        IsResizable = false;
        IsHidden = true;
        IsVisibleInTree = false;
        SetSize(360, 180);

        var layout = new VerticalLayout(this)
        {
            Dock = Pos.Fill,
            Padding = new Padding(10),
            Spacing = 6,
        };

        _stageLabel = new Label(layout)
        {
            Alignment = Pos.Center,
            Text = Strings.Fishing.StageWaiting,
        };

        _stageTimerLabel = new Label(layout)
        {
            Text = Strings.Fishing.StageTimer.ToString("0.0s"),
        };

        _resolveTimerLabel = new Label(layout)
        {
            Text = Strings.Fishing.ResolveTimer.ToString("0.0s"),
        };

        var buttonRow = new HorizontalLayout(layout)
        {
            Dock = Pos.Bottom,
            Height = 32,
            Spacing = 8,
        };

        _hookButton = new Button(buttonRow)
        {
            Text = Strings.Fishing.Hook,
            Width = 120,
        };

        _hookButton.Clicked += (_, _) => PacketSender.SendSuccessFishing();

        _cancelButton = new Button(buttonRow)
        {
            Text = Strings.Fishing.Cancel,
            Width = 120,
        };

        _cancelButton.Clicked += (_, _) => PacketSender.SendCancelFishing();

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }

    public void Update(Player player)
    {
        if (player == null || player.FishingStage == FishingStage.None)
        {
            if (!IsHidden)
            {
                Hide();
            }

            return;
        }

        if (IsHidden)
        {
            Show();
        }

        if (_lastStage != player.FishingStage)
        {
            PlayStageFeedback(player.FishingStage);
        }

        _lastStage = player.FishingStage;

        _stageLabel.Text = player.FishingStage switch
        {
            FishingStage.Hooked => Strings.Fishing.StageHooked,
            FishingStage.Resolving => player.FishingCanceled ? Strings.Fishing.StageCanceled : Strings.Fishing.StageResolving,
            FishingStage.WaitingForBite => Strings.Fishing.StageWaiting,
            _ => Strings.Fishing.StageWaiting
        };

        var now = Timing.Global.Milliseconds;
        var stageRemaining = Math.Max(0, player.FishingStageTimer - now);
        var resolveRemaining = Math.Max(0, player.FishingResolveTimer - now);

        _stageTimerLabel.Text = Strings.Fishing.StageTimer.ToString(
            TimeSpan.FromMilliseconds(stageRemaining).TotalSeconds.ToString("0.0s")
        );

        _resolveTimerLabel.Text = Strings.Fishing.ResolveTimer.ToString(
            TimeSpan.FromMilliseconds(resolveRemaining).TotalSeconds.ToString("0.0s")
        );

        _hookButton.IsDisabled = player.FishingStage == FishingStage.WaitingForBite || player.IsFishingCancellationRequested;
        _cancelButton.IsDisabled = player.IsFishingCancellationRequested;
    }

    private void PlayStageFeedback(FishingStage stage)
    {
        switch (stage)
        {
            case FishingStage.Hooked:
                Audio.AddGameSound(BiteSound, false);
                break;
            case FishingStage.Resolving:
                Audio.AddGameSound(ResolveSound, false);
                break;
        }
    }
}
