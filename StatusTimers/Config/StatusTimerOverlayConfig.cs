using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using Newtonsoft.Json;
using StatusTimers.Enums;
using StatusTimers.Layout;
using StatusTimers.Models;
using System;
using System.Collections.Generic;
using System.Numerics;

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
        }
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
        }
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
        }
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
        }
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
        }
        // No TextStyle for progress bar
    };

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

    public class NodePartConfig
    {
        public bool IsVisible { get; set; }
        public bool? BackgroundEnabled { get; set; }
        public StatusNodeAnchorConfig Anchor { get; set; }
        public TextStyle? Style { get; set; } // Only set for nodes with text
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

    public class TextStyle : IEquatable<TextStyle>
    {
        public event Action? Changed;

        public int FontSize {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public FontType FontType {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4 TextColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4 TextOutlineColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public TextFlags TextFlags {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public override bool Equals(object? obj) => Equals(obj as TextStyle);

        public bool Equals(TextStyle? other)
        {
            if (other is null) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return FontSize == other.FontSize
                   && FontType == other.FontType
                   && TextColor.Equals(other.TextColor)
                   && TextOutlineColor.Equals(other.TextOutlineColor)
                   && TextFlags == other.TextFlags;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FontSize, FontType, TextColor, TextOutlineColor, TextFlags);
        }
    }
}
