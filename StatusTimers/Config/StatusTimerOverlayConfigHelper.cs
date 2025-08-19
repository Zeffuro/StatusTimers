using StatusTimers.Enums;

namespace StatusTimers.Config;

public static class StatusTimerOverlayConfigHelper
{
    private static void EnsureAllNodePartStyleKindsSet(StatusTimerOverlayConfig config)
    {
        if (config.Icon.StyleKind == NodePartStyleKind.None) {
            config.Icon.StyleKind = NodePartStyleKind.Icon;
        }

        if (config.Name.StyleKind == NodePartStyleKind.None) {
            config.Name.StyleKind = NodePartStyleKind.Text;
        }

        if (config.Timer.StyleKind == NodePartStyleKind.None) {
            config.Timer.StyleKind = NodePartStyleKind.Text;
        }

        if (config.Actor.StyleKind == NodePartStyleKind.None) {
            config.Actor.StyleKind = NodePartStyleKind.Text;
        }

        if (config.Progress.StyleKind == NodePartStyleKind.None) {
            config.Progress.StyleKind = NodePartStyleKind.Bar;
        }
    }

    private static void EnsureAllNodePartStylesConsistent(StatusTimerOverlayConfig config)
    {
        var defaultConfig = new StatusTimerOverlayConfig(NodeKind.Combined);

        EnsureNodePartStyle(config.Icon, defaultConfig.Icon);
        EnsureNodePartStyle(config.Name, defaultConfig.Name);
        EnsureNodePartStyle(config.Timer, defaultConfig.Timer);
        EnsureNodePartStyle(config.Actor, defaultConfig.Actor);
        EnsureNodePartStyle(config.Progress, defaultConfig.Progress);
    }

    private static void EnsureNodePartStyle(
        StatusTimerOverlayConfig.NodePartConfig nodePart,
        StatusTimerOverlayConfig.NodePartConfig defaultNodePart)
    {
        switch (nodePart.StyleKind)
        {
            case NodePartStyleKind.Text:
                if (nodePart.Style == null && defaultNodePart.Style != null) {
                    nodePart.Style = defaultNodePart.Style.Clone();
                }

                nodePart.StyleBar = null;
                break;
            case NodePartStyleKind.Bar:
                if (nodePart.StyleBar == null && defaultNodePart.StyleBar != null) {
                    nodePart.StyleBar = defaultNodePart.StyleBar.Clone();
                }

                nodePart.Style = null;
                break;
            case NodePartStyleKind.Icon:
            case NodePartStyleKind.None:
            default:
                nodePart.Style = null;
                nodePart.StyleBar = null;
                break;
        }
    }

    public static void MigrateLegacyConfig(StatusTimerOverlayConfig config)
    {
        EnsureAllNodePartStyleKindsSet(config);
        EnsureAllNodePartStylesConsistent(config);
    }
}
