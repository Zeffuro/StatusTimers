using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace StatusTimers.Windows;

public class ConfigWindow(Configuration config) : Window("StatusTimers Config"), IDisposable
{
    public override void Draw()
    {
        if (ImGui.Checkbox("Config Option", ref config.ConfigOption))
        {
            config.Save();
        }
    }

    public void Dispose()
    {
    }
}