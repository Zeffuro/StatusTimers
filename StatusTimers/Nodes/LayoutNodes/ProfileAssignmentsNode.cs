using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using ClassJob = Lumina.Excel.Sheets.ClassJob;
using StatusTimers.Helpers;
using StatusTimers.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class ProfileAssignmentsNode : VerticalListNode {
    private readonly List<StringDropDownNode> _dropdowns = [];
    private readonly Action _onLayoutChanged;

    public ProfileAssignmentsNode(OverlayManager manager, Action onLayoutChanged) {
        _onLayoutChanged = onLayoutChanged;
        Width = 600;
        FitContents = true;
        ItemSpacing = 8;
        IsVisible = true;
        if (!manager.ProfilesAvailable) {
            AddNode(new TextNode {
                String = manager.ProfileError,
                Width = 600,
                Height = 80,
                FontSize = 14,
                IsVisible = true
            });
            return;
        }

        List<string> profiles = [string.Empty, ..manager.ProfileNames];
        var hudLayouts = new CollapsingHeaderNode {
            String = "HUD Layouts",
            Width = 600,
            IsCollapsed = false,
            ItemSpacing = 4,
            FirstItemSpacing = 4,
            IsVisible = true,
            OnToggle = _ => UpdateLayout()
        };
        List<NodeBase> hudNodes = [
            new CheckboxNode {
                X = 8,
                Width = 580,
                Height = 24,
                IsVisible = true,
                String = "Switch profiles when changing HUD layout",
                IsChecked = manager.AutoSwitchHudLayouts,
                OnClick = enabled => ImportExportResetHelper.Run(() => manager.SetAutoSwitchHudLayouts(enabled))
            },
            new TextNode {
                X = 8,
                String = "Job assignments take priority when both are enabled.",
                Width = 580,
                Height = 24,
                FontSize = 14,
                IsVisible = true
            }
        ];
        for (uint layout = 0; layout < 4; layout++) {
            uint layoutId = layout;
            var dropdown = new StringDropDownNode {
                GetLabelFunction = value => string.IsNullOrEmpty(value) ? "None" : value,
                Options = profiles,
                SelectedOption = manager.GetHudLayoutProfile(layoutId) ?? string.Empty,
                Width = 330,
                Height = 24,
                MaxListOptions = 8,
                IsVisible = true
            };
            dropdown.OnOptionSelected = value => {
                ImportExportResetHelper.Run(() => manager.AssignHudLayoutProfile(layoutId,
                    string.IsNullOrEmpty(value) ? null : value));
                dropdown.SelectedOption = manager.GetHudLayoutProfile(layoutId) ?? string.Empty;
            };
            _dropdowns.Add(dropdown);
            var row = new HorizontalListNode {
                X = 8,
                Width = 580,
                Height = 24,
                ItemSpacing = 6,
                IsVisible = true
            };
            row.AddNode([
                new TextNode {
                    String = $"HUD Layout {layout + 1}",
                    Width = 220,
                    Height = 24,
                    FontSize = 14,
                    IsVisible = true
                },
                dropdown
            ]);
            hudNodes.Add(row);
        }
        hudLayouts.AddNode(hudNodes);

        var jobProfiles = new CollapsingHeaderNode {
            String = "Job Profiles",
            Width = 600,
            IsCollapsed = false,
            ItemSpacing = 6,
            FirstItemSpacing = 4,
            IsVisible = true,
            OnToggle = _ => UpdateLayout()
        };
        List<NodeBase> jobNodes = [new CheckboxNode {
            X = 8,
            Width = 580,
            Height = 24,
            IsVisible = true,
            String = "Switch profiles when changing job",
            IsChecked = manager.AutoSwitchJobs,
            OnClick = enabled => ImportExportResetHelper.Run(() => manager.SetAutoSwitchJobs(enabled))
        }];
        var jobs = GlobalServices.DataManager.GetExcelSheet<ClassJob>()
            .Where(job => job.RowId != 0 && !string.IsNullOrWhiteSpace(job.Name.ToString()))
            .GroupBy(GetRole)
            .OrderBy(group => group.Key);
        string[] roles = ["Tank", "Healer", "Melee", "Ranged", "Caster", "Crafter", "Gatherer", "Other"];
        foreach (var group in jobs) {
            var role = new CollapsingHeaderNode {
                X = 8,
                String = roles[group.Key],
                Width = 584,
                IsCollapsed = false,
                ItemSpacing = 4,
                FirstItemSpacing = 4,
                IsVisible = true,
                OnToggle = _ => UpdateLayout()
            };
            List<NodeBase> rows = [];
            foreach (var job in group.OrderBy(job => job.RowId)) {
                var dropdown = new StringDropDownNode {
                    GetLabelFunction = value => string.IsNullOrEmpty(value) ? "None" : value,
                    Options = profiles,
                    SelectedOption = manager.GetJobProfile(job.RowId) ?? string.Empty,
                    Width = 330,
                    Height = 24,
                    MaxListOptions = 8,
                    IsVisible = true
                };
                dropdown.OnOptionSelected = value => {
                    ImportExportResetHelper.Run(() => manager.AssignJobProfile(job.RowId,
                        string.IsNullOrEmpty(value) ? null : value));
                    dropdown.SelectedOption = manager.GetJobProfile(job.RowId) ?? string.Empty;
                };
                _dropdowns.Add(dropdown);
                var name = job.Name.ToString();
                var row = new HorizontalListNode {
                    X = 8,
                    Width = 568,
                    Height = 24,
                    ItemSpacing = 6,
                    IsVisible = true
                };
                row.AddNode([
                    new IconImageNode {
                        IconId = 62000u + job.RowId,
                        Width = 24,
                        Height = 24,
                        FitTexture = true,
                        IsVisible = true
                    },
                    new TextNode {
                        String = char.ToUpper(name[0], CultureInfo.CurrentCulture) + name[1..],
                        Width = 182,
                        Height = 24,
                        FontSize = 14,
                        IsVisible = true
                    },
                    dropdown
                ]);
                rows.Add(row);
            }
            role.AddNode(rows);
            jobNodes.Add(role);
        }
        jobProfiles.AddNode(jobNodes);
        AddNode([
            new TextNode {
                String = $"Active profile: {manager.ActiveProfile}",
                Width = 600,
                Height = 24,
                FontSize = 14,
                IsVisible = true
            },
            hudLayouts,
            jobProfiles
        ]);
    }

    private void UpdateLayout() {
        foreach (var dropdown in _dropdowns) {
            dropdown.Collapse(false);
        }

        RecalculateLayout();
        _onLayoutChanged();
    }

    private static int GetRole(ClassJob job) => job.Role switch {
        1 => 0,
        4 => 1,
        2 => 2,
        3 => job.PrimaryStat == 4 ? 4 : 3,
        _ => job.ClassJobCategory.RowId switch { 33 => 5, 32 => 6, _ => 7 }
    };
}
