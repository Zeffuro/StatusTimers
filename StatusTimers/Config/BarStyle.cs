using System;
using System.Numerics;
using Newtonsoft.Json;
using StatusTimers.Enums;

namespace StatusTimers.Config;

public class BarStyle
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

    public Vector4? ProgressColor {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public Vector4? BackgroundColor {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public Vector4? BorderColor {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public bool? BorderVisible {
        get;
        set {
            if (field != value) {
                field = value;
                _isDirty = true;
                Changed?.Invoke();
            }
        }
    }

    public ProgressBarType BarType {
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

    public ProgressBarColorTreatment ColorTreatment {
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

    public BarStyle Clone() => new() {
        ProgressColor = ProgressColor,
        BackgroundColor = BackgroundColor,
        BorderColor = BorderColor,
        BorderVisible = BorderVisible,
        BarType = BarType,
        ColorTreatment = ColorTreatment
    };

    public void CopyMissingFrom(BarStyle defaults) {
        BorderColor ??= defaults.BorderColor;
        BorderVisible ??= defaults.BorderVisible;
        ProgressColor ??= defaults.ProgressColor;
        BackgroundColor ??= defaults.BackgroundColor;
    }
}
