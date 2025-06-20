using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using StatusTimers.Helpers;
using StatusTimers.StatusSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Windows;

public abstract class StatusTimerWindow<TKey> : NativeAddon {
    private const int PoolSize = 100;

    private const float FramePadding = 8.0f;
    private const float UnitPadding = 10.0f;
    private const float VerticalPadding = UnitPadding / 4.0f;
    private readonly NodeKind _nodeKind;

    private readonly IStatusSource<TKey> _source;

    protected readonly Dictionary<TKey, StatusTimerNode> Active = new();
    protected readonly Stack<StatusTimerNode> Pool = new();

    private TextNode _headerNode;

    protected StatusTimerWindow(IStatusSource<TKey> source, NodeKind nodeKind) {
        _source = source;
        _nodeKind = nodeKind;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        WindowNode.IsVisible = false;

        _headerNode = new TextNode {
            IsVisible = true,
            X = FramePadding * 2 + 44,
            Width = 300,
            Height = 26,
            FontSize = 24,
            TextColor = ColorHelper.GetColor(50),
            TextOutlineColor = ColorHelper.GetColor(54),
            TextFlags = TextFlags.Edge,
            Text = Title
        };
        NativeController.AttachNode(_headerNode, this);

        for (int i = 0; i < PoolSize; i++) {
            StatusTimerNode node = new();
            NativeController.AttachNode(node, this);
            node.IsVisible = false;
            Pool.Push(node);
        }
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        IReadOnlyList<StatusInfo> current = _source.Fetch();
        Dictionary<TKey, StatusInfo> map = current.GroupBy(_source.KeyOf)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (TKey gone in Active.Keys.Except(map.Keys).ToArray()) {
            StatusTimerNode node = Active[gone];
            node.IsVisible = false;
            Pool.Push(node);
            Active.Remove(gone);
        }

        foreach (KeyValuePair<TKey, StatusInfo> kv in map)
            if (Active.TryGetValue(kv.Key, out StatusTimerNode? node)) {
                if (NeedsUpdate(node.StatusInfo, kv.Value))
                    node.StatusInfo = kv.Value;
            }
            else if (Pool.TryPop(out node)) {
                node.Kind = _nodeKind;
                node.StatusInfo = kv.Value;
                node.IsVisible = true;
                Active[kv.Key] = node;
            }

        LayoutNodes();
    }

    private static bool NeedsUpdate(StatusInfo a, StatusInfo b) {
        return Math.Abs(a.RemainingSeconds - b.RemainingSeconds) > 0.01f ||
               a.IsPermanent != b.IsPermanent ||
               a.Stacks != b.Stacks;
    }

    private void LayoutNodes() {
        float x = FramePadding;
        float y = _headerNode.Height + FramePadding;

        IOrderedEnumerable<StatusTimerNode> sortedNodes = Active.Values
            .OrderByDescending(node => node.StatusInfo.IsPermanent)
            .ThenByDescending(node => node.StatusInfo.RemainingSeconds);

        foreach (StatusTimerNode node in sortedNodes) {
            node.Position = new Vector2(x, y);
            y += node.Height + VerticalPadding;
        }
    }
}
