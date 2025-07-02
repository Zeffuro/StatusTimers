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
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Nodes;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class ColorPickerAddon : NativeAddon
{
    private Action<Vector4>? _onPicked;
    private Vector4 _initialColor;
    private Vector4 _workingColor;
    private float _hue, _saturation, _value; // HSV representation
    private ColorPreviewNode? _colorPreview;
    private TextInputNode? _hexInput;
    private NumericInputNode? _redInput, _greenInput, _blueInput, _alphaInput, _hueInput, _saturationInput, _valueInput;
    private ImGuiImageNode? _svSquareNode, _svSquareNode2, _svSquareNode3, _hueBarNode, _alphaBarNode;
    private IDalamudTextureWrap? _svTexture, _svTexture2, _svTexture3, _hueTexture, _alphaTexture;
    private SimpleComponentNode? _svContainer, _hueContainer, _alphaContainer;
    private NodeBase? _svCrosshair, _hueCrosshair, _alphaCrosshair;
    private bool _isDraggingSV, _isDraggingHue, _isDraggingAlpha;
    private bool _isUpdating = false;

    private const int OptionOffset = 18;

    public ColorPickerAddon(OverlayManager overlayManager) {
        Task.Run(LoadTexturesBlocking);
    }

    public void Show(Vector4 initialColor, Action<Vector4> onPicked)
    {
        _initialColor = initialColor;
        _workingColor = initialColor;
        ColorToHSV(_workingColor, out _hue, out _saturation, out _value);
        _onPicked = onPicked;
        Open();
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon)
    {
        GlobalServices.Framework.RunOnFrameworkThread(LoadTexturesBlocking);
        CreateAdvancedColorPicker();
    }

    private void CreateAdvancedColorPicker()
    {
        var mainList = new VerticalListNode
        {
            Position = ContentStartPosition with { X =+ OptionOffset },
            IsVisible = true,
            Width = Size.X,
            Height = Size.Y,
            ItemVerticalSpacing = 0,
            FitContents = false,
        };
        NativeController.AttachNode(mainList, this);

        var pickerRow = new HorizontalListNode<NodeBase>
        {
            IsVisible = true,
            Width = 320,
            Height = 256,
            ItemHorizontalSpacing = 10
        };
        mainList.AddNode(pickerRow);

        _svContainer = new SimpleComponentNode() {
            IsVisible = true,
            Size = new Vector2(256, 256),
            EnableEventFlags = true
        };

        _svContainer.AddEvent(AddonEventType.MouseDown, OnSVMouse);
        _svContainer.AddEvent(AddonEventType.MouseMove, OnSVMouse);
        _svContainer.AddEvent(AddonEventType.MouseUp, OnSVMouse);
        _svContainer.AddEvent(AddonEventType.MouseOut, OnSVMouse);

        pickerRow.AddNode(_svContainer);

        _svSquareNode = new ImGuiImageNode()
        {
            IsVisible = true,
            Size = new Vector2(256, 256),
            Alpha = 1f,
            WrapMode = 2,
            ImageNodeFlags = 0,
            EnableEventFlags = true
        };

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_svTexture != null) {
                _svSquareNode.LoadTexture(_svTexture);
                _svSquareNode.TextureSize = _svTexture.Size;
            }
        });
        NativeController.AttachNode(_svSquareNode, _svContainer);

        _svSquareNode2 = new ImGuiImageNode()
        {
            IsVisible = true,
            Size = new Vector2(256, 256),
            Alpha = 1f,
            WrapMode = 2,
            ImageNodeFlags = 0
        };

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_svTexture2 != null) {
                _svSquareNode2.LoadTexture(_svTexture2);
                _svSquareNode2.TextureSize = _svTexture2.Size;
            }
        });
        NativeController.AttachNode(_svSquareNode2, _svContainer);

        _svSquareNode3 = new ImGuiImageNode()
        {
            IsVisible = true,
            Size = new Vector2(256, 256),
            Alpha = 1,
            WrapMode = 2,
            ImageNodeFlags = 0
        };

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_svTexture3 != null) {
                _svSquareNode3.LoadTexture(_svTexture3);
                _svSquareNode3.TextureSize = _svTexture3.Size;
            }
        });
        NativeController.AttachNode(_svSquareNode3, _svContainer);

        _svCrosshair = new SimpleImageNode() {
            IsVisible = true,
            Size = new Vector2(32, 32),
            TexturePath = "ui/uld/AreaMap.tex",
            TextureSize = new Vector2(32, 32),
            TextureCoordinates = new Vector2(252, 0),
            WrapMode = 2,
            ImageNodeFlags = 0,
        };
        NativeController.AttachNode(_svCrosshair, _svContainer);

        _hueContainer = new SimpleComponentNode() {
            IsVisible = true,
            Size = new Vector2(20, 256),
            EnableEventFlags = true
        };

        _hueContainer.AddEvent(AddonEventType.MouseDown, OnHueMouse);
        _hueContainer.AddEvent(AddonEventType.MouseMove, OnHueMouse);
        _hueContainer.AddEvent(AddonEventType.MouseUp, OnHueMouse);
        _hueContainer.AddEvent(AddonEventType.MouseOut, OnHueMouse);

        // Hue Bar (vertical, 128px)
        _hueBarNode = new ImGuiImageNode
        {
            IsVisible = true,
            Width = 20,
            Height = 256,
            LoadedTexture = _hueTexture,
        };
        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_hueTexture != null) {
                _hueBarNode.LoadTexture(_hueTexture);
                _hueBarNode.TextureSize = _hueTexture.Size;
            }
        });
        NativeController.AttachNode(_hueBarNode, _hueContainer);

        _hueCrosshair = new ResNode() {
            IsVisible = true,
            Size = new Vector2(20, 2),
        };
        NativeController.AttachNode(_hueCrosshair, _hueContainer);

        NativeController.AttachNode(new BackgroundImageNode() {
            IsVisible = true,
            Color = KnownColor.White.Vector(),
            Size = new Vector2(20, 1),
        }, _hueCrosshair);
        NativeController.AttachNode(new BackgroundImageNode {
            IsVisible = true,
            Y = 1,
            Color = KnownColor.Gray.Vector(),
            Size = new Vector2(20, 1),
        }, _hueCrosshair);

        pickerRow.AddNode(_hueContainer);

        _alphaContainer = new SimpleComponentNode() {
            IsVisible = true,
            Size = new Vector2(20, 256),
            EnableEventFlags = true
        };

        _alphaContainer.AddEvent(AddonEventType.MouseDown, OnAlphaMouse);
        _alphaContainer.AddEvent(AddonEventType.MouseMove, OnAlphaMouse);
        _alphaContainer.AddEvent(AddonEventType.MouseUp, OnAlphaMouse);
        _alphaContainer.AddEvent(AddonEventType.MouseOut, OnAlphaMouse);

        // Alpha Bar (vertical, 128px)
        _alphaBarNode = new ImGuiImageNode
        {
            IsVisible = true,
            Width = 20,
            Height = 256,
            LoadedTexture = _alphaTexture,
            EnableEventFlags = true,
        };
        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_alphaTexture != null) {
                _alphaBarNode.LoadTexture(_alphaTexture);
                _alphaBarNode.TextureSize = _alphaTexture.Size;
            }
        });
        NativeController.AttachNode(_alphaBarNode, _alphaContainer);

        _alphaCrosshair = new ResNode() {
            IsVisible = true,
            Size = new Vector2(20, 2),
        };
        NativeController.AttachNode(_alphaCrosshair, _alphaContainer);

        NativeController.AttachNode(new BackgroundImageNode() {
            IsVisible = true,
            Color = KnownColor.White.Vector(),
            Size = new Vector2(20, 1),
        }, _alphaCrosshair);
        NativeController.AttachNode(new BackgroundImageNode {
            IsVisible = true,
            Y = 1,
            Color = KnownColor.Gray.Vector(),
            Size = new Vector2(20, 1),
        }, _alphaCrosshair);

        pickerRow.AddNode(_alphaContainer);

        VerticalListNode listNode = new() {
            IsVisible = true,
            Width = 64,
            Height = 128,
        };

        pickerRow.AddNode(listNode);

        listNode.AddNode(new TextNode {
            IsVisible = true,
            Width = 64,
            Height = TextStyles.OptionLabel.Height,
            Text = "Current",
            TextColor = TextStyles.OptionLabel.TextColor,
            FontSize = TextStyles.Defaults.FontSize,
            AlignmentType = AlignmentType.Center
        });

        _colorPreview = new ColorPreviewNode(GlobalServices.NativeController) {
            IsVisible = true,
            Size = new Vector2(100),
            Color = _workingColor
        };

        listNode.AddNode(_colorPreview);

        listNode.AddNode(new TextNode {
            IsVisible = true,
            Width = 64,
            Height = TextStyles.OptionLabel.Height,
            Text = "Original",
            TextColor = TextStyles.OptionLabel.TextColor,
            FontSize = TextStyles.Defaults.FontSize,
            AlignmentType = AlignmentType.Center
        });

        listNode.AddNode(new ColorPreviewNode(GlobalServices.NativeController) {
            IsVisible = true,
            Size = new Vector2(100),
            Color = _initialColor
        });

        // RGBA
        var rgbaInputRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 500,
            Height = 34,
            FitPadding = 2,
            AlignmentFlags = FlexFlags.CenterVertically
        };
        mainList.AddNode(rgbaInputRow);

        _redInput = AddColorInput(rgbaInputRow, "R", (int)(_workingColor.X * 255), val => {
            SetComponent(0, ClampMax(val));
        });

        _greenInput = AddColorInput(rgbaInputRow, "G", (int)(_workingColor.Y * 255), val => {
            SetComponent(1, ClampMax(val));
        });

        _blueInput = AddColorInput(rgbaInputRow, "B", (int)(_workingColor.Z * 255), val => {
            SetComponent(2, ClampMax(val));
        });

        _alphaInput = AddColorInput(rgbaInputRow, "A", (int)(_workingColor.W * 255), val => {
            SetComponent(3, ClampMax(val));
        });

        // HSV
        var hsvInputRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 375,
            Height = 34,
            FitPadding =  2,
            AlignmentFlags = FlexFlags.CenterVertically
        };

        mainList.AddNode(hsvInputRow);

        _hueInput = AddColorInput(hsvInputRow, "H", (int)(_hue * 360), val => {
            if (_isUpdating) {
                return;
            }

            _hue = ClampMin(val / 360f);
            _workingColor = HSVToColor(_hue, _saturation, _value, _workingColor.W);
            UpdateAllFields();
        }, 0, 360);

        _saturationInput = AddColorInput(hsvInputRow, "S", (int)(_saturation * 100), val => {
            if (_isUpdating) {
                return;
            }

            _saturation = ClampMin(val / 100f);
            _workingColor = HSVToColor(_hue, _saturation, _value, _workingColor.W);
            UpdateAllFields();
        }, 0, 100);

        _valueInput = AddColorInput(hsvInputRow, "V", (int)(_value * 100), val => {
            if (_isUpdating) {
                return;
            }

            _value = ClampMin(val / 100f);
            _workingColor = HSVToColor(_hue, _saturation, _value, _workingColor.W);
            UpdateAllFields();
        }, 0, 100);

        var thirdRow = new HorizontalFlexNode<NodeBase>
        {
            IsVisible = true,
            Width = 300,
            Height = 28,
            FitPadding =  2,
            AlignmentFlags = FlexFlags.CenterVertically
        };
        mainList.AddNode(thirdRow);

        // Hex Input
        _hexInput = new TextInputNode
        {
            IsVisible = true,
            X = 12,
            Width = 100,
            Height = 28,
            MaxCharacters = 0,
            ShowLimitText = false,
            String = ColorToHex(_workingColor),
            OnInputComplete = v => { if (TryParseHex(v.TextValue, out Vector4 col)) {
                    SetWorkingColorAndUpdate(col);
                }
            }
        };

        thirdRow.AddNode(_hexInput);

        thirdRow.AddNode(new TextDropDownNode
        {
            X = 0,
            Y = 0,
            IsVisible = true,
            Width = 220,
            Height = 28,
            MaxListOptions = 3,
            Options = CommonUIColor.CommonColors.Keys.ToList(),
            OnOptionSelected = selectedDisplayName => {
                var color = CommonUIColor.CommonColors[selectedDisplayName];
                SetWorkingColorAndUpdate(color);
            }
        });

        mainList.AddDummy(new ResNode(), 28);

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

        UpdateAllFields();
    }

    private void LoadTexturesBlocking() {
        var svPath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\sv_overlay_1_white.png");
        var svPath2 = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\sv_overlay_2_horizontal_gradient.png");
        var svPath3 = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\sv_overlay_3_vertical_black.png");
        var huePath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\hue_bar.png");
        var alphaPath = Path.Combine(GlobalServices.PluginInterface.AssemblyLocation.Directory?.FullName!,
            @"Media\Textures\alpha_bar.png");

        _svTexture = GlobalServices.TextureProvider.GetFromFile(svPath).RentAsync().Result;
        _svTexture2 = GlobalServices.TextureProvider.GetFromFile(svPath2).RentAsync().Result;
        _svTexture3 = GlobalServices.TextureProvider.GetFromFile(svPath3).RentAsync().Result;
        _hueTexture = GlobalServices.TextureProvider.GetFromFile(huePath).RentAsync().Result;
        _alphaTexture = GlobalServices.TextureProvider.GetFromFile(alphaPath).RentAsync().Result;
    }

    // --- Input/Update helpers ---

    private NumericInputNode AddColorInput(HorizontalFlexNode<NodeBase> row, string label, int value, Action<int> onChanged, int min = 0, int max = 255)
    {
        ResNode resNode = new ResNode {
            IsVisible = true,
            Width = 80,
            Height = 28
        };
        row.AddNode(resNode);

        TextNode labelNode = new TextNode
        {
            Text = label,
            IsVisible = true,
            Y = 3,
            Width = 14,
            Height = 28,
            FontSize = 14,
        };
        NativeController.AttachNode(labelNode, resNode);

        var node = new NumericInputNode()
        {
            IsVisible = true,
            X = 14,
            Width = 80,
            Height = 28,
            Min = min,
            Max = max,
            Value = value,
            OnValueUpdate = onChanged
        };
        NativeController.AttachNode(node, resNode);

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
    private unsafe void OnColorPickerMouse(
        AddonEventData eventData,
        SimpleComponentNode? containerNode,
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

        switch (eventData.AtkEventType)
        {
            case AddonEventType.MouseDown:
                if (containerNode != null && containerNode.CheckCollision((AtkEventData*)eventData.AtkEventDataPointer))
                {
                    GlobalServices.EventManager.SetCursor(AddonCursorType.Hidden);
                    isDragging = true;
                    UpdateFromMouse(mouseData, node, updateAction, verticalOnly);
                }
                break;
            case AddonEventType.MouseMove:
                if (isDragging) {
                    UpdateFromMouse(mouseData, node, updateAction, verticalOnly);
                }
                break;
            case AddonEventType.MouseUp:
                GlobalServices.EventManager.ResetCursor();
                isDragging = false;
                UpdateFromMouse(mouseData, node, updateAction, verticalOnly);
                UpdateAllFields();
                break;
            case AddonEventType.MouseOut:
                if (isDragging) {
                    GlobalServices.EventManager.ResetCursor();
                    isDragging = false;
                }
                else
                {
                    GlobalServices.EventManager.ResetCursor();
                }
                break;
        }
    }

    private void UpdateFromMouse(
        AtkEventData.AtkMouseData mouseData,
        ImGuiImageNode node,
        Action<float, float> updateAction,
        bool verticalOnly)
    {
        float x = ClampMin((mouseData.PosX - node.ScreenX) / node.Width);
        float y = ClampMin((mouseData.PosY - node.ScreenY) / node.Height);
        if (verticalOnly) {
            updateAction(0, y);
        }
        else {
            updateAction(x, y);
        }
    }

    private void OnSVMouse(AddonEventData eventData)
    {
        OnColorPickerMouse(
            eventData,
            _svContainer,
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
            _hueContainer,
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
            _alphaContainer,
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

    private void SetCrosshairPosition(NodeBase? crosshairNode, float targetX, float targetY)
    {
        if (crosshairNode == null) {
            return;
        }

        float offsetX = -16f;
        float offsetY = -16f;

        if (crosshairNode is ResNode lineCrosshair)
        {
            offsetX = -lineCrosshair.Size.X / 2f;
            offsetY = -lineCrosshair.Size.Y / 2f;
        }

        crosshairNode.Position = new Vector2(targetX + offsetX, targetY + offsetY);
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

        if (_hueInput != null) {
            _hueInput.Value = (int)(_hue * 360);
        }

        if (_saturationInput != null) {
            _saturationInput.Value = (int)(_saturation * 100);
        }

        if (_valueInput != null) {
            _valueInput.Value = (int)(_value * 100);
        }

        if (_hexInput != null) {
            _hexInput.String = ColorToHex(_workingColor);
        }

        if (_svSquareNode != null) {
            UpdateSVSquareAppearance();
        }

        if (_svCrosshair != null && _svSquareNode != null)
        {
            float crossX = _saturation * _svSquareNode.Width;
            float crossY = (1f - _value) * _svSquareNode.Height;

            crossX = Math.Max(0, Math.Min(_svSquareNode.Width, crossX));
            crossY = Math.Max(0, Math.Min(_svSquareNode.Height, crossY));

            SetCrosshairPosition(_svCrosshair, crossX, crossY);;
        }

        if (_hueCrosshair != null && _hueBarNode != null)
        {
            float crossY = _hue * _hueBarNode.Height;
            SetCrosshairPosition(_hueCrosshair, _hueBarNode.Width / 2f, crossY);
        }

        if (_alphaCrosshair != null && _alphaBarNode != null)
        {
            float crossY = (1f - _workingColor.W) * _alphaBarNode.Height;
            SetCrosshairPosition(_alphaCrosshair, _alphaBarNode.Width / 2f, crossY);
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
        if (_svSquareNode2 != null)
        {
            Vector4 pureHueColor = HSVToColor(_hue, 1f, 1f, 1f);
            _svSquareNode2.MultiplyColor = new Vector3(pureHueColor.X, pureHueColor.Y, pureHueColor.Z);
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
