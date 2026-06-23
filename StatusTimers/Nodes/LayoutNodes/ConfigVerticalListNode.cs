using System;
using System.Linq;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace StatusTimers.Nodes.LayoutNodes;

public class ConfigVerticalListNode : VerticalListNode {
    protected override void OnRecalculateLayout() {
        var visibleNodes = NodeList.Where(node => node.IsVisible).ToList();

        var startY = Anchor switch {
            VerticalListAnchor.Top => FirstItemSpacing,
            VerticalListAnchor.Bottom => Height,
            _ => 0.0f,
        };

        foreach (var node in visibleNodes) {
            if (Anchor is VerticalListAnchor.Bottom) {
                startY -= node.Height + ItemSpacing;
            }

            node.Y = startY;

            if (FitWidth) {
                node.Width = Width;
            }
            else {
                switch (Alignment) {
                    case VerticalListAlignment.Right:
                        node.X = Width - node.Width;
                        break;

                    case VerticalListAlignment.Left:
                        node.X = 0.0f;
                        break;
                }
            }

            AdjustNode(node);

            if (Anchor is VerticalListAnchor.Top) {
                startY += node.Height + ItemSpacing;
            }
        }

        if (FitContents) {
            Height = visibleNodes.Count is 0
                ? Math.Max(0.0f, FirstItemSpacing)
                : visibleNodes.Sum(node => node.Height) + ItemSpacing * (visibleNodes.Count - 1) + FirstItemSpacing;
        }
    }
}
