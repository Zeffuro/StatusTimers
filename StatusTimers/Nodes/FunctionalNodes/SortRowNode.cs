using KamiToolKit.Nodes;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class SortRowNode : HorizontalFlexNode
{
    public SortRowNode(
        string labelText,
        Func<SortCriterion> criterionGetter,
        Action<SortCriterion> criterionSetter,
        Func<SortOrder> orderGetter,
        Action<SortOrder> orderSetter,
        Dictionary<SortCriterion, string> criteriaMap)
    {
        AddNode(new OptionLabelNode(labelText, false));

        AddNode(new TextDropDownNode
        {
            IsVisible = true,
            Width = 200,
            Height = 24,
            MaxListOptions = 5,
            Options = new List<string>(criteriaMap.Values),
            SelectedOption = criteriaMap.TryGetValue(criterionGetter(), out var selectedString) ? selectedString : criteriaMap.Values.First(),
            OnOptionSelected = selectedDisplayName =>
            {
                var selected = Util.FindKeyByValue(criteriaMap, selectedDisplayName);
                if (selected != null)
                {
                    criterionSetter(selected.Value);
                }
            }
        });

        var orderMap = new Dictionary<SortOrder, string>
        {
            { SortOrder.Ascending, "Ascending" },
            { SortOrder.Descending, "Descending" }
        };

        AddNode(new TextDropDownNode
        {
            IsVisible = true,
            Width = 180,
            Height = 24,
            MaxListOptions = 2,
            Options = new List<string>(orderMap.Values),
            SelectedOption = orderMap.TryGetValue(orderGetter(), out var sel) ? sel : orderMap.Values.First(),
            OnOptionSelected = selectedDisplayName =>
            {
                var selected = Util.FindKeyByValue(orderMap, selectedDisplayName);
                if (selected != null)
                {
                    orderSetter(selected.Value);
                }
            }
        });
    }
}
