using Dalamud.Game.Addon.Events;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.System;
using StatusTimers.Helpers;
using StatusTimers.Windows;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Factories;

public class ColorPickerAddon : NativeAddon
{
    private Action<Vector4>? _onPicked;
    private Vector4 _workingColor;
    private float _hue, _saturation, _value; // HSV representation
    private BackgroundImageNode? _colorPreviewBackground;
    private BackgroundImageNode? _colorPreview;
    private SliderNode? _redSlider, _greenSlider, _blueSlider, _alphaSlider;
    private TextInputNode? _hexInput;
    private TextInputNode? _rInput, _gInput, _bInput, _aInput;
    private ImGuiImageNode? _svSquareNode, _hueBarNode, _alphaBarNode;
    private bool _isUpdating = false;

    public ColorPickerAddon(OverlayManager overlayManager)
    {
    }

    public void Show(Vector4 initialColor, Action<Vector4> onPicked)
    {
        _workingColor = initialColor;
        _onPicked = onPicked;
        Open();
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        //CreateColorPicker();
        CreateAdvancedColorPicker();
    }

    private void SetWorkingColor(Vector4 color)
    {
        _workingColor = color;
        ColorToHSV(color, out _hue, out _saturation, out _value);
    }

    private void CreateAdvancedColorPicker()
    {
        var mainList = new VerticalListNode
        {
            Position = ContentStartPosition,
            IsVisible = true,
            Width = Size.X,
            Height = Size.Y,
            ItemVerticalSpacing = 0,
            FitContents = false,
        };
        NativeController.AttachNode(mainList, this);

        // Picker Row: [SV Square][Spacing][Hue Bar][Spacing][Alpha Bar][Preview]
        var pickerRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 320,
            Height = 170,
            FitPadding = 8,
            AlignmentFlags = FlexFlags.CenterVertically,
        };
        mainList.AddNode(pickerRow);

        GlobalServices.Logger.Info(Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Textures\sv_greyscale.png"));
        // SV Square (128x128)
        _svSquareNode = new ImGuiImageNode()
        {
            IsVisible = true,
            Width = 128,
            Height = 128,
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Textures\sv_greyscale.png"),
        };
        //_svSquareNode.OnMouseDown += OnSVMouse;
        //_svSquareNode.OnMouseMove += OnSVMouse;
        pickerRow.AddNode(_svSquareNode);

        // Hue Bar (vertical, 128px)
        _hueBarNode = new ImGuiImageNode
        {
            IsVisible = true,
            Width = 20,
            Height = 128,
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Textures\hue_bar.png"),
        };
        //_hueBarNode.OnMouseDown += OnHueMouse;
        //_hueBarNode.OnMouseMove += OnHueMouse;
        pickerRow.AddNode(_hueBarNode);

        // Alpha Bar (vertical, 128px)
        _alphaBarNode = new ImGuiImageNode
        {
            IsVisible = true,
            Width = 20,
            Height = 128,
            TexturePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!, @"Media\Textures\alpha_bar.png"),
        };
        //_alphaBarNode.OnMouseDown += OnAlphaMouse;
        //_alphaBarNode.OnMouseMove += OnAlphaMouse;
        pickerRow.AddNode(_alphaBarNode);

        // Color Preview
        var previewContainer = new ResNode
        {
            IsVisible = true,
            Width = 64,
            Height = 64,
            X = 0,
        };
        pickerRow.AddNode(previewContainer);

        _colorPreviewBackground = new BackgroundImageNode
        {
            IsVisible = true,
            Color = KnownColor.Black.Vector(),
            Width = 52,
            Height = 52,
            Position = new Vector2(6, 6),
        };
        NativeController.AttachNode(_colorPreviewBackground, previewContainer);

        _colorPreview = new BackgroundImageNode
        {
            IsVisible = true,
            Color = _workingColor,
            Width = 48,
            Height = 48,
            Position = new Vector2(8, 8),
        };
        NativeController.AttachNode(_colorPreview, previewContainer);

