using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;

namespace StatusTimers.Layout
{
    public class TextStyle : IEquatable<TextStyle>
    {
        public event Action? Changed;

        public int FontSize {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public FontType FontType {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4 TextColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public Vector4 TextOutlineColor {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public TextFlags TextFlags {
            get;
            set {
                if (field != value) {
                    field = value;
                    Changed?.Invoke();
                }
            }
        }

        public override bool Equals(object? obj) => Equals(obj as TextStyle);

        public bool Equals(TextStyle? other)
        {
            if (other is null) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return FontSize == other.FontSize
                   && FontType == other.FontType
                   && TextColor.Equals(other.TextColor)
                   && TextOutlineColor.Equals(other.TextOutlineColor)
                   && TextFlags == other.TextFlags;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FontSize, FontType, TextColor, TextOutlineColor, TextFlags);
        }
    }
}
