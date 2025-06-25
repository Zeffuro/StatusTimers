using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Numerics;

namespace StatusTimers.Windows;

public unsafe class OverlayManager : IDisposable {
    public OverlayManager() {
        ConfigurationWindow = new ConfigurationWindow(this) {
            InternalName = "StatusTimersConfiguration",
            Title = "StatusTimers Configuration",
            Size = new Vector2(640, 512),
            NativeController = Services.NativeController
        };

        Services.NameplateAddonController.OnAttach += AttachNodes;
        Services.NameplateAddonController.OnDetach += DetachNodes;
    }

    private ConfigurationWindow ConfigurationWindow { get; }

    private EnemyMultiDoTOverlay? EnemyMultiDoTOverlay { get; set; } = new() {
        Title = "Enemy DoTs",
        Size = new Vector2(400, 400)
    };

    private PlayerCombinedStatusesOverlay? PlayerCombinedOverlay { get; set; } = new() {
        Title = "Player Statuses",
        Size = new Vector2(400, 400)
    };

    public PlayerCombinedStatusesOverlay? PlayerCombinedOverlayInstance => PlayerCombinedOverlay;
    public EnemyMultiDoTOverlay? EnemyMultiDoTOverlayInstance => EnemyMultiDoTOverlay;

    public void Dispose() {

        Services.NativeController.DetachNode(PlayerCombinedOverlay);
        Services.NativeController.DetachNode(EnemyMultiDoTOverlay);



        ConfigurationWindow.Dispose();
        Services.NameplateAddonController.PreEnable -= PreAttach;
        Services.NameplateAddonController.OnAttach -= AttachNodes;
        Services.NameplateAddonController.OnDetach -= DetachNodes;
    }

    private void PreAttach(AddonNamePlate* addonNamePlate) {
    }

    private void AttachNodes(AddonNamePlate* addonNamePlate) {
        Services.NativeController.AttachNode(PlayerCombinedOverlay, addonNamePlate->RootNode);
        Services.NativeController.AttachNode(EnemyMultiDoTOverlay, addonNamePlate->RootNode);

        EnemyMultiDoTOverlay?.Setup();
        PlayerCombinedOverlay?.Setup();
    }

    private void DetachNodes(AddonNamePlate* addonNamePlate) {
        Services.NativeController.DetachNode(PlayerCombinedOverlay, () =>
        {
            PlayerCombinedOverlay?.OnDispose();
            PlayerCombinedOverlay?.Dispose();
            EnemyMultiDoTOverlay = null;
        });
        Services.NativeController.DetachNode(EnemyMultiDoTOverlay, () => {
            EnemyMultiDoTOverlay?.OnDispose();
            EnemyMultiDoTOverlay?.Dispose();
            EnemyMultiDoTOverlay = null;
        });
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
