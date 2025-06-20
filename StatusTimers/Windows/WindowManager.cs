using System;
using System.Numerics;

namespace StatusTimers.Windows;

public class WindowManager : IDisposable {
    private EnemyMultiDoTWindow EnemyMultiDoTWindow { get; } = new() {
        InternalName = "StatusTimersEnemyDoTs",
        Title = "Enemy DoTs",
        Size = new Vector2(400, 400),
        NativeController = Services.NativeController
    };

    private PlayerCombinedStatusesWindow PlayerCombinedWindow { get; } = new() {
        InternalName = "StatusTimersCombinedStatuses",
        Title = "Player Statuses",
        Size = new Vector2(400, 400),
        NativeController = Services.NativeController
    };

    private ConfigurationWindow ConfigurationWindow { get; } = new() {
        InternalName = "StatusTimersConfiguration",
        Title = "StatusTimers Configuration",
        Size = new Vector2(640, 512),
        NativeController = Services.NativeController
    };

    public void Dispose() {
        EnemyMultiDoTWindow.Dispose();
        PlayerCombinedWindow.Dispose();
    }

    public void OpenAll() {
        EnemyMultiDoTWindow.Open();
        PlayerCombinedWindow.Open();
    }

    public void CloseAll() {
        EnemyMultiDoTWindow.Close();
        PlayerCombinedWindow.Close();
    }

    public void ToggleConfig() {
        ConfigurationWindow.Toggle();
    }
}
