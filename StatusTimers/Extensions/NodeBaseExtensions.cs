using KamiToolKit.Nodes;
using KamiToolKit.System;

namespace StatusTimers.Extensions;

public static class NodeBaseExtensions
{
    public static void SetText(this NodeBase node, string value)
    {
        switch (node)
        {
            case TextNode tn: tn.Text = value; break;
            case TextNineGridNode ngn: ngn.Label = value; break;
            default: Services.Services.Logger.Warning($"[NodeBaseExtensions] SetText called on unsupported node type: {node?.GetType().Name}"); break;
        }
    }
}
