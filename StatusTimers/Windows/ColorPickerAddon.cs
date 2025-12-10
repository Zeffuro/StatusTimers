using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Widgets;
using StatusTimers.Enums;
using System;
using System.Linq;
using System.Numerics;

namespace StatusTimers.Windows;

public class ColorPickerAddon : NativeAddon {
    private Action<Vector4>? _onPicked;
    private Vector4 _initialColor;
    private Vector4 _workingColor;
    private Vector4 _defaultColor = new(1,1,1,1);
    private bool _confirmed;
    private bool _closing;
    private bool _suppressCallbacks;

    private ColorPickerWidget? _colorPickerWidget;
    private TextDropDownNode? _colorDropdown;
    private SliderNode? _sliderR, _sliderG, _sliderB, _sliderA;
    private bool _updatingFromSliders;

    private TextButtonNode? _confirmButton;
    private ColorOptionTextButtonNode? _defaultButton;
    private TextButtonNode? _cancelButton;

    public void Show(Vector4 initialColor, Action<Vector4> onPicked)
    {
        _initialColor = initialColor;
        _workingColor = initialColor;
        _onPicked = onPicked;
        _suppressCallbacks = false;
        Open();
        _onPicked?.Invoke(_workingColor);
        _colorPickerWidget?.SetColor(_workingColor);
        _closing = false; // reset closing flag when showing
    }

    public void Show(Vector4 initialColor, Vector4 defaultColor, Action<Vector4> onPicked) {
        _defaultColor = defaultColor;
        Show(initialColor, onPicked);
    }

    public void CloseSilently()
    {
        if (_closing) {
            return;
        }
        _suppressCallbacks = true;
        Close();
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        var mainList = new VerticalListNode {
            IsVisible = true,
            Position = ContentStartPosition,
            Width = Size.X,
            Height = Size.Y,
            FitContents = false,
            ItemSpacing = 2,
        };
        mainList.AttachNode(this);

        var contentRow = new HorizontalFlexNode {
            IsVisible = true,
            Width = Size.X - 24f,
            Height = Size.Y - 24f,
            FitPadding = 8,
        };
        mainList.AddNode(contentRow);

        _colorPickerWidget = new ColorPickerWidget {
            IsVisible = true,
            Width = 400,
            Height = 340,
        };
        _colorPickerWidget.SetColor(_workingColor);
        contentRow.AddNode(_colorPickerWidget);

        BuildSliders(mainList, 380, contentRow.Height - 28f - 8f);

        _colorDropdown = new TextDropDownNode {
            IsVisible = true,
            Width = 376,
            Height = 26,
            X = 1,
            MaxListOptions = 8,
            Options = CommonUIColor.CommonColors.Keys.ToList(),
            OnOptionSelected = displayName => {
                var color = CommonUIColor.CommonColors[displayName];
                _workingColor = color;
                _colorPickerWidget?.SetColor(color);
                _onPicked?.Invoke(color);
            }
        };
        mainList.AddNode(_colorDropdown);

        _confirmButton = new TextButtonNode {
            IsVisible = true,
            Size = new Vector2(100f, 24f),
            Position = ContentStartPosition + new Vector2(0f, ContentSize.Y - 24f),
            String = "Confirm",
            OnClick = OnOk,
        };
        _confirmButton.AttachNode(this);

        _defaultButton = new ColorOptionTextButtonNode {
            IsVisible = true,
            Size = new Vector2(100f, 24f),
            Position = ContentStartPosition + new Vector2(ContentSize.X / 2f - 50f, ContentSize.Y - 24f),
            String = "Default",
            DefaultColor = _defaultColor,
            DefaultHsvaColor = ColorHelpers.RgbaToHsv(_defaultColor),
            OnClick = ApplyDefault,
        };
        _defaultButton.AttachNode(this);

        _cancelButton = new TextButtonNode {
            IsVisible = true,
            Size = new Vector2(100f, 24f),
            Position = ContentStartPosition + new Vector2(ContentSize.X - 100f, ContentSize.Y - 24f),
            String = "Cancel",
            OnClick = OnCancel,
        };
        _cancelButton.AttachNode(this);

        _closing = false;
    }

