using StatusTimers.Enums;
using System;

namespace StatusTimers.Models;

public record StatusInfo(
    uint Id,
    uint IconId,
    string Name,
    string Description,
    float RemainingSeconds,
    float MaxSeconds,
    ulong GameObjectId,
    bool SelfInflicted,
    uint Stacks,
    byte PartyPriority,
    bool IsPermanent = false,
    string? ActorName = null,
    char? EnemyLetter = null,
    StatusCategory StatusType = StatusCategory.Buff
) {
    public StatusKey Key => new(GameObjectId, Id);
    public virtual bool Equals(StatusInfo? other) => other != null && Id == other.Id && GameObjectId == other.GameObjectId && IconId == other.IconId;
    public override int GetHashCode() => HashCode.Combine(Id, GameObjectId, IconId);
}
