using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using StatusTimers.Helpers;

namespace StatusTimers.Windows;

public class ConfigurationWindow : NativeAddon
{
    private readonly Dictionary<NodeKind, ScrollingAreaNode> _configScrollingAreas = new();
    private readonly Dictionary<NodeKind, TextButtonNode> _configTabButtons = new();
    private string _currentFilterInput = "10";
    
    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        ResNode tabBar = new ResNode
        {
            Position = ContentStartPosition,
            Width = ContentSize.X,
            Height = 28,
            IsVisible = true,
        };
        NativeController.AttachNode(tabBar, this);

        const int buttonHeight = 28;
        const int buttonSpacing = 0;
        NodeKind[] nodeKinds   = Enum.GetValues<NodeKind>();
        int buttonWidth = (int)MathF.Floor((ContentSize.X - buttonSpacing * (nodeKinds.Length - 1)) / (float)nodeKinds.Length);

        int x = 0;
        foreach (var (kind, _) in nodeKinds.Select((kind, index) => (kind, index)))
        {
            _configTabButtons[kind] = new TextButtonNode
            {
                X = x,
                Width = buttonWidth,
                Height = buttonHeight,
                IsVisible = true,
                Label = kind.ToString(),
                OnClick = () => OnTabButtonClick(kind),
            };
            NativeController.AttachNode(_configTabButtons[kind], tabBar);
            
            _configScrollingAreas[kind] = new ScrollingAreaNode
            {
                X = ContentStartPosition.X,
                Y = ContentStartPosition.Y + tabBar.Height,
                Width = ContentSize.X,
                Height = ContentSize.Y - tabBar.Height,
                ContentHeight = 2000.0f,
                IsVisible = false,
            };
            NativeController.AttachNode(_configScrollingAreas[kind], this);

            if (kind == NodeKind.Combined)
            {
                CheckboxNode dismissStatus = new CheckboxNode
                {
                    X = ContentStartPosition.X + 20,
                    Height = buttonHeight,
                    IsVisible = true,
                    LabelText = "Allow dismissing status by right-clicking the status icon.",
                };
                NativeController.AttachNode(dismissStatus, _configScrollingAreas[kind].ContentNode);
            }

            if (kind == NodeKind.MultiDoT)
            {
                CheckboxNode targetEnemy = new CheckboxNode
                {
                    X = ContentStartPosition.X + 20,
                    Height = buttonHeight,
                    IsVisible = true,
                    LabelText = "Allow targeting the enemy by clicking the status icon.",
                };
                NativeController.AttachNode(targetEnemy, _configScrollingAreas[kind].ContentNode);
            }

            TextInputNode filterListInput = new TextInputNode
            {
                Y = ContentStartPosition.Y,
                Height = buttonHeight,
                Size = new Vector2(300, 28),
                IsVisible = true,
                OnInputReceived = (input) =>
                {
                    _currentFilterInput = input.TextValue.ToString();
                    //filterDropdown.FilterFunction
                },
            };
            NativeController.AttachNode(filterListInput, _configScrollingAreas[kind].ContentNode);
            
            LuminaDropDownNode<Status> filterDropdown = new LuminaDropDownNode<Status>
            {
                Y = ContentStartPosition.Y + filterListInput.Height,
                Height = buttonHeight,
                Size = new Vector2(300, 28),
                IsVisible = true,
                OptionListHeight = 125.0f,
                
                /*FilterFunction = status => status.Icon != 0 && !status.Name.ExtractText().IsNullOrEmpty() &&
                                           (status.Name.ContainsText(Encoding.UTF8.GetBytes(_currentFilterInput)) || status.RowId.ToString().Contains(_currentFilterInput)),*/
                FilterFunction = status => status.RowId.ToString().Contains(_currentFilterInput),
                LabelFunction = status => $"{status.RowId} {status.Name.ExtractText()}"
            };
            NativeController.AttachNode(filterDropdown, _configScrollingAreas[kind].ContentNode);

            x += buttonWidth + buttonSpacing;
        }
        _configScrollingAreas.First().Value.IsVisible = true;
    }

    private void OnTabButtonClick(NodeKind kind)
    {
        _configTabButtons.Values.ToList().ForEach(button => button.SetChecked(false));
        _configTabButtons[kind].SetChecked(true);
        
        _configScrollingAreas.Values.ToList().ForEach(node => node.IsVisible = false);
        _configScrollingAreas[kind].IsVisible = true;
    }
    
    protected override unsafe void OnUpdate(AtkUnitBase* addon)
    {
    }
}

internal static unsafe class TextButtonNodeExtensions
{
    public static void SetChecked(this TextButtonNode button, bool isChecked)
    {
        var pointer = (AtkComponentButton*)button;
        pointer->SetChecked(isChecked);
    }
}