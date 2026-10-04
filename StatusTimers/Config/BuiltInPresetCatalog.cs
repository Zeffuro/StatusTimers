using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using static StatusTimers.Config.StatusTimerOverlayConfig;

namespace StatusTimers.Config;

public sealed record BuiltInPreset(string Id, string Name, string Description);

public static class BuiltInPresetCatalog
{
    public static IReadOnlyList<BuiltInPreset> All { get; } = Array.AsReadOnly(new[]
    {
        new BuiltInPreset("classic", "Classic", "Default layout."),
        new BuiltInPreset("compact-icons", "Compact Icons", "Icons with timers underneath."),
        new BuiltInPreset("buff-bars", "Buff Bars", "Statuses in a vertical list with progressbars."),
        new BuiltInPreset("split", "Split Buffs/Debuffs", "Separate buff and debuff overlays.")
    });

    public static OverlaySetup Create(string id, OverlaySetup current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (id is not ("classic" or "compact-icons" or "buff-bars" or "split")) {
            throw new ArgumentException("Unknown built-in preset.", nameof(id));
        }

        var setup = ConfigTransfer.CloneSetup(current);
        foreach (var kind in Enum.GetValues<NodeKind>())
        {
            if (!setup.Overlays.TryGetValue(kind, out var config)) {
                setup.Overlays[kind] = config = new StatusTimerOverlayConfig(kind);
            }

            ApplyClassic(config, kind);
            config.Enabled = kind == NodeKind.MultiDoT || (id == "split"
                ? kind is NodeKind.Buffs or NodeKind.Debuffs
                : kind == NodeKind.Combined);

            switch (id)
            {
                case "classic":
                    config.ItemsPerLine = 8;
                    config.MaxStatuses = 16;
                    break;
                case "compact-icons":
                    ApplyCompactIcons(config);
                    break;
                case "buff-bars":
                    ApplyBars(config, kind, new Vector4(0.36f, 0.64f, 0.90f, 1));
                    break;
                case "split":
                    var color = kind switch
                    {
                        NodeKind.Buffs => new Vector4(0.32f, 0.76f, 0.49f, 1),
                        NodeKind.Debuffs => new Vector4(0.90f, 0.39f, 0.36f, 1),
                        _ => new Vector4(0.77f, 0.59f, 0.90f, 1)
                    };
                    ApplyBars(config, kind, color);
                    break;
            }
        }
        return setup;
    }

    private static void ApplyClassic(StatusTimerOverlayConfig config, NodeKind kind)
    {
        var defaults = new StatusTimerOverlayConfig(kind);
        config.Icon = defaults.Icon;
        config.Name = defaults.Name;
        config.Timer = defaults.Timer;
        config.Actor = defaults.Actor;
        config.Progress = defaults.Progress;
        config.Background = defaults.Background;
        config.RowWidth = defaults.RowWidth;
        config.RowHeight = defaults.RowHeight;
        config.FillRowsFirst = defaults.FillRowsFirst;
        config.ItemsPerLine = defaults.ItemsPerLine;
        config.MaxStatuses = defaults.MaxStatuses;
        config.GrowDirection = defaults.GrowDirection;
        config.ScaleInt = defaults.ScaleInt;
        config.StatusHorizontalPadding = defaults.StatusHorizontalPadding;
        config.StatusVerticalPadding = defaults.StatusVerticalPadding;
    }

    private static void ApplyCompactIcons(StatusTimerOverlayConfig config)
    {
        config.RowWidth = 48;
        config.RowHeight = 64;
        config.FillRowsFirst = true;
        config.ItemsPerLine = 8;
        config.StatusHorizontalPadding = 4;
        config.StatusVerticalPadding = 4;
        config.Name.IsVisible = false;
        config.Actor.IsVisible = false;
        config.Progress.IsVisible = false;
        config.Background.IsVisible = false;
        config.Icon.Anchor = Rectangle(8, 0, 32, 42);
        config.Timer.Anchor = Rectangle(0, 43, 48, 20);
        config.Timer.BackgroundEnabled = false;
        config.Timer.Style!.FontSize = 14;
        config.Timer.Style.FontType = FontType.Axis;
        config.Timer.Style.Alignment = AlignmentType.Center;
    }

    private static void ApplyBars(StatusTimerOverlayConfig config, NodeKind kind, Vector4 color)
    {
        var showActor = kind == NodeKind.MultiDoT;
        config.RowWidth = 300;
        config.MaxStatuses = 12;
        config.RowHeight = 48;
        config.FillRowsFirst = true;
        config.ItemsPerLine = 1;
        config.StatusHorizontalPadding = 8;
        config.StatusVerticalPadding = 4;
        config.Icon.Anchor = Rectangle(0, 3, 32, 42);
        config.Name.Anchor = Rectangle(40, 0, 176, 22);
        config.Name.Style!.FontSize = 16;
        config.Name.Style.Alignment = AlignmentType.Left;
        config.Timer.Anchor = Rectangle(236, 0, 60, 22);
        config.Timer.Style!.FontSize = 16;
        config.Timer.Style.FontType = FontType.Axis;
        config.Timer.Style.Alignment = AlignmentType.Right;
        config.Timer.BackgroundEnabled = false;
        config.Actor.IsVisible = showActor;
        config.Actor.Anchor = Rectangle(40, 22, 208, 16);
        config.Actor.Style!.FontSize = 12;
        config.Actor.Style.Alignment = AlignmentType.Left;
        config.Progress.Anchor = Rectangle(40, showActor ? 41 : 34, 256, 6);
        config.Progress.StyleBar = new BarStyle
        {
            ColorTreatment = ProgressBarColorTreatment.Flat,
            ProgressColor = color,
            BackgroundColor = new Vector4(0.08f, 0.09f, 0.12f, 0.85f),
            BorderColor = new Vector4(0, 0, 0, 0.9f),
            BorderVisible = true
        };
        config.Background.IsVisible = false;
        config.Background.Anchor = Rectangle(0, 0, 300, 48);
    }

    private static StatusNodeAnchorConfig Rectangle(float x, float y, float width, float height) => new()
    {
        AnchorTo = AnchorTarget.ContainerLeft,
        Alignment = AnchorAlignment.Left | AnchorAlignment.Top,
        OffsetX = x,
        OffsetY = y,
        Width = width,
        Height = height
    };
}
