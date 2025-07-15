using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Extensions;
using StatusTimers.Helpers;
using StatusTimers.Interfaces;
using StatusTimers.Models;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Services;

public static class StatusManager {
    private static readonly ExcelSheet<Item> _itemSheet = Services.DataManager.GetExcelSheet<Item>();
    private static readonly ExcelSheet<LuminaStatus> _statusSheet = Services.DataManager.GetExcelSheet<LuminaStatus>();
    private static FrozenDictionary<uint, uint> _itemFoodToItemLut;
    private static readonly Dictionary<uint, float> StatusDurations = new();

    private static readonly FrozenSet<uint> HarmfulStatusIds = Services.DataManager
        .GetExcelSheet<LuminaStatus>()!
        .Where(s => s is { IsPermanent: false, StatusCategory: 2 })
        .Select(s => s.RowId)
        .ToFrozenSet();

    private static readonly List<StatusInfo> _hostileStatusBuffer = new(64);

    static StatusManager() {
        PopulateDictionaries();
    }

    public static unsafe IReadOnlyList<StatusInfo> GetPlayerStatuses(StatusTimerOverlayConfig? config) {
        IPlayerCharacter? player = Services.ClientState.LocalPlayer;
        if (player?.StatusList == null) {
            return [];
        }

        Character* character = (Character*) player.Address;

        var result = new List<StatusInfo>();
        var statusManager = character->GetStatusManager();

        for (int i = 0; i < statusManager->NumValidStatuses; i++) {
            var status = statusManager->Status[i];
            if (status.StatusId == 0) {
                continue;
            }

            var transformed = TransformStatus(status, player.GameObjectId, config);
            if (transformed.HasValue) {
                result.Add(transformed.Value);
            }
        }

        return result;
    }

    public static unsafe IReadOnlyList<StatusInfo> GetHostileStatuses(StatusTimerOverlayConfig? config) {
        _hostileStatusBuffer.Clear();

        IPlayerCharacter? player = Services.ClientState.LocalPlayer;
        if (player == null) {
            return _hostileStatusBuffer;
        }
        var characterManager = CharacterManager.Instance();
        var battleCharas = characterManager->BattleCharas;
        foreach (var battleCharaPointer in battleCharas)
        {
            if (battleCharaPointer == null) {
                continue;
            }

            var battleChar = battleCharaPointer.Value;

            if (!battleChar->GetIsTargetable() || battleChar->ObjectIndex == player.ObjectIndex) {
                continue;
            }

            foreach (var statusId in HarmfulStatusIds) {

                int statusIndex = battleChar->StatusManager.GetStatusIndex(statusId);
                if (statusIndex == -1) {
                    continue;
                }

                var status = battleChar->StatusManager.Status[statusIndex];

                StatusInfo? transformedStatus = TransformStatus(status, battleChar->GetGameObjectId(), config, battleChar);
                if (transformedStatus.HasValue) {
                    _hostileStatusBuffer.Add(transformedStatus.Value);
                }
            }
        }

        return _hostileStatusBuffer;
    }

    private static unsafe StatusInfo? TransformStatus(Status status, ulong objectId, StatusTimerOverlayConfig? config, BattleChara* battleChar = null) {
        if (!_statusSheet.TryGetRow(status.StatusId, out LuminaStatus gameData)) {
            return null;
        };

        uint id = status.StatusId;
        uint iconId = gameData.Icon;
        string name = gameData.Name.ExtractText();
        float remainingSeconds = status.RemainingTime;
        ulong sourceObjectId = objectId;
        uint stacks = gameData.MaxStacks;
        bool isPerma = gameData.IsPermanent;
        byte partyPrio = gameData.PartyListPriority;
        StatusCategory statusType = gameData.StatusCategory == 1 ? StatusCategory.Buff : StatusCategory.Debuff;

        if (!StatusDurations.TryGetValue(id, out float maxSeconds) || remainingSeconds > maxSeconds) {
            maxSeconds = remainingSeconds;
            StatusDurations[id] = maxSeconds;
        }

        if (!config.ShowPermaIcons && isPerma) {
            return null;
        }

        if (stacks > 0 && status.Param > 0 && !gameData.IsFcBuff) {
            iconId = gameData.Icon + (uint)Math.Max(0, status.Param - 1);
        }

        string? actorName = null;
        char? enemyLetter = null;

        IPlayerCharacter? player = Services.ClientState.LocalPlayer;

        bool selfInflicted = player.GameObjectId == sourceObjectId;

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
                        //iconId = iconIdResolved;
                    }

                    break;
            }
        }

        return new StatusInfo(id, iconId, name, remainingSeconds, maxSeconds, sourceObjectId, selfInflicted, stacks,
            partyPrio, isPerma,
            actorName, enemyLetter, statusType);
    }

    // Thanks Craftimizer for the info on food: https://github.com/WorkingRobot/Craftimizer/blob/main/Craftimizer/Utils/FoodStatus.cs#L23
    private static void PopulateDictionaries() {
        Dictionary<uint, uint> lut = new();
        foreach (Item item in from item in _itemSheet
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
        if (!_itemFoodToItemLut.TryGetValue((uint)(param - 10000), out uint itemId)) {
            return null;
        }

        return !_itemSheet.TryGetRow(itemId, out Item item) ? null : new FoodParams(item.Name.ExtractText(), item.Icon);
    }
}
