namespace StatusTimers.Models;

public readonly record struct StatusKey(ulong GameObjectId, uint StatusId, ulong SourceObjectId = 0);
