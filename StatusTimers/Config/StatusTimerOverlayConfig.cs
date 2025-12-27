using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using Newtonsoft.Json;
using StatusTimers.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
// ReSharper disable InconsistentNaming

namespace StatusTimers.Config;

/// <summary>
/// Stores overlay configuration and notifies listeners on change.
/// </summary>
public class StatusTimerOverlayConfig
{
    public event Action<string, bool>? OnPropertyChanged;

    public void Notify(string property, bool updateNodes = false)
        => OnPropertyChanged?.Invoke(property, updateNodes);

    public StatusTimerOverlayConfig(NodeKind kind) {
        switch (kind) {
            case NodeKind.MultiDoT:
                PrimarySort = SortCriterion.EnemyLetter;
                SecondarySort = SortCriterion.TimeRemaining;
                TertiarySort = SortCriterion.None;
                break;
            case NodeKind.Combined:
            default:
                PrimarySort = SortCriterion.StatusType;
                SecondarySort = SortCriterion.OwnStatusFirst;
                TertiarySort = SortCriterion.PartyPriority;
                break;
        }
    }

    // Node Part Configs

    [JsonProperty]
    public NodePartConfig Icon { get; set; } = new()
    {
        IsVisible = true,
        BackgroundEnabled = null,
        Anchor = new StatusNodeAnchorConfig
        {
            AnchorTo = AnchorTarget.ContainerLeft,
            OffsetX = 0,
            OffsetY = 0,
            Alignment = AnchorAlignment.Left,
            Width = 48,
            Height = 64
        },
        StyleKind = NodePartStyleKind.Icon
    };

    [JsonProperty]
    public NodePartConfig Name { get; set; } = new()
    {
        IsVisible = true,
        BackgroundEnabled = false,
        Anchor = new StatusNodeAnchorConfig
        {
            AnchorTo = AnchorTarget.IconRight,
            OffsetX = 8,
            OffsetY = 8,
            Alignment = AnchorAlignment.Left,
            Width = 180,
            Height = 28
        },
        Style = new TextStyle
        {
            FontSize = 20,
            FontType = FontType.Axis,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge
        },
        StyleKind = NodePartStyleKind.Text
    };

    [JsonProperty]
    public NodePartConfig Timer { get; set; } = new()
    {
        IsVisible = true,
        BackgroundEnabled = true,
        Anchor = new StatusNodeAnchorConfig
        {
            AnchorTo = AnchorTarget.NameRight,
            OffsetX = 30,
            OffsetY = 0,
            Alignment = AnchorAlignment.VerticalCenter | AnchorAlignment.Right,
            Width = 44,
            Height = 22
        },
        Style = new TextStyle
        {
            FontSize = 20,
            FontType = FontType.TrumpGothic,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(53),
            TextFlags = TextFlags.Edge
        },
        StyleKind = NodePartStyleKind.Text
    };

    [JsonProperty]
    public NodePartConfig Actor { get; set; } = new()
    {
        IsVisible = true,
        BackgroundEnabled = false,
        Anchor = new StatusNodeAnchorConfig
        {
            AnchorTo = AnchorTarget.NameBottom,
            OffsetX = 0,
            OffsetY = -6,
            Alignment = AnchorAlignment.Left,
            Width = 120,
            Height = 12
        },
        Style = new TextStyle
        {
            FontSize = 14,
            FontType = FontType.Axis,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(54),
            TextFlags = TextFlags.Edge
        },
        StyleKind = NodePartStyleKind.Text
    };

    [JsonProperty]
    public NodePartConfig Progress { get; set; } = new()
    {
        IsVisible = true,
        BackgroundEnabled = null,
        Anchor = new StatusNodeAnchorConfig
        {
            AnchorTo = AnchorTarget.ActorNameBottom,
            OffsetX = -7,
            OffsetY = 20,
            Alignment = AnchorAlignment.Bottom,
            Height = 20,
            Width = 200
        },
        StyleBar = new BarStyle
        {
            BackgroundColor = KnownColor.Black.Vector(),
            ProgressColor = ColorHelper.GetColor(45),
            BorderColor = KnownColor.Black.Vector(),
            BorderVisible = true
        },
        StyleKind = NodePartStyleKind.Bar
    };

