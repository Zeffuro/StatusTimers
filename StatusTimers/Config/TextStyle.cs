using System;
using System.Numerics;
using Newtonsoft.Json;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusTimers.Config;

public class TextStyle
{
    public event Action? Changed;

    [JsonIgnore]
    private bool _isDirty;

    [JsonIgnore]
    public bool IsDirty
    {
        get
        {
            var wasDirty = _isDirty;
            _isDirty = false;
            return wasDirty;
        }
    }

    public int FontSize {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public FontType FontType {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public Vector4 TextColor {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public Vector4 TextOutlineColor {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public TextFlags TextFlags {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public AlignmentType? Alignment {
        get;
        set {
            if (field == value) {
                return;
            }

            field = value;
            _isDirty = true;
            Changed?.Invoke();
        }
    }

    public TextStyle Clone() => new() {
        FontSize = FontSize,
        FontType = FontType,
        TextColor = TextColor,
        TextOutlineColor = TextOutlineColor,
        TextFlags = TextFlags,
        Alignment = Alignment
    };

    public void CopyMissingFrom(TextStyle defaults) {
        if (FontSize == 0) {
            FontSize = defaults.FontSize;
        }
        if (FontType == default) {
            FontType = defaults.FontType;
        }
        if (TextColor == default) {
            TextColor = defaults.TextColor;
        }
        if (TextOutlineColor == default) {
            TextOutlineColor = defaults.TextOutlineColor;
        }
        if (TextFlags == default) {
            TextFlags = defaults.TextFlags;
        }
    }
}
