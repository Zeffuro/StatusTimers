using Dalamud.Game.Addon.Events;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.System;
using StatusTimers.Helpers;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class ColorPickerAddon : NativeAddon
{
    private Action<Vector4>? _onPicked;
    private Vector4 _workingColor;
    private float _hue, _saturation, _value; // HSV representation
    private BackgroundImageNode? _colorPreviewBackground;
    private BackgroundImageNode? _colorPreview;
    private TextInputNode? _hexInput;
    private NumericInputNode? _redInput, _greenInput, _blueInput, _alphaInput;
    private ImGuiImageNode? _svSquareNode, _hueBarNode, _alphaBarNode;
    private IDalamudTextureWrap? _svTexture, _hueTexture, _alphaTexture;
    private bool _isDraggingSV, _isDraggingHue, _isDraggingAlpha;
    private bool _isUpdating = false;

    public ColorPickerAddon(OverlayManager overlayManager) {
        Task.Run(() => {
            LoadTexturesBlocking();
        });
    }

    public void Show(Vector4 initialColor, Action<Vector4> onPicked)
    {
        _workingColor = initialColor;
        _onPicked = onPicked;
        Open();
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        GlobalServices.Framework.RunOnFrameworkThread(LoadTexturesBlocking);
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
        var pickerRow = new HorizontalListNode<NodeBase>
        {
            IsVisible = true,
            Width = 320,
            Height = 170,
            ItemHorizontalSpacing = 10
        };
        mainList.AddNode(pickerRow);

        _svSquareNode = new ImGuiImageNode()
        {
            IsVisible = true,
            Width = 128,
            Height = 128,
            WrapMode = 2,
            Color = new Vector4(0, 0, 0, 1),
            LoadedTexture = _svTexture,
            EnableEventFlags = true,
        };

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_svTexture != null) {
                _svSquareNode.LoadTexture(_svTexture);
                _svSquareNode.TextureSize = _svTexture.Size;
            }
        });

        _svSquareNode.AddEvent(AddonEventType.MouseDown, OnSVMouse);
        _svSquareNode.AddEvent(AddonEventType.MouseMove, OnSVMouse);
        _svSquareNode.AddEvent(AddonEventType.MouseUp, OnSVMouse);

        pickerRow.AddNode(_svSquareNode);

        // Hue Bar (vertical, 128px)
        _hueBarNode = new ImGuiImageNode
        {
            IsVisible = true,
            Width = 20,
            Height = 128,
            LoadedTexture = _hueTexture,
            EnableEventFlags = true,
        };
        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_hueTexture != null) {
                _hueBarNode.LoadTexture(_hueTexture);
                _hueBarNode.TextureSize = _hueTexture.Size;
            }
        });

        _hueBarNode.AddEvent(AddonEventType.MouseDown, OnHueMouse);
        _hueBarNode.AddEvent(AddonEventType.MouseMove, OnHueMouse);
        _hueBarNode.AddEvent(AddonEventType.MouseUp, OnHueMouse);

        pickerRow.AddNode(_hueBarNode);

        // Alpha Bar (vertical, 128px)
        _alphaBarNode = new ImGuiImageNode
        {
            IsVisible = true,
            Width = 20,
            Height = 128,
            LoadedTexture = _alphaTexture,
            EnableEventFlags = true,
        };
        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_alphaTexture != null) {
                _alphaBarNode.LoadTexture(_alphaTexture);
                _alphaBarNode.TextureSize = _alphaTexture.Size;
            }
        });

        _alphaBarNode.AddEvent(AddonEventType.MouseDown, OnAlphaMouse);
        _alphaBarNode.AddEvent(AddonEventType.MouseMove, OnAlphaMouse);
        _alphaBarNode.AddEvent(AddonEventType.MouseUp, OnAlphaMouse);

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
            FitPadding =  0,
        };
        mainList.AddNode(inputRow);

        _redInput = AddColorInput(inputRow, "R", (int)(_workingColor.X * 255), val => {
            SetComponent(0, ClampMax(val));
        });

        _greenInput = AddColorInput(inputRow, "G", (int)(_workingColor.Y * 255), val => {
            SetComponent(1, ClampMax(val));
        });

        _blueInput = AddColorInput(inputRow, "B", (int)(_workingColor.Z * 255), val => {
            SetComponent(2, ClampMax(val));
        });

        _alphaInput = AddColorInput(inputRow, "A", (int)(_workingColor.W * 255), val => {
            SetComponent(3, ClampMax(val));
        });

        // Hex Input
        _hexInput = new TextInputNode
        {
            IsVisible = true,
            Width = 200,
            Height = 28,
            MaxCharacters = 0,
            ShowLimitText = false,
            String = ColorToHex(_workingColor),
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
            Label = "OKdd",
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

    private void LoadTexturesBlocking() {
        var svPath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\sv_greyscale.png");
        var huePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\hue_bar.png");
        var alphaPath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\alpha_bar.png");

        _svTexture = GlobalServices.TextureProvider.GetFromFile(svPath).RentAsync().Result;
        _hueTexture = GlobalServices.TextureProvider.GetFromFile(huePath).RentAsync().Result;
        _alphaTexture = GlobalServices.TextureProvider.GetFromFile(alphaPath).RentAsync().Result;
    }

    // --- Input/Update helpers ---

    private NumericInputNode AddColorInput(HorizontalFlexNode<NodeBase> row, string label, int value, Action<int> onChanged)
    {
        row.AddNode(new TextNode
        {
            Text = label,
            IsVisible = true,
            Width = 14,
            Height = 28,
            FontSize = 14,
        });
        var node = new NumericInputNode()
        {
            IsVisible = true,
            Width = 80,
            Height = 28,
            Min = 0,
            Max = 255,
            Value = value,
            OnValueUpdate = onChanged
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
    private void OnColorPickerMouse(
        AddonEventData eventData,
        ImGuiImageNode? node,
        ref bool isDragging,
        Action<float, float> updateAction,
        bool verticalOnly = false)
    {
        var mouseData = eventData.GetMouseData();
        if (node == null) {
            return;
        }

        if (mouseData.ButtonId != 0) {
            return;
        }

        float x = (mouseData.PosX - node.ScreenX) / node.Width;
        float y = (mouseData.PosY - node.ScreenY) / node.Height;

        switch (eventData.AtkEventType)
        {
            case AddonEventType.MouseDown:
                isDragging = true;
                UpdateFromMouse(mouseData, node, updateAction, verticalOnly);
                break;
            case AddonEventType.MouseMove when isDragging:
                if (x < 0 || x > 1 || y < 0 || y > 1) {
                    isDragging = false;
                    return;
                }
                UpdateFromMouse(mouseData, node, updateAction, verticalOnly);
                break;
            case AddonEventType.MouseUp:
                _isDraggingSV = false;
                _isDraggingHue = false;
                _isDraggingAlpha = false;

                break;
        }
    }

    private void UpdateFromMouse(
        AtkEventData.AtkMouseData mouseData,
        ImGuiImageNode node,
        Action<float, float> updateAction,
        bool verticalOnly)
    {
        float x = (mouseData.PosX - node.ScreenX) / node.Width;
        float y = (mouseData.PosY - node.ScreenY) / node.Height;
        if (verticalOnly) {
            updateAction(0, y);
        }
        else {
            updateAction(x, y);
        }
    }

    // Attach these handlers to your nodes:
    private void OnSVMouse(AddonEventData eventData)
    {
        OnColorPickerMouse(
            eventData,
            _svSquareNode,
            ref _isDraggingSV,
            (x, y) => {
                _saturation = ClampMin(x);
                _value = 1f - ClampMin(y);
                _workingColor = HSVToColor(_hue, _saturation, _value, _workingColor.W);
                UpdateAllFields();
            }
        );
    }

    private void OnHueMouse(AddonEventData eventData)
    {
        OnColorPickerMouse(
            eventData,
            _hueBarNode,
            ref _isDraggingHue,
            (x, y) => {
                _hue = ClampMin(y);
                _workingColor = HSVToColor(_hue, _saturation, _value, _workingColor.W);
                UpdateAllFields();
            },
            verticalOnly: true
        );
    }

    private void OnAlphaMouse(AddonEventData eventData)
    {
        OnColorPickerMouse(
            eventData,
            _alphaBarNode,
            ref _isDraggingAlpha,
            (x, y) => {
                float a = 1f - ClampMin(y);
                _workingColor.W = a;
                UpdateAllFields();
            },
            verticalOnly: true
        );
    }

    // --- Field Update ---

    private void UpdateAllFields()
    {
        _isUpdating = true;

        if (_colorPreview != null) {
            _colorPreview.Color = _workingColor;
        }

        if (_redInput != null) {
            _redInput.Value = (int)(_workingColor.X * 255f);
        }

        if (_greenInput != null) {
            _greenInput.Value = (int)(_workingColor.Y * 255f);
        }

        if (_blueInput != null) {
            _blueInput.Value = (int)(_workingColor.Z * 255f);
        }

        if (_alphaInput != null) {
            _alphaInput.Value = (int)(_workingColor.W * 255f);
        }

        if (_hexInput != null) {
            _hexInput.String = ColorToHex(_workingColor);
        }

        if (_svSquareNode != null) {
            UpdateSVSquareAppearance();
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

    private static float ClampMin(float v) => Math.Max(0, Math.Min(1, v));
    private static int ClampMax(int v) => Math.Max(0, Math.Min(255, v));

    private void UpdateSVSquareAppearance()
    {
        if (_svSquareNode != null)
        {
            //_svSquareNode.AddColor = HSVToColor(_hue, 1f, 1f, 1f).AsVector3();
            //_svSquareNode = HSVToColor(_hue, 1f, 1f, 1f).AsVector3(); // Full S/V at current hue
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
