using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Config;
using StatusTimers.Helpers;
using StatusTimers.Nodes.FunctionalNodes;
using StatusTimers.Windows;
using System.Linq;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Nodes.LayoutNodes;

public sealed class ProfileSectionNode : VerticalListNode {
    public ProfileSectionNode(OverlayManager manager) {
        Width = 600;
        FitContents = true;
        ItemSpacing = 6;
        IsVisible = true;

        var activeProfileLabel = new TextNode {
            String = $"Active profile: {manager.ActiveProfile}",
            Width = 600,
            Height = 24,
            FontSize = 14,
            IsVisible = true
        };
        if (!manager.ProfilesAvailable) {
            AddNode([
                activeProfileLabel,
                new TextNode {
                    String = manager.ProfileError,
                    Width = 600,
                    Height = 80,
                    FontSize = 14,
                    IsVisible = true
                }
            ]);
            return;
        }

        var names = manager.ProfileNames.ToList();
        var selected = manager.ActiveProfile;
        var profilesRow = new HorizontalListNode {
            Width = 600,
            Height = 28,
            ItemSpacing = 6,
            IsVisible = true
        };
        profilesRow.AddNode([
            new StringDropDownNode {
                Options = names,
                SelectedOption = selected,
                OnOptionSelected = value => selected = value,
                Width = 260,
                Height = 24,
                Y = 2,
                MaxListOptions = 8,
                IsVisible = true
            },
            new TextButtonNode {
                String = "Load",
                Width = 92,
                Height = 28,
                IsVisible = true,
                LabelNode = { TextColor = ColorHelper.GetColor(50) },
                OnClick = () => ImportExportResetHelper.Run(() => manager.SwitchProfile(selected))
            },
            new TextButtonNode {
                String = "Delete",
                Width = 92,
                Height = 28,
                IsVisible = true,
                LabelNode = { TextColor = ColorHelper.GetColor(50) },
                TextTooltip = "Delete profile\n(hold shift to confirm)",
                OnClick = () => {
                    if (GlobalServices.KeyState[VirtualKey.SHIFT]) {
                        ImportExportResetHelper.Run(() => manager.DeleteProfile(selected));
                    }
                }
            }
        ]);

        var nameInput = new TextInputNode {
            String = string.Empty,
            Width = 260,
            Height = 28,
            IsVisible = true,
            MaxCharacters = 64
        };
        var nameRow = new HorizontalListNode {
            Width = 600,
            Height = 28,
            ItemSpacing = 6,
            IsVisible = true
        };
        nameRow.AddNode([
            nameInput,
            new TextButtonNode {
                String = "Save As",
                Width = 92,
                Height = 28,
                IsVisible = true,
                LabelNode = { TextColor = ColorHelper.GetColor(50) },
                OnClick = () => ImportExportResetHelper.Run(() => manager.SaveProfile(nameInput.String.ToString()))
            },
            new TextButtonNode {
                String = "Rename",
                Width = 92,
                Height = 28,
                IsVisible = true,
                LabelNode = { TextColor = ColorHelper.GetColor(50) },
                OnClick = () => ImportExportResetHelper.Run(() => manager.RenameProfile(selected, nameInput.String.ToString()))
            }
        ]);

        var preset = BuiltInPresetCatalog.All[0];
        var description = new TextNode {
            String = preset.Description,
            Width = 600,
            Height = 24,
            FontSize = 14,
            IsVisible = true
        };
        var presetsRow = new HorizontalListNode {
            Width = 600,
            Height = 28,
            ItemSpacing = 6,
            IsVisible = true
        };
        presetsRow.AddNode([
            new StringDropDownNode {
                Options = BuiltInPresetCatalog.All.Select(item => item.Name).ToList(),
                SelectedOption = preset.Name,
                OnOptionSelected = value => {
                    preset = BuiltInPresetCatalog.All.First(item => item.Name == value);
                    description.String = preset.Description;
                },
                Width = 260,
                Height = 24,
                Y = 2,
                MaxListOptions = 8,
                IsVisible = true
            },
            new TextButtonNode {
                String = "Create Profile",
                Width = 140,
                Height = 28,
                IsVisible = true,
                LabelNode = { TextColor = ColorHelper.GetColor(50) },
                TextTooltip = "Creates a new profile using this preset. Keeps filters.",
                OnClick = () => ImportExportResetHelper.Run(() => manager.ApplyPreset(preset.Id))
            }
        ]);

        var importExportRow = new HorizontalListNode {
            Width = 600,
            Height = 28,
            ItemSpacing = 6,
            IsVisible = true
        };
        importExportRow.AddNode([
            new TextButtonNode {
                String = "Export Profile",
                Width = 140,
                Height = 28,
                IsVisible = true,
                LabelNode = { TextColor = ColorHelper.GetColor(50) },
                OnClick = () => ImportExportResetHelper.Run(() => {
                    ImGui.SetClipboardText(manager.ExportProfile());
                    return "Profile exported to clipboard.";
                })
            },
            new TextButtonNode {
                String = "Import Profile",
                Width = 140,
                Height = 28,
                IsVisible = true,
                LabelNode = { TextColor = ColorHelper.GetColor(50) },
                TextTooltip = "Import profile from clipboard\n(hold shift to confirm)",
                OnClick = () => {
                    if (GlobalServices.KeyState[VirtualKey.SHIFT]) {
                        ImportExportResetHelper.Run(() => manager.ImportProfile(ImGui.GetClipboardText()));
                    }
                }
            }
        ]);

        AddNode([
            activeProfileLabel,
            new TextNode {
                String = "Changes are saved automatically.",
                Width = 600,
                Height = 24,
                FontSize = 14,
                IsVisible = true
            },
            profilesRow,
            new TextNode {
                String = "Profile name",
                Width = 600,
                Height = 24,
                FontSize = 14,
                IsVisible = true
            },
            nameRow,
            new SectionHeaderNode("Presets"),
            presetsRow,
            description,
            new SectionHeaderNode("Import / Export"),
            importExportRow,
            new TextNode {
                String = "Older exports can be imported in the overlay tabs.",
                Width = 600,
                Height = 24,
                FontSize = 14,
                IsVisible = true
            }
        ]);
        RecalculateLayout();
    }

}
