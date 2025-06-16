using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using StatusTimers.Helpers;

namespace StatusTimers.Windows;

public class MainWindow() : Window("StatusTimers"), IDisposable
{
    public override void Draw()
    {
        foreach (var status in StatusManager.GetPlayerStatuses())
        {
            DrawHelper.DrawIcon(status.IconId);
            ImGui.SameLine();
            ImGui.TextUnformatted($"{status.Name}: {status.RemainingSeconds:0.0}s");
        }
    }

    public void Dispose()
    {
    }
}