using Dalamud.Interface;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using StatusTimers.Config;
using StatusTimers.Windows;
using System;
using System.Drawing;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Factories;

public class ColorPickerTemp {
    public static VerticalListNode<NodeBase> CreateColorPickerSection(
        StatusTimerOverlayConfig? config,
        OverlayManager? overlayManager,
        FilterUIFactory.FilterChangedCallback? onChanged = null) {
        var section = new VerticalListNode<NodeBase> {
            IsVisible = true,
            Width = 600,
            Height = 600,
            ItemVerticalSpacing = 4,
            FitContents = true,
        };

        var currentColor = ColorHelper.GetColor(53);
        var test = KnownColor.DarkRed.Vector().AsVector3();

        var colorPreview = new SimpleImageNode {
            TexturePath = "ui/icon/090000/090449.tex",
            Color = currentColor,
            Width = 24,
            Height = 24,
            IsVisible = true,
        };

        section.AddNode(colorPreview);

        section.AddNode(new TextButtonNode {
            IsVisible = true,
            Label = "Change",
            Height = 28,
            Width = 32,
            OnClick = () => {
                GlobalServices.Framework.RunOnTick(() => {
                    overlayManager?.ColorPickerInstance?.Show(currentColor, newColor => {
                        colorPreview.Color = newColor;
                    });
                }, delayTicks: 3);
            }
        });
        /*
        var alpha = ConfigurationUIFactory.CreateSliderOption(
            0,
            255,
            1,
            () => (int)(currentColor.W * 255),
            value => {
                currentColor.W = value / 255f;
                colorPreview.Color = currentColor;
                onChanged?.Invoke();
            }
        );

        var red = ConfigurationUIFactory.CreateSliderOption(
            0,
            255,
            1,
            () => (int)(currentColor.X * 255),
            value => {
                currentColor.X = value / 255f;
                colorPreview.Color = currentColor;
                onChanged?.Invoke();
            }
        );

        var green = ConfigurationUIFactory.CreateSliderOption(
            0,
            255,
            1,
            () => (int)(currentColor.Y * 255),
            value => {
                currentColor.Y = value / 255f;
                colorPreview.Color = currentColor;
                onChanged?.Invoke();
            }
        );

        var blue = ConfigurationUIFactory.CreateSliderOption(
            0,
            255,
            1,
            () => (int)(currentColor.Z * 255),
            value => {
                currentColor.Z = value / 255f;
                colorPreview.Color = currentColor;
                onChanged?.Invoke();
            }
        );

        section.AddNode(colorPreview);
        section.AddNode(red);
        section.AddNode(green);
        section.AddNode(blue);
        section.AddNode(alpha);
        */

        return section;
    }
}
