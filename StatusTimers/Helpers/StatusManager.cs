using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using StatusTimers.Extensions;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Status = Dalamud.Game.ClientState.Statuses.Status;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Helpers;

public static class StatusManager {
    private static readonly ExcelSheet<Item> _itemSheet = Services.DataManager.GetExcelSheet<Item>();
    private static FrozenDictionary<uint, uint> _itemFoodToItemLut;
    private static readonly Dictionary<uint, float> StatusDurations = new();

    private static readonly FrozenSet<uint> HarmfulStatusIds = Services.DataManager
        .GetExcelSheet<LuminaStatus>()!
        .Where(s => s is { IsPermanent: false })
        .Select(s => s.RowId)
        .ToFrozenSet();

    private static readonly List<StatusInfo> _hostileStatusBuffer = new(64);

    static StatusManager() {
        PopulateDictionaries();
    }

    public static IReadOnlyList<StatusInfo> GetPlayerStatuses() {
        IPlayerCharacter? player = Services.ClientState.LocalPlayer;
        if (player?.StatusList == null)
            return [];

        return player.StatusList
            .Where(status => status.StatusId != 0)
            .Select(status => TransformStatus(status, player.GameObjectId))
            .ToList();
    }

    public static IReadOnlyList<StatusInfo> GetHostileStatuses() {
        _hostileStatusBuffer.Clear();

        IPlayerCharacter? player = Services.ClientState.LocalPlayer;
        if (player == null)
            return _hostileStatusBuffer;

        foreach (IGameObject obj in Services.ObjectTable) {
            if (obj is not IBattleChara target ||
                target.ObjectIndex == player.GameObjectId ||
                !target.IsTargetable ||
                !target.IsHostile())
                continue;

            StatusList statusList = target.StatusList;
            for (int i = 0; i < statusList.Length; i++) {
                Status? status = statusList[i];
                if (status.StatusId == 0 || status.SourceId != player.GameObjectId)
                    continue;

                if (!HarmfulStatusIds.Contains(status.StatusId))
                    continue;

                _hostileStatusBuffer.Add(TransformStatus(status, target.GameObjectId));
            }
        }

        return _hostileStatusBuffer;
    }

    private static StatusInfo TransformStatus(Status status, ulong objectId) {
        LuminaStatus gameData = status.GameData.Value;

        uint id = status.StatusId;
        uint iconId = gameData.Icon;
        string name = gameData.Name.ExtractText();
        float remainingSeconds = status.RemainingTime;
        ulong sourceObjectId = objectId;
        uint stacks = gameData.MaxStacks;
        bool isPerma = gameData.IsPermanent;

        if (!StatusDurations.TryGetValue(id, out float maxSeconds) || remainingSeconds > maxSeconds) {
            maxSeconds = remainingSeconds;
            StatusDurations[id] = maxSeconds;
        }

        // TODO Make extraction optional based on configuration
        string? actorName = null;
        char? enemyLetter = null;

        IGameObject? actor = Services.ObjectTable.FirstOrDefault(o => o is not null && o.GameObjectId == objectId);
        IPlayerCharacter? player = Services.ClientState.LocalPlayer;

        //Services.Logger.Info($"Actor name changed{actor.Name} {actor.ObjectIndex} ");
        if (actor is not null && player != null && actor.GameObjectId != player.GameObjectId) {
            actorName = actor.Name.TextValue;
            enemyLetter = EnemyListHelper.GetEnemyLetter((uint)actor.GameObjectId);
        }

        // TODO Add configuration to resolve food/pots
        switch (status.StatusId) {
            case 48: // Well Fed
            case 49: // Medicated
                (string Name, uint IconId)? resolved = ResolveFoodParam(status.Param);
                if (resolved is (string nameResolved, uint iconIdResolved)) name = nameResolved;
                //iconId = iconIdResolved;
                break;
        }

        return new StatusInfo(id, iconId, name, remainingSeconds, maxSeconds, sourceObjectId, stacks, isPerma,
            actorName, enemyLetter);
    }

    // Thanks Craftimizer for the info on food: https://github.com/WorkingRobot/Craftimizer/blob/main/Craftimizer/Utils/FoodStatus.cs#L23
    private static void PopulateDictionaries() {
        Dictionary<uint, uint> lut = new();
        foreach (Item item in from item in _itemSheet
                 let isFood = item.ItemUICategory.RowId == 46
                 let isMedicine = item.ItemUICategory.RowId == 44
                 where isFood || isMedicine
                 select item) {
            if (item.ItemAction.ValueNullable is not { } itemAction)
                continue;

            if (itemAction.Type is not (844 or 845 or 846))
                continue;

            if (Services.DataManager.GetExcelSheet<ItemFood>().GetRowOrDefault(itemAction.Data[1]) is not { } itemFood)
                continue;

            lut.TryAdd(itemFood.RowId, item.RowId);
        }

        _itemFoodToItemLut = lut.ToFrozenDictionary();
    }

    private static (string Name, uint IconId)? ResolveFoodParam(ushort param) {
        if (!_itemFoodToItemLut.TryGetValue((uint)(param - 10000), out uint itemId))
            return null;

        if (!_itemSheet.TryGetRow(itemId, out Item item))
            return null;

        return (item.Name.ExtractText(), item.Icon);
    }
}

public readonly struct StatusInfo(
    uint id,
    uint iconId,
    string name,
    float remainingSeconds,
    float maxSeconds,
    ulong gameObjectId,
    uint stacks,
    bool isPermanent = false,
    string? actorName = null,
    char? enemyLetter = null) {
    public uint Id { get; } = id;
    public uint IconId { get; } = iconId;
    public string Name { get; } = name;
    public float RemainingSeconds { get; } = remainingSeconds;
    public float MaxSeconds { get; } = maxSeconds;
    public bool IsPermanent { get; } = isPermanent;
    public ulong GameObjectId { get; } = gameObjectId;
    public uint Stacks { get; } = stacks;
    public string? ActorName { get; } = actorName;
    public char? EnemyLetter { get; } = enemyLetter;

    public StatusKey Key => new(GameObjectId, Id);
}

public readonly record struct StatusKey(ulong GameObjectId, uint StatusId);
