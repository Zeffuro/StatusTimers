using Dalamud.Game.Text;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;

namespace StatusTimers.Helpers;

public static unsafe class EnemyListHelper {
    private static readonly Dictionary<uint, int> EntityIdToLocalIndex = new();

    // Call this once per frame or on enemy list change
    public static void UpdateEnemyListMapping() {
        EntityIdToLocalIndex.Clear();
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.EnemyList);
        if (numberArray == null) {
            return;
        }

        var enemyListNumberInstance = EnemyListNumberArray.Instance();
        var enemyNumberArrayEnemies = enemyListNumberInstance->Enemies;
        int enemyCount = enemyListNumberInstance->EnemyCount;

        if(enemyCount == 0)
        {
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            EntityIdToLocalIndex[(uint)enemyNumberArrayEnemies[i].EntityId] = i;
        }
    }

    public static char? GetEnemyLetter(uint entityId) {
        if (!EntityIdToLocalIndex.TryGetValue(entityId, out int index)) {
            return null;
        }

        var enemyStringArray = EnemyListStringArray.Instance();
        if (enemyStringArray == null) {
            return null;
        }

        var enemyStringArrayMembers = enemyStringArray->Members;
        if (enemyStringArrayMembers.IsEmpty || enemyStringArrayMembers.Length <= index)
        {
            return null;
        }

        var name = enemyStringArrayMembers[index].EnemyName.AsReadOnlySeStringSpan().ExtractText();
        foreach (var character in name) {
            if (character is >= (char)SeIconChar.BoxedLetterA and <= (char)SeIconChar.BoxedLetterZ) {
                return character;
            }
        }
        return null;
    }

    public static void Clear()
        => EntityIdToLocalIndex.Clear();
}
