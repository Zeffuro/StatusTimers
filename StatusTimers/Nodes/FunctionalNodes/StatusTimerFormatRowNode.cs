using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Enums;
using System;
using System.Linq;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class StatusTimerFormatRowNode : HorizontalListNode
{
    public StatusTimerFormatRowNode(
        Func<StatusTimerOverlayConfig> getConfig,
        Action<string>? onChanged = null) {
        X = 36;
        Width = 600 - 18;
        Height = 32;
        IsVisible = true;

        var textInput = new TextInputNode {
            String = getConfig().TimerFormat,
            Y = -3,
            Width = 180,
            Height = 32,
            OnInputReceived = value => {
                getConfig().TimerFormat = value.ToString();
            },
            IsVisible = true
        };

        var formatDropdown = new TextDropDownNode
        {
            Options = TimerFormats.Formats.Keys.ToList(),
            SelectedOption = TimerFormats.Formats.FirstOrDefault(kvp => kvp.Value == getConfig().TimerFormat).Key
                             ?? TimerFormats.Formats.Keys.First(),
            OnOptionSelected = selectedKey => {
                textInput.String = TimerFormats.Formats[selectedKey];
                getConfig().TimerFormat = TimerFormats.Formats[selectedKey];;
            },
            Width = 240,
            Height = 28,
            IsVisible = true
        };

        AddNode(new OptionLabelNode("Timer Format", false) {
            Width = 140
        });
        AddNode(formatDropdown);
        AddNode(textInput);
    }
}
