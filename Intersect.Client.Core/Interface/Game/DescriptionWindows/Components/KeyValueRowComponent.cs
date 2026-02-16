using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Game.DescriptionWindows.Components;

public partial class KeyValueRowComponent : ComponentBase
{
    private readonly ImagePanel _icon;
    private readonly Label _keyLabel;
    private readonly Label _valueLabel;

    public KeyValueRowComponent(Base parent) : this(parent, string.Empty, string.Empty)
    {
    }

    public KeyValueRowComponent(Base parent, string key, string value) : base(parent, "KeyValueRow")
    {
        _icon = new ImagePanel(this, "Icon");
        _keyLabel = new Label(this, "Key") { Text = key };
        _valueLabel = new Label(this, "Value") { Text = value };
        SetIcon(null);
    }

    /// <summary>
    /// Set the <see cref="Color"/> of the key text.
    /// </summary>
    /// <param name="color">The <see cref="Color"/> to draw the key text in.</param>
    public void SetKeyTextColor(Color color) => _keyLabel.SetTextColor(color, ComponentState.Normal);

    /// <summary>
    /// Set the <see cref="Color"/> of the value text.
    /// </summary>
    /// <param name="color">The <see cref="Color"/> to draw the value text in.</param>
    public void SetValueTextColor(Color color) => _valueLabel.SetTextColor(color, ComponentState.Normal);

    public void SetKeyText(string key) => _keyLabel.SetText(key);

    public void SetValueText(string value) => _valueLabel.SetText(value);

    public void SetText(string key, string value)
    {
        SetKeyText(key);
        SetValueText(value);
    }

    public void SetIcon(IGameTexture? texture)
    {
        _icon.Texture = texture;
        _icon.IsHidden = texture == null;
    }
}
