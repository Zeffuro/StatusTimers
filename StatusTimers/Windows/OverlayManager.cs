using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using StatusTimers.Models;
using StatusTimers.Nodes.LayoutNodes;
using System;
using System.Numerics;

namespace StatusTimers.Windows;

public unsafe class OverlayManager : IDisposable {
    private bool _isDisposed;
    private ConfigurationWindow? _configurationWindow;
    private OverlayRootNode? _statusTimerRootNode;
    private EnemyMultiDoTOverlay? _enemyMultiDoTOverlay;
    private PlayerCombinedStatusesOverlay? _playerCombinedOverlay;
    private ColorPickerAddon? _colorPickerAddon;

    public OverlayManager() {
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
    }

    private void AttachNodes(AddonNamePlate* addonNamePlate) {
        DetachAndDisposeAll();

        var screenSize = new Vector2(AtkStage.Instance()->ScreenSize.Width, AtkStage.Instance()->ScreenSize.Height);

        _statusTimerRootNode = new OverlayRootNode(screenSize, Services.Services.NativeController);

        _playerCombinedOverlay = new PlayerCombinedStatusesOverlay {
            NodeId = 2,
            Position = new Vector2(100, 100),
            Size = new Vector2(400, 400)
        };
        _enemyMultiDoTOverlay = new EnemyMultiDoTOverlay {
            NodeId = 3,
            Position = new Vector2(600, 100),
            Size = new Vector2(400, 400)
        };

        _statusTimerRootNode.AddOverlay(_playerCombinedOverlay);
        _statusTimerRootNode.AddOverlay(_enemyMultiDoTOverlay);

        _colorPickerAddon = new ColorPickerAddon() {
            InternalName = "StatusTimerColorPicker",
            Title = "Pick a color",
            Size = new Vector2(540, 500),
            NativeController = Services.Services.NativeController
        };

        _configurationWindow = new ConfigurationWindow(this) {
            InternalName = "StatusTimersConfiguration",
            Title = "StatusTimers Configuration",
            Size = new Vector2(640, 512),
            NativeController = Services.Services.NativeController
        };

        if (addonNamePlate != null && _statusTimerRootNode != null)
        {
            _statusTimerRootNode.AttachAllToNativeController(addonNamePlate->RootNode);
        }

        _enemyMultiDoTOverlay?.Setup();
        _playerCombinedOverlay?.Setup();
    }

    private void DetachNodes(AddonNamePlate* addonNamePlate) {
        DetachAndDisposeAll();
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
        if (_statusTimerRootNode != null)
        {
            _statusTimerRootNode.Cleanup();
            _statusTimerRootNode = null;
        }
        _playerCombinedOverlay = null;
        _enemyMultiDoTOverlay = null;
    }

    public void OnUpdate() {
        if (_isDisposed) {
            return;
        }

        _playerCombinedOverlay?.OnUpdate();
        _enemyMultiDoTOverlay?.OnUpdate();
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
