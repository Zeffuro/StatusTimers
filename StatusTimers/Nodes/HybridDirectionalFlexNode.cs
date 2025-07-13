using KamiToolKit.Nodes;
using KamiToolKit.System;
using System;
using System.Linq;

namespace StatusTimers.Nodes
{
    public enum FlexGrowDirection
    {
        DownRight,
        DownLeft,
        UpRight,
        UpLeft
    }

    public class HybridDirectionalFlexNode : HybridDirectionalFlexNode<NodeBase> { }

    public class HybridDirectionalFlexNode<T> : LayoutListNode where T : NodeBase
    {
        public FlexGrowDirection GrowDirection { get;
            set {
                field = value;
                RecalculateLayout();
            }
        } = FlexGrowDirection.DownRight;

        public int ItemsPerLine {
            get;
            set {
                field = value;
                RecalculateLayout();
            }
        } = 1;

        public bool FillRowsFirst {
            get;
            set {
                field = value;
                RecalculateLayout();
            }
        } = true;

        public override void RecalculateLayout()
        {
            int visibleCount = NodeList.Count(n => n.IsVisible);
            if (visibleCount == 0) {
                return;
            }

            int itemsPerLine = Math.Max(1, ItemsPerLine);

            float nodeWidth = NodeList.First(n => n.IsVisible).Width;
            float nodeHeight = NodeList.First(n => n.IsVisible).Height;

            bool alignRight = GrowDirection is FlexGrowDirection.DownLeft or FlexGrowDirection.UpLeft;
            bool alignBottom = GrowDirection is FlexGrowDirection.UpRight or FlexGrowDirection.UpLeft;

            float startX = alignRight ? Width : 0f;
            float startY = alignBottom ? Height : 0f;

            int idx = 0;
            for (int i = 0; i < NodeList.Count; i++)
            {
                var node = NodeList[i];
                if (!node.IsVisible) {
                    continue;
                }

                int row, col;
                if (FillRowsFirst)
                {
                    row = idx / itemsPerLine;
                    col = idx % itemsPerLine;
                }
                else
                {
                    col = idx / itemsPerLine;
                    row = idx % itemsPerLine;
                }

                float x = alignRight
                    ? startX - (col + 1) * nodeWidth - col * ItemSpacing
                    : startX + col * (nodeWidth + ItemSpacing);

                float y = alignBottom
                    ? startY - (row + 1) * nodeHeight - row * ItemSpacing
                    : startY + row * (nodeHeight + ItemSpacing);

                node.X = x;
                node.Y = y;
                AdjustNode(node);
                idx++;
            }
        }
    }
}
