using System;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

public class DeathWindow : Window
{
    private readonly Panel _contentPanel;
    private readonly Panel _buttonPanel;
    private readonly Label _messageLabel;
    private readonly Button _respawnButton;
    private bool _respawnRequested;
    private long _respawnAvailableAt;
    private int _lastCountdownSeconds = -1;

    public DeathWindow(Canvas canvas) : base(canvas, Strings.DeathWindow.Title, false, nameof(DeathWindow))
    {
        DisableResizing();

        Alignment = [Alignments.Center];
        MinimumSize = new Point(320, 160);
        Size = MinimumSize;
        IsResizable = false;
        IsClosable = false;
        Padding = new Padding(12);
        InnerPanelPadding = new Padding(8);

        TitleLabel.FontSize = 14;
        TitleLabel.TextColorOverride = Color.White;

        _contentPanel = new Panel(this, nameof(_contentPanel))
        {
            BackgroundColor = Color.Transparent,
            Dock = Pos.Fill,
        };

        _messageLabel = new Label(_contentPanel, nameof(_messageLabel))
        {
            AutoSizeToContents = false,
            Dock = Pos.Fill,
            Text = Strings.DeathWindow.Message,
            TextAlign = Pos.Center,
        };

        _buttonPanel = new Panel(this, nameof(_buttonPanel))
        {
            BackgroundColor = Color.Transparent,
            Dock = Pos.Bottom,
            Height = 44,
        };

        _respawnButton = new Button(_buttonPanel, nameof(_respawnButton))
        {
            Alignment = [Alignments.Center],
            AutoSizeToContents = true,
            MinimumSize = new Point(140, 28),
            Padding = new Padding(8, 4),
            Text = Strings.DeathWindow.Respawn,
        };
        _respawnButton.Clicked += RespawnButtonOnClicked;

        Interface.InputBlockingComponents.Add(this);

        IsHidden = true;
    }

    protected override void EnsureInitialized()
    {
    }

    protected override void Dispose(bool disposing)
    {
        _ = Interface.InputBlockingComponents.Remove(this);
        base.Dispose(disposing);
    }

    protected override void OnVisibilityChanged(object? sender, VisibilityChangedEventArgs eventArgs)
    {
        base.OnVisibilityChanged(sender, eventArgs);

        if (eventArgs.IsVisibleInTree)
        {
            _respawnRequested = false;
            _lastCountdownSeconds = -1;
            var respawnTime = Options.Instance.Player.DeathSeconds > 0
                ? (long)Options.Instance.Player.DeathSeconds * 1000
                : 0;
            _respawnAvailableAt = respawnTime > 0
                ? Timing.Global.Milliseconds + respawnTime
                : 0;
            _respawnButton.IsDisabled = _respawnAvailableAt > Timing.Global.Milliseconds;
            UpdateCountdownLabel();
            MakeModal(dim: true);
            BringToFront();
        }
        else
        {
            RemoveModal();
        }
    }

    public void Update()
    {
        if (IsHidden || _respawnRequested)
        {
            return;
        }

        if (_respawnAvailableAt <= Timing.Global.Milliseconds)
        {
            _respawnButton.IsDisabled = false;
        }

        UpdateCountdownLabel();
    }

    private void RespawnButtonOnClicked(Base sender, MouseButtonState arguments)
    {
        if (_respawnRequested)
        {
            return;
        }

        _respawnRequested = true;
        _respawnButton.IsDisabled = true;
        PacketSender.SendRespawn();
    }

    private void UpdateCountdownLabel()
    {
        if (_respawnAvailableAt <= 0)
        {
            _messageLabel.Text = Strings.DeathWindow.Message;
            _lastCountdownSeconds = -1;
            return;
        }

        var remainingMs = Math.Max(_respawnAvailableAt - Timing.Global.Milliseconds, 0);
        var remainingSeconds = (int)Math.Ceiling(remainingMs / 1000d);
        if (remainingSeconds != _lastCountdownSeconds)
        {
            _lastCountdownSeconds = remainingSeconds;
            _messageLabel.Text = Strings.DeathWindow.MessageWithCountdown.ToString(remainingSeconds);
        }
    }
}