        // Row: RGBA + Hex Input
        var inputRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 320,
            Height = 34,
            FitPadding = 8,
        };
        mainList.AddNode(inputRow);

        _rInput = AddColorInput(inputRow, "R", (int)(_workingColor.X * 255), v => { if (int.TryParse(v, out int val)) {
                SetComponent(0, ClampByte(val));
            }
        });

        _gInput = AddColorInput(inputRow, "G", (int)(_workingColor.Y * 255), v => { if (int.TryParse(v, out int val)) {
                SetComponent(1, ClampByte(val));
            }
        });

        _bInput = AddColorInput(inputRow, "B", (int)(_workingColor.Z * 255), v => { if (int.TryParse(v, out int val)) {
                SetComponent(2, ClampByte(val));
            }
        });

        _aInput = AddColorInput(inputRow, "A", (int)(_workingColor.W * 255), v => { if (int.TryParse(v, out int val)) {
                SetComponent(3, ClampByte(val));
            }
        });

        // Hex Input
        _hexInput = new TextInputNode
        {
            IsVisible = true,
            Width = 80,
            Height = 28,
            MaxCharacters = 9,
            //Text = ColorToHex(_workingColor),
            OnInputComplete = v => { if (TryParseHex(v.TextValue, out Vector4 col)) {
                    SetWorkingColorAndUpdate(col);
                }
            }
        };
        inputRow.AddNode(_hexInput);

        // Button row (OK | Cancel)
        var buttonRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 180,
            Height = 36,
            X = (Size.X - 180) / 2f,
            AlignmentFlags = FlexFlags.CenterHorizontally,
            FitPadding = 12
        };

        buttonRow.AddNode(new TextButtonNode
        {
            IsVisible = true,
            Height = 28,
            Width = 70,
            Label = "OKd",
            OnClick = OnOk
        });
        buttonRow.AddNode(new TextButtonNode
        {
            IsVisible = true,
            Height = 28,
            Width = 70,
            Label = "Cancel",
            OnClick = OnCancel
        });

        mainList.AddNode(buttonRow);

        UpdateAllFields();
    }

    // --- Input/Update helpers ---

    private TextInputNode AddColorInput(HorizontalFlexNode<NodeBase> row, string label, int value, Action<string> onChanged)
    {
        row.AddNode(new TextNode
        {
            Text = label,
            IsVisible = true,
            Width = 14,
            Height = 28,
            FontSize = 13,
        });
        var node = new TextInputNode
        {
            IsVisible = true,
            Width = 32,
            Height = 28,
            MaxCharacters = 3,
            ShowLimitText = false,
            Text = value.ToString(),
            OnInputComplete = (seString) => {
                onChanged(seString.TextValue);
            },
        };
        row.AddNode(node);
        return node;
    }

    private void SetComponent(int idx, int value)
    {
        if (_isUpdating) {
            return;
        }

        var c = _workingColor;
        switch (idx)
        {
            case 0: c.X = value / 255f; break;
            case 1: c.Y = value / 255f; break;
            case 2: c.Z = value / 255f; break;
            case 3: c.W = value / 255f; break;
        }
        SetWorkingColorAndUpdate(c);
    }

    private void SetWorkingColorAndUpdate(Vector4 color)
    {
        _workingColor = color;
        ColorToHSV(_workingColor, out _hue, out _saturation, out _value);
        UpdateAllFields();
    }

    // --- Mouse Event Handlers ---
