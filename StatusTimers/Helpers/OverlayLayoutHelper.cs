using StatusTimers.Config;
using System;
using System.Numerics;

namespace StatusTimers.Helpers;

public static class OverlayLayoutHelper {
    public static Vector2 CalculateOverlaySize(StatusTimerOverlayConfig config) {

        float totalWidth;
        float totalHeight;

        int maxItems = config.MaxStatuses;
        int itemsPerLine = Math.Min(config.ItemsPerLine, maxItems);

        if (config.FillRowsFirst) {
            int numRows = (int)Math.Ceiling(maxItems / (double)itemsPerLine);

            int itemsInLastRow = maxItems % itemsPerLine;
            if (itemsInLastRow == 0 && maxItems > 0) {
                itemsInLastRow = itemsPerLine;
            }

            float widestRowWidth = Math.Max(
                itemsPerLine * config.RowWidth + (itemsPerLine - 1) * config.StatusHorizontalPadding,
                itemsInLastRow * config.RowWidth + Math.Max(0, (itemsInLastRow - 1)) * config.StatusHorizontalPadding
            );

            float allRowsHeight = numRows * config.RowHeight +
                                  (numRows - 1) * config.StatusVerticalPadding;

            totalWidth = widestRowWidth;
            totalHeight = allRowsHeight;
        }
        else {
            int numCols = (int)Math.Ceiling(maxItems / (double)itemsPerLine);

            int itemsInLastCol = maxItems % itemsPerLine;
            if (itemsInLastCol == 0 && maxItems > 0) {
                itemsInLastCol = itemsPerLine;
            }

            float tallestColHeight = Math.Max(
                itemsPerLine * config.RowHeight + (itemsPerLine - 1) * config.StatusVerticalPadding,
                itemsInLastCol * config.RowHeight + Math.Max(0, (itemsInLastCol - 1)) * config.StatusVerticalPadding
            );

            float allColsWidth = numCols * config.RowWidth +
                                 (numCols - 1) * config.StatusHorizontalPadding;

            totalWidth = allColsWidth;
            totalHeight = tallestColHeight;
        }

        return new Vector2(Math.Max(0, totalWidth), Math.Max(0, totalHeight));
    }
}
