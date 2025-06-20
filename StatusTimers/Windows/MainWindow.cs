using Dalamud.Interface.Windowing;
using ImGuiNET;
using StatusTimers.Helpers;
using System;

namespace StatusTimers.Windows;

public class MainWindow() : Window("StatusTimers"), IDisposable {
    public void Dispose() {
    }

    public override void Draw() {
        foreach (StatusInfo status in StatusManager.GetPlayerStatuses()) {
            DrawHelper.DrawIcon(status.IconId);
            ImGui.SameLine();
            ImGui.TextUnformatted($"{status.Name}: {status.RemainingSeconds:0.0}s");
        }
    }
}