/*
    private void OnSVMouse(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var node = _svSquareNode;
        if (node == null) return;
        float x = e.X / node.Width;
        float y = e.Y / node.Height;
        _saturation = Clamp01(x);
        _value = 1f - Clamp01(y);
        _workingColor = HSVToColor(_hue, _saturation, _value, _workingColor.W);
        UpdateAllFields();
    }

    private void OnHueMouse(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var node = _hueBarNode;
        if (node == null) return;
        float y = e.Y / node.Height;
        _hue = Clamp01(y);
        _workingColor = HSVToColor(_hue, _saturation, _value, _workingColor.W);
        UpdateAllFields();
    }

    private void OnAlphaMouse(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var node = _alphaBarNode;
        if (node == null) return;
        float y = e.Y / node.Height;
        float a = 1f - Clamp01(y);
        _workingColor.W = a;
        UpdateAllFields();
    }
*/
    // --- Field Update ---

    private void UpdateAllFields()
    {
        _isUpdating = true;
        // Update preview
        if (_colorPreview != null) {
            _colorPreview.Color = _workingColor;
        }

        // Update RGBA inputs
        if (_rInput != null) {
            _rInput.Text = ((int)(_workingColor.X * 255f)).ToString();
        }

        if (_gInput != null) {
            _gInput.Text = ((int)(_workingColor.Y * 255f)).ToString();
        }

        if (_bInput != null) {
            _bInput.Text = ((int)(_workingColor.Z * 255f)).ToString();
        }

        if (_aInput != null) {
            _aInput.Text = ((int)(_workingColor.W * 255f)).ToString();
        }

        // Update hex input
        if (_hexInput != null) {
            _hexInput.Text = ColorToHex(_workingColor);
            //_hexInput.Text = "Heydd";
        }

        _isUpdating = false;
    }

    // --- Color Conversion ---

    private static Vector4 HSVToColor(float h, float s, float v, float a)
    {
        // h, s, v: 0-1
        int i = (int)(h * 6f);
        float f = h * 6f - i;
        float p = v * (1f - s);
        float q = v * (1f - f * s);
        float t = v * (1f - (1f - f) * s);

        float r = 0, g = 0, b = 0;
        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }
        return new Vector4(r, g, b, a);
    }

    private static void ColorToHSV(Vector4 color, out float h, out float s, out float v)
    {
        float r = color.X, g = color.Y, b = color.Z;
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        v = max;

        float delta = max - min;
        if (max == 0f)
        {
            s = 0f;
            h = 0f;
            return;
        }
        s = delta / max;
        if (delta == 0)
        {
            h = 0;
        }
        else if (max == r)
        {
            h = (g - b) / delta % 6f / 6f;
        }
        else if (max == g)
        {
            h = (b - r) / delta / 6f + (1f / 3f);
        }
        else
        {
            h = (r - g) / delta / 6f + (2f / 3f);
        }

        if (h < 0)
        {
            h += 1f;
        }
    }

    private static string ColorToHex(Vector4 color)
    {
        int r = (int)(color.X * 255f);
        int g = (int)(color.Y * 255f);
        int b = (int)(color.Z * 255f);
        int a = (int)(color.W * 255f);
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    private static bool TryParseHex(string hex, out Vector4 color)
    {
        color = Vector4.One;
        if (hex.StartsWith("#")) {
            hex = hex.Substring(1);
        }

        if (hex.Length != 8) {
            return false;
        }

        if (int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out int r) &&
            int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out int g) &&
            int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out int b) &&
            int.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, null, out int a))
        {
            color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }
        return false;
    }

    private static float Clamp01(float v) => Math.Max(0, Math.Min(1, v));
    private static int ClampByte(int v) => Math.Max(0, Math.Min(255, v));

    private HorizontalListNode<NodeBase> CreateLabeledSliderRow(string label, ref SliderNode? slider, float initial, Action<int> setter)
    {
        var flex = new HorizontalListNode<NodeBase>
        {
            IsVisible = true,
            Width = 340,
            Height = 30,
        };

        flex.AddNode(new TextNode
        {
            IsVisible = true,
            Width = 20,
            Height = 30,
            Y = 2,
            FontSize = 15,
            TextColor = TextStyles.OptionLabel.TextColor,
            TextOutlineColor = TextStyles.Defaults.OutlineColor,
            Text = label
        });

        slider = new SliderNode
        {
            IsVisible = true,
            Min = 0,
            Max = 255,
            Step = 1,
            Value = (int)(initial * 255f),
            Width = 200,
            Height = 30,
            OnValueChanged = setter
        };
        flex.AddNode(slider);

        return flex;
    }

    private void CreateColorPicker()
    {
        var mainList = new VerticalListNode
        {
            Position = ContentStartPosition,
            IsVisible = true,
            Width = Size.X,
            Height = Size.Y,
            ItemVerticalSpacing = 0,
            FitContents = true,
        };
        NativeController.AttachNode(mainList, this);

        // Color preview area (background with color on top, centered)
        var previewContainer = new ResNode
        {
            Width = 64,
            Height = 64,
            IsVisible = true,
            // Center the preview horizontally
            X = (Size.X - 64) / 2f,
        };
        mainList.AddNode(previewContainer);

        _colorPreviewBackground = new BackgroundImageNode
        {
            Color = KnownColor.Black.Vector(),
            Width = 52,
            Height = 52,
            Position = new Vector2(6, 6),
            IsVisible = true,
        };
        NativeController.AttachNode(_colorPreviewBackground, previewContainer);

        _colorPreview = new BackgroundImageNode
        {
            Color = _workingColor,
            Width = 48,
            Height = 48,
            Position = new Vector2(8, 8),
            IsVisible = true,
        };
        NativeController.AttachNode(_colorPreview, previewContainer);

        // Sliders
        mainList.AddNode(CreateLabeledSliderRow("R", ref _redSlider, _workingColor.X, v => { _workingColor.X = v / 255f; UpdateColorPreview(); }));
        mainList.AddNode(CreateLabeledSliderRow("G", ref _greenSlider, _workingColor.Y, v => { _workingColor.Y = v / 255f; UpdateColorPreview(); }));
        mainList.AddNode(CreateLabeledSliderRow("B", ref _blueSlider, _workingColor.Z, v => { _workingColor.Z = v / 255f; UpdateColorPreview(); }));
        mainList.AddNode(CreateLabeledSliderRow("A", ref _alphaSlider, _workingColor.W, v => { _workingColor.W = v / 255f; UpdateColorPreview(); }));

        // Button row (OK | Cancel)
        var buttonRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 180,
            Height = 36,
            X = (Size.X - 180) / 2f,
            AlignmentFlags = FlexFlags.CenterHorizontally,
            FitPadding = 12
        };

        buttonRow.AddNode(new TextButtonNode
        {
            IsVisible = true,
            Height = 28,
            Width = 70,
            Label = "OK",
            OnClick = OnOk
        });
        buttonRow.AddNode(new TextButtonNode
        {
            IsVisible = true,
            Height = 28,
            Width = 70,
            Label = "Cancel",
            OnClick = OnCancel
        });

        mainList.AddNode(buttonRow);
    }

    private void UpdateColorPreview()
    {
        if (_colorPreview != null)
        {
            _colorPreview.Color = _workingColor;
        }
    }

    private void OnOk()
    {
        _onPicked?.Invoke(_workingColor);
        Close();
    }

    private void OnCancel()
    {
        Close();
    }
}
