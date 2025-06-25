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
            NativeController = Services.Services.NativeController
        };

        Services.Services.NameplateAddonController.OnAttach += AttachNodes;
        Services.Services.NameplateAddonController.OnDetach += DetachNodes;
    }

    private ConfigurationWindow ConfigurationWindow { get; }

    private EnemyMultiDoTOverlay? EnemyMultiDoTOverlay { get; set; } = new() {
        Size = new Vector2(400, 400)
    };

    private PlayerCombinedStatusesOverlay? PlayerCombinedOverlay { get; } = new() {
        Size = new Vector2(400, 400)
    };

    public PlayerCombinedStatusesOverlay? PlayerCombinedOverlayInstance => PlayerCombinedOverlay;
    public EnemyMultiDoTOverlay? EnemyMultiDoTOverlayInstance => EnemyMultiDoTOverlay;

    public void Dispose() {
        Services.Services.NativeController.DetachNode(PlayerCombinedOverlay);
        Services.Services.NativeController.DetachNode(EnemyMultiDoTOverlay);

        ConfigurationWindow.Dispose();
        Services.Services.NameplateAddonController.PreEnable -= PreAttach;
        Services.Services.NameplateAddonController.OnAttach -= AttachNodes;
        Services.Services.NameplateAddonController.OnDetach -= DetachNodes;
    }

    private void PreAttach(AddonNamePlate* addonNamePlate) {
    }

    private void AttachNodes(AddonNamePlate* addonNamePlate) {
        Services.Services.NativeController.AttachNode(PlayerCombinedOverlay, addonNamePlate->RootNode);
        Services.Services.NativeController.AttachNode(EnemyMultiDoTOverlay, addonNamePlate->RootNode);

        EnemyMultiDoTOverlay?.Setup();
        PlayerCombinedOverlay?.Setup();
    }

    private void DetachNodes(AddonNamePlate* addonNamePlate) {
        Services.Services.NativeController.DetachNode(PlayerCombinedOverlay, () => {
            PlayerCombinedOverlay?.OnDispose();
            PlayerCombinedOverlay?.Dispose();
            EnemyMultiDoTOverlay = null;
        });
        Services.Services.NativeController.DetachNode(EnemyMultiDoTOverlay, () => {
            EnemyMultiDoTOverlay?.OnDispose();
            EnemyMultiDoTOverlay?.Dispose();
            EnemyMultiDoTOverlay = null;
        });
    }

    public void OnUpdate() {
        PlayerCombinedOverlay?.OnUpdate();
        EnemyMultiDoTOverlay?.OnUpdate();
    }

    public void ToggleConfig() {
        ConfigurationWindow.Toggle();
    }
}
