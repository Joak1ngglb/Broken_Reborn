using Intersect.Framework.Core;

namespace Intersect.Client.Core;

public static partial class Flash
{
    private static float sCurrentIntensity;

    private static int sDurationMs;

    private static long sLastUpdate;

    private static Color sFlashColor = Color.White;

    public static float Alpha => Math.Clamp(sCurrentIntensity, 0f, 1f) * 255f;

    public static Color Color => new((int)Alpha, sFlashColor.R, sFlashColor.G, sFlashColor.B);

    public static bool IsActive => sCurrentIntensity > 0f;

    public static void Trigger(float intensity, int durationMs, Color color)
    {
        sCurrentIntensity = Math.Clamp(intensity, 0f, 1f);
        sDurationMs = Math.Max(1, durationMs);
        sFlashColor = color ?? Color.White;
        sLastUpdate = Timing.Global.MillisecondsUtc;
    }

    public static void Cancel()
    {
        sCurrentIntensity = 0f;
        sDurationMs = 0;
    }

    public static void Update()
    {
        if (sCurrentIntensity <= 0f)
        {
            return;
        }

        var elapsed = Timing.Global.MillisecondsUtc - sLastUpdate;
        sCurrentIntensity -= elapsed / (float)sDurationMs;
        if (sCurrentIntensity < 0f)
        {
            sCurrentIntensity = 0f;
        }

        sLastUpdate = Timing.Global.MillisecondsUtc;
    }
}
