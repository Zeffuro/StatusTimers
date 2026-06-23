using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using System;
using System.Runtime.CompilerServices;

namespace StatusTimers.Nodes.LayoutNodes;

internal static class ConfigLayoutRecalculation {
    public static void RecalculateBottomUp(NodeBase node) {
        if (!node.IsVisible) {
            return;
        }

        if (node is not ILayoutListNode layoutNode) {
            return;
        }

        foreach (var childNode in layoutNode.Nodes) {
            RecalculateBottomUp(childNode);
        }

        layoutNode.RecalculateLayout();
    }

    public static void UpdateScrollParams(ScrollingNode<ConfigVerticalListNode> scrollingNode) {
        var state = UseManualMouseWheel(scrollingNode);
        UpdateScrollParamsCore(scrollingNode, state);
        ApplyManualScroll(scrollingNode, state);
    }

    private static void UpdateScrollParamsCore(
        ScrollingNode<ConfigVerticalListNode> scrollingNode,
        ManualScrollState state
    ) {
        var barHeight = Math.Max(1, (int)MathF.Ceiling(scrollingNode.ScrollingCollisionNode.Height));
        var contentHeight = Math.Max(1, (int)MathF.Ceiling(scrollingNode.ContentNode.Height));
        var effectiveContentHeight = Math.Max(contentHeight, barHeight);

        state.HasOverflow = contentHeight > barHeight;

        if (state.BarHeight == barHeight && state.ContentHeight == effectiveContentHeight) {
            return;
        }

        state.BarHeight = barHeight;
        state.ContentHeight = effectiveContentHeight;

        scrollingNode.ScrollBarNode.UpdateScrollParams(barHeight, effectiveContentHeight);
        DisableScrollbarInput(scrollingNode.ScrollBarNode);
    }

    private static unsafe ManualScrollState UseManualMouseWheel(
        ScrollingNode<ConfigVerticalListNode> scrollingNode
    ) {
        if (ManualScrollStates.TryGetValue(scrollingNode, out var state)) {
            return state;
        }

        state = new ManualScrollState {
            ScrollPosition = Math.Max(0, (int)MathF.Round(-scrollingNode.ContentNode.Y)),
            ScrollSpeed = scrollingNode.ScrollBarNode.ScrollSpeed
        };
        ManualScrollStates.Add(scrollingNode, state);

        var listener = (AtkEventListener*)scrollingNode.ScrollBarNode;
        ((AtkResNode*)scrollingNode.ClippingContentNode)->AtkEventManager.UnregisterEvent(
            AtkEventType.MouseWheel, 5, listener, false);
        ((AtkResNode*)scrollingNode.ScrollingCollisionNode)->AtkEventManager.UnregisterEvent(
            AtkEventType.MouseWheel, 5, listener, false);
        ((AtkResNode*)scrollingNode.ContentNode)->AtkEventManager.UnregisterEvent(
            AtkEventType.MouseWheel, 5, listener, false);

        scrollingNode.ClippingContentNode.AddEvent(AtkEventType.MouseWheel, OnMouseWheel);
        scrollingNode.ScrollingCollisionNode.AddEvent(AtkEventType.MouseWheel, OnMouseWheel);
        scrollingNode.ContentNode.AddEvent(AtkEventType.MouseWheel, OnMouseWheel);

        return state;

        void OnMouseWheel(
            AtkEventListener* thisPtr,
            AtkEventType eventType,
            int eventParam,
            AtkEvent* atkEvent,
            AtkEventData* atkEventData
        ) {
            if (!state.HasOverflow) {
                return;
            }

            state.ScrollSpeed = scrollingNode.ScrollBarNode.ScrollSpeed;
            state.ScrollPosition += atkEventData->MouseData.WheelDirection >= 1
                ? -state.ScrollSpeed
                : state.ScrollSpeed;
            ApplyManualScroll(scrollingNode, state);
            atkEvent->SetEventIsHandled();
        }
    }

    private static void ApplyManualScroll(
        ScrollingNode<ConfigVerticalListNode> scrollingNode,
        ManualScrollState state
    ) {
        var viewportHeight = Math.Max(0.0f, scrollingNode.ScrollingCollisionNode.Height);
        var contentHeight = Math.Max(0.0f, scrollingNode.ContentNode.Height);
        var maxScroll = Math.Max(0, (int)MathF.Ceiling(contentHeight - viewportHeight));

        state.HasOverflow = maxScroll > 0;
        state.ScrollPosition = Math.Clamp(state.ScrollPosition, 0, maxScroll);
        scrollingNode.ScrollBarNode.ScrollPosition = state.ScrollPosition;
        scrollingNode.ContentNode.Y = -state.ScrollPosition;
    }

    private static void DisableScrollbarInput(ScrollBarNode scrollBarNode) {
        scrollBarNode.IsAcceptingMouseWheelEvents = false;
        scrollBarNode.RemoveNodeFlags(
            NodeFlags.RespondToMouse,
            NodeFlags.HasCollision,
            NodeFlags.EmitsEvents,
            NodeFlags.Focusable
        );
        scrollBarNode.CollisionNode.RemoveNodeFlags(
            NodeFlags.RespondToMouse,
            NodeFlags.HasCollision,
            NodeFlags.EmitsEvents,
            NodeFlags.Focusable
        );
    }

    private sealed class ManualScrollState {
        public int BarHeight;
        public int ContentHeight;
        public bool HasOverflow;
        public int ScrollPosition;
        public int ScrollSpeed;
    }

    private static readonly ConditionalWeakTable<
        ScrollingNode<ConfigVerticalListNode>,
        ManualScrollState
    > ManualScrollStates = new();
}
