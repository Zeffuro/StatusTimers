using StatusTimers.Enums;

namespace StatusTimers.Layout;

public class StatusNodeLayoutConfig
{
    public float RowWidth { get; set; } = 270f;
    public float RowHeight { get; set; } = 48f;

    public StatusNodeAnchorConfig IconAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.ContainerLeft,
        OffsetX = 0,
        OffsetY = 0,
        Alignment = AnchorAlignment.Left,
        Width = 48,
        Height = 64
    };

    public StatusNodeAnchorConfig NameAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.IconRight,
        OffsetX = 8,
        OffsetY = 8,
        Alignment = AnchorAlignment.Left,
        Width = 180,
        Height = 28
    };

    public StatusNodeAnchorConfig TimerAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.NameRight,
        OffsetX = 30,
        OffsetY = 0,
        Alignment = AnchorAlignment.VerticalCenter | AnchorAlignment.Right,
        Width = 44,
        Height = 22
    };

    public StatusNodeAnchorConfig ProgressAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.ActorNameBottom,
        OffsetX = -7,
        OffsetY = 20,
        Alignment = AnchorAlignment.Bottom,
        Height = 20,
        Width = 200
    };

    public StatusNodeAnchorConfig ActorNameAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.NameBottom,
        OffsetX = 0,
        OffsetY = -6,
        Alignment = AnchorAlignment.Left,
        Width = 120,
        Height = 12
    };
}
