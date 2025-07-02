using Dalamud.Interface;
using KamiToolKit.Classes;
using System.Collections.Generic;
using System.Numerics;

namespace StatusTimers.Enums;

public static class CommonUIColor
{
    public static readonly Dictionary<string, Vector4> CommonColors = new()
    {
        { "Black", ColorHelper.GetColor(51) },
        { "Light Grey", ColorHelper.GetColor(2) },
        { "Dark Grey", ColorHelper.GetColor(55) },
        { "White", ColorHelper.GetColor(50) },
        { "Quest Title Yellow", ColorHelper.GetColor(54) },
        { "Quest Description Blue", ColorHelper.GetColor(53) },
        { "Yellow Configuration Option", ColorHelper.GetColor(8) },
    };
}
