using Dalamud.Game.ClientState.Statuses;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.Nodes.TabBar;
using KamiToolKit.System;
using Lumina.Excel.Sheets;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class ConfigurationWindow(OverlayManager overlayManager) : NativeAddon {
    private const float OptionOffset = 18;
    private const float CheckBoxHeight = 16;
    private const float CheckBoxWidth = 300;
    private const float ButtonHeight = 30;

    private static readonly Dictionary<GrowDirection, string> GrowDirectionMap = new() {
        { GrowDirection.DownRight, "Down and Right" },
        { GrowDirection.DownLeft, "Down and Left" },
        { GrowDirection.UpRight, "Up and Right" },
        { GrowDirection.UpLeft, "Up and Left" }
    };

    private static readonly Dictionary<FontType, string> FontMap = new() {
        { FontType.Axis, "Axis" },
        { FontType.Miedinger, "Miedinger" },
        { FontType.TrumpGothic, "Trump Gothic" },
        { FontType.Jupiter, "Jupiter" },
        { FontType.JupiterLarge, "Jupiter Large" }
    };

    private readonly Dictionary<NodeKind, VerticalListNode<NodeBase>> _configLists = new();
    private readonly Dictionary<NodeKind, ScrollingAreaNode> _configScrollingAreas = new();
    private TabBarNode _tabBar;
    //private ModalNode _modal;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        //_modal = new ModalNode();
        //NativeController.AttachNode(_modal.RootNode, this);
        SetupOptions();
    }

    private void OnTabButtonClick(NodeKind kind) {
        foreach ((NodeKind k, ScrollingAreaNode node) in _configScrollingAreas) {
            node.IsVisible = k == kind;
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        Enum.GetValues(typeof(NodeKind)).Cast<NodeKind>().ToList()
            .ForEach(kind => {
                var overlay = GetOverlayByKind(kind);
                if (overlay != null) {
                    overlay.IsPreviewEnabled = false;
                    overlay.IsLocked = true;
                }
            });
    }

    private void SetupOptions() {
        _tabBar = new() {
            Position = ContentStartPosition,
            Size = ContentSize with { Y = 24 },
            Height = 24,
            IsVisible = true
        };

        NodeKind[] nodeKinds = Enum.GetValues<NodeKind>();

        foreach ((NodeKind kind, int _) in nodeKinds.Select((kind, index) => (kind, index))) {
            IOverlayConfiguration? currentOverlayConfig = GetOverlayByKind(kind);
            _tabBar.AddTab(kind.ToString(), () => OnTabButtonClick(kind));

            _configScrollingAreas[kind] = new ScrollingAreaNode {
                X = ContentStartPosition.X,
                Y = ContentStartPosition.Y + _tabBar.Height,
                Width = ContentSize.X,
                Height = ContentSize.Y - _tabBar.Height,
                ContentHeight = 2000.0f,
                IsVisible = false
            };
            NativeController.AttachNode(_configScrollingAreas[kind], this);

            _configLists[kind] = new VerticalListNode<NodeBase> {
                Height = 500,
                Width = _configScrollingAreas[kind].ContentNode.Width,
                IsVisible = true,
                ItemVerticalSpacing = 3
            };
            NativeController.AttachNode(_configLists[kind], _configScrollingAreas[kind].ContentNode);

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(CreateTwoOptionsRow(
                new CircleButtonNode {
                    Height = ButtonHeight,
                    Width = ButtonHeight,
                    IsVisible = true,
                    Tooltip = "Export Configuration",
                    Icon = ButtonIcon.Document,
                    AddColor = new Vector3(150, 0, 150),
                    OnClick = () => {/*
                        _modal.Show("Yessss", (chosen) => {
                            GlobalServices.Logger.Info($"Chosen {chosen}");
                        });
                        GlobalServices.Logger.Info("Export");*/
                    }
                },
                new CircleButtonNode {
                    X = 52,
                    Height = ButtonHeight,
                    Width = ButtonHeight,
                    IsVisible = true,
                    Tooltip = "Import Configuration",
                    Icon = ButtonIcon.Document,
                    AddColor = new Vector3(0, 150, 150),
                    OnClick = () => {
                        GlobalServices.Logger.Info("Import");
                    }
                },
                ButtonHeight
                ));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = TextStyles.Header.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.Header.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Visual Settings"
            });

            _configLists[kind].AddNode(CreateTwoOptionsRow(
                CreateCheckboxOption("Enabled",
                    () => currentOverlayConfig.IsVisible,
                    isChecked => currentOverlayConfig.IsVisible = isChecked),
                CreateCheckboxOption("Preview Mode",
                    () => currentOverlayConfig.IsPreviewEnabled,
                    isChecked => currentOverlayConfig.IsPreviewEnabled = isChecked),
                CheckBoxHeight)
            );
            _configLists[kind].AddNode(CreateCheckboxOption("Locked",
                () => currentOverlayConfig.IsLocked,
                isChecked => currentOverlayConfig.IsLocked = isChecked));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(CreateTwoOptionsRow(new TextNode {
                        IsVisible = true,
                        X = OptionOffset,
                        Width = 120,
                        Height = TextStyles.OptionLabel.Height,
                        FontSize = TextStyles.Defaults.FontSize,
                        TextColor = TextStyles.OptionLabel.TextColor,
                        TextOutlineColor = TextStyles.Defaults.OutlineColor,
                        TextFlags = TextStyles.Defaults.Flags,
                        Text = "Scale"
                    },
                    CreateLabeledNumericOption("Horizontal Padding",
                        () => currentOverlayConfig.StatusHorizontalPadding,
                        value => currentOverlayConfig.StatusHorizontalPadding = value),
                    CheckBoxHeight
                )
            );

            _configLists[kind].AddNode();

            _configLists[kind].AddNode(CreateTwoOptionsRow(
                CreateSliderOption(
                    5,
                    200,
                    5,
                    () => currentOverlayConfig.ScaleInt,
                    value => currentOverlayConfig.ScaleInt = value
                ),
                CreateLabeledNumericOption("Vertical Padding",
                    () => currentOverlayConfig.StatusVerticalPadding,
                    value => currentOverlayConfig.StatusVerticalPadding = value),
                30)
            );

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            TextNode statusPerLineNode = new() {
                X = OptionOffset,
                IsVisible = true,
                Width = 300,
                Height = TextStyles.OptionLabel.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.OptionLabel.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = $"Statuses per {(currentOverlayConfig.FillRowsFirst ? "row" : "column")}"
            };

            _configLists[kind].AddNode(CreateTwoOptionsRow(
                    statusPerLineNode,
                    new TextNode {
                        X = OptionOffset,
                        IsVisible = true,
                        Width = 300,
                        Height = TextStyles.OptionLabel.Height,
                        FontSize = TextStyles.Defaults.FontSize,
                        TextColor = TextStyles.OptionLabel.TextColor,
                        TextOutlineColor = TextStyles.Defaults.OutlineColor,
                        TextFlags = TextStyles.Defaults.Flags,
                        Text = "Max statuses displayed"
                    },
                    TextStyles.OptionLabel.Height
                )
            );

            _configLists[kind].AddNode(CreateTwoOptionsRow(
                    CreateSliderOption(
                        1,
                        30,
                        1,
                        () => currentOverlayConfig.ItemsPerLine,
                        value => currentOverlayConfig.ItemsPerLine = value
                    ),
                    CreateSliderOption(
                        1,
                        30,
                        1,
                        () => currentOverlayConfig.MaxStatuses,
                        value => currentOverlayConfig.MaxStatuses = value
                    ),
                    30
                )
            );

            _configLists[kind].AddNode(CreateTwoOptionsRow(
                new ResNode(),
                CreateLabeledDropdown(
                    "Grow direction",
                    () => currentOverlayConfig.GrowDirection,
                    value => currentOverlayConfig.GrowDirection = value,
                    GrowDirectionMap
                ),
                CheckBoxHeight
            ));

            _configLists[kind].AddNode(CreateCheckboxOption("Fill columns first",
                () => !currentOverlayConfig.FillRowsFirst,
                isChecked => {
                    currentOverlayConfig.FillRowsFirst = !isChecked;
                    statusPerLineNode.Text = $"Statuses per {(currentOverlayConfig.FillRowsFirst ? "row" : "column")}";
                }));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(CreateCheckboxOption("Show status name",
                () => currentOverlayConfig.ShowStatusName,
                isChecked => currentOverlayConfig.ShowStatusName = isChecked));

            _configLists[kind].AddNode(CreateTwoOptionsRow(
                CreateCheckboxOption("Show time remaining",
                    () => currentOverlayConfig.ShowStatusRemaining,
                    isChecked => currentOverlayConfig.ShowStatusRemaining = isChecked),
                CreateLabeledDropdown(
                    "Font",
                    () => currentOverlayConfig.StatusRemainingTextStyle.FontType,
                    value => currentOverlayConfig.StatusRemainingTextStyle.FontType = value,
                    FontMap
                ),
                CheckBoxHeight
            ));

            _configLists[kind].AddNode(CreateCheckboxOption("Show time remaining background",
                () => currentOverlayConfig.ShowStatusRemainingBackground,
                isChecked => currentOverlayConfig.ShowStatusRemainingBackground = isChecked));

            _configLists[kind].AddNode(CreateCheckboxOption("Show progressbar",
                () => currentOverlayConfig.ShowProgress,
                isChecked => currentOverlayConfig.ShowProgress = isChecked));

            _configLists[kind].AddNode(CreateCheckboxOption("Animations Enabled",
                () => currentOverlayConfig.AnimationsEnabled,
                isChecked => currentOverlayConfig.AnimationsEnabled = isChecked));


            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

                _configLists[kind].AddNode(CreateCheckboxOption("Show enemy name",
                    () => currentOverlayConfig.ShowActorName,
                    isChecked => currentOverlayConfig.ShowActorName = isChecked));

                _configLists[kind].AddNode(CreateCheckboxOption("Show enemy letter",
                    () => currentOverlayConfig.ShowActorLetter,
                    isChecked => currentOverlayConfig.ShowActorLetter = isChecked));
            }

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = TextStyles.Header.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.Header.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Functional Settings"
            });

            if (kind == NodeKind.Combined) {
                _configLists[kind].AddNode(CreateCheckboxOption("Hide permanent statuses",
                    () => !currentOverlayConfig.ShowPermaIcons,
                    isChecked => currentOverlayConfig.ShowPermaIcons = !isChecked));

                _configLists[kind].AddNode(CreateCheckboxOption(
                    "Allow dismissing status by right-clicking the status icon.",
                    () => currentOverlayConfig.AllowDismissStatus,
                    isChecked => currentOverlayConfig.AllowDismissStatus = isChecked));
            }

            if (kind == NodeKind.MultiDoT) {
                _configLists[kind].AddNode(CreateCheckboxOption(
                    "Allow targeting the enemy by clicking the status icon.",
                    () => currentOverlayConfig.AllowTargetActor,
                    isChecked => currentOverlayConfig.AllowTargetActor = isChecked));
            }

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight);

            _configLists[kind].AddNode(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = TextStyles.Header.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.Header.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Sorting Priority"
            });

            Dictionary<SortCriterion, string> sortCriteriaMap = new() {
                { SortCriterion.None, "None" },
                { SortCriterion.StatusType, "Status Type" },
                { SortCriterion.TimeRemaining, "Time Remaining" },
                { SortCriterion.OwnStatusFirst, "Own Status First" },
                { SortCriterion.PartyPriority, "Party Priority" }
            };

            if (kind == NodeKind.MultiDoT) {
                sortCriteriaMap.Add(SortCriterion.EnemyLetter, "Enemy Letter");
            }

            _configLists[kind].AddNode(CreateSortPriorityRow(
                "Primary:",
                () => currentOverlayConfig.PrimarySort,
                value => currentOverlayConfig.PrimarySort = value,
                () => currentOverlayConfig.PrimarySortOrder,
                value => currentOverlayConfig.PrimarySortOrder = value,
                sortCriteriaMap
            ));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight * 5);

            _configLists[kind].AddNode(CreateSortPriorityRow(
                "Secondary:",
                () => currentOverlayConfig.SecondarySort,
                value => currentOverlayConfig.SecondarySort = value,
                () => currentOverlayConfig.SecondarySortOrder,
                value => currentOverlayConfig.SecondarySortOrder = value,
                sortCriteriaMap
            ));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight * 5);

            _configLists[kind].AddNode(CreateSortPriorityRow(
                "Tertiary:",
                () => currentOverlayConfig.TertiarySort,
                value => currentOverlayConfig.TertiarySort = value,
                () => currentOverlayConfig.TertiarySortOrder,
                value => currentOverlayConfig.TertiarySortOrder = value,
                sortCriteriaMap
            ));

            _configLists[kind].AddDummy(new ResNode(), CheckBoxHeight * 5);

            _configLists[kind].AddNode(new TextNode {
                IsVisible = true,
                Width = 120,
                Height = TextStyles.Header.Height,
                FontSize = TextStyles.Defaults.FontSize,
                TextColor = TextStyles.Header.TextColor,
                TextOutlineColor = TextStyles.Defaults.OutlineColor,
                TextFlags = TextStyles.Defaults.Flags,
                Text = "Filter Settings"
            });

            var filterSectionNode = new VerticalListNode<NodeBase> {
                IsVisible = true,
                Width = 600,
                Height = 400,
                ItemVerticalSpacing = 4
            };
            _configLists[kind].AddNode(filterSectionNode);

            List<LuminaStatus> allStatusList = GlobalServices.DataManager.GetExcelSheet<LuminaStatus>()
                .Where(status => status.RowId != 0).ToList();

            RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
        }

        NativeController.AttachNode(_tabBar, this);
        _configScrollingAreas.First().Value.IsVisible = true;
    }

    #region Helper Methods

    private CheckboxNode CreateCheckboxOption(string label, Func<bool> getter, Action<bool> setter) {
        CheckboxNode checkboxNode = new() {
            X = OptionOffset,
            Width = CheckBoxWidth,
            Height = CheckBoxHeight,
            IsVisible = true,
            LabelText = label,
            IsChecked = getter(),
            OnClick = setter
        };
        return checkboxNode;
    }

    private HorizontalFlexNode<NodeBase> CreateTwoOptionsRow(NodeBase first, NodeBase second, float height) {
        HorizontalFlexNode<NodeBase> flexNode = new() {
            IsVisible = true,
            X = OptionOffset,
            Width = 600,
            Height = height,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };
        flexNode.AddNode(first);
        flexNode.AddNode(second);
        return flexNode;
    }

    private HorizontalFlexNode<NodeBase> CreateLabeledDropdown<TEnum>(
        string labelText,
        Func<TEnum> getter,
        Action<TEnum> setter,
        IReadOnlyDictionary<TEnum, string> enumToDisplayNameMap
    )
        where TEnum : Enum {
        HorizontalFlexNode<NodeBase> flexNode = new() {
            IsVisible = true,
            X = OptionOffset,
            Width = 280,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };

        TextNode labelNode = new() {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 20,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            Text = labelText
        };
        flexNode.AddNode(labelNode);

        TextDropDownNode dropdownNode = new() {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 140,
            Height = 24,
            MaxListOptions = 5,
            Options = enumToDisplayNameMap.Values.ToList(),
            OnOptionSelected = selectedDisplayName => {
                TEnum selectedEnum = enumToDisplayNameMap
                    .FirstOrDefault(pair => pair.Value == selectedDisplayName).Key;
                setter(selectedEnum);
            },
            SelectedOption = enumToDisplayNameMap[getter()]
        };
        flexNode.AddNode(dropdownNode);
        return flexNode;
    }

    private HorizontalListNode<NodeBase> CreateFilteredDropdown<T>(
        Func<List<T>> optionsProvider,
        Func<T, string> displaySelector,
        Func<T, uint>? iconSelector,
        Action<T?> setter,
        bool allowNoResult = true
    ) {
        HorizontalListNode<NodeBase> flexNode = new() {
            IsVisible = true,
            X = OptionOffset,
            Width = 600,
            Height = 60,
        };

        TextDropDownNode? textDropDown = null;
        IconImageNode? iconNode = null;
        List<T> lastFiltered = new();
        T? currentSelection = default;

        TextInputNode textInput = new() {
            IsVisible = true,
            Width = 200,
            Height = 28,
        };

        TextButtonNode textButton = new() {
            IsVisible = true,
            Y = 2,
            Height = 28,
            Width = 32,
            Label = "+",
            OnClick = () => setter(currentSelection)
        };

        void ReplaceDropDown(List<T> filtered) {
            lastFiltered = filtered;
            if (textDropDown != null) {
                flexNode.RemoveNode(textDropDown);
            }
            if (textButton != null) {
                flexNode.RemoveNode(textButton);
            }

            // Always have at least one option for the dropdown to avoid issues
            List<string> displayOptions;
            if (filtered.Count == 0 && allowNoResult) {
                displayOptions = new List<string> { "No results found" };
            } else {
                displayOptions = filtered.Select(displaySelector).ToList();
            }

            textDropDown = new TextDropDownNode {
                IsVisible = true,
                Y = 2,
                Width = 200,
                Height = 28,
                MaxListOptions = 5,
                Options = displayOptions,
                OnOptionSelected = s => {
                    SetIcon(s, filtered);
                    if (s == "No results found") {
                        return;
                    }
                },
                SelectedOption = displayOptions.FirstOrDefault() ?? "No results found",
            };
            flexNode.AddNode(textDropDown);
            flexNode.AddNode(textButton);

            if (displayOptions.Count > 0 && displayOptions[0] != "No results found") {
                SetIcon(displayOptions[0], filtered);
            }
            else {
                SetIcon("", filtered);
            }
        }

        void SetIcon(string selectedOption, List<T> filtered) {
            var idx = filtered.FindIndex(opt => displaySelector(opt) == selectedOption);
            currentSelection = idx >= 0 ? filtered[idx] : default;
            if (iconNode != null && currentSelection != null && iconSelector != null) {
                iconNode.IconId = iconSelector(currentSelection);
            }
            else {
                iconNode.IconId = 230402;
            }
        }

        void UpdateDropdownOptions(string filter) {
            var allOptions = optionsProvider();
            var filtered = allOptions
                .Where(opt =>
                    string.IsNullOrEmpty(filter) ||
                    (opt is LuminaStatus s &&
                     (s.RowId.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                      s.Name.ExtractText()?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true)
                    )
                )
                .ToList();

            ReplaceDropDown(filtered);
        }

        textInput.OnInputComplete = input => {
            string filter = input.TextValue ?? "";
            GlobalServices.Framework.RunOnTick(() => UpdateDropdownOptions(filter), delayTicks: 1);
        };

        flexNode.AddNode(textInput);
        flexNode.AddDummy(new ResNode(), 120);
        if (iconSelector != null) {
            iconNode = new IconImageNode {
                Size = new Vector2(24, 32),
                IsVisible = true,
                IconId = 0
            };
            flexNode.AddNode(iconNode);
        }
        UpdateDropdownOptions(string.Empty);

        return flexNode;
    }

    private HorizontalFlexNode<NodeBase> CreateLabeledNumericOption(string labelText, Func<int> getter,
        Action<int> setter) {
        HorizontalFlexNode<NodeBase> flexNode = new() {
            IsVisible = true,
            X = OptionOffset,
            Width = 300,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };

        TextNode labelNode = new() {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 150,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            Text = labelText
        };
        flexNode.AddNode(labelNode);

        NumericInputNode numericInput = new() {
            X = OptionOffset,
            Width = 120,
            Height = CheckBoxHeight,
            IsVisible = true,
            Value = getter(),
            OnValueUpdate = setter
        };
        flexNode.AddNode(numericInput);
        return flexNode;
    }

    private SliderNode CreateSliderOption(int min, int max, int step, Func<int> getter, Action<int> setter) {
        SliderNode sliderNode = new() {
            X = OptionOffset,
            Width = 280,
            Height = 30,
            IsVisible = true,
            Min = min,
            Max = max,
            Step = step,
            Value = getter(),
            OnValueChanged = setter
        };
        return sliderNode;
    }

    private HorizontalFlexNode<NodeBase> CreateSortPriorityRow(
        string labelText,
        Func<SortCriterion> criterionGetter,
        Action<SortCriterion> criterionSetter,
        Func<SortOrder> orderGetter,
        Action<SortOrder> orderSetter,
        Dictionary<SortCriterion, string> availableCriteriaMap
    ) {
        HorizontalFlexNode<NodeBase> flexNode = new() {
            IsVisible = true,
            X = OptionOffset,
            Width = 600,
            Height = 24,
            AlignmentFlags = FlexFlags.FitHeight,
            FitPadding = 4
        };

        TextNode labelNode = new() {
            IsVisible = true,
            Width = 60,
            Height = TextStyles.OptionLabel.Height,
            FontSize = TextStyles.Defaults.FontSize,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            TextFlags = TextStyles.Defaults.Flags,
            Text = labelText
        };
        flexNode.AddNode(labelNode);

        TextDropDownNode criterionDropdown = new() {
            IsVisible = true,
            Width = 200,
            Height = 24,
            MaxListOptions = 5,
            Options = availableCriteriaMap.Values.ToList(),
            SelectedOption = availableCriteriaMap.TryGetValue(criterionGetter(), out string? selectedCriterion)
                ? selectedCriterion
                : availableCriteriaMap.Values.First(),
            OnOptionSelected = selectedDisplayName => {
                SortCriterion selected = availableCriteriaMap.FirstOrDefault(p => p.Value == selectedDisplayName).Key;
                criterionSetter(selected);
            }
        };
        flexNode.AddNode(criterionDropdown);

        Dictionary<SortOrder, string> orderMap = new() {
            { SortOrder.Ascending, "Ascending" },
            { SortOrder.Descending, "Descending" }
        };

        TextDropDownNode orderDropdown = new() {
            IsVisible = true,
            Width = 180,
            Height = 24,
            MaxListOptions = 2,
            Options = orderMap.Values.ToList(),
            SelectedOption = orderMap.TryGetValue(orderGetter(), out string? selectedOrder)
                ? selectedOrder
                : orderMap.Values.First(),
            OnOptionSelected = selectedDisplayName => {
                SortOrder selected = orderMap.FirstOrDefault(p => p.Value == selectedDisplayName).Key;
                orderSetter(selected);
            }
        };
        flexNode.AddNode(orderDropdown);

        return flexNode;
    }

    private VerticalListNode<NodeBase> CreateStatusListNode(
        HashSet<uint> filterList,
        List<LuminaStatus> allStatuses,
        Action<uint> onRemove = null
    ) {
        var statusListNode = new VerticalListNode<NodeBase> {
            Height = 200,
            Width = 300,
            IsVisible = true,
            ItemVerticalSpacing = 4
        };

        foreach (var rowId in filterList.ToList().Order()) {
            var status = allStatuses.FirstOrDefault(s => s.RowId == rowId);

            var row = new HorizontalListNode<NodeBase> {
                X = OptionOffset,
                Height = 32,
                Width = 300,
                IsVisible = true,
                ItemHorizontalSpacing = 4,
            };

            row.AddNode(new IconImageNode {
                Y = -4,
                Size = new Vector2(24, 32),
                IsVisible = true,
                IconId = status.Icon
            });

            row.AddNode(new TextNode {
                Text = status.RowId.ToString(),
                IsVisible = true,
                Height = 24,
                Width = 40,
                AlignmentType = AlignmentType.Right
            });

            row.AddNode(new TextNode {
                Text = status.Name.ExtractText(),
                IsVisible = true,
                Height = 24,
                Width = 180,
                AlignmentType = AlignmentType.Left
            });

            row.AddNode(new TextButtonNode {
                Label = "-",
                Width = 32,
                Height = 28,
                IsVisible = true,
                OnClick = () => {
                    onRemove?.Invoke(rowId);
                }
            });

            statusListNode.AddNode(row);
        }

        return statusListNode;
    }

    private void RebuildFilterSection(IOverlayConfiguration currentOverlayConfig,
        VerticalListNode<NodeBase> filterSectionNode,
        List<LuminaStatus> allStatusList
    ) {
        filterSectionNode.Clear();

        filterSectionNode.AddNode(CreateCheckboxOption(
            "Enabled",
            () => currentOverlayConfig.FilterEnabled,
            isChecked => {
                currentOverlayConfig.FilterEnabled = isChecked;
                RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
            }));

        filterSectionNode.AddDummy(new ResNode(), CheckBoxHeight);

        if (currentOverlayConfig.FilterEnabled) {
            RadioButtonGroupNode radioButtonGroup = new() {
                X = OptionOffset,
                Width = 200,
                Height = TextStyles.Header.Height,
                IsVisible = true,
            };
            if (currentOverlayConfig.FilterIsBlacklist) {
                radioButtonGroup.AddButton("Blacklist", () => {
                    currentOverlayConfig.FilterIsBlacklist = true;
                    RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
                });
                radioButtonGroup.AddButton("Whitelist", () => {
                    currentOverlayConfig.FilterIsBlacklist = false;
                    RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
                });
            }
            else {
                radioButtonGroup.AddButton("Whitelist", () => {
                    currentOverlayConfig.FilterIsBlacklist = false;
                    RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
                });
                radioButtonGroup.AddButton("Blacklist", () => {
                    currentOverlayConfig.FilterIsBlacklist = true;
                    RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
                });
            }
            filterSectionNode.AddNode(radioButtonGroup);

            var filteredDropdownNode = CreateFilteredDropdown(
                optionsProvider: () => allStatusList,
                displaySelector: status => $"{status.RowId} {status.Name.ExtractText()}",
                iconSelector: status => status.Icon,
                setter: val => {
                    if (currentOverlayConfig.FilterList.Add(val.RowId)) {
                        RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
                    }
                });

            filterSectionNode.AddNode(filteredDropdownNode);

            var statusListNode = CreateStatusListNode(
                currentOverlayConfig.FilterList,
                allStatusList,
                onRemove: rowId => {
                    if (currentOverlayConfig.FilterList.Remove(rowId)) {
                        RebuildFilterSection(currentOverlayConfig, filterSectionNode, allStatusList);
                    }
                }
            );
            filterSectionNode.AddNode(statusListNode);
        }
    }

    private IOverlayConfiguration? GetOverlayByKind(NodeKind kind) {
        return kind switch {
            NodeKind.Combined => overlayManager.PlayerCombinedOverlayInstance,
            NodeKind.MultiDoT => overlayManager.EnemyMultiDoTOverlayInstance,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
                "The provided NodeKind is not supported by GetOverlayByKind.")
        };
    }

    #endregion
}
