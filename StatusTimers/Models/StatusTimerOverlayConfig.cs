using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using Newtonsoft.Json;
using StatusTimers.Enums;
using System;
using System.Collections.Generic;

namespace StatusTimers.Models;

/// <summary>
/// Stores overlay configuration and notifies listeners on change.
/// </summary>
public class StatusTimerOverlayConfig
{
    public event Action<string, bool, bool>? OnPropertyChanged;

    private void Notify(string property, bool updateNodes = false, bool needsRebuild = false)
        => OnPropertyChanged?.Invoke(property, updateNodes, needsRebuild);

    [JsonProperty]
    public bool FilterStatuses
    {
        get => field;
        set
        {
            if (FilterStatuses != value)
            {
                field = value;
                Notify(nameof(FilterStatuses));
            }
        }
    } = true;

    [JsonProperty]
    public bool ShowIcon
    {
        get => field;
        set
        {
            if (ShowIcon != value)
            {
                field = value;
                Notify(nameof(ShowIcon), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool AllowDismissStatus
    {
        get => field;
        set
        {
            if (AllowDismissStatus != value)
            {
                field = value;
                Notify(nameof(AllowDismissStatus), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool AllowTargetActor
    {
        get => field;
        set
        {
            if (AllowTargetActor != value)
            {
                field = value;
                Notify(nameof(AllowTargetActor), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool AnimationsEnabled
    {
        get => field;
        set
        {
            if (AnimationsEnabled != value)
            {
                field = value;
                Notify(nameof(AnimationsEnabled), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool FillRowsFirst
    {
        get => field;
        set
        {
            if (FillRowsFirst != value)
            {
                field = value;
                Notify(nameof(FillRowsFirst), needsRebuild: true);
            }
        }
    } = false;

    [JsonProperty]
    public bool FilterEnabled
    {
        get => field;
        set
        {
            if (FilterEnabled != value)
            {
                field = value;
                Notify(nameof(FilterEnabled), updateNodes: true);
            }
        }
    } = false;

    [JsonProperty]
    public bool FilterIsBlacklist
    {
        get => field;
        set
        {
            if (FilterIsBlacklist != value)
            {
                field = value;
                Notify(nameof(FilterIsBlacklist), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public HashSet<uint> FilterList
    {
        get => field;
        set
        {
            if (!EqualityComparer<HashSet<uint>>.Default.Equals(FilterList, value))
            {
                field = value;
                Notify(nameof(FilterList), updateNodes: true);
            }
        }
    } = new();

    [JsonProperty]
    public GrowDirection GrowDirection
    {
        get => field;
        set
        {
            if (GrowDirection != value)
            {
                field = value;
                Notify(nameof(GrowDirection), needsRebuild: true);
            }
        }
    } = GrowDirection.DownRight;

    [JsonProperty]
    public int ItemsPerLine
    {
        get => field;
        set
        {
            if (ItemsPerLine != value)
            {
                field = value;
                Notify(nameof(ItemsPerLine), needsRebuild: true);
            }
        }
    } = 16;

    [JsonProperty]
    public int MaxStatuses
    {
        get => field;
        set
        {
            if (MaxStatuses != value)
            {
                field = value;
                Notify(nameof(MaxStatuses), needsRebuild: true);
            }
        }
    } = 30;

    [JsonProperty]
    public int ScaleInt
    {
        get => field;
        set
        {
            if (ScaleInt != value)
            {
                field = value;
                Notify(nameof(ScaleInt), needsRebuild: true);
            }
        }
    } = 100;

    [JsonProperty]
    public bool ShowStatusName
    {
        get => field;
        set
        {
            if (ShowStatusName != value)
            {
                field = value;
                Notify(nameof(ShowStatusName), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool ShowStatusRemaining
    {
        get => field;
        set
        {
            if (ShowStatusRemaining != value)
            {
                field = value;
                Notify(nameof(ShowStatusRemaining), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool ShowStatusRemainingBackground
    {
        get => field;
        set
        {
            if (ShowStatusRemainingBackground != value)
            {
                field = value;
                Notify(nameof(ShowStatusRemainingBackground), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool ShowProgress
    {
        get => field;
        set
        {
            if (ShowProgress != value)
            {
                field = value;
                Notify(nameof(ShowProgress), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public int StatusHorizontalPadding
    {
        get => field;
        set
        {
            if (StatusHorizontalPadding != value)
            {
                field = value;
                Notify(nameof(StatusHorizontalPadding), needsRebuild: true);
            }
        }
    } = 4;

    [JsonProperty]
    public int StatusVerticalPadding
    {
        get => field;
        set
        {
            if (StatusVerticalPadding != value)
            {
                field = value;
                Notify(nameof(StatusVerticalPadding), needsRebuild: true);
            }
        }
    } = 4;

    [JsonProperty]
    public TextStyle StatusRemainingTextStyle
    {
        get => field;
        set
        {
            if (!Equals(StatusRemainingTextStyle, value))
            {
                field = value;
                Notify(nameof(StatusRemainingTextStyle));
            }
        }
    } = new()
    {
        Width = 120,
        Height = 22,
        FontSize = 20,
        FontType = FontType.Axis,
        TextColor = ColorHelper.GetColor(50),
        TextOutlineColor = ColorHelper.GetColor(53),
        TextFlags = TextFlags.Edge
    };

    [JsonProperty]
    public SortCriterion PrimarySort
    {
        get => field;
        set
        {
            if (PrimarySort != value)
            {
                field = value;
                Notify(nameof(PrimarySort), needsRebuild: true);
            }
        }
    }

    [JsonProperty]
    public SortCriterion SecondarySort
    {
        get => field;
        set
        {
            if (SecondarySort != value)
            {
                field = value;
                Notify(nameof(SecondarySort), needsRebuild: true);
            }
        }
    }

    [JsonProperty]
    public SortCriterion TertiarySort
    {
        get => field;
        set
        {
            if (TertiarySort != value)
            {
                field = value;
                Notify(nameof(TertiarySort), needsRebuild: true);
            }
        }
    }

    [JsonProperty]
    public SortOrder PrimarySortOrder
    {
        get => field;
        set
        {
            if (PrimarySortOrder != value)
            {
                field = value;
                Notify(nameof(PrimarySortOrder), needsRebuild: true);
            }
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public SortOrder SecondarySortOrder
    {
        get => field;
        set
        {
            if (SecondarySortOrder != value)
            {
                field = value;
                Notify(nameof(SecondarySortOrder), needsRebuild: true);
            }
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public SortOrder TertiarySortOrder
    {
        get => field;
        set
        {
            if (TertiarySortOrder != value)
            {
                field = value;
                Notify(nameof(TertiarySortOrder), needsRebuild: true);
            }
        }
    } = SortOrder.Ascending;

    [JsonProperty]
    public bool ShowActorLetter
    {
        get => field;
        set
        {
            if (ShowActorLetter != value)
            {
                field = value;
                Notify(nameof(ShowActorLetter), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool ShowActorName
    {
        get => field;
        set
        {
            if (ShowActorName != value)
            {
                field = value;
                Notify(nameof(ShowActorName), updateNodes: true);
            }
        }
    } = true;

    [JsonProperty]
    public bool ShowPermaIcons
    {
        get => field;
        set
        {
            if (ShowPermaIcons != value)
            {
                field = value;
                Notify(nameof(ShowPermaIcons));
            }
        }
    } = true;

    [JsonProperty]
    public bool StatusAsItemName
    {
        get => field;
        set
        {
            if (StatusAsItemName != value)
            {
                field = value;
                Notify(nameof(StatusAsItemName));
            }
        }
    } = true;
}
