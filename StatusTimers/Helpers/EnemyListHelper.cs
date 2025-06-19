using System;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace StatusTimers.Helpers;

public static unsafe class EnemyListHelper
{
    private static readonly Dictionary<uint, int> ObjectIdToLocalIndex = new();

    // Call this once per frame or on enemy list change
    public static void UpdateEnemyListMapping()
    {
        var ui = Framework.Instance()->GetUIModule();
        if (ui == null) return;

        var rapture = ui->GetRaptureAtkModule();
        if (rapture == null) return;

        // Enemy info is at NumberArray index 21 (not 19)
        var numberArray = rapture->AtkModule.GetNumberArrayData(21);
        if (numberArray == null) return;

        int enemyCount = numberArray->AtkArrayData.Size < 2 ? 0 : numberArray->IntArray[1];
        ObjectIdToLocalIndex.Clear();

        for (int i = 0; i < enemyCount; i++)
        {
            int objectIdIndex = 8 + i * 6;
            if (numberArray->AtkArrayData.Size <= objectIdIndex)
                break;

            uint objectId = (uint)numberArray->IntArray[objectIdIndex];
            ObjectIdToLocalIndex[objectId] = i;
        }
    }

    public static char? GetEnemyLetter(uint objectId)
    {
        if (!ObjectIdToLocalIndex.TryGetValue(objectId, out int localIndex))
            return null;

        var ui = Framework.Instance()->GetUIModule();
        if (ui == null) return null;

        var rapture = ui->GetRaptureAtkModule();
        if (rapture == null) return null;

        // Use StringArray index 19 for names
        var stringArrayData = rapture->AtkModule.GetStringArrayData(19);
        if (stringArrayData == null) return null;

        int stringIndex = localIndex * 2;
        if (stringArrayData->AtkArrayData.Size <= stringIndex)
            return null;

        string name = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(stringArrayData->StringArray[stringIndex])).ToString();
        if (string.IsNullOrEmpty(name))
            return null;

        char letterSymbol = name[0];
        return letterSymbol;
    }
}