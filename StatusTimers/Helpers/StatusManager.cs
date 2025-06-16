using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using LuminaStatus = Lumina.Excel.Sheets.Status;

namespace StatusTimers.Helpers;

public static class StatusManager
{
    public static IReadOnlyList<StatusInfo> GetPlayerStatuses()
    {
        var player = Services.ClientState.LocalPlayer;
        if (player?.StatusList == null)
            return [];

        return player.StatusList
            .Where(status => status.StatusId != 0)
            .Select(status =>
            {
                return new StatusInfo(
                    status.StatusId,
                    status.GameData.Value.Icon,
                    status.GameData.Value.Name.ExtractText(),
                    status.RemainingTime,
                    status.SourceId,
                    status.GameData.Value.MaxStacks
                );
            })
            .ToList();
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