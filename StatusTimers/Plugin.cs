using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using StatusTimers.Windows;
using Dalamud.Game.Command;
using KamiToolKit;

namespace StatusTimers;

public class Plugin : IDalamudPlugin
{
    public const string CommandName = "/statustimers";

    public Configuration Configuration;

    public readonly WindowSystem WindowSystem = new("StatusTimers");
    public MainWindow MainWindow;
    public ConfigWindow ConfigWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();

        this.Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        this.MainWindow = new MainWindow();
        this.ConfigWindow = new ConfigWindow(this.Configuration);
        this.WindowSystem.AddWindow(this.MainWindow);
        this.WindowSystem.AddWindow(this.ConfigWindow);

        Services.NativeController = new NativeController(pluginInterface);
        
        DrawWindow();

        Services.PluginInterface.UiBuilder.Draw += this.DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUi;
        
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open the main window"
        });
    }

    public void DrawWindow()
    {
        Services.AddonStatusTimers = new AddonStatusTimers
        {
            InternalName = "StatusTimers",
            Title = "StatusTimers",
            Size = new Vector2(200, 400),
            NativeController = Services.NativeController,
            WindowOptions = 
        };
        
        Services.AddonStatusTimers.Open();
        
    }

    public void Dispose()
    {
        Services.CommandManager.RemoveHandler(CommandName);

        this.Configuration.Save();

        this.WindowSystem.RemoveAllWindows();
        this.MainWindow.Dispose();
        this.ConfigWindow.Dispose();

        Services.PluginInterface.UiBuilder.Draw -= this.DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        
        Services.AddonStatusTimers.Dispose();
        Services.NativeController.Dispose();
    }

    private void DrawUi() => this.WindowSystem.Draw();
    private void ToggleMainUi() => this.MainWindow.Toggle();
    private void ToggleConfigUi() => this.ConfigWindow.Toggle();

    private void OnCommand(string command, string args)
    {
        DrawWindow();
        if (args is "settings" or "config")
        {
            this.ToggleConfigUi();
        }
        else
        {
            this.ToggleMainUi();
        }
    }
}