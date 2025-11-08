using KamiToolKit.Nodes;
using KamiToolKit.System;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Extensions;

public static class NodeBaseExtensions
{
    public static void SetText(this NodeBase node, string value)
    {
        switch (node)
        {
            case TextNode tn: tn.String = value; break;
            case TextNineGridNode ngn: ngn.String = value; break;
            default: GlobalServices.Logger.Warning($"[NodeBaseExtensions] SetText called on unsupported node type: {node.GetType().Name}"); break;
        }
    }
}
