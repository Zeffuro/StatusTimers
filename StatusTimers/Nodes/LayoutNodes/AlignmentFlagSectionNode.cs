using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using System;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Layout;
using System.Numerics;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class AlignmentFlagSectionNode : VerticalListNode
{
    private readonly Func<AnchorAlignment> _getAlignment;
    private readonly Action<AnchorAlignment> _setAlignment;
    private readonly Action? _onChanged;

    private const float OptionOffset = 18;

    public AlignmentFlagSectionNode(
        Func<AnchorAlignment> getAlignment,
        Action<AnchorAlignment> setAlignment,
        Action? onChanged = null)
    {
        _getAlignment = getAlignment;
        _setAlignment = setAlignment;
        _onChanged = onChanged;

        X = OptionOffset;
        ItemSpacing = 4;
        IsVisible = true;
        Width = 600;
        FitContents = true;

        AddNode(new TextNode
        {
            String = "Alignment",
            IsVisible = true,
            Height = 20,
            Width = 120,
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
            TextColor = TextStyles.OptionLabel.TextColor
        });

        var horizontalRow = new HorizontalListNode
        {
            X = OptionOffset,
            Width = 600,
            Height = 18,
            ItemSpacing = 8,
            IsVisible = true
        };

        horizontalRow.AddNode(new TextNode
        {
            String = "Horizontal:",
            IsVisible = true,
            Height = 20,
            Width = 114,
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
            TextColor = TextStyles.OptionLabel.TextColor
        });

        horizontalRow.AddNode(new CheckboxNode
        {
            String = "Left",
            Width = 90,
            Height = 20,
            IsVisible = true,
            IsChecked = _getAlignment().HasFlag(AnchorAlignment.Left),
            OnClick = isChecked => ToggleFlag(isChecked, AnchorAlignment.Left)
        });
        horizontalRow.AddNode(new CheckboxNode
        {
            String = "Right",
            Width = 90,
            Height = 20,
            IsVisible = true,
            IsChecked = _getAlignment().HasFlag(AnchorAlignment.Right),
            OnClick = isChecked => ToggleFlag(isChecked, AnchorAlignment.Right)
        });
        horizontalRow.AddNode(new CheckboxNode
        {
            String = "HCenter",
            Width = 90,
            Height = 20,
            IsVisible = true,
            IsChecked = _getAlignment().HasFlag(AnchorAlignment.HorizontalCenter),
            OnClick = isChecked => ToggleFlag(isChecked, AnchorAlignment.HorizontalCenter)
        });
        horizontalRow.AddNode(new CheckboxNode
        {
            String = "Center",
            Width = 90,
            Height = 20,
            IsVisible = true,
            IsChecked = _getAlignment().HasFlag(AnchorAlignment.Center),
            OnClick = isChecked => ToggleFlag(isChecked, AnchorAlignment.Center)
        });

        AddNode(horizontalRow);

        var verticalRow = new HorizontalListNode
        {
            X = OptionOffset,
            Width = 600,
            Height = 28,
            ItemSpacing = 8,
            IsVisible = true
        };

        verticalRow.AddNode(new TextNode
        {
            String = "Vertical:",
            IsVisible = true,
            Height = 20,
            Width = 114,
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
            TextColor = TextStyles.OptionLabel.TextColor
        });

        verticalRow.AddNode(new CheckboxNode
        {
            String = "Top",
            Width = 90,
            Height = 20,
            IsVisible = true,
            IsChecked = _getAlignment().HasFlag(AnchorAlignment.Top),
            OnClick = isChecked => ToggleFlag(isChecked, AnchorAlignment.Top)
        });
        verticalRow.AddNode(new CheckboxNode
        {
            String = "Bottom",
            Width = 90,
            Height = 20,
            IsVisible = true,
            IsChecked = _getAlignment().HasFlag(AnchorAlignment.Bottom),
            OnClick = isChecked => ToggleFlag(isChecked, AnchorAlignment.Bottom)
        });
        verticalRow.AddNode(new CheckboxNode
        {
            String = "VCenter",
            Width = 90,
            Height = 20,
            IsVisible = true,
            IsChecked = _getAlignment().HasFlag(AnchorAlignment.VerticalCenter),
            OnClick = isChecked => ToggleFlag(isChecked, AnchorAlignment.VerticalCenter)
        });

        AddNode(verticalRow);
    }

    private void ToggleFlag(bool isChecked, AnchorAlignment flag)
    {
        var val = _getAlignment();
        var num = (int)val;
        var fnum = (int)flag;
        if (isChecked) {
            num |= fnum;
        }
        else {
            num &= ~fnum;
        }

        _setAlignment((AnchorAlignment)num);
        _onChanged?.Invoke();
    }
}
