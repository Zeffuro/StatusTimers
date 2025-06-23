using StatusTimers.Windows;

namespace StatusTimers.Helpers;

public interface IOverlayConfiguration {
    bool AllowDismissStatus { get; set; }
    bool AllowTargetActor { get; set; }
    bool FillRowsFirst { get; set; }
    GrowDirection GrowDirection { get; set; }
    bool IsLocked { get; set; }
    bool IsPreviewEnabled { get; set; }
    bool IsVisible { get; set; }
    int ItemsPerLine { get; set; }
    int MaxStatuses { get; set; }
    int ScaleInt { get; set; }
    bool ShowActorLetter { get; set; }
    bool ShowActorName { get; set; }
    bool ShowPermaIcons { get; set; }
    bool ShowProgress { get; set; }
    bool ShowStatusName { get; set; }
    bool ShowStatusRemaining { get; set; }
    bool ShowStatusRemainingBackground { get; set; }
    bool StatusAsItemName { get; set; }
    int StatusHorizontalPadding { get; set; }
    int StatusVerticalPadding { get; set; }
    SortCriterion PrimarySort { get; set; }
    SortCriterion SecondarySort { get; set; }
    SortCriterion TertiarySort { get; set; }

    SortOrder PrimarySortOrder { get; set; }
    SortOrder SecondarySortOrder { get; set; }
    SortOrder TertiarySortOrder { get; set; }
}
