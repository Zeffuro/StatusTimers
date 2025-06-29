using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Classes;
using System;
using System.Numerics;

namespace StatusTimers.Windows;

public unsafe class OverlayManager : IDisposable {
    private ConfigurationWindow? configurationWindow;
    private EnemyMultiDoTOverlay? enemyMultiDoTOverlay;
    private PlayerCombinedStatusesOverlay? playerCombinedOverlay;

    public OverlayManager() {
        Services.Services.NameplateAddonController.PreEnable += PreAttach;
        Services.Services.NameplateAddonController.OnAttach += AttachNodes;
        Services.Services.NameplateAddonController.OnDetach += DetachNodes;
    }

    public PlayerCombinedStatusesOverlay? PlayerCombinedOverlayInstance => playerCombinedOverlay;
    public EnemyMultiDoTOverlay? EnemyMultiDoTOverlayInstance => enemyMultiDoTOverlay;

    public void Dispose() {
        // Detach and dispose overlays and config window
        DetachAndDisposeAll();
        Services.Services.NameplateAddonController.PreEnable -= PreAttach;
        Services.Services.NameplateAddonController.OnAttach -= AttachNodes;
        Services.Services.NameplateAddonController.OnDetach -= DetachNodes;
    }

    private void PreAttach(AddonNamePlate* addonNamePlate) {
    }

    private void AttachNodes(AddonNamePlate* addonNamePlate) {
        DetachAndDisposeAll();

        playerCombinedOverlay = new PlayerCombinedStatusesOverlay {
            Position = new Vector2(100, 100),
            Size = new Vector2(400, 400)
        };
        enemyMultiDoTOverlay = new EnemyMultiDoTOverlay {
            Position = new Vector2(600, 100),
            Size = new Vector2(400, 400)
        };

        configurationWindow = new ConfigurationWindow(this) {
            InternalName = "StatusTimersConfiguration",
            Title = "StatusTimers Configuration",
            Size = new Vector2(640, 512),
            NativeController = Services.Services.NativeController
        };

        if (addonNamePlate != null) {
            if (playerCombinedOverlay != null) {
                Services.Services.NativeController.AttachNode(playerCombinedOverlay, addonNamePlate->RootNode);
            }

            if (enemyMultiDoTOverlay != null) {
                Services.Services.NativeController.AttachNode(enemyMultiDoTOverlay, addonNamePlate->RootNode);
            }
        }

        enemyMultiDoTOverlay?.Setup();
        playerCombinedOverlay?.Setup();
    }

    private void DetachNodes(AddonNamePlate* addonNamePlate) {
        DetachAndDisposeAll();
    }

    private void DetachAndDisposeAll() {
        if (configurationWindow != null) {
            configurationWindow.Dispose();
            configurationWindow = null;
        }
        if (playerCombinedOverlay != null) {
            Services.Services.NativeController.DetachNode(playerCombinedOverlay);
            playerCombinedOverlay.OnDispose();
            playerCombinedOverlay.Dispose();
            playerCombinedOverlay = null;
        }
        if (enemyMultiDoTOverlay != null) {
            Services.Services.NativeController.DetachNode(enemyMultiDoTOverlay);
            enemyMultiDoTOverlay.OnDispose();
            enemyMultiDoTOverlay.Dispose();
            enemyMultiDoTOverlay = null;
        }
    }

    public void OnUpdate() {
        playerCombinedOverlay?.OnUpdate();
        enemyMultiDoTOverlay?.OnUpdate();
    }

    public void ToggleConfig() {
        configurationWindow?.Toggle();
    }

    public void OpenConfig() {
        configurationWindow?.Open();
    }
}
