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
    private const long CRITICAL_FLAG_TIMEOUT = 1000;
    private const float CRITICAL_DIGIT_SCALE = 0.75f;
    private const int DIGIT_SPACING = 0;
    private const int DIGIT_WIDTH = 9;
    private const int DIGIT_HEIGHT = 12;
    private static bool _nextDamageIsCritical;
    private static long _criticalFlagTime;

    private readonly List<IGameTexture> _digitTextures = [];
    private bool _isCritical;
    private float _criticalRotation;
    private IGameTexture? _criticalTexture;
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
        RefreshCriticalFlag();

        Map = map;
        X = x;
        Y = y;
        Text = text;
        Color = color;
        XOffset = Globals.Random.Next(-30, 30); //+- 16 pixels so action msg's don't overlap!
        TransmissionTimer = Timing.Global.MillisecondsUtc + 1000;

        var hasNumericText = false;
        foreach (var character in Text)
        {
            if (!char.IsDigit(character))
            {
                continue;
            }

            hasNumericText = true;
            break;
        }
        var hasCriticalText = Text.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase) ||
                              Text.Contains("CRIT", StringComparison.OrdinalIgnoreCase);

        if (!hasNumericText && hasCriticalText)
        {
            _nextDamageIsCritical = true;
            _criticalFlagTime = Timing.Global.MillisecondsUtc;
            return;
        }

        if (hasNumericText)
        {
            _isCritical = hasCriticalText || _nextDamageIsCritical;
            _criticalRotation = Globals.Random.Next(-10, 11);
            _nextDamageIsCritical = false;
            _criticalFlagTime = 0;
        }
    }

    public void Draw(int mapX, int mapY, int tileWidth, int tileHeight)
    {
        RefreshCriticalFlag();

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

            if (_isCritical)
            {
                _criticalTexture = Globals.ContentManager.GetTexture(TextureType.Misc, "critical.png");
             }
            if (_digitTextures.Count != numericText.Length)
            {
                _digitTextures.Clear();
            }

            _texturesLoaded = true;
        }

        if (_digitTextures.Count > 0)
        {
            var digitScale = _isCritical ? CRITICAL_DIGIT_SCALE : 1f;
            var digitWidth = DIGIT_WIDTH * digitScale;
            var digitHeight = DIGIT_HEIGHT * digitScale;
            var totalWidth = _digitTextures.Count * digitWidth + (_digitTextures.Count - 1) * DIGIT_SPACING;
            var startX = x - totalWidth / 2f;

            if (_isCritical && _criticalTexture != null)
            {
                var criticalX = x - _criticalTexture.Width / 2f;
                var criticalY = y - _criticalTexture.Height / 2f + digitHeight / 2f;
                Graphics.Renderer.DrawTexture(
                    _criticalTexture,
                    0,
                    0,
                    _criticalTexture.Width,
                    _criticalTexture.Height,
                    criticalX,
                    criticalY,
                    _criticalTexture.Width,
                    _criticalTexture.Height,
                    Color.White,
                    rotationDegrees: _criticalRotation
                );
            }

            for (var i = 0; i < _digitTextures.Count; i++)
            {
                var texture = _digitTextures[i];
                var drawX = startX + i * (digitWidth + DIGIT_SPACING);

                Graphics.Renderer.DrawTexture(
                    texture,
                    0,
                    0,
                    texture.Width,
                    texture.Height,
                    drawX,
                    y,
                    digitWidth,
                    digitHeight,
                    Color
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
        RefreshCriticalFlag();

        if (TransmissionTimer <= Timing.Global.MillisecondsUtc)
        {
            _digitTextures.Clear();
            _criticalTexture = null;
            _criticalRotation = 0;
            _isCritical = false;
            _texturesLoaded = false;
            (Map as MapInstance)?.ActionMessages.Remove(this);
        }
    }

    private static void RefreshCriticalFlag()
    {
        if (!_nextDamageIsCritical)
        {
            return;
        }

        if (Timing.Global.MillisecondsUtc - _criticalFlagTime <= CRITICAL_FLAG_TIMEOUT)
        {
            return;
        }

        _nextDamageIsCritical = false;
        _criticalFlagTime = 0;
    }

}
