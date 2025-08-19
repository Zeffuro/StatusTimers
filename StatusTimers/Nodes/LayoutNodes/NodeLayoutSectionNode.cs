using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class NodeLayoutSectionNode : VerticalListNode
{
    public NodeLayoutSectionNode(
        string label,
        StatusTimerOverlayConfig.NodePartConfig nodePart,
        OverlayManager? overlayManager,
        Action onChanged = null,
        Action onToggled = null)
    {
        X = 18;
        Width = 600;
        ItemSpacing = 0;
        IsVisible = true;
        FitContents = true;

        VerticalListNode settingsGroup = null;

        var enabledCheckbox = new CheckboxNode
        {
            LabelText = $"Show {label}",
            Width = 180,
            Height = 22,
            IsVisible = true,
            IsChecked = nodePart.IsVisible,
            OnClick = isChecked =>
            {
                nodePart.IsVisible = isChecked;
                if (settingsGroup != null)
                {
                    settingsGroup.IsVisible = isChecked;
                    Height = isChecked ? 220 : 16;
                    FitContents = isChecked;
                    RecalculateLayout();
                }
                onChanged?.Invoke();
                onToggled?.Invoke();
            }
        };
        AddNode(enabledCheckbox);

        settingsGroup = new VerticalListNode
        {
            X = 18,
            Height = 220,
            Width = 600,
            ItemSpacing = 0,
            IsVisible = nodePart.IsVisible
        };

        // Background row
        var backgroundRow = new HorizontalFlexNode
        {
            IsVisible = nodePart.BackgroundEnabled != null,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };
        settingsGroup.AddNode(backgroundRow);

        var backgroundCheckbox = new CheckboxNode
        {
            LabelText = $"Show {label} background",
            Width = 180,
            Height = 22,
            IsVisible = nodePart.BackgroundEnabled != null,
            IsChecked = nodePart.BackgroundEnabled ?? false,
            OnClick = isChecked =>
            {
                nodePart.BackgroundEnabled = isChecked;
                onChanged?.Invoke();
                onToggled?.Invoke();
            }
        };
        backgroundRow.AddNode(backgroundCheckbox);

        // Style rows
        if (nodePart.Style != null && overlayManager != null)
        {
            var styleRow = new HorizontalFlexNode
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                FitPadding = 4
            };
            settingsGroup.AddNode(styleRow);

            styleRow.AddNode(new LabeledDropdownOptionNode<FontType>(
                "Font",
                () => nodePart.Style.FontType,
                v => { nodePart.Style.FontType = v; onChanged?.Invoke(); },
                FontMap
            ));

            styleRow.AddNode(new LabeledNumericOptionNode(
                "Font Size",
                () => nodePart.Style.FontSize,
                v => { nodePart.Style.FontSize = v; onChanged?.Invoke(); }
            ));

            var styleRow2 = new HorizontalFlexNode
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                FitPadding = 4
            };
            settingsGroup.AddNode(styleRow2);

            styleRow2.AddNode(new ColorPreviewOptionNode(
                "Text Color",
                () => nodePart.Style.TextColor,
                c => { nodePart.Style.TextColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged,
                32
            ));

            styleRow2.AddNode(new ColorPreviewOptionNode(
                "Text Outline Color",
                () => nodePart.Style.TextOutlineColor,
                c => { nodePart.Style.TextOutlineColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged,
                32
            ));
        }

        // Offset row
        var offsetRow = new HorizontalFlexNode
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };
        settingsGroup.AddNode(offsetRow);

        offsetRow.AddNode(new LabeledNumericOptionNode("Offset X",
            () => (int)nodePart.Anchor.OffsetX,
            v => { nodePart.Anchor.OffsetX = v; onChanged?.Invoke(); }));

        offsetRow.AddNode(new LabeledNumericOptionNode("Offset Y",
            () => (int)nodePart.Anchor.OffsetY,
            v => { nodePart.Anchor.OffsetY = v; onChanged?.Invoke(); }));

        // Size row
        var sizeRow = new HorizontalFlexNode
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };
        settingsGroup.AddNode(sizeRow);

        sizeRow.AddNode(new LabeledNumericOptionNode("Width",
            () => (int)nodePart.Anchor.Width,
            v => { nodePart.Anchor.Width = v; onChanged?.Invoke(); }));

        sizeRow.AddNode(new LabeledNumericOptionNode("Height",
            () => (int)nodePart.Anchor.Height,
            v => { nodePart.Anchor.Height = v; onChanged?.Invoke(); }));

        // Anchor target row
        var anchorTargetDict = ((AnchorTarget[])Enum.GetValues(typeof(AnchorTarget)))
            .ToDictionary(a => a, a => a.ToString());
        var anchorToRow = new HorizontalFlexNode
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 8
        };
        settingsGroup.AddNode(anchorToRow);

        anchorToRow.AddNode(new LabeledDropdownOptionNode<AnchorTarget>("Anchor",
            () => nodePart.Anchor.AnchorTo,
            v => { nodePart.Anchor.AnchorTo = v; onChanged?.Invoke(); },
            anchorTargetDict));

        // Alignment flags
        settingsGroup.AddNode(new AlignmentFlagSectionNode(
            () => nodePart.Anchor.Alignment,
            v => { nodePart.Anchor.Alignment = v; onChanged?.Invoke(); },
            onChanged
        ));

        AddNode(settingsGroup);
    }

    private static readonly Dictionary<FontType, string> FontMap = new() {
        { FontType.Axis, "Axis" },
        { FontType.Miedinger, "Miedinger" },
        { FontType.TrumpGothic, "Trump Gothic" },
        { FontType.Jupiter, "Jupiter" },
        { FontType.JupiterLarge, "Jupiter Large" }
    };
}