    private void BuildSliders(VerticalListNode parent, float availableWidth, float availableHeight) {
        var sliderContainer = new VerticalListNode {
            IsVisible = true,
            Width = availableWidth,
            Height = Math.Min(availableHeight, 160f),
            FitContents = true,
            ItemSpacing = 2,
        };
        parent.AddNode(sliderContainer);

        var sliderInnerWidth = availableWidth - 34f;
        _sliderR = CreateChannelSlider("R", c => c.X, (c, v) => c with { X = v }, sliderInnerWidth);
        _sliderG = CreateChannelSlider("G", c => c.Y, (c, v) => c with { Y = v }, sliderInnerWidth);
        _sliderB = CreateChannelSlider("B", c => c.Z, (c, v) => c with { Z = v }, sliderInnerWidth);
        _sliderA = CreateChannelSlider("A", c => c.W, (c, v) => c with { W = v }, sliderInnerWidth);

        AddSliderRow(sliderContainer, "R", _sliderR, availableWidth);
        AddSliderRow(sliderContainer, "G", _sliderG, availableWidth);
        AddSliderRow(sliderContainer, "B", _sliderB, availableWidth);
        AddSliderRow(sliderContainer, "A", _sliderA, availableWidth);

        SyncSlidersToColor(_workingColor);
    }

    private void AddSliderRow(VerticalListNode container, string label, SliderNode? slider, float availableWidth) {
        var row = new HorizontalListNode() {
            IsVisible = true,
            Width = availableWidth,
            Height = 20f,
        };
        var text = new TextNode {
            IsVisible = true,
            Width = 16f,
            Height = 16f,
            FontSize = 12,
            String = label,
        };
        row.AddNode(text);
        if (slider != null) {
            row.AddNode(slider);
        }
        container.AddNode(row, false);
    }

    private SliderNode CreateChannelSlider(string label, Func<Vector4, float> getter, Func<Vector4, float, Vector4> setter, float sliderWidth) {
        var slider = new SliderNode {
            IsVisible = true,
            Width = sliderWidth,
            Height = 22f,
            Range = 0..255,
            Step = 1,
            DecimalPlaces = 0,
        };
        slider.Value = (int)Math.Round(getter(_workingColor) * 255);
        slider.OnValueChanged = v => {
            if (_updatingFromSliders) {
                return;
            }
            _workingColor = setter(_workingColor, v / 255f);
            _colorPickerWidget?.SetColor(_workingColor);
            _onPicked?.Invoke(_workingColor);
        };
        return slider;
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        if (_closing) { return; }
        if (_colorPickerWidget == null) {
            return;
        }
        var current = _colorPickerWidget.ColorPreviewWithInput.Color;
        if (!NearlyEqual(_workingColor, current)) {
            _workingColor = current;
            SyncSlidersToColor(_workingColor);
            _onPicked?.Invoke(_workingColor);
        }
    }

    private void ApplyDefault() {
        _workingColor = _defaultColor;
        _colorPickerWidget?.SetColor(_workingColor);
        SyncSlidersToColor(_workingColor);
        _onPicked?.Invoke(_workingColor);
    }

    private void SyncSlidersToColor(Vector4 color) {
        _updatingFromSliders = true;
        if (_sliderR != null) { _sliderR.Value = (int)Math.Round(color.X * 255); }
        if (_sliderG != null) { _sliderG.Value = (int)Math.Round(color.Y * 255); }
        if (_sliderB != null) { _sliderB.Value = (int)Math.Round(color.Z * 255); }
        if (_sliderA != null) { _sliderA.Value = (int)Math.Round(color.W * 255); }
        _updatingFromSliders = false;
    }

    private static bool NearlyEqual(in Vector4 a, in Vector4 b, float eps = 1e-4f)
    {
        return MathF.Abs(a.X - b.X) <= eps &&
               MathF.Abs(a.Y - b.Y) <= eps &&
               MathF.Abs(a.Z - b.Z) <= eps &&
               MathF.Abs(a.W - b.W) <= eps;
    }

    private void OnOk() { _confirmed = true; _closing = true; _onPicked?.Invoke(_workingColor); Close(); }
    private void OnCancel() { _confirmed = true; _closing = true; _workingColor = _initialColor; _colorPickerWidget?.SetColor(_workingColor); _onPicked?.Invoke(_initialColor); Close(); }

    protected override unsafe void OnHide(AtkUnitBase* addon)
    {
        if (!_suppressCallbacks && !_confirmed) { _onPicked?.Invoke(_initialColor); }
        _confirmed = false;
        _closing = false;
        _suppressCallbacks = false;
    }

    public Vector4 DefaultColor { get => _defaultColor; set { _defaultColor = value; if (_defaultButton != null) { _defaultButton.DefaultColor = value; _defaultButton.DefaultHsvaColor = ColorHelpers.RgbaToHsv(value); } } }
}
