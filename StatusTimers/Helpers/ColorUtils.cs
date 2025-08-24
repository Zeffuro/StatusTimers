using System;
using System.Globalization;
using System.Numerics;

namespace StatusTimers.Helpers;

public abstract class ColorUtils {
    public static Vector4 HSVToColor(float h, float s, float v, float a)
    {
        // h, s, v: 0-1
        int i = (int)(h * 6f);
        float f = h * 6f - i;
        float p = v * (1f - s);
        float q = v * (1f - f * s);
        float t = v * (1f - (1f - f) * s);

        float r = 0, g = 0, b = 0;
        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }
        return new Vector4(r, g, b, a);
    }

    public static void ColorToHSV(Vector4 color, out float h, out float s, out float v)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        v = max;

        float delta = max - min;
        if (max == 0f)
        {
            s = 0f;
            h = 0f;
            return;
        }
        s = delta / max;
        if (delta == 0)
        {
            h = 0;
        }
        else if (max == r)
        {
            h = (g - b) / delta % 6f / 6f;
        }
        else if (max == g)
        {
            h = (b - r) / delta / 6f + (1f / 3f);
        }
        else
        {
            h = (r - g) / delta / 6f + (2f / 3f);
        }

        if (h < 0)
        {
            h += 1f;
        }
    }

    public static string ColorToHex(Vector4 color)
    {
        int r = (int)(color.X * 255f);
        int g = (int)(color.Y * 255f);
        int b = (int)(color.Z * 255f);
        int a = (int)(color.W * 255f);
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    public static bool TryParseHex(string hex, out Vector4 color)
    {
        color = Vector4.One;
        if (hex.StartsWith("#")) {
            hex = hex.Substring(1);
        }

        if (hex.Length == 6) {
            // #RRGGBB
            if (int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out int r) &&
                int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out int g) &&
                int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out int b))
            {
                color = new Vector4(r / 255f, g / 255f, b / 255f, 1f);
                return true;
            }
        } else if (hex.Length == 8) {
            // #RRGGBBAA
            if (int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out int r) &&
                int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out int g) &&
                int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out int b) &&
                int.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, null, out int a))
            {
                color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
                return true;
            }
        }

        return false;
    }

    public static float ClampMin(float v) => Math.Max(0, Math.Min(1, v));
    public static int ClampMax(int v) => Math.Max(0, Math.Min(255, v));
}
