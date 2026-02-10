using System.Text;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Framework.Core;
using Intersect.Utilities;

namespace Intersect.Client.Maps;


public partial class ActionMessage : IActionMessage
{
    private const int DIGIT_SPACING = 0;
    private const int DIGIT_WIDTH = 9;
    private const int DIGIT_HEIGHT = 12;

    private readonly List<IGameTexture> _digitTextures = [];
    private bool _texturesLoaded;

    public Color Color { get; init; }

    public IMapInstance Map { get; init; }

    public string Text { get; init; }

    public long TransmissionTimer { get; init; }

    public int X { get; init; }

    public int XOffset { get; init; }

    public int Y { get; init; }

    public ActionMessage(MapInstance map, int x, int y, string text, Color color)
    {
        Map = map;
        X = x;
        Y = y;
        Text = text;
        Color = color;
        XOffset = Globals.Random.Next(-30, 30); //+- 16 pixels so action msg's don't overlap!
        TransmissionTimer = Timing.Global.MillisecondsUtc + 1000;
    }

    public void Draw(int mapX, int mapY, int tileWidth, int tileHeight)
    {
        var x = mapX + X * tileWidth + XOffset;
        var y = mapY + Y * tileHeight - tileHeight * 2 * (1000 - (int)(TransmissionTimer - Timing.Global.MillisecondsUtc)) / 1000;

        if (!_texturesLoaded)
        {
            _digitTextures.Clear();
            var numberBuilder = new StringBuilder();
            foreach (var character in Text)
            {
                if (char.IsDigit(character))
                {
                    numberBuilder.Append(character);
                }
            }

            var numericText = numberBuilder.ToString();
            foreach (var character in numericText)
            {
                var texture = Globals.ContentManager.GetTexture(TextureType.Misc, $"{character}.png");
                if (texture != null)
                {
                    _digitTextures.Add(texture);
                }
            }

            _texturesLoaded = true;
        }

        if (_digitTextures.Count > 0)
        {
            var totalWidth = _digitTextures.Count * DIGIT_WIDTH + (_digitTextures.Count - 1) * DIGIT_SPACING;
            var startX = x - totalWidth / 2f;

            for (var i = 0; i < _digitTextures.Count; i++)
            {
                var texture = _digitTextures[i];
                var drawX = startX + i * (DIGIT_WIDTH + DIGIT_SPACING);

                Graphics.Renderer.DrawTexture(
                    texture,
                    0,
                    0,
                    texture.Width,
                    texture.Height,
                    drawX,
                    y,
                    DIGIT_WIDTH,
                    DIGIT_HEIGHT,
                    Color.White
                );
            }

            return;
        }

        var textWidth = Graphics.Renderer.MeasureText(Text, Graphics.ActionMsgFont, Graphics.ActionMsgFontSize, 1).X;
        Graphics.Renderer.DrawString(
            Text,
            Graphics.ActionMsgFont,
            Graphics.ActionMsgFontSize,
            x - textWidth / 2f,
            y,
            1,
            Color,
            true,
            null,
            new Color(40, 40, 40)
        );
    }

    public void TryRemove()
    {
        if (TransmissionTimer <= Timing.Global.MillisecondsUtc)
        {
            _digitTextures.Clear();
            _texturesLoaded = false;
            (Map as MapInstance)?.ActionMessages.Remove(this);
        }
    }

}
