using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using StatusTimers.Models;
using StatusTimers.Nodes.LayoutNodes;
using System;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class OverlayManager : IDisposable {
    private bool _isDisposed;
    private ConfigurationWindow? _configurationWindow;
    private EnemyMultiDoTOverlay? _enemyMultiDoTOverlay;
    private PlayerCombinedStatusesOverlay? _playerCombinedOverlay;
    private ColorPickerAddon? _colorPickerAddon;

    public PlayerCombinedStatusesOverlay? PlayerCombinedOverlayInstance => _playerCombinedOverlay;
    public EnemyMultiDoTOverlay? EnemyMultiDoTOverlayInstance => _enemyMultiDoTOverlay;
    public ColorPickerAddon? ColorPickerInstance => _colorPickerAddon;

    public void Dispose() {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;

        DetachAndDisposeAll();
    }

    public void Setup() {
        DetachAndDisposeAll();

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            _playerCombinedOverlay = new PlayerCombinedStatusesOverlay {
                NodeId = 2,
                Position = new Vector2(100, 100),
                Size = new Vector2(400, 400),
                IsVisible = true
            };
            _enemyMultiDoTOverlay = new EnemyMultiDoTOverlay {
                NodeId = 3,
                Position = new Vector2(600, 100),
                Size = new Vector2(400, 400),
                IsVisible = true
            };


            GlobalServices.OverlayController.AddNode(_playerCombinedOverlay);
            GlobalServices.OverlayController.AddNode(_enemyMultiDoTOverlay);
        });

        _colorPickerAddon = new ColorPickerAddon {
            InternalName = "StatusTimerColorPicker",
            Title = "Pick a color",
            Size = new Vector2(400, 540)
        };

        _configurationWindow = new ConfigurationWindow(this) {
            InternalName = "StatusTimersConfiguration",
            Title = "StatusTimers Configuration",
            Size = new Vector2(640, 512)
        };

        GlobalServices.Logger.Info($"Setting up overlay 111");

        _enemyMultiDoTOverlay?.Setup();
        _playerCombinedOverlay?.Setup();
    }

    private void DetachAndDisposeAll() {
        if (_colorPickerAddon != null)
        {
            _colorPickerAddon.Dispose();
            _colorPickerAddon = null;
        }
        if (_configurationWindow != null)
        {
            _configurationWindow.Dispose();
            _configurationWindow = null;
        }

        _playerCombinedOverlay = null;
        _enemyMultiDoTOverlay = null;
    }

    public void RestartOverlay<TOverlay>(ref TOverlay overlayInstance, Func<TOverlay> creator)
        where TOverlay : StatusTimerOverlay<StatusKey>
    {
        overlayInstance.OnDispose();
        overlayInstance = creator();
        overlayInstance.Setup();
    }

    public void ToggleConfig() {
        if (_isDisposed) {
            return;
        }

        _configurationWindow?.Toggle();
    }

    public void OpenConfig() {
        if (_isDisposed) {
            return;
        }

        _configurationWindow?.Open();
    }
}