    [JsonProperty]
    public NodePartConfig Background { get; set; } = new()
    {
        IsVisible = false,
        BackgroundEnabled = null,
        Anchor = new StatusNodeAnchorConfig
        {
            AnchorTo = AnchorTarget.ContainerLeft,
            OffsetX = 0,
            OffsetY = 0,
            Alignment = AnchorAlignment.Left,
            Height = 60,
            Width = 300
        },
        StyleKind = NodePartStyleKind.Background
    };

    [JsonProperty]
    public string TimerFormat {
        get;
        set {
            if (field != value) {
                field = value;
                Notify(nameof(TimerFormat), updateNodes: true);
            }
        }
    } = "{S.0}s";

    [JsonProperty]
    public int RowWidth {
        get;
        set
        {
            if (RowWidth != value)
            {
                field = value;
                Notify(nameof(RowWidth));
            }
        }
    } = 300;

    [JsonProperty]
    public int RowHeight {
        get;
        set
        {
            if (RowHeight != value)
            {
                field = value;
                Notify(nameof(RowHeight));
            }
        }
    } = 60;

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
                Notify(nameof(FillRowsFirst));
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
                Notify(nameof(GrowDirection));
            }
        }
    } = GrowDirection.DownRight;

    [JsonProperty]
    public bool Enabled {
        get => field;
        set {
            if (field != value) {
                field = value;
                Notify(nameof(Enabled));
            }
        }
    } = false;

    [JsonProperty]
    public int ItemsPerLine
    {
        get => field;
        set
        {
            if (ItemsPerLine != value)
            {
                field = value;
                Notify(nameof(ItemsPerLine));
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
                Notify(nameof(MaxStatuses));
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
                Notify(nameof(ScaleInt));
            }
        }
    } = 100;

    [JsonProperty]
    public int StatusHorizontalPadding
    {
        get => field;
        set
        {
            if (StatusHorizontalPadding != value)
            {
                field = value;
                Notify(nameof(StatusHorizontalPadding));
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
                Notify(nameof(StatusVerticalPadding));
            }
        }
    } = 4;

    [JsonProperty]
    public SortCriterion PrimarySort
    {
        get => field;
        set
        {
            if (PrimarySort != value)
            {
                field = value;
                Notify(nameof(PrimarySort));
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
                Notify(nameof(SecondarySort));
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
                Notify(nameof(TertiarySort));
            }
        }
    }

    [JsonProperty]
    public Vector2 Position {
        get => field;
        set {
            if (field != value) {
                field = value;
                Notify(nameof(Position));
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
                Notify(nameof(PrimarySortOrder));
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
                Notify(nameof(SecondarySortOrder));
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
                Notify(nameof(TertiarySortOrder));
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

    [JsonProperty]
    public bool HideStatusAboveSecondsEnabled
    {
        get => field;
        set
        {
            if (HideStatusAboveSecondsEnabled != value)
            {
                field = value;
                Notify(nameof(HideStatusAboveSecondsEnabled));
            }
        }
    } = false;

    [JsonProperty]
    public int HideStatusAboveSeconds
    {
        get => field;
        set
        {
            if (HideStatusAboveSeconds != value)
            {
                field = value;
                Notify(nameof(HideStatusAboveSeconds));
            }
        }
    } = 30;

    [JsonProperty]
    public bool HideStatusUnderSecondsEnabled
    {
        get => field;
        set
        {
            if (HideStatusUnderSecondsEnabled != value)
            {
                field = value;
                Notify(nameof(HideStatusUnderSecondsEnabled));
            }
        }
    } = false;

    [JsonProperty]
    public int HideStatusUnderSeconds
    {
        get => field;
        set
        {
            if (HideStatusUnderSeconds != value)
            {
                field = value;
                Notify(nameof(HideStatusUnderSeconds));
            }
        }
    } = 15;

    [JsonProperty]
    public bool SelfAppliedStatusesOnly
    {
        get => field;
        set
        {
            if (SelfAppliedStatusesOnly != value)
            {
                field = value;
                Notify(nameof(SelfAppliedStatusesOnly));
            }
        }
    } = true;

    [JsonProperty]
    public bool InCombatOnly
    {
        get => field;
        set
        {
            if (InCombatOnly != value)
            {
                field = value;
                Notify(nameof(InCombatOnly));
            }
        }
    } = false;

    public class NodePartConfig
    {
        public bool IsVisible { get; set; }
        public bool? BackgroundEnabled { get; set; }
        public required StatusNodeAnchorConfig Anchor { get; set; }
        public TextStyle? Style { get; set; } // Only set for nodes with text
        public BarStyle? StyleBar { get; set; } // Only set for nodes with progress bar
        public NodePartStyleKind StyleKind { get; set; } = NodePartStyleKind.None;

        public void EnsureStyleConsistency()
        {
            switch (StyleKind)
            {
                case NodePartStyleKind.Text:
                    Style ??= new TextStyle();
                    StyleBar = null;
                    break;
                case NodePartStyleKind.Bar:
                    StyleBar ??= new BarStyle();
                    Style = null;
                    break;
                case NodePartStyleKind.Icon:
                    Style = null;
                    StyleBar = null;
                    break;
                case NodePartStyleKind.None:
                default:
                    Style = null;
                    StyleBar = null;
                    break;
            }
        }
    }

    public class StatusNodeAnchorConfig
    {
        public AnchorTarget AnchorTo { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public AnchorAlignment Alignment { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public class BarStyle
    {
        public event Action? Changed;

        [JsonIgnore]
        private bool _isDirty;

        [JsonIgnore]
        public bool IsDirty
        {
            get
            {
                var wasDirty = _isDirty;
                _isDirty = false;
                return wasDirty;
            }
        }

        public Vector4? ProgressColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4? BackgroundColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4? BorderColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public bool? BorderVisible {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public BarStyle Clone() => new() {
            ProgressColor = ProgressColor,
            BackgroundColor = BackgroundColor,
            BorderColor = BorderColor,
            BorderVisible = BorderVisible
        };

        public void CopyMissingFrom(BarStyle defaults) {
            BorderColor ??= defaults.BorderColor;
            BorderVisible ??= defaults.BorderVisible;
            ProgressColor ??= defaults.ProgressColor;
            BackgroundColor ??= defaults.BackgroundColor;
        }
    }

    public class TextStyle
    {
        public event Action? Changed;

        [JsonIgnore]
        private bool _isDirty;

        [JsonIgnore]
        public bool IsDirty
        {
            get
            {
                var wasDirty = _isDirty;
                _isDirty = false;
                return wasDirty;
            }
        }

        public int FontSize {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public FontType FontType {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4 TextColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4 TextOutlineColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public TextFlags TextFlags {
            get;
            set {
                if (field != value) {
                    field = value;
                    _isDirty = true;
                    Changed?.Invoke();
                }
            }
        }

        public TextStyle Clone() => new() {
            FontSize = FontSize,
            FontType = FontType,
            TextColor = TextColor,
            TextOutlineColor = TextOutlineColor,
            TextFlags = TextFlags
        };

        public void CopyMissingFrom(TextStyle defaults) {
            if (FontSize == 0) {
                FontSize = defaults.FontSize;
            }
            if (FontType == default) {
                FontType = defaults.FontType;
            }
            if (TextColor == default) {
                TextColor = defaults.TextColor;
            }
            if (TextOutlineColor == default) {
                TextOutlineColor = defaults.TextOutlineColor;
            }
            if (TextFlags == default) {
                TextFlags = defaults.TextFlags;
            }
        }
    }
}

public static class BarStyleDefaults
{
    public static readonly Vector4 BackgroundColor = KnownColor.Black.Vector();
    public static readonly Vector4 BorderColor = KnownColor.Black.Vector();
    public static readonly Vector4 ProgressColor = ColorHelper.GetColor(45);
    public static readonly bool BorderVisible = true;
}
