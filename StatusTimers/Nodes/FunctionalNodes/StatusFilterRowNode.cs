using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class StatusFilterRowNode : HorizontalListNode {
    private readonly IconImageNode _statusIconNode;
    private readonly TextNode _statusIdTextNode;
    private readonly TextNode _statusNameTextNode;
    private readonly TextButtonNode _statusRemoveButtonNode;

    private readonly Action _onRemove;

    public StatusFilterRowNode(Status status, Action onRemove) {
        Status = status;
        _onRemove = onRemove;

        _statusIconNode = new IconImageNode
        {
            Y = -4,
            Size = new System.Numerics.Vector2(24, 32),
            IsVisible = true,
            IconId = status.Icon
        };
        AddNode(_statusIconNode);

        _statusIdTextNode = new TextNode
        {
            String = status.RowId.ToString(),
            IsVisible = true,
            Height = 24,
            Width = 60,
            AlignmentType = AlignmentType.Right
        };
        AddNode(_statusIdTextNode);

        _statusNameTextNode = new TextNode
        {
            ReadOnlySeString = status.Name,
            IsVisible = true,
            Height = 24,
            Width = 180,
            AlignmentType = AlignmentType.Left
        };
        AddNode(_statusNameTextNode);

        _statusRemoveButtonNode = new TextButtonNode {
            String = "-",
            Width = 32,
            Height = 28,
            IsVisible = true,
            OnClick = () => {
                _onRemove();
            }
        };
        AddNode(_statusRemoveButtonNode);
    }

    public Status Status { get; }

    public void RemoveButtonNodeOnClick() {
        _statusRemoveButtonNode.OnClick = null;
        RemoveNode(_statusRemoveButtonNode);
    }
}
