using System.Collections.Generic;

namespace StatusTimers.Enums;

public static class AnchorTargetMap
{
    public static readonly Dictionary<AnchorTarget, (string nodeKey, string edge)> Map = new()
    {
        { AnchorTarget.IconLeft,      ("Icon", "Left") },
        { AnchorTarget.IconRight,     ("Icon", "Right") },
        { AnchorTarget.IconTop,       ("Icon", "Top") },
        { AnchorTarget.IconBottom,    ("Icon", "Bottom") },
        { AnchorTarget.NameLeft,      ("Name", "Left") },
        { AnchorTarget.NameRight,     ("Name", "Right") },
        { AnchorTarget.NameTop,       ("Name", "Top") },
        { AnchorTarget.NameBottom,    ("Name", "Bottom") },
        { AnchorTarget.TimerLeft,     ("Timer", "Left") },
        { AnchorTarget.TimerRight,    ("Timer", "Right") },
        { AnchorTarget.TimerTop,      ("Timer", "Top") },
        { AnchorTarget.TimerBottom,   ("Timer", "Bottom") },
        { AnchorTarget.ProgressLeft,  ("Progress", "Left") },
        { AnchorTarget.ProgressRight, ("Progress", "Right") },
        { AnchorTarget.ProgressTop,   ("Progress", "Top") },
        { AnchorTarget.ProgressBottom,("Progress", "Bottom") },
        { AnchorTarget.ActorNameLeft,   ("ActorName", "Left") },
        { AnchorTarget.ActorNameRight,  ("ActorName", "Right") },
        { AnchorTarget.ActorNameTop,    ("ActorName", "Top") },
        { AnchorTarget.ActorNameBottom, ("ActorName", "Bottom") },
        { AnchorTarget.BackgroundLeft,   ("Background", "Left") },
        { AnchorTarget.BackgroundRight,  ("Background", "Right") },
        { AnchorTarget.BackgroundTop,    ("Background", "Top") },
        { AnchorTarget.BackgroundBottom, ("Background", "Bottom") }
    };
}
