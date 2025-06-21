using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Classes;
using System;
using System.Numerics;

namespace StatusTimers.Windows;

public unsafe class WindowManager : IDisposable {
    public WindowManager() {
        Services.NameplateAddonController.OnAttach += AttachNodes;
        Services.NameplateAddonController.OnDetach += DetachNodes;
    }
    private EnemyMultiDoTWindow EnemyMultiDoTWindow { get; } = new() {
        Position = new Vector2(200, 400),
        Title = "Enemy DoTs",
        Size = new Vector2(400, 400),
    };

    private PlayerCombinedStatusesWindow PlayerCombinedWindow { get; } = new() {
        Position = new Vector2(200, 800),
        Title = "Player Statuses",
        Size = new Vector2(400, 400),
    };

    private ConfigurationWindow ConfigurationWindow { get; } = new() {
        InternalName = "StatusTimersConfiguration",
        Title = "StatusTimers Configuration",
        Size = new Vector2(640, 512),
        NativeController = Services.NativeController
    };

    public void Dispose() {
        Services.NameplateAddonController.PreEnable -= PreAttach;
        Services.NameplateAddonController.OnAttach -= AttachNodes;
        Services.NameplateAddonController.OnDetach -= DetachNodes;
    }

    private void PreAttach(AddonNamePlate* addonNamePlate) {

    }

    private void AttachNodes(AddonNamePlate* addonNamePlate) {
        Services.Logger.Info("Test");
        Services.Logger.Info("Attaching Nodes");
        Services.NativeController.AttachNode(PlayerCombinedWindow, addonNamePlate->RootNode, NodePosition.AsFirstChild);
        Services.NativeController.AttachNode(EnemyMultiDoTWindow, addonNamePlate->RootNode, NodePosition.AsFirstChild);
    }

    private void DetachNodes(AddonNamePlate* addonNamePlate) {
        Services.NativeController.DetachNode(PlayerCombinedWindow, () => {
            PlayerCombinedWindow?.Dispose();
        });
        Services.NativeController.DetachNode(EnemyMultiDoTWindow, () => {
            EnemyMultiDoTWindow?.Dispose();
        });
    }

    public void OnUpdate() {
        PlayerCombinedWindow?.OnUpdate();
        EnemyMultiDoTWindow?.OnUpdate();
    }

    public void OpenAll() {
        //EnemyMultiDoTWindow.Open();
        //PlayerCombinedWindow.Open();
    }

    public void CloseAll() {
        //EnemyMultiDoTWindow.Close();
        //PlayerCombinedWindow.Close();
    }

    public void ToggleConfig() {
        ConfigurationWindow.Toggle();
    }
}
