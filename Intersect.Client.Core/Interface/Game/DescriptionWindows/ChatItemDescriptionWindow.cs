using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Client.Interface.Game.DescriptionWindows;

public partial class ChatItemDescriptionWindow() : ItemDescriptionWindow()
{
    private readonly Button _closeButton;

    public ChatItemDescriptionWindow()
    {
        _closeButton = new Button(this, "ChatDescriptionCloseButton");
        _closeButton.SetText("X");
        _closeButton.SizeToContents();
        _closeButton.Clicked += CloseButtonOnClicked;
        _closeButton.IsTabable = false;
    }

    public new void Show(
        ItemDescriptor item,
        int amount,
        ItemProperties? itemProperties = default,
        string valueLabel = ""
    )
    {
        base.Show(item, amount, itemProperties, valueLabel);
        PositionCloseButton();
        _closeButton.Show();
    }

    public override void Hide()
    {
        if (Interface.GameUi.ChatItemDescriptionWindow == this)
        {
            Interface.GameUi.GameCanvas.RemoveChild(this, true);
            Interface.GameUi.ChatItemDescriptionWindow = default;
        }

        if (Interface.GameUi.SpellDescriptionWindow != default)
        {
            Interface.GameUi.GameCanvas.RemoveChild(Interface.GameUi.SpellDescriptionWindow, true);
            Interface.GameUi.SpellDescriptionWindow = default;
        }
    }

    private void PositionCloseButton()
    {
        if (_closeButton == null)
        {
            return;
        }

        const int padding = 4;
        var posX = Width - _closeButton.Width - padding;
        var posY = padding;
        _closeButton.SetPosition(posX, posY);
    }

    private void CloseButtonOnClicked(Base sender, MouseButtonState arguments)
    {
        Hide();
    }
}
