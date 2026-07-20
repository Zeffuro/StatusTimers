using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit.UiOverlay;

namespace StatusTimers.Services;

public class Services {
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IKeyState KeyState { get; private set; } = null!;
    [PluginService] public static INotificationManager NotificationManager { get; private set; } = null!;
    [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public static IPluginLog Logger { get; private set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;

    public static OverlayController OverlayController { get; set; } = null!;

    public static void Clear() {
        ClientState = null!;
        CommandManager = null!;
        Condition = null!;
        PluginInterface = null!;
        DataManager = null!;
        Framework = null!;
        KeyState = null!;
        NotificationManager = null!;
        ObjectTable = null!;
        Logger = null!;
        TargetManager = null!;
        OverlayController = null!;
    }
}
