using KamiToolKit;
using KamiToolKit.Nodes;
using System;
using System.Linq;

namespace StatusTimers.Nodes.LayoutNodes
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

        public float HorizontalPadding {
            get;
            set {
                field = value;
                RecalculateLayout();
            }
        } = 1;

        public float VerticalPadding {
            get;
            set {
                field = value;
                RecalculateLayout();
            }
        } = 1;

        protected override void InternalRecalculateLayout()
        {
            if (NodeList.Count == 0) {
                return;
            }

            int itemsPerLine = Math.Max(1, ItemsPerLine);

            float nodeWidth = NodeList.First().Width;
            float nodeHeight = NodeList.First().Height;

            bool alignRight = GrowDirection is FlexGrowDirection.DownLeft or FlexGrowDirection.UpLeft;
            bool alignBottom = GrowDirection is FlexGrowDirection.UpRight or FlexGrowDirection.UpLeft;

            float startX = alignRight ? Width : 0f;
            float startY = alignBottom ? Height : 0f;

            int idx = 0;
            foreach (var node in NodeList)
            {
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
                    ? startX - (col + 1) * nodeWidth - col * HorizontalPadding
                    : startX + col * (nodeWidth + HorizontalPadding);

                float y = alignBottom
                    ? startY - (row + 1) * nodeHeight - row * VerticalPadding
                    : startY + row * (nodeHeight + VerticalPadding);

                node.X = x;
                node.Y = y;
                AdjustNode(node);
                idx++;
            }
        }
    }
}
