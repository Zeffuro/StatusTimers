using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;

namespace StatusTimers.Windows;

public class ConfigWindow(Configuration config) : Window("StatusTimers Config"), IDisposable {
    public void Dispose() {
    }

    public override void Draw() {
        if (ImGui.Checkbox("Config Option", ref config.ConfigOption)) {
            config.Save();
        }
    }
}
