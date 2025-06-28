using StatusTimers.Enums;

namespace StatusTimers.Layout;

public class StatusNodeLayoutConfig
{
    // Overall row/container size
    public float RowWidth { get; set; } = 270f;
    public float RowHeight { get; set; } = 48f;

    // Individual element anchor configs
    public StatusNodeAnchorConfig IconAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.ContainerLeft,
        OffsetX = 0,
        OffsetY = 0,
        Alignment = AnchorAlignment.Left,
        Width = 48,
        Height = 48
    };

    public StatusNodeAnchorConfig NameAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.IconRight,
        OffsetX = 8,
        OffsetY = 0,
        Alignment = AnchorAlignment.Left,
        Width = 120,
        Height = 22
    };

    public StatusNodeAnchorConfig TimerAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.ContainerRight,
        OffsetX = -4,
        OffsetY = 0,
        Alignment = AnchorAlignment.Right,
        Width = 44,
        Height = 22
    };

    public StatusNodeAnchorConfig ProgressAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.NameBottom,
        OffsetX = 0,
        OffsetY = 2,
        Alignment = AnchorAlignment.StretchHorizontal,
        Height = 8
        // Width will be set dynamically between NameAnchor and TimerAnchor
    };

    public StatusNodeAnchorConfig ActorNameAnchor { get; set; } = new()
    {
        AnchorTo = AnchorTarget.NameBottom,
        OffsetX = 0,
        OffsetY = 1,
        Alignment = AnchorAlignment.Left,
        Width = 120,
        Height = 12
    };
}
