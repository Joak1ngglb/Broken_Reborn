using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Localization;
using Intersect.Client.Networking;

namespace Intersect.Client.Interface.Game;

public sealed class DeathWindow : Window
{
    private readonly Label _messageLabel;
    private readonly Button _respawnButton;

    public DeathWindow(Canvas gameCanvas) : base(gameCanvas, Strings.DeathWindow.Title, false, nameof(DeathWindow))
    {
        IsResizable = false;

        SetSize(360, 140);

        _messageLabel = new Label(this, nameof(_messageLabel))
        {
            Text = Strings.DeathWindow.Message,
            X = 20,
            Y = 46,
            AutoSizeToContents = true,
        };

        _respawnButton = new Button(this, nameof(_respawnButton))
        {
            Text = Strings.DeathWindow.Respawn,
            Width = 120,
            Height = 28,
            X = Width / 2 - 60,
            Y = 86,
        };
        _respawnButton.Clicked += RespawnButtonClicked;

        Hide();
    }

    protected override void EnsureInitialized()
    {
        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());

        SetPosition(
            Graphics.Renderer.ScreenWidth / 2 - Width / 2,
            Graphics.Renderer.ScreenHeight / 2 - Height / 2
        );
    }

    private void RespawnButtonClicked(Base sender, MouseButtonState arguments)
    {
        PacketSender.SendRespawn();
        Hide();
    }
}
