using System;
using System.Numerics;

namespace StatusTimers.Windows;

public class WindowManager : IDisposable
{
    private EnemyMultiDoTWindow EnemyMultiDoTWindow { get; } = new()
    {
        InternalName = "StatusTimersEnemyDoTs",
        Title = "Enemy DoTs",
        Size = new Vector2(400, 400),
        NativeController = Services.NativeController,
    };

    private PlayerCombinedStatusesWindow PlayerCombinedWindow { get; } = new()
    {
        InternalName = "StatusTimersCombinedStatuses",
        Title = "Player Statuses",
        Size = new Vector2(400, 400),
        NativeController = Services.NativeController,
    };

    public void OpenAll()
    {
        EnemyMultiDoTWindow.Open();
        PlayerCombinedWindow.Open();
    }

    public void Dispose()
    {
        EnemyMultiDoTWindow.Dispose();
        PlayerCombinedWindow.Dispose();
    }
}