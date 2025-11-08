using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using System;
using System.Numerics;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Windows;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class ColorPreviewOptionNode : HorizontalFlexNode
{
    private readonly Func<Vector4> _getColor;
    private readonly Action<Vector4> _setColor;
    private readonly OverlayManager _overlayManager;
    private readonly Action? _onChanged;
    private readonly float _size;

    public ColorPreviewOptionNode(
        string labelText,
        Func<Vector4> getColor,
        Vector4 defaultColor,
        Action<Vector4> setColor,
        OverlayManager overlayManager,
        Action? onChanged = null,
        float size = 32f)
    {
        _getColor = getColor;
        _setColor = setColor;
        _overlayManager = overlayManager;
        _onChanged = onChanged;
        _size = size;

        IsVisible = true;
        X = 18;
        Width = 272;
        Height = 24;
        AlignmentFlags = FlexFlags.FitHeight;
        FitPadding = 4;

        AddNode(new TextNode
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
            String = labelText
        });

        var colorPreviewButton = new ColorPreviewButtonNode(GlobalServices.NativeController)
        {
            Y = -2,
            IsVisible = true,
            Size = new Vector2(_size),
            Color = _getColor(),
        };
        colorPreviewButton.OnClick = () =>
        {
            var startColor = _getColor();
            GlobalServices.Framework.RunOnTick(() =>
            {
                _overlayManager.ColorPickerInstance?.Show(startColor, defaultColor, newColor =>
                {
                    _setColor(newColor);
                    colorPreviewButton.Color = newColor;
                    _onChanged?.Invoke();
                });
            }, delayTicks: 3);
        };
        AddNode(colorPreviewButton);
    }
}
