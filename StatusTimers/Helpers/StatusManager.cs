using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Status = Dalamud.Game.ClientState.Statuses.Status;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Helpers;

public static class StatusManager
{
    private static ExcelSheet<Item> _itemSheet = Services.DataManager.GetExcelSheet<Item>();
    private static FrozenDictionary<uint, uint> _itemFoodToItemLut;

    static StatusManager()
    {
        PopulateDictionaries();
    }
    
    public static IReadOnlyList<StatusInfo> GetPlayerStatuses()
    {
        var player = Services.ClientState.LocalPlayer;
        if (player?.StatusList == null)
            return [];

        return player.StatusList
            .Where(status => status.StatusId != 0)
            .Select(TransformStatus)
            .ToList();
    }
    private static StatusInfo TransformStatus(Status status)
    {
        LuminaStatus gameData = status.GameData.Value;
        
        uint id = status.StatusId;
        uint iconId = gameData.Icon;
        string name = gameData.Name.ExtractText();
        float remainingSeconds = status.RemainingTime;
        uint sourceObjectId = status.SourceId;
        uint stacks = gameData.MaxStacks;

        // TODO Add configuration to resolve food/pots
        switch (status.StatusId)
        {
            case 48:    // Well Fed
            case 49:    // Medicated
                var resolved = ResolveFoodParam(status.Param);
                if (resolved is (string nameResolved, uint iconIdResolved))
                {
                    name = nameResolved;
                    //iconId = iconIdResolved;
                }
                break;
        }

        return new StatusInfo(id, iconId, name, remainingSeconds, sourceObjectId, stacks);
    }

    // Thanks Craftimizer for the info on food: https://github.com/WorkingRobot/Craftimizer/blob/main/Craftimizer/Utils/FoodStatus.cs#L23
    private static void PopulateDictionaries()
    {
        Dictionary<uint, uint> lut = new Dictionary<uint, uint>();
        foreach (Item item in from item in _itemSheet let isFood = item.ItemUICategory.RowId == 46 let isMedicine = item.ItemUICategory.RowId == 44 where isFood || isMedicine select item)
        {
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

    private static (string Name, uint IconId)? ResolveFoodParam(ushort param)
    {
        
        if (!_itemFoodToItemLut.TryGetValue((uint)(param - 10000), out uint itemId))
            return null;

        if (!_itemSheet.TryGetRow(itemId, out Item item))
            return null;

        return (item.Name.ExtractText(), item.Icon);
    }
}

public readonly struct StatusInfo(uint id, uint iconId, string name, float remainingSeconds, uint sourceObjectId, uint stacks)
{
    public uint Id { get; } = id;
    public uint IconId { get; } = iconId;
    public string Name { get; } = name;
    public float RemainingSeconds { get; } = remainingSeconds;
    public uint SourceObjectId { get; } = sourceObjectId;
    public uint Stacks { get; } = stacks;
}