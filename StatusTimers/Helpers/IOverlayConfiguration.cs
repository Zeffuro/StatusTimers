using StatusTimers.Windows;

namespace StatusTimers.Helpers;

public interface IOverlayConfiguration {
    bool AllowDismissStatus { get; set; }
    bool AllowTargetActor { get; set; }
    bool FillRowsFirst { get; set; }
    GrowDirection GrowDirection { get; set; }
    bool IsLocked { get; set; }
    bool IsVisible { get; set; }
    int ItemsPerLine { get; set; }
    bool ShowActorLetter { get; set; }
    bool ShowActorName { get; set; }
    bool ShowPermaIcons { get; set; }
    bool ShowProgress { get; set; }
    bool ShowStatusName { get; set; }
    bool ShowStatusRemaining { get; set; }
    bool ShowStatusRemainingBackground { get; set; }
    bool StatusAsItemName { get; set; }
}
