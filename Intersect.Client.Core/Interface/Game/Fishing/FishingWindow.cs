using System;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.Layout;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core;

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

        var layout = new DockBase(this)
        {
            Dock = Pos.Fill,
            Padding = new Padding(10),
        };

        var contentTable = new Table(layout)
        {
            CellSpacing = new Point(0, 6),
            Dock = Pos.Fill,
            FitRowHeightToContents = true,
            SizeToContents = true,
        };

        _stageLabel = new Label(contentTable)
        {
            Alignment = [Alignments.Center],
            Text = Strings.Fishing.StageWaiting,
        };

        _ = contentTable.AddRow(_stageLabel);

        _stageTimerLabel = new Label(contentTable)
        {
            Text = Strings.Fishing.StageTimer.ToString("0.0s"),
        };

        _ = contentTable.AddRow(_stageTimerLabel);

        _resolveTimerLabel = new Label(contentTable)
        {
            Text = Strings.Fishing.ResolveTimer.ToString("0.0s"),
        };

        _ = contentTable.AddRow(_resolveTimerLabel);

        var buttonRow = contentTable.AddRow(columnCount: 2);
        buttonRow.CellSpacing = new Point(8, 0);

        _hookButton = new Button(buttonRow)
        {
            Text = Strings.Fishing.Hook,
            Width = 120,
        };

        _hookButton.Clicked += (_, _) => PacketSender.SendSuccessFishing();

        buttonRow.SetCellContents(0, _hookButton);

        _cancelButton = new Button(buttonRow)
        {
            Text = Strings.Fishing.Cancel,
            Width = 120,
        };

        buttonRow.SetCellContents(1, _cancelButton);

        _cancelButton.Clicked += (_, _) => PacketSender.SendCancelFishing();

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
    }
    protected override void EnsureInitialized()
    {
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
