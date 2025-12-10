using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Overlay;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.Services;
using StatusTimers.StatusSources;
using System;
using System.Collections.Generic;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class OverlayManager : IDisposable {
    private bool _isDisposed;
    private ConfigurationWindow? _configurationWindow;
    private StatusTimerOverlayNode<StatusKey>? _playerCombinedOverlay;
    private StatusTimerOverlayNode<StatusKey>? _enemyMultiDoTOverlay;
    private StatusDataSourceManager<StatusKey>? _playerDataSource;
    private StatusDataSourceManager<StatusKey>? _enemyDataSource;
    private ColorPickerAddon? _colorPickerAddon;

    public StatusTimerOverlayNode<StatusKey>? PlayerCombinedOverlayInstance => _playerCombinedOverlay;
    public StatusTimerOverlayNode<StatusKey>? EnemyMultiDoTOverlayInstance => _enemyMultiDoTOverlay;
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

        GlobalServices.Framework.RunOnFrameworkThread(CreateAndAttachOverlays);

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
    }

    private void DetachAndDisposeAll() {
        if (_colorPickerAddon != null)
        {
            _colorPickerAddon.CloseSilently();
            _colorPickerAddon.Dispose();
            _colorPickerAddon = null;
        }
        if (_configurationWindow != null)
        {
            _configurationWindow.Dispose();
            _configurationWindow = null;
        }

        _playerCombinedOverlay?.Dispose();
        _playerCombinedOverlay = null;
        _enemyMultiDoTOverlay?.Dispose();
        _enemyMultiDoTOverlay = null;
        _playerDataSource = null;
        _enemyDataSource = null;
    }

    private void CreateAndAttachOverlays()
    {
        _playerCombinedOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.Combined);
        _enemyMultiDoTOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.MultiDoT);
        _playerCombinedOverlay.Initialize();
        _enemyMultiDoTOverlay.Initialize();
        _playerDataSource = new StatusDataSourceManager<StatusKey>(
            new PlayerCombinedStatusesSource(),
            NodeKind.Combined,
            () => _playerCombinedOverlay?.IsPreviewEnabled ?? false,
            () => _playerCombinedOverlay!.OverlayConfig.ShowPermaIcons,
            () => _playerCombinedOverlay!.OverlayConfig.MaxStatuses,
            () => _playerCombinedOverlay!.OverlayConfig.ItemsPerLine);
        _enemyDataSource = new StatusDataSourceManager<StatusKey>(
            new EnemyMultiDoTSource(),
            NodeKind.MultiDoT,
            () => _enemyMultiDoTOverlay?.IsPreviewEnabled ?? false,
            () => _enemyMultiDoTOverlay!.OverlayConfig.ShowPermaIcons,
            () => _enemyMultiDoTOverlay!.OverlayConfig.MaxStatuses,
            () => _enemyMultiDoTOverlay!.OverlayConfig.ItemsPerLine);

        _playerCombinedOverlay.SetStatusProvider(() =>
            _playerDataSource?.FetchAndProcessStatuses(_playerCombinedOverlay!.OverlayConfig) ??
            new List<StatusInfo>());

        _enemyMultiDoTOverlay.SetStatusProvider(() =>
            _enemyDataSource?.FetchAndProcessStatuses(_enemyMultiDoTOverlay!.OverlayConfig) ??
            new List<StatusInfo>());


        GlobalServices.OverlayController.AddNode(_playerCombinedOverlay);
        GlobalServices.OverlayController.AddNode(_enemyMultiDoTOverlay);
    }

    public void ToggleConfig() {
        if (_isDisposed) {
            return;
        }

        CloseColorPicker();
        _configurationWindow?.Toggle();
    }

    public void OpenConfig() {
        if (_isDisposed) {
            return;
        }

        CloseColorPicker();
        _configurationWindow?.Open();
    }

    internal void CloseColorPicker() {
        _colorPickerAddon?.CloseSilently();
    }
}
