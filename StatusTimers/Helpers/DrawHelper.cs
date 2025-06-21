using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;

namespace StatusTimers.Helpers;

internal static class DrawHelper {
    public static void DrawIcon(uint iconId) {
        GameIconLookup lookup = new(iconId);
        IDalamudTextureWrap? textureWrap = Services.TextureProvider.GetFromGameIcon(lookup).GetWrapOrDefault();
        if (textureWrap != null) {
            ImGui.Image(textureWrap.ImGuiHandle, textureWrap.Size);
        }
    }
}
