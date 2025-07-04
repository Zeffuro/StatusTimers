using StatusTimers.Enums;

namespace StatusTimers.Models;

public readonly record struct StatusInfo(
    uint Id,
    uint IconId,
    string Name,
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
}
