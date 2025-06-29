using System;

namespace StatusTimers.Enums;

[Flags]
public enum AnchorAlignment
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Top = 1 << 2,
    Bottom = 1 << 3,
    Center = 1 << 4,
    HorizontalCenter = 1 << 5,
    VerticalCenter = 1 << 6
}
