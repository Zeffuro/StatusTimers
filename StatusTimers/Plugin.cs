using System;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using StatusTimers.Windows;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using KamiToolKit;
using StatusTimers.Helpers;

namespace StatusTimers;

public class Plugin : IDalamudPlugin
{
    public const string CommandName = "/statustimers";

    public Configuration Configuration;

    //public readonly WindowSystem WindowSystem = new("StatusTimers");
    public readonly WindowManager WindowManager;
    //public MainWindow MainWindow;
    //public ConfigWindow ConfigWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();

        this.Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        Services.NativeController = new NativeController(pluginInterface);
        
        WindowManager = new WindowManager();
        
        /*
        this.MainWindow = new MainWindow();
        this.ConfigWindow = new ConfigWindow(this.Configuration);
        this.WindowSystem.AddWindow(this.MainWindow);
        this.WindowSystem.AddWindow(this.ConfigWindow);
        
        
        Services.PluginInterface.UiBuilder.Draw += this.DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUi;
        */
        
        Services.Framework.Update += OnFrameworkUpdate;
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open the main window"
        });
        
        WindowManager.OpenAll();
    }
    
    private void OnFrameworkUpdate(IFramework framework)
    {
        EnemyListHelper.UpdateEnemyListMapping();
    }

    public void Dispose()
    {
        Services.Framework.Update -= OnFrameworkUpdate;
        Services.CommandManager.RemoveHandler(CommandName);

        this.Configuration.Save();
        
        /*
        this.WindowSystem.RemoveAllWindows();
        this.MainWindow.Dispose();
        this.ConfigWindow.Dispose();

        Services.PluginInterface.UiBuilder.Draw -= this.DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        */
        
        WindowManager.Dispose();
        Services.NativeController.Dispose();
    }
/*
    private void DrawUi() => this.WindowSystem.Draw();
    private void ToggleMainUi() => this.MainWindow.Toggle();
    private void ToggleConfigUi() => this.ConfigWindow.Toggle();
    */

    private void OnCommand(string command, string args)
    {
        if (args is "settings" or "config")
        {
            //this.ToggleConfigUi();
        }
        else
        {
            WindowManager.OpenAll();
            //this.ToggleMainUi();
        }
    }
}