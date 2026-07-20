using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit;
using KamiToolKit.UiOverlay;
using StatusTimers.Config;
using StatusTimers.Extensions;
using StatusTimers.Helpers;
using StatusTimers.Services;
using StatusTimers.Windows;
using System;
using System.Threading;
using System.Threading.Tasks;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers;

public class Plugin : IAsyncDalamudPlugin {
    private const string CommandName = "/statustimers";
    private static readonly TimeSpan FrameworkStartupTimeout = TimeSpan.FromSeconds(15);

    [PluginService]
    private static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    private OverlayManager OverlayManager { get; set; } = null!;

    public async Task LoadAsync(CancellationToken cancellationToken) {
        PluginInterface.Create<GlobalServices>();

        BackupHelper.DoConfigBackup(PluginInterface);

        KamiToolKitLibrary.Initialize(PluginInterface);

        await GlobalServices.Framework.RunSafelyWithTimeout(() => {
            GlobalServices.OverlayController = new OverlayController();
        }, cancellationToken, FrameworkStartupTimeout);

        OverlayManager = new OverlayManager();

        await OverlayManager.SetupAsync(cancellationToken);

        GlobalServices.PluginInterface.UiBuilder.OpenMainUi += OverlayManager.ToggleConfig;
        GlobalServices.PluginInterface.UiBuilder.OpenConfigUi += OverlayManager.ToggleConfig;

        GlobalServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Open the main window"
        });

        if (GlobalServices.ClientState.IsLoggedIn) {
            await GlobalServices.Framework.RunSafelyWithTimeout(OnLogin, cancellationToken, FrameworkStartupTimeout);
        }

        GlobalServices.Framework.Update += OnFrameworkUpdate;
        GlobalServices.ClientState.Login += OnLogin;
    }

    public async ValueTask DisposeAsync() {
        try {
            GlobalServices.Framework.Update -= OnFrameworkUpdate;
            GlobalServices.ClientState.Login -= OnLogin;
            GlobalServices.CommandManager.RemoveHandler(CommandName);

            if (OverlayManager is not null) {
                GlobalServices.PluginInterface.UiBuilder.OpenMainUi -= OverlayManager.ToggleConfig;
                GlobalServices.PluginInterface.UiBuilder.OpenConfigUi -= OverlayManager.ToggleConfig;

                await OverlayManager.DisposeAsync();
                OverlayManager = null!;
            }

            await GlobalServices.Framework.RunSafely(() => GlobalServices.OverlayController?.Dispose());
            await GlobalServices.Framework.RunSafely(KamiToolKitLibrary.Dispose);
        }
        finally {
            OverlayConfigRegistry.Clear();
            EnemyListHelper.Clear();
            StatusManager.ClearTransientState();
            GlobalServices.Clear();
            PluginInterface = null!;
        }
    }

    private void OnFrameworkUpdate(IFramework framework) {
        EnemyListHelper.UpdateEnemyListMapping();
    }

    private void OnCommand(string command, string args) {
        if (args is "toggleall") {
            GlobalServices.Framework.RunOnFrameworkThread(() => {
                OverlayManager.PlayerCombinedOverlayInstance?.IsVisible = !OverlayManager.PlayerCombinedOverlayInstance.IsVisible;
                OverlayManager.EnemyMultiDoTOverlayInstance?.IsVisible = !OverlayManager.EnemyMultiDoTOverlayInstance.IsVisible;
                OverlayManager.PlayerBuffsOverlayInstance?.IsVisible = !OverlayManager.PlayerBuffsOverlayInstance.IsVisible;
                OverlayManager.PlayerDebuffsOverlayInstance?.IsVisible = !OverlayManager.PlayerDebuffsOverlayInstance.IsVisible;
            });
        }
        else {
            OverlayManager.ToggleConfig();
        }
    }

    private void OnLogin() {
        #if DEBUG
            OverlayManager.OpenConfig();
        #endif
    }
}
