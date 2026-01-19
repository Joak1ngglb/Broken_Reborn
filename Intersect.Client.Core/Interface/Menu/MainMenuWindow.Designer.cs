using System.Diagnostics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.ControlInternal;

namespace Intersect.Client.Interface.Menu;

public partial class MainMenuWindow
{
    protected override void EnsureInitialized()
    {
        var canvas = Canvas ?? throw new InvalidOperationException($"Not attached to a {nameof(Canvas)}");

        Button[] visiblePrimaryButtons = new []
        {
            _buttonStart,
            _buttonExit,
        }.Where(button => button.IsVisibleInTree).ToArray();

        Button[] visibleSecondaryButtons = new []
        {
            _buttonSettings,
            _buttonCredits,
        }.Where(button => button.IsVisibleInTree).ToArray();

        const int defaultWidth = 87;
        const int defaultHeight = 154;
        const int secondaryWidth = 64;
        const int secondaryHeight = 64;
        const int secondarySpacing = 8;

        var secondaryPanelWidth = visibleSecondaryButtons.Length > 0 ? secondaryWidth : 0;
        var secondaryPanelHeight = visibleSecondaryButtons.Length > 0
            ? secondaryHeight * visibleSecondaryButtons.Length + secondarySpacing * (visibleSecondaryButtons.Length - 1)
            : 0;
        var secondaryPanelMargin = visibleSecondaryButtons.Length > 0 ? secondarySpacing : 0;
        var contentHeight = defaultHeight > secondaryPanelHeight ? defaultHeight : secondaryPanelHeight;

        Size = new Point(
            defaultWidth * visiblePrimaryButtons.Length + secondaryPanelWidth + secondaryPanelMargin + InnerPanelPadding.Left + InnerPanelPadding.Right,
            contentHeight + TitleBarBounds.Bottom + InnerPanelPadding.Top + InnerPanelPadding.Bottom
        );

        Titlebar.MouseInputEnabled = false;
        TitleLabel.FontSize = 14;
        TitleLabel.TextColorOverride = Color.White;

        _secondaryButtonsPanel.Size = new Point(secondaryPanelWidth, secondaryPanelHeight);
        _secondaryButtonsPanel.Margin = new Margin(secondaryPanelMargin, 0, 0, 0);

        foreach (var button in visiblePrimaryButtons)
        {
            button.Size = new Point(defaultWidth, defaultHeight);
            button.FontName = "sourcesansproblack";
            button.FontSize = 12;
            button.Padding = new Padding(0, 24, 0, 0);
            button.Dock = Pos.Left;

            var buttonName = button.Name;
            button.SetStateTexture(ComponentState.Normal, $"mainmenu{buttonName}.png");
            button.SetStateTexture(ComponentState.Active, $"mainmenu{buttonName}_clicked.png");
            button.SetStateTexture(ComponentState.Disabled, $"mainmenu{buttonName}_disabled.png");
            button.SetStateTexture(ComponentState.Hovered, $"mainmenu{buttonName}_hovered.png");
        }

        foreach (var button in visibleSecondaryButtons)
        {
            button.Size = new Point(secondaryWidth, secondaryHeight);
            button.FontName = "sourcesansproblack";
            button.FontSize = 10;
            button.Padding = new Padding(0, 12, 0, 0);

            var buttonName = button.Name;
            button.SetStateTexture(ComponentState.Normal, $"mainmenu{buttonName}.png");
            button.SetStateTexture(ComponentState.Active, $"mainmenu{buttonName}_clicked.png");
            button.SetStateTexture(ComponentState.Disabled, $"mainmenu{buttonName}_disabled.png");
            button.SetStateTexture(ComponentState.Hovered, $"mainmenu{buttonName}_hovered.png");
        }

        LoadJsonUi(GameContentManager.UI.Menu, Graphics.Renderer.GetResolutionString());
    }
}
