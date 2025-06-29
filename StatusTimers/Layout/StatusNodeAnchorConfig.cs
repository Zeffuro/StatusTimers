using StatusTimers.Enums;

namespace StatusTimers.Layout;

public class StatusNodeAnchorConfig
{
    public AnchorTarget AnchorTo { get; set; }
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public AnchorAlignment Alignment { get; set; } = AnchorAlignment.Left;
    public float Width { get; set; }
    public float Height { get; set; }
}
