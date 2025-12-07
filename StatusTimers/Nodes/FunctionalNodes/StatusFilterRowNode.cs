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

    public required Action? OnRemove { get => _statusRemoveButtonNode.OnClick; set => _statusRemoveButtonNode.OnClick = value;}

    public StatusFilterRowNode(Status status) {
        Status = status;

        _statusIconNode = new IconImageNode
        {
            NodeId = 2,
            Y = -4,
            Size = new System.Numerics.Vector2(24, 32),
            IsVisible = true,
            IconId = status.Icon,
            FitTexture = true
        };
        AddNode(_statusIconNode);

        _statusIdTextNode = new TextNode
        {
            NodeId = 3,
            String = status.RowId.ToString() + " ",
            IsVisible = true,
            Height = 24,
            Width = 50,
            AlignmentType = AlignmentType.Right
        };
        AddNode(_statusIdTextNode);

        _statusNameTextNode = new TextNode
        {
            NodeId = 4,
            SeString = status.Name,
            IsVisible = true,
            Height = 24,
            Width = 220,
            AlignmentType = AlignmentType.Left
        };
        AddNode(_statusNameTextNode);

        _statusRemoveButtonNode = new TextButtonNode {
            NodeId = 5,
            String = "-",
            Width = 32,
            Height = 28,
            IsVisible = true
        };
        AddNode(_statusRemoveButtonNode);
    }

    public Status Status { get; }
}
