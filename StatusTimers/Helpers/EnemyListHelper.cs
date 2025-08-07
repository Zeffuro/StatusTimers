using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;

namespace StatusTimers.Helpers;

public static unsafe class EnemyListHelper {
    private static readonly Dictionary<uint, int> EntityIdToLocalIndex = new();

    // Call this once per frame or on enemy list change
    public static void UpdateEnemyListMapping() {
        var enemyListNumberInstance = EnemyListNumberArray.Instance();
        var enemyNumberArrayEnemies = enemyListNumberInstance->Enemies;
        int enemyCount = enemyListNumberInstance->Unk1;

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

        var enemyStringArrayMembers = EnemyListStringArray.Instance()->Members;
        if (enemyStringArrayMembers.IsEmpty || enemyStringArrayMembers.Length <= index)
        {
            return null;
        }

        string name = enemyStringArrayMembers[index].EnemyName;
        if (string.IsNullOrEmpty(name)) {
            return null;
        }

        char letterSymbol = name[0];
        return letterSymbol;
    }
}
