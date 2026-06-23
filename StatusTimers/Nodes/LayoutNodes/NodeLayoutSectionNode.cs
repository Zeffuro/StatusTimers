using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class NodeLayoutSectionNode : ConfigVerticalListNode
{
    public NodeLayoutSectionNode(
        string label,
        StatusTimerOverlayConfig.NodePartConfig nodePart,
        StatusTimerOverlayConfig.NodePartConfig baseDefaultsPart,
        OverlayManager? overlayManager,
        Action? onChanged = null,
        Action? onToggled = null)
    {
        X = 18;
        Width = 600;
        Height = 16;
        ItemSpacing = 0;
        IsVisible = true;
        FitContents = true;

        var settingsGroup = new ConfigVerticalListNode
        {
            X = 18,
            Height = 220,
            Width = 600,
            ItemSpacing = 2,
            IsVisible = nodePart.IsVisible,
            FitContents = true,
        };

        var enabledCheckbox = new CheckboxNode
        {
            String = $"Show {label}",
            Width = 180,
            Height = 20,
            IsVisible = true,
            IsChecked = nodePart.IsVisible,
            OnClick = isChecked =>
            {
                nodePart.IsVisible = isChecked;
                settingsGroup.IsVisible = isChecked;
                onChanged?.Invoke();
                onToggled?.Invoke();
            }
        };
        var settingsNodes = new List<NodeBase>();

        // Background row
        var backgroundRow = new HorizontalFlexNode
        {
            IsVisible = nodePart.BackgroundEnabled != null,
            Width = 562,
            Height = 32,
            ItemSpacing = 4
        };
        settingsNodes.Add(backgroundRow);

        var backgroundCheckbox = new CheckboxNode
        {
            String = $"Show {label} background",
            Width = 180,
            Height = 20,
            IsVisible = nodePart.BackgroundEnabled != null,
            IsChecked = nodePart.BackgroundEnabled ?? false,
            OnClick = isChecked =>
            {
                nodePart.BackgroundEnabled = isChecked;
                onChanged?.Invoke();
                onToggled?.Invoke();
            }
        };
        backgroundRow.AddNode([backgroundCheckbox]);

        if (nodePart.StyleKind == NodePartStyleKind.Bar && nodePart.StyleBar != null && overlayManager != null)
        {
            var defaultBarStyle = baseDefaultsPart.StyleBar;
            var borderDefaultColor = defaultBarStyle?.BorderColor ?? nodePart.StyleBar.BorderColor;
            var progressDefaultColor = defaultBarStyle?.ProgressColor ?? nodePart.StyleBar.ProgressColor;
            var backgroundDefaultColor = defaultBarStyle?.BackgroundColor ?? nodePart.StyleBar.BackgroundColor;
            var barStyleRow = new HorizontalFlexNode
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                ItemSpacing = 4
            };
            settingsNodes.Add(barStyleRow);

            var borderCheckbox = new CheckboxOptionNode {
                String = "Show Border",
                IsChecked = nodePart.StyleBar.BorderVisible ?? BarStyleDefaults.BorderVisible,
                OnClick = isChecked => {
                    nodePart.StyleBar.BorderVisible = isChecked;
                    onChanged?.Invoke();
                }
            };

            var borderColorNode = new ColorPreviewOptionNode(
                $"Border Color",
                () => nodePart.StyleBar.BorderColor ?? BarStyleDefaults.BorderColor, borderDefaultColor ?? BarStyleDefaults.BorderColor,
                c => { nodePart.StyleBar.BorderColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged
            );
            barStyleRow.AddNode([borderCheckbox, borderColorNode]);

            var barStyleRow2 = new HorizontalFlexNode
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                ItemSpacing = 4
            };
            settingsNodes.Add(barStyleRow2);

            var progressColorNode = new ColorPreviewOptionNode(
                "Progress Color",
                () => nodePart.StyleBar.ProgressColor ?? BarStyleDefaults.ProgressColor, progressDefaultColor ?? BarStyleDefaults.ProgressColor,
                c => { nodePart.StyleBar.ProgressColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged
            );
            var backgroundColorNode = new ColorPreviewOptionNode(
                "Background Color",
                () => nodePart.StyleBar.BackgroundColor ?? BarStyleDefaults.BackgroundColor, backgroundDefaultColor ?? BarStyleDefaults.BackgroundColor,
                c => { nodePart.StyleBar.BackgroundColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged
            );
            barStyleRow2.AddNode([progressColorNode, backgroundColorNode]);
        }

        // Style rows
        if (nodePart.Style != null && overlayManager != null)
        {
            var defaultTextStyle = baseDefaultsPart.Style;
            var textDefaultColor = defaultTextStyle?.TextColor ?? nodePart.Style.TextColor;
            var outlineDefaultColor = defaultTextStyle?.TextOutlineColor ?? nodePart.Style.TextOutlineColor;
            var styleRow = new HorizontalFlexNode
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                ItemSpacing = 4
            };
            settingsNodes.Add(styleRow);

            var fontDropdownNode = new LabeledDropdownOptionNode<FontType>(
                "Font",
                () => nodePart.Style.FontType,
                v => { nodePart.Style.FontType = v; onChanged?.Invoke(); },
                FontMap
            );

            var fontSizeNode = new LabeledNumericOptionNode(
                "Font Size",
                () => nodePart.Style.FontSize,
                v => { nodePart.Style.FontSize = v; onChanged?.Invoke(); }
            );
            styleRow.AddNode([fontDropdownNode, fontSizeNode]);

            var styleRow2 = new HorizontalFlexNode
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                ItemSpacing = 4
            };
            settingsNodes.Add(styleRow2);

            var textColorNode = new ColorPreviewOptionNode(
                "Text Color",
                () => nodePart.Style.TextColor, textDefaultColor,
                c => { nodePart.Style.TextColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged
            );
            var textOutlineColorNode = new ColorPreviewOptionNode(
                "Text Outline Color",
                () => nodePart.Style.TextOutlineColor, outlineDefaultColor,
                c => { nodePart.Style.TextOutlineColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged
            );
            styleRow2.AddNode([textColorNode, textOutlineColorNode]);
        }

        // Offset row
        var offsetRow = new HorizontalFlexNode
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            ItemSpacing = 4
        };
        settingsNodes.Add(offsetRow);

        var offsetXNode = new LabeledNumericOptionNode("Offset X",
            () => (int)nodePart.Anchor.OffsetX,
            v => { nodePart.Anchor.OffsetX = v; onChanged?.Invoke(); });

        var offsetYNode = new LabeledNumericOptionNode("Offset Y",
            () => (int)nodePart.Anchor.OffsetY,
            v => { nodePart.Anchor.OffsetY = v; onChanged?.Invoke(); });
        offsetRow.AddNode([offsetXNode, offsetYNode]);

        // Size row
        var sizeRow = new HorizontalFlexNode
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            ItemSpacing = 4
        };
        settingsNodes.Add(sizeRow);

        var widthNode = new LabeledNumericOptionNode("Width",
            () => (int)nodePart.Anchor.Width,
            v => { nodePart.Anchor.Width = v; onChanged?.Invoke(); }, false);

        var heightNode = new LabeledNumericOptionNode("Height",
            () => (int)nodePart.Anchor.Height,
            v => { nodePart.Anchor.Height = v; onChanged?.Invoke(); }, false);
        sizeRow.AddNode([widthNode, heightNode]);

        // Anchor target row
        var anchorTargetDict = ((AnchorTarget[])Enum.GetValues(typeof(AnchorTarget)))
            .ToDictionary(a => a, a => a.ToString());
        var anchorToRow = new HorizontalFlexNode
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            ItemSpacing = 8
        };
        settingsNodes.Add(anchorToRow);

        anchorToRow.AddNode([new LabeledDropdownOptionNode<AnchorTarget>("Anchor",
            () => nodePart.Anchor.AnchorTo,
            v => { nodePart.Anchor.AnchorTo = v; onChanged?.Invoke(); },
            anchorTargetDict)]);

        // Alignment flags
        settingsNodes.Add(new AlignmentFlagSectionNode(
            () => nodePart.Anchor.Alignment,
            v => { nodePart.Anchor.Alignment = v; onChanged?.Invoke(); },
            onChanged
        ));

        settingsGroup.AddNode(settingsNodes);
        AddNode([enabledCheckbox, settingsGroup]);

        RecalculateLayout();
    }

    private static readonly Dictionary<FontType, string> FontMap = new() {
        { FontType.Axis, "Axis" },
        { FontType.MiedingerMed, "Miedinger Med" },
        { FontType.Miedinger, "Miedinger" },
        { FontType.TrumpGothic, "Trump Gothic" },
        { FontType.Jupiter, "Jupiter" },
        { FontType.JupiterLarge, "Jupiter Large" }
    };
}
