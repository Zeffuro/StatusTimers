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

namespace StatusTimers.Factories;

public static class NodeLayoutUIFactory
{
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;
    public static VerticalListNode<NodeBase> CreateNodeLayoutSection(
        string label,
        StatusNodeAnchorConfig anchorConfig,
        Func<bool> getEnabled = null,
        Action<bool> setEnabled = null,
        Func<TextStyle> getStyle = null,
        Action<TextStyle> setStyle = null,
        Action onChanged = null)
    {
        var section = new VerticalListNode<NodeBase>
        {
            X = OptionOffset,
            Height = 200,
            Width = 600,
            ItemVerticalSpacing = 0,
            IsVisible = true
        };

        if (getEnabled != null && setEnabled != null)
        {
            section.AddNode(new CheckboxNode
            {
                LabelText = $"Show {label}",
                Width = 180,
                Height = 22,
                IsVisible = true,
                IsChecked = getEnabled(),
                OnClick = isChecked => { setEnabled(isChecked); onChanged?.Invoke(); }
            });
        }

        if (getStyle != null && setStyle != null)
        {
            var style = getStyle();
            var styleRow = new HorizontalFlexNode<NodeBase>
            {
                X = OptionOffset,
                IsVisible = true,
                Width = 562,
                Height = 32,
                FitPadding = 4
            };

            styleRow.AddNode(ConfigurationUIFactory.CreateLabeledDropdown(
                "Font",
                () => style.FontType,
                v => { var s = getStyle(); s.FontType = v; setStyle(s); onChanged?.Invoke(); },
                ConfigurationUIFactory.FontMap
            ));

            styleRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption(
                "Font Size",
                () => (int)style.FontSize,
                v => { var s = getStyle(); s.FontSize = v; setStyle(s); onChanged?.Invoke(); }
            ));
            section.AddNode(styleRow);
        }

        var offsetRow = new HorizontalFlexNode<NodeBase>
        {
            X = OptionOffset,
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };
        offsetRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Offset X",
            () => (int)anchorConfig.OffsetX,
            v => { anchorConfig.OffsetX = v; onChanged?.Invoke(); }));
        offsetRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Offset Y",
            () => (int)anchorConfig.OffsetY,
            v => { anchorConfig.OffsetY = v; onChanged?.Invoke(); }));
        section.AddNode(offsetRow);

        var sizeRow = new HorizontalFlexNode<NodeBase>
        {
            X = OptionOffset,
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 4
        };
        sizeRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Width",
            () => (int)anchorConfig.Width,
            v => { anchorConfig.Width = v; onChanged?.Invoke(); }));
        sizeRow.AddNode(ConfigurationUIFactory.CreateLabeledNumericOption("Height",
            () => (int)anchorConfig.Height,
            v => { anchorConfig.Height = v; onChanged?.Invoke(); }));
        section.AddNode(sizeRow);

        var anchorTargetDict = ((AnchorTarget[])Enum.GetValues(typeof(AnchorTarget)))
            .ToDictionary(a => a, a => a.ToString());
        var anchorToRow = new HorizontalFlexNode<NodeBase>
        {
            X = OptionOffset,
            IsVisible = true,
            Width = 562,
            Height = 32,
            FitPadding = 8
        };
        anchorToRow.AddNode(ConfigurationUIFactory.CreateLabeledDropdown("Anchor",
            () => anchorConfig.AnchorTo,
            v => { anchorConfig.AnchorTo = v; onChanged?.Invoke(); },
            anchorTargetDict));
        section.AddNode(anchorToRow);

        section.AddNode(CreateEnumFlagCheckboxGrid(
            "Alignment",
            () => anchorConfig.Alignment,
            v => { anchorConfig.Alignment = v; onChanged?.Invoke(); },
            columns: 4,
            onChanged: onChanged
        ));

        return section;
    }

    private static VerticalListNode<NodeBase> CreateEnumFlagCheckboxGrid<TEnum>(
        string label,
        Func<TEnum> get,
        Action<TEnum> set,
        int columns = 4,
        Action onChanged = null
    ) where TEnum : Enum
    {
        var allFlags = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Where(v => Convert.ToInt32(v) != 0).ToArray();
        int numRows = (int)Math.Ceiling((double)allFlags.Length / columns);
        float height = numRows * CheckBoxHeight;

        var node = new VerticalListNode<NodeBase>
        {
            X = OptionOffset,
            Height = height,
            IsVisible = true,
            ItemVerticalSpacing = 2
        };
        if (!string.IsNullOrEmpty(label))
        {
            node.AddNode(new TextNode
            {
                Text = label,
                IsVisible = true,
                Height = 18,
                Width = 120,
                FontSize = 14,
                TextColor = TextStyles.OptionLabel.TextColor
            });
        }

        for (int i = 0; i < allFlags.Length; i += columns)
        {
            var row = new HorizontalFlexNode<NodeBase>
            {
                X = OptionOffset,
                Height = 28,
                Width = 600,
                IsVisible = true,
                FitPadding = 8
            };

            for (int j = 0; j < columns && i + j < allFlags.Length; j++)
            {
                var flag = allFlags[i + j];
                row.AddNode(new CheckboxNode
                {
                    LabelText = flag.ToString(),
                    Width = 110,
                    Height = 24,
                    IsVisible = true,
                    IsChecked = get().HasFlag(flag),
                    OnClick = isChecked =>
                    {
                        var val = get();
                        var num = Convert.ToInt32(val);
                        var fnum = Convert.ToInt32(flag);
                        if (isChecked) {
                            num |= fnum;
                        }
                        else {
                            num &= ~fnum;
                        }

                        set((TEnum)Enum.ToObject(typeof(TEnum), num));
                        onChanged?.Invoke();
                    }
                });
            }
            node.AddNode(row);
        }
        return node;
    }
}
