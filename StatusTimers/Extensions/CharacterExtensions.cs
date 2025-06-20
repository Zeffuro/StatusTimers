using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace StatusTimers.Extensions;

public static class CharacterExtensions {
    public static unsafe bool IsHostile(this ICharacter character) {
        Character* chara = (Character*)character.Address;

        return character != null
               && (character.SubKind == (byte)BattleNpcSubKind.Enemy ||
                   character.SubKind == (byte)BattleNpcSubKind.BattleNpcPart)
               && chara->CharacterData.Battalion >
               0; // Since its not super clear, CharacterData.Battalion used for determining friend/enemy state
    }
}
