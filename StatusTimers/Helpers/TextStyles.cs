using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using System.Numerics;

namespace StatusTimers.Helpers
{
    public static class TextStyles
    {
        public static class Defaults
        {
            public const int FontSize = 14;
            public static readonly Vector4 OutlineColor = ColorHelper.GetColor(0);
            public const TextFlags Flags = TextFlags.Edge;
        }

        public static class Header
        {
            public const int Height = 16;
            public static readonly Vector4 TextColor = ColorHelper.GetColor(2);
        }

        public static class OptionLabel
        {
            public const int Height = 20;
            public static readonly Vector4 TextColor = ColorHelper.GetColor(8);
        }
    }
}
