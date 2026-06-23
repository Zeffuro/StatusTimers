using StatusTimers.Enums;
using StatusTimers.Extensions;
using StatusTimers.Logic;
using StatusTimers.Models;
using StatusTimers.Services;
using StatusTimers.StatusSources;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class OverlayManager : IAsyncDisposable {
    private bool _isDisposed;
    private ConfigurationWindow? _configurationWindow;
    private StatusTimerOverlayNode<StatusKey>? _playerCombinedOverlay;
    private StatusTimerOverlayNode<StatusKey>? _enemyMultiDoTOverlay;
    private StatusTimerOverlayNode<StatusKey>? _playerBuffsOverlay;
    private StatusTimerOverlayNode<StatusKey>? _playerDebuffsOverlay;
    private StatusDataSourceManager<StatusKey>? _playerDataSource;
    private StatusDataSourceManager<StatusKey>? _enemyDataSource;
    private StatusDataSourceManager<StatusKey>? _playerBuffsDataSource;
    private StatusDataSourceManager<StatusKey>? _playerDebuffsDataSource;
    private StatusNodeActionService? _statusActionService;
    private ColorPickerAddon? _colorPickerAddon;

    public StatusTimerOverlayNode<StatusKey>? PlayerCombinedOverlayInstance => _playerCombinedOverlay;
    public StatusTimerOverlayNode<StatusKey>? EnemyMultiDoTOverlayInstance => _enemyMultiDoTOverlay;
    public StatusTimerOverlayNode<StatusKey>? PlayerBuffsOverlayInstance => _playerBuffsOverlay;
    public StatusTimerOverlayNode<StatusKey>? PlayerDebuffsOverlayInstance => _playerDebuffsOverlay;
    public ColorPickerAddon? ColorPickerInstance => _colorPickerAddon;

    public async ValueTask DisposeAsync() {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;

        await DetachAndDisposeAllAsync();
    }

    public async Task SetupAsync(CancellationToken cancellationToken) {
        await DetachAndDisposeAllAsync();

        await GlobalServices.Framework.Run(CreateAndAttachOverlays, cancellationToken);

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

    private async ValueTask DetachAndDisposeAllAsync() {
        var colorPickerAddon = _colorPickerAddon;
        var configurationWindow = _configurationWindow;

        _colorPickerAddon = null;
        _configurationWindow = null;

        await GlobalServices.Framework.RunSafely(() => {
            colorPickerAddon?.CloseSilently();

            if (_playerCombinedOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_playerCombinedOverlay);
                _playerCombinedOverlay = null;
            }

            if (_enemyMultiDoTOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_enemyMultiDoTOverlay);
                _enemyMultiDoTOverlay = null;
            }

            if (_playerBuffsOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_playerBuffsOverlay);
                _playerBuffsOverlay = null;
            }

            if (_playerDebuffsOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_playerDebuffsOverlay);
                _playerDebuffsOverlay = null;
            }
        });

        _playerDataSource = null;
        _enemyDataSource = null;
        _playerBuffsDataSource = null;
        _playerDebuffsDataSource = null;

        await Task.WhenAll(
            DisposeAddonAsync(configurationWindow).AsTask(),
            DisposeAddonAsync(colorPickerAddon).AsTask());
    }

    private static async ValueTask DisposeAddonAsync(IAsyncDisposable? addon) {
        if (addon == null) {
            return;
        }

        await addon.DisposeAsync();
    }

    private void CreateAndAttachOverlays()
    {
        _playerCombinedOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.Combined);
        _enemyMultiDoTOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.MultiDoT);
        _playerBuffsOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.Buffs);
        _playerDebuffsOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.Debuffs);

        _playerCombinedOverlay.Initialize();
        _enemyMultiDoTOverlay.Initialize();
        _playerBuffsOverlay.Initialize();
        _playerDebuffsOverlay.Initialize();

        _statusActionService = new StatusNodeActionService();
        var playerSource = new PlayerCombinedStatusesSource();

        _playerDataSource = new StatusDataSourceManager<StatusKey>(
            playerSource,
            NodeKind.Combined,
            () => _playerCombinedOverlay?.IsPreviewEnabled ?? false,
            () => _playerCombinedOverlay!.OverlayConfig.ShowPermaIcons,
            () => _playerCombinedOverlay!.OverlayConfig.MaxStatuses,
            () => _playerCombinedOverlay!.OverlayConfig.ItemsPerLine);
        _playerBuffsDataSource = new StatusDataSourceManager<StatusKey>(
            new StatusCategoryFilteredSource<StatusKey>(new PlayerCombinedStatusesSource(), StatusCategory.Buff),
            NodeKind.Buffs,
            () => _playerBuffsOverlay?.IsPreviewEnabled ?? false,
            () => _playerBuffsOverlay!.OverlayConfig.ShowPermaIcons,
            () => _playerBuffsOverlay!.OverlayConfig.MaxStatuses,
            () => _playerBuffsOverlay!.OverlayConfig.ItemsPerLine);
        _playerDebuffsDataSource = new StatusDataSourceManager<StatusKey>(
            new StatusCategoryFilteredSource<StatusKey>(new PlayerCombinedStatusesSource(), StatusCategory.Debuff),
            NodeKind.Debuffs,
            () => _playerDebuffsOverlay?.IsPreviewEnabled ?? false,
            () => _playerDebuffsOverlay!.OverlayConfig.ShowPermaIcons,
            () => _playerDebuffsOverlay!.OverlayConfig.MaxStatuses,
            () => _playerDebuffsOverlay!.OverlayConfig.ItemsPerLine);
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
        _playerBuffsOverlay.SetStatusProvider(() =>
            _playerBuffsDataSource?.FetchAndProcessStatuses(_playerBuffsOverlay!.OverlayConfig) ??
            new List<StatusInfo>());
        _playerDebuffsOverlay.SetStatusProvider(() =>
            _playerDebuffsDataSource?.FetchAndProcessStatuses(_playerDebuffsOverlay!.OverlayConfig) ??
            new List<StatusInfo>());

        _playerCombinedOverlay.SetNodeActionHandler(_statusActionService.Handle);
        _enemyMultiDoTOverlay.SetNodeActionHandler(_statusActionService.Handle);
        _playerBuffsOverlay.SetNodeActionHandler(_statusActionService.Handle);
        _playerDebuffsOverlay.SetNodeActionHandler(_statusActionService.Handle);

        GlobalServices.OverlayController.AddNode(_playerCombinedOverlay);
        GlobalServices.OverlayController.AddNode(_enemyMultiDoTOverlay);
        GlobalServices.OverlayController.AddNode(_playerBuffsOverlay);
        GlobalServices.OverlayController.AddNode(_playerDebuffsOverlay);
    }

    public void ToggleConfig() {
        if (_isDisposed) {
            return;
        }

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_isDisposed) {
                return;
            }

            CloseColorPicker();
            _configurationWindow?.Toggle();
        });
    }

    public void OpenConfig() {
        if (_isDisposed) {
            return;
        }

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_isDisposed) {
                return;
            }

            CloseColorPicker();
            _configurationWindow?.Open();
        });
    }

    internal void CloseColorPicker() {
        _colorPickerAddon?.CloseSilently();
    }
}

