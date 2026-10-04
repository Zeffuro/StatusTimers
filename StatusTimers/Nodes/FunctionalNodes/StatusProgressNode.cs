using System;
using System.Numerics;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using StatusTimers.Enums;
using StatusTimers.Nodes.FunctionalNodes.Gauge;

namespace StatusTimers.Nodes.FunctionalNodes;

public sealed class StatusProgressNode : ResNode
{
    private readonly NodeBase barNode;
    private readonly ComponentGaugeProgressNode? gaugeNode;
    private readonly NineGridNode? legacyFillNode;
    private readonly NineGridNode? legacyBackgroundNode;
    private readonly NodeBase borderNode;
    private readonly bool ownsBorder;
    private readonly Vector3 legacyFillMultiply;
    private float progress;
    private Vector4 barColor;

    public ProgressBarType BarType { get; }

    public StatusProgressNode(ProgressBarType barType)
    {
        BarType = barType;
        switch (barType)
        {
            case ProgressBarType.Cast:
                gaugeNode = new ProgressBarCastGaugeNode();
                barNode = gaugeNode;
                break;
            case ProgressBarType.EnemyCast:
                gaugeNode = new ProgressBarEnemyCastGaugeNode();
                barNode = gaugeNode;
                break;
            case ProgressBarType.ToDo:
                gaugeNode = new ProgressBarToDoGaugeNode();
                barNode = gaugeNode;
                break;
            case ProgressBarType.PartyListHp:
                gaugeNode = new ProgressBarPartyListHpNode();
                barNode = gaugeNode;
                break;
            case ProgressBarType.LimitBreak:
                gaugeNode = new ProgressBarLimitBreakGaugeNode();
                barNode = gaugeNode;
                break;
            case ProgressBarType.ToDoLegacy:
                var toDo = new ProgressBarNode();
                barNode = toDo;
                legacyFillNode = toDo.ForegroundNode;
                legacyBackgroundNode = toDo.BackgroundNode;
                break;
            case ProgressBarType.EnemyCastLegacy:
                var enemyCast = new ProgressBarEnemyCastNode();
                barNode = enemyCast;
                legacyFillNode = enemyCast.ProgressNode;
                legacyBackgroundNode = enemyCast.BackgroundImageNode;
                break;
            default:
                var cast = new ProgressBarCastNode();
                barNode = cast;
                legacyFillNode = cast.ProgressNode;
                legacyBackgroundNode = cast.BackgroundImageNode;
                break;
        }

        barNode.IsVisible = true;
        barNode.AttachNode(this);
        legacyFillMultiply = legacyFillNode?.MultiplyColor ?? Vector3.One;
        var existingBorder = barNode is ProgressBarCastNode castBar
            ? castBar.BorderImageNode
            : gaugeNode?.BorderNode ?? (NodeBase?)gaugeNode?.BorderImageNode;
        if (existingBorder != null)
        {
            borderNode = existingBorder;
        }
        else
        {
            ownsBorder = true;
            borderNode = new SimpleNineGridNode
            {
                IsVisible = true,
                TexturePath = "ui/uld/Parameter_Gauge.tex",
                TextureSize = new Vector2(160, 20),
                TextureCoordinates = Vector2.Zero,
                LeftOffset = 20,
                RightOffset = 20,
            };
            borderNode.AttachNode(this);
        }
    }

    public float Progress
    {
        get => progress;
        set
        {
            progress = float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
            ApplyProgress();
        }
    }

    public ProgressBarColorTreatment ColorTreatment
    {
        get;
        set
        {
            field = value;
            if (gaugeNode != null) {
                gaugeNode.ColorTreatment = value;
            }

            ApplyBarColor();
        }
    }

    public Vector4 BarColor
    {
        get => barColor;
        set
        {
            barColor = value;
            ApplyBarColor();
        }
    }

    public Vector4 BackgroundColor
    {
        set
        {
            if (gaugeNode != null) {
                gaugeNode.BackgroundColor = value;
            }
            else if (legacyBackgroundNode != null) {
                GaugeColors.Apply(legacyBackgroundNode, value,
                    BarType == ProgressBarType.ToDoLegacy ? GaugeColorMode.Flat : GaugeColorMode.Additive);
            }
        }
    }

    public Vector4 BorderColor
    {
        set => GaugeColors.Apply(borderNode, value, GaugeColorMode.Additive);
    }

    public bool BorderVisible
    {
        get => borderNode.IsVisible;
        set => borderNode.IsVisible = value;
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        if (barNode == null) {
            return;
        }

        barNode.Size = Size;
        if (ownsBorder) {
            borderNode.Size = Size;
        }

        ApplyProgress();
    }

    private void ApplyProgress()
    {
        if (gaugeNode != null) {
            gaugeNode.Progress = progress;
        }
        else if (legacyFillNode != null) {
            legacyFillNode.Width = Width * progress;
        }
    }

    private void ApplyBarColor()
    {
        if (gaugeNode != null)
        {
            gaugeNode.BarColor = barColor;
            return;
        }

        if (legacyFillNode == null) {
            return;
        }

        var mode = ColorTreatment switch
        {
            ProgressBarColorTreatment.Flat => GaugeColorMode.Flat,
            ProgressBarColorTreatment.LegacyAdditive => GaugeColorMode.Additive,
            ProgressBarColorTreatment.NativeTint => BarType == ProgressBarType.EnemyCastLegacy
                ? GaugeColorMode.Multiply : GaugeColorMode.Additive,
            _ => BarType == ProgressBarType.ToDoLegacy ? GaugeColorMode.Flat : GaugeColorMode.Additive,
        };
        var multiplyColor = ColorTreatment switch
        {
            ProgressBarColorTreatment.LegacyAdditive => new Vector3(90, 75, 75) / 255,
            ProgressBarColorTreatment.NativeTint when BarType == ProgressBarType.CastLegacy => new Vector3(0.9f, 0.75f, 0.75f),
            _ => legacyFillMultiply,
        };
        GaugeColors.Apply(legacyFillNode, barColor, mode, multiplyColor);
    }
}
