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

    /*
      Plan en pseudocódigo (detallado):
      - Este método se llamará cada frame para mantener la ventana de muerte consistente con la resolución y la UI.
      - Recalcular la posición centrada de la ventana si la resolución de pantalla cambia.
      - Centrar el botón de respawn horizontalmente dentro de la ventana (en caso de que el ancho cambie).
      - Mantener la etiqueta de mensaje en su posición relativa y asegurar que su AutoSizeToContents esté habilitado.
      - No realizar lógica de juego (p. ej. comprobaciones de estado del jugador) aquí para evitar dependencias desconocidas;
        la ventana seguirá siendo mostrada/ocultada por el resto del sistema. Este método debe ser resiliente a excepciones
        para no romper el bucle de UI.
    */

    public void Update()
    {
        try
        {
            // Mantener la ventana centrada en pantalla por si cambia la resolución en tiempo de ejecución.
            var targetX = Graphics.Renderer.ScreenWidth / 2 - Width / 2;
            var targetY = Graphics.Renderer.ScreenHeight / 2 - Height / 2;
            SetPosition(targetX, targetY);

            // Asegurar que el botón de respawn esté centrado horizontalmente dentro de la ventana.
            if (_respawnButton != null)
            {
                _respawnButton.X = Width / 2 - _respawnButton.Width / 2;
            }

            // Mantener la posición y comportamiento de la etiqueta de mensaje.
            if (_messageLabel != null)
            {
                _messageLabel.X = 20;
                _messageLabel.Y = 46;
                _messageLabel.AutoSizeToContents = true;
            }
        }
        catch
        {
            // Silenciar excepciones para evitar romper el bucle de UI.
        }
    }

    private void RespawnButtonClicked(Base sender, MouseButtonState arguments)
    {
        PacketSender.SendRespawn();
        Hide();
    }
}
