using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;

namespace StatusTimers.Models;

public class TextStyle {
    public float Width { get; set; }
    public float Height { get; set; }
    public int FontSize { get; set; }
    public FontType FontType { get; set; }
    public Vector4 TextColor { get; set; }
    public Vector4 TextOutlineColor { get; set; }
    public TextFlags TextFlags { get; set; }
}
