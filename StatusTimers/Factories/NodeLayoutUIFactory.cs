using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Layout;
using StatusTimers.Models;
using StatusTimers.Nodes;
using StatusTimers.Windows;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Factories;

public static class NodeLayoutUIFactory
{
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;
    private const float SectionHeight = 220;
    public static VerticalListNode<NodeBase> CreateNodeLayoutSection(
        string label,
        StatusTimerOverlayConfig.NodePartConfig nodePart,
        OverlayManager? overlayManager,
        Action onChanged = null,
        Action onToggled = null)
    {
        var section = new VerticalListNode<NodeBase>
        {
            X = OptionOffset,
            Width = 600,
            ItemVerticalSpacing = 0,
            IsVisible = true,
            FitContents = true,
        };
        VerticalListNode<NodeBase> settingsGroup = null;

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
                    section.Height = isChecked ? SectionHeight : CheckBoxHeight;
                    section.FitContents = isChecked;
                    section.RecalculateLayout();
                }
                onChanged?.Invoke();
                onToggled?.Invoke();
            }
        };
        section.AddNode(enabledCheckbox);

        settingsGroup = new VerticalListNode<NodeBase>
        {
            X = OptionOffset,
            Height = SectionHeight,
            Width = 600,
            ItemVerticalSpacing = 0,
            IsVisible = nodePart.IsVisible
        };

        settingsGroup = new VerticalListNode<NodeBase>
        {
            X = OptionOffset,
            Height = SectionHeight,
            Width = 600,
            ItemVerticalSpacing = 0,
            IsVisible = nodePart.IsVisible
        };

        var backgroundRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = nodePart.BackgroundEnabled != null,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };
        settingsGroup.AddNode(backgroundRow);

        var backgroundCheckbox = new CheckboxNode {
            LabelText = $"Show {label} background",
            Width = 180,
            Height = 22,
            IsVisible = nodePart.BackgroundEnabled != null,
            IsChecked = nodePart.BackgroundEnabled ?? false,
            OnClick = isChecked => {
                nodePart.BackgroundEnabled = isChecked;
                onChanged?.Invoke();
                onToggled?.Invoke();
            }
        };
        backgroundRow.AddNode(backgroundCheckbox);

        if (nodePart.Style != null && overlayManager != null)
        {
            var styleRow = new HorizontalFlexNode<NodeBase>
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                FitPadding = 4
            };

            settingsGroup.AddNode(styleRow);

            styleRow.AddNode(ConfigurationUIFactory.CreateLabeledDropdown(
                "Font",
                () => nodePart.Style.FontType,
                v => { nodePart.Style.FontType = v; onChanged?.Invoke(); },
                ConfigurationUIFactory.FontMap
            ));

            styleRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption(
                "Font Size",
                () => nodePart.Style.FontSize,
                v => { nodePart.Style.FontSize = v; onChanged?.Invoke(); }
            ));

            var styleRow2 = new HorizontalFlexNode<NodeBase>
            {
                IsVisible = true,
                Width = 562,
                Height = 32,
                FitPadding = 4
            };

            settingsGroup.AddNode(styleRow2);

            styleRow2.AddNode(CreateColorPreviewButton(
                "Text Color",
                () => nodePart.Style.TextColor,
                c => { nodePart.Style.TextColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged,
                32
            ));

            styleRow2.AddNode(CreateColorPreviewButton(
                "Text Outline Color",
                () => nodePart.Style.TextOutlineColor,
                c => { nodePart.Style.TextOutlineColor = c; onChanged?.Invoke(); },
                overlayManager,
                onChanged,
                32
            ));
        }

        var offsetRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };

        settingsGroup.AddNode(offsetRow);

        offsetRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Offset X",
            () => (int)nodePart.Anchor.OffsetX,
            v => { nodePart.Anchor.OffsetX = v; onChanged?.Invoke(); }));

        offsetRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Offset Y",
            () => (int)nodePart.Anchor.OffsetY,
            v => { nodePart.Anchor.OffsetY = v; onChanged?.Invoke(); }));

        var sizeRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };
        settingsGroup.AddNode(sizeRow);

        sizeRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Width",
            () => (int)nodePart.Anchor.Width,
            v => { nodePart.Anchor.Width = v; onChanged?.Invoke(); }));

        sizeRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Height",
            () => (int)nodePart.Anchor.Height,
            v => { nodePart.Anchor.Height = v; onChanged?.Invoke(); }));

        var anchorTargetDict = ((AnchorTarget[])Enum.GetValues(typeof(AnchorTarget)))
            .ToDictionary(a => a, a => a.ToString());
        var anchorToRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 8
        };
        settingsGroup.AddNode(anchorToRow);

        anchorToRow.AddNode(ConfigurationUIFactory.CreateLabeledDropdown("Anchor",
            () => nodePart.Anchor.AnchorTo,
            v => { nodePart.Anchor.AnchorTo = v; onChanged?.Invoke(); },
            anchorTargetDict));;

        settingsGroup.AddNode(CreateAlignmentFlagSection(
            () => nodePart.Anchor.Alignment,
            v => { nodePart.Anchor.Alignment = v; onChanged?.Invoke(); },
            onChanged
        ));

        section.AddNode(settingsGroup);

        return section;
    }

    public static VerticalListNode<NodeBase> CreateAlignmentFlagSection(
        Func<AnchorAlignment> getAlignment,
        Action<AnchorAlignment> setAlignment,
        Action onChanged = null
    )
    {
        var alignmentSection = new VerticalListNode<NodeBase>
        {
            X = OptionOffset,
            ItemVerticalSpacing = 0,
            IsVisible = true,
            Width = 600,
        };

        alignmentSection.AddNode(new TextNode
        {
            Text = "Alignment",
            IsVisible = true,
            Height = 20,
            Width = 120,
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
            TextColor = TextStyles.OptionLabel.TextColor
        });

        var horizontalRow = new HorizontalListNode<NodeBase>
        {
            X = OptionOffset,
            Width = 600,
            Height = 28,
            ItemHorizontalSpacing = 8,
            IsVisible = true
        };

        horizontalRow.AddNode(new TextNode
        {
            Text = "Horizontal:",
            IsVisible = true,
            Height = 18,
            Width = 114,
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
            TextColor = TextStyles.OptionLabel.TextColor
        });

        horizontalRow.AddNode(new CheckboxNode
        {
            LabelText = "Left",
            Width = 100,
            Height = 18,
            IsVisible = true,
            IsChecked = getAlignment().HasFlag(AnchorAlignment.Left),
            OnClick = isChecked =>
            {
                ToggleFlag(isChecked, AnchorAlignment.Left, getAlignment, setAlignment, onChanged);
            }
        });
        horizontalRow.AddNode(new CheckboxNode
        {
            LabelText = "Right",
            Width = 100,
            Height = 18,
            IsVisible = true,
            IsChecked = getAlignment().HasFlag(AnchorAlignment.Right),
            OnClick = isChecked =>
            {
                ToggleFlag(isChecked, AnchorAlignment.Right, getAlignment, setAlignment, onChanged);
            }
        });
        horizontalRow.AddNode(new CheckboxNode
        {
            LabelText = "HCenter",
            Width = 100,
            Height = 18,
            IsVisible = true,
            IsChecked = getAlignment().HasFlag(AnchorAlignment.HorizontalCenter),
            OnClick = isChecked =>
            {
                ToggleFlag(isChecked, AnchorAlignment.HorizontalCenter, getAlignment, setAlignment, onChanged);
            }
        });
        horizontalRow.AddNode(new CheckboxNode
        {
            LabelText = "Center",
            Width = 100,
            Height = 18,
            IsVisible = true,
            IsChecked = getAlignment().HasFlag(AnchorAlignment.Center),
            OnClick = isChecked =>
            {
                ToggleFlag(isChecked, AnchorAlignment.Center, getAlignment, setAlignment, onChanged);
            }
        });

        alignmentSection.AddNode(horizontalRow);

        var verticalRow = new HorizontalListNode<NodeBase>
        {
            X = OptionOffset,
            Width = 600,
            Height = 28,
            ItemHorizontalSpacing = 8,
            IsVisible = true
        };

        verticalRow.AddNode(new TextNode
        {
            Text = "Vertical:",
            IsVisible = true,
            Height = 18,
            Width = 114,
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
            TextColor = TextStyles.OptionLabel.TextColor
        });

        verticalRow.AddNode(new CheckboxNode
        {
            LabelText = "Top",
            Width = 100,
            Height = 18,
            IsVisible = true,
            IsChecked = getAlignment().HasFlag(AnchorAlignment.Top),
            OnClick = isChecked =>
            {
                ToggleFlag(isChecked, AnchorAlignment.Top, getAlignment, setAlignment, onChanged);
            }
        });
        verticalRow.AddNode(new CheckboxNode
        {
            LabelText = "Bottom",
            Width = 100,
            Height = 18,
            IsVisible = true,
            IsChecked = getAlignment().HasFlag(AnchorAlignment.Bottom),
            OnClick = isChecked =>
            {
                ToggleFlag(isChecked, AnchorAlignment.Bottom, getAlignment, setAlignment, onChanged);
            }
        });
        verticalRow.AddNode(new CheckboxNode
        {
            LabelText = "VCenter",
            Width = 100,
            Height = 18,
            IsVisible = true,
            IsChecked = getAlignment().HasFlag(AnchorAlignment.VerticalCenter),
            OnClick = isChecked =>
            {
                ToggleFlag(isChecked, AnchorAlignment.VerticalCenter, getAlignment, setAlignment, onChanged);
            }
        });

        alignmentSection.AddNode(verticalRow);

        return alignmentSection;
    }


    private static void ToggleFlag(
        bool isChecked,
        AnchorAlignment flag,
        Func<AnchorAlignment> getAlignment,
        Action<AnchorAlignment> setAlignment,
        Action onChanged)
    {
        var val = getAlignment();
        var num = (int)val;
        var fnum = (int)flag;
        if (isChecked) {
            num |= fnum;
        }
        else {
            num &= ~fnum;
        }

        setAlignment((AnchorAlignment)num);
        onChanged?.Invoke();
    }

    public static HorizontalFlexNode<NodeBase> CreateColorPreviewButton(
        string labelText,
        Func<Vector4> getColor,
        Action<Vector4> setColor,
        OverlayManager overlayManager,
        Action onChanged = null,
        float size = 32f
    )
    {
        var flexNode = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            X = OptionOffset,
            Width = 272,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };

        flexNode.AddNode(new TextNode
        {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 20,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            AlignmentType = AlignmentType.Left,
            Text = labelText
        });

        var colorPreviewButton = new ColorPreviewButtonNode(GlobalServices.NativeController)
        {
            Y = -2,
            IsVisible = true,
            Size = new Vector2(size),
            Color = getColor(),
        };
        colorPreviewButton.OnClick = () =>
        {
            var startColor = getColor();
            GlobalServices.Framework.RunOnTick(() =>
            {
                overlayManager?.ColorPickerInstance?.Show(startColor, newColor =>
                {
                    setColor(newColor);
                    colorPreviewButton.Color = newColor;
                    onChanged?.Invoke();
                });
            }, delayTicks: 3);
        };
        flexNode.AddNode(colorPreviewButton);
        return flexNode;
    }
}
