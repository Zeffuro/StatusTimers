using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Helpers;
using StatusTimers.Models;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Services;

public static class StatusManager {
    private static readonly ExcelSheet<Item> ItemSheet = Services.DataManager.GetExcelSheet<Item>();
    private static readonly ExcelSheet<LuminaStatus> StatusSheet = Services.DataManager.GetExcelSheet<LuminaStatus>();
    private static FrozenDictionary<uint, uint>? _itemFoodToItemLut;
    private static readonly Dictionary<uint, float> StatusDurations = new();

    private static readonly FrozenSet<uint> HarmfulStatusIds = Services.DataManager
        .GetExcelSheet<LuminaStatus>()
        .Where(s => s is { IsPermanent: false, StatusCategory: 2 })
        .Select(s => s.RowId)
        .ToFrozenSet();

    private static readonly List<StatusInfo> HostileStatusBuffer = new(64);

    static StatusManager() {
        PopulateDictionaries();
    }

    public static unsafe IReadOnlyList<StatusInfo> GetPlayerStatuses(StatusTimerOverlayConfig? config) {
        IPlayerCharacter? player = Services.ObjectTable.LocalPlayer;
        if (player?.StatusList == null) {
            return [];
        }

        Character* character = (Character*) player.Address;

        var result = new List<StatusInfo>();
        var statusManager = character->GetStatusManager();

        for (int i = 0; i < statusManager->NumValidStatuses; i++) {
            ref var status = ref statusManager->Status[i];
            if (status.StatusId == 0) {
                continue;
            }

            var transformed = TransformStatus(ref status, player.GameObjectId, config);
            if (transformed != null) {
                result.Add(transformed);
            }
        }

        return result;
    }

    public static unsafe IReadOnlyList<StatusInfo> GetHostilePlayerStatuses(StatusTimerOverlayConfig? config) {
        HostileStatusBuffer.Clear();

        IPlayerCharacter? player = Services.ObjectTable.LocalPlayer;
        if (player == null) {
            return HostileStatusBuffer;
        }

        foreach (BattleChara* battleChara in CharacterManager.Instance()->BattleCharas)
        {
            if (battleChara == null || !battleChara->GetIsTargetable() || battleChara->GetGameObjectId() == player.GameObjectId) {
                continue;
            }

            ref var statusManager = ref battleChara->StatusManager;
            for (int i = 0; i < statusManager.NumValidStatuses; i++)
            {
                ref var status = ref statusManager.Status[i];
                if (!HarmfulStatusIds.Contains(status.StatusId)) {
                    continue;
                }

                if (status.SourceObject.Id != player.GameObjectId) {
                    continue;
                }

                StatusInfo? transformedStatus = TransformStatus(ref status, battleChara->GetGameObjectId(), config, battleChara);
                if (transformedStatus != null) {
                    HostileStatusBuffer.Add(transformedStatus);
                }
            }
        }

        return HostileStatusBuffer;
    }

    private static unsafe StatusInfo? TransformStatus(ref Status status, ulong objectId, StatusTimerOverlayConfig? config, BattleChara* battleChar = null) {
        if (!StatusSheet.TryGetRow(status.StatusId, out LuminaStatus gameData) || config == null) {
            return null;
        }

        uint id = status.StatusId;
        uint iconId = gameData.Icon;
        string name = gameData.Name.ExtractText();
        string description = gameData.Description.ExtractText();
        float remainingSeconds = status.RemainingTime;
        ulong sourceObjectId = objectId;
        uint stacks = gameData.MaxStacks;
        bool isPerma = gameData.IsPermanent;
        byte partyPrio = gameData.PartyListPriority;
        StatusCategory statusType = gameData.StatusCategory == 1 ? StatusCategory.Buff : StatusCategory.Debuff;

        if (isPerma) {
            remainingSeconds = 99999;
        }

        if (!StatusDurations.TryGetValue(id, out float maxSeconds) || remainingSeconds > maxSeconds) {
            maxSeconds = remainingSeconds;
            StatusDurations[id] = maxSeconds;
        }

        if (!config.ShowPermaIcons && isPerma) {
            return null;
        }

        if(config.HideStatusAboveSecondsEnabled && maxSeconds > config.HideStatusAboveSeconds) {
            return null;
        }

        if(config.HideStatusUnderSecondsEnabled && maxSeconds < config.HideStatusUnderSeconds) {
            return null;
        }

        if (!gameData.IsFcBuff) {
            int stackCount = stacks > 0 ? Math.Max(1, (int)status.Param) : 0;
            if (stackCount > 0) {
                iconId = gameData.Icon + (uint)(stackCount - 1);
            }
        }

        string? actorName = null;
        char? enemyLetter = null;

        IPlayerCharacter? player = Services.ObjectTable.LocalPlayer;

        bool selfInflicted = player != null && player.GameObjectId == status.SourceObject;

        if (config.SelfAppliedStatusesOnly && !selfInflicted) {
            return null;
        }

        if (battleChar is not null && player != null && objectId != player.GameObjectId) {
            actorName = battleChar->NameString;
            enemyLetter = EnemyListHelper.GetEnemyLetter((uint)objectId);
        }

        if (config.StatusAsItemName) {
            switch (status.StatusId) {
                case 48: // Well Fed
                case 49: // Medicated
                    FoodParams? resolved = ResolveFoodParam(status.Param);
                    if (resolved != null) {
                        name = resolved.Name;
                        //iconId = resolved.IconId;
                    }

                    break;
            }
        }

        return new StatusInfo(id, iconId, name, description, remainingSeconds, maxSeconds, sourceObjectId, selfInflicted, stacks,
            partyPrio, isPerma,
            actorName, enemyLetter, statusType);
    }

    // Thanks Craftimizer for the info on food: https://github.com/WorkingRobot/Craftimizer/blob/main/Craftimizer/Utils/FoodStatus.cs#L23
    private static void PopulateDictionaries() {
        Dictionary<uint, uint> lut = new();
        foreach (Item item in from item in ItemSheet
                 let isFood = item.ItemUICategory.RowId == 46
                 let isMedicine = item.ItemUICategory.RowId == 44
                 where isFood || isMedicine
                 select item) {
            if (item.ItemAction.ValueNullable is not { } itemAction) {
                continue;
            }

            if (itemAction.Type is not (844 or 845 or 846)) {
                continue;
            }

            if (Services.DataManager.GetExcelSheet<ItemFood>().GetRowOrDefault(itemAction.Data[1]) is not
                { } itemFood) {
                continue;
            }

            lut.TryAdd(itemFood.RowId, item.RowId);
        }

        _itemFoodToItemLut = lut.ToFrozenDictionary();
    }

    private static FoodParams? ResolveFoodParam(ushort param) {
        uint itemId = 0;
        if (_itemFoodToItemLut != null && !_itemFoodToItemLut.TryGetValue((uint)(param - 10000), out itemId)) {
            return null;
        }

        return !ItemSheet.TryGetRow(itemId, out Item item) ? null : new FoodParams(item.Name.ExtractText(), item.Icon);
    }
}
