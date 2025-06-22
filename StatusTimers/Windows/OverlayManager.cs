using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Classes;
using System;
using System.Numerics;

namespace StatusTimers.Windows;

public unsafe class OverlayManager : IDisposable {

    private ConfigurationWindow ConfigurationWindow { get; }
    public OverlayManager() {
        ConfigurationWindow = new ConfigurationWindow(this) {
            InternalName = "StatusTimersConfiguration",
            Title = "StatusTimers Configuration",
            Size = new Vector2(640, 512),
            NativeController = Services.NativeController
        };

        EnemyMultiDoTOverlay.Setup();
        PlayerCombinedOverlay.Setup();

        Services.NameplateAddonController.OnAttach += AttachNodes;
        Services.NameplateAddonController.OnDetach += DetachNodes;
    }

    private EnemyMultiDoTOverlay EnemyMultiDoTOverlay { get; } = new() {
        Title = "Enemy DoTs",
        Size = new Vector2(400, 400),
    };

    private PlayerCombinedStatusesOverlay PlayerCombinedOverlay { get; } = new() {
        Title = "Player Statuses",
        Size = new Vector2(400, 400),
    };

    public PlayerCombinedStatusesOverlay PlayerCombinedOverlayInstance => PlayerCombinedOverlay;
    public EnemyMultiDoTOverlay EnemyMultiDoTOverlayInstance => EnemyMultiDoTOverlay;

    public void Dispose() {
        Services.NativeController.DetachNode(PlayerCombinedOverlay);
        Services.NativeController.DetachNode(EnemyMultiDoTOverlay);

        PlayerCombinedOverlay.Dispose();
        EnemyMultiDoTOverlay.Dispose();

        ConfigurationWindow.Dispose();
        Services.NameplateAddonController.PreEnable -= PreAttach;
        Services.NameplateAddonController.OnAttach -= AttachNodes;
        Services.NameplateAddonController.OnDetach -= DetachNodes;
    }

    private void PreAttach(AddonNamePlate* addonNamePlate) {

    }

    private void AttachNodes(AddonNamePlate* addonNamePlate) {
        Services.NativeController.AttachNode(PlayerCombinedOverlay, addonNamePlate->RootNode, NodePosition.AsLastChild);
        Services.NativeController.AttachNode(EnemyMultiDoTOverlay, addonNamePlate->RootNode, NodePosition.AsLastChild);
    }

    private void DetachNodes(AddonNamePlate* addonNamePlate) {
        Services.NativeController.DetachNode(PlayerCombinedOverlay);
        Services.NativeController.DetachNode(EnemyMultiDoTOverlay);
    }

    public void OnUpdate() {
        PlayerCombinedOverlay?.OnUpdate();
        EnemyMultiDoTOverlay?.OnUpdate();
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
