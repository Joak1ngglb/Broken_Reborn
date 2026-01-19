using System.Security.Cryptography;
using System.Text;
using Intersect.Client.Core;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Interface.Menu;

public partial class MainMenuWindow : Window
{
    private readonly Button _buttonCredits;
    private readonly Button _buttonExit;
    private readonly Button _buttonSettings;
    private readonly Button _buttonStart;
    private readonly MainMenu _mainMenu;

    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public MainMenuWindow(Canvas canvas, MainMenu mainMenu) : base(canvas, Strings.MainMenu.Title, false, $"{nameof(MainMenuWindow)}_{(ClientContext.IsSinglePlayer ? "singleplayer" : "online")}")
    {
        _mainMenu = mainMenu;

        Alignment = [Alignments.Center];

        IsClosable = false;
        IsResizable = false;
        Padding = Padding.Zero;
        InnerPanelPadding = new Padding(8);
        Titlebar.MouseInputEnabled = false;

        _buttonStart = new Button(this, nameof(_buttonStart))
        {
            IsTabable = true,
            IsVisibleInTree = ClientContext.IsSinglePlayer,
            Text = Strings.MainMenu.Start,
        };
        _buttonStart.Clicked += _buttonStart_Clicked;

        _buttonSettings = new Button(this, nameof(_buttonSettings))
        {
            IsTabable = true,
            Text = Strings.MainMenu.Settings,
        };
        _buttonSettings.Clicked += _buttonSettings_Clicked;

        if (!string.IsNullOrEmpty(Strings.MainMenu.SettingsTooltip))
        {
            _buttonSettings.SetToolTipText(Strings.MainMenu.SettingsTooltip);
        }

        _buttonCredits = new Button(this, nameof(_buttonCredits))
        {
            IsTabable = true,
            Text = Strings.MainMenu.Credits,
        };
        _buttonCredits.Clicked += _buttonCredits_Clicked;

        _buttonExit = new Button(this, nameof(_buttonExit))
        {
            IsTabable = true,
            Text = Strings.MainMenu.Exit,
        };
        _buttonExit.Clicked += _buttonExit_Clicked;
    }

    private void _buttonCredits_Clicked(Base sender, MouseButtonState arguments) => _mainMenu.SwitchToWindow<CreditsWindow>();

    private static void _buttonExit_Clicked(Base sender, MouseButtonState arguments)
    {
        ApplicationContext.Context.Value?.Logger.LogInformation("User clicked exit button.");
        Globals.IsRunning = false;
    }

    private void _buttonSettings_Clicked(Base sender, MouseButtonState arguments) => _mainMenu.SettingsButton_Clicked();

    private void _buttonStart_Clicked(Base sender, MouseButtonState arguments)
    {
        Hide();
        Networking.Network.TryConnect();
        const string singleplayer = "singleplayer";
        PacketSender.SendLogin(singleplayer, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(singleplayer))));
    }

    internal void Reset() => _buttonSettings.Show();

    internal void Update()
    {
    }


    internal void UpdateDisabled()
    {
    }
}
