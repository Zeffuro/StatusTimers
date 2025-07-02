using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Classes;
using StatusTimers.Factories;
using System;
using System.Numerics;

namespace StatusTimers.Windows;

public unsafe class OverlayManager : IDisposable {
    private bool _isDisposed = false;
    private ConfigurationWindow? _configurationWindow;
    private EnemyMultiDoTOverlay? _enemyMultiDoTOverlay;
    private PlayerCombinedStatusesOverlay? _playerCombinedOverlay;
    private ColorPickerAddon? _colorPickerAddon;

    public OverlayManager() {
        Services.Services.NameplateAddonController.PreEnable += PreAttach;
        Services.Services.NameplateAddonController.OnAttach += AttachNodes;
        Services.Services.NameplateAddonController.OnDetach += DetachNodes;
    }

    public PlayerCombinedStatusesOverlay? PlayerCombinedOverlayInstance => _playerCombinedOverlay;
    public EnemyMultiDoTOverlay? EnemyMultiDoTOverlayInstance => _enemyMultiDoTOverlay;
    public ColorPickerAddon? ColorPickerInstance => _colorPickerAddon;

    public void Dispose() {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;

        DetachAndDisposeAll();
        Services.Services.NameplateAddonController.PreEnable -= PreAttach;
        Services.Services.NameplateAddonController.OnAttach -= AttachNodes;
        Services.Services.NameplateAddonController.OnDetach -= DetachNodes;
    }

    private void PreAttach(AddonNamePlate* addonNamePlate) {
    }

    private void AttachNodes(AddonNamePlate* addonNamePlate) {
        DetachAndDisposeAll();

        _playerCombinedOverlay = new PlayerCombinedStatusesOverlay {
            Position = new Vector2(100, 100),
            Size = new Vector2(400, 400)
        };
        _enemyMultiDoTOverlay = new EnemyMultiDoTOverlay {
            Position = new Vector2(600, 100),
            Size = new Vector2(400, 400)
        };

        _colorPickerAddon = new ColorPickerAddon(this) {
            InternalName = "StatusTimerColorPicker",
            Title = "Pick a color",
            Size = new Vector2(540, 460),
            NativeController = Services.Services.NativeController
        };

        _configurationWindow = new ConfigurationWindow(this) {
            InternalName = "StatusTimersConfiguration",
            Title = "StatusTimers Configuration",
            Size = new Vector2(640, 512),
            NativeController = Services.Services.NativeController
        };

        if (addonNamePlate != null) {
            if (_playerCombinedOverlay != null) {
                Services.Services.NativeController.AttachNode(_playerCombinedOverlay, addonNamePlate->RootNode);
            }

            if (_enemyMultiDoTOverlay != null) {
                Services.Services.NativeController.AttachNode(_enemyMultiDoTOverlay, addonNamePlate->RootNode);
            }
        }

        _enemyMultiDoTOverlay?.Setup();
        _playerCombinedOverlay?.Setup();
    }

    private void DetachNodes(AddonNamePlate* addonNamePlate) {
        DetachAndDisposeAll();
    }

    private void DetachAndDisposeAll() {
        if (_colorPickerAddon != null) {
            _colorPickerAddon.Dispose();
            _colorPickerAddon = null;
        }
        if (_configurationWindow != null) {
            _configurationWindow.Dispose();
            _configurationWindow = null;
        }
        if (_playerCombinedOverlay != null) {
            Services.Services.NativeController.DetachNode(_playerCombinedOverlay);
            _playerCombinedOverlay.OnDispose();
            _playerCombinedOverlay.Dispose();
            _playerCombinedOverlay = null;
        }
        if (_enemyMultiDoTOverlay != null) {
            Services.Services.NativeController.DetachNode(_enemyMultiDoTOverlay);
            _enemyMultiDoTOverlay.OnDispose();
            _enemyMultiDoTOverlay.Dispose();
            _enemyMultiDoTOverlay = null;
        }
    }

    public void OnUpdate() {
        if (_isDisposed) {
            return;
        }

        _playerCombinedOverlay?.OnUpdate();
        _enemyMultiDoTOverlay?.OnUpdate();
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
