using System;

namespace StatusTimers.Layout;

public readonly struct StatusNodeDisplayConfig {
    public bool ShowIcon { get; init; }
    public bool ShowStatusName { get; init; }
    public bool ShowStatusRemaining { get; init; }
    public bool ShowProgress { get; init; }
    public bool ShowStatusRemainingBackground { get; init; }
    public TextStyle StatusRemainingTextStyle { get; init; }
    public bool ShowActorLetter { get; init; }
    public bool ShowActorName { get; init; }
    public bool AllowDismissStatus { get; init; }
    public bool AllowTargetActor { get; init; }
    public bool AnimationsEnabled { get; init; }
    public bool IsLocked { get; init; }

    public StatusNodeDisplayConfig(
        bool showIcon,
        bool showStatusName,
        bool showStatusRemaining,
        bool showProgress,
        bool showStatusRemainingBackground,
        TextStyle statusRemainingTextStyle,
        bool showActorLetter,
        bool showActorName,
        bool allowDismissStatus,
        bool allowTargetActor,
        bool animationsEnabled,
        bool isLocked) {
        ShowIcon = showIcon;
        ShowStatusName = showStatusName;
        ShowStatusRemaining = showStatusRemaining;
        ShowProgress = showProgress;
        ShowStatusRemainingBackground = showStatusRemainingBackground;
        StatusRemainingTextStyle = statusRemainingTextStyle;
        ShowActorLetter = showActorLetter;
        ShowActorName = showActorName;
        AllowDismissStatus = allowDismissStatus;
        AllowTargetActor = allowTargetActor;
        AnimationsEnabled = animationsEnabled;
        IsLocked = isLocked;
    }

    public override bool Equals(object? obj) {
        return obj is StatusNodeDisplayConfig other &&
               ShowIcon == other.ShowIcon &&
               ShowStatusName == other.ShowStatusName &&
               ShowStatusRemaining == other.ShowStatusRemaining &&
               ShowProgress == other.ShowProgress &&
               ShowStatusRemainingBackground == other.ShowStatusRemainingBackground &&
               Equals(StatusRemainingTextStyle, other.StatusRemainingTextStyle) &&
               ShowActorLetter == other.ShowActorLetter &&
               ShowActorName == other.ShowActorName &&
               AllowDismissStatus == other.AllowDismissStatus &&
               AllowTargetActor == other.AllowTargetActor &&
               AnimationsEnabled == other.AnimationsEnabled &&
               IsLocked == other.IsLocked;
    }

    public override int GetHashCode() {
        HashCode hash = new();
        hash.Add(ShowIcon);
        hash.Add(ShowStatusName);
        hash.Add(ShowStatusRemaining);
        hash.Add(ShowProgress);
        hash.Add(ShowStatusRemainingBackground);
        hash.Add(StatusRemainingTextStyle);
        hash.Add(ShowActorLetter);
        hash.Add(ShowActorName);
        hash.Add(AllowDismissStatus);
        hash.Add(AllowTargetActor);
        hash.Add(AnimationsEnabled);
        hash.Add(IsLocked);
        return hash.ToHashCode();
    }
}
