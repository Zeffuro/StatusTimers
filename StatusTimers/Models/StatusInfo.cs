using StatusTimers.Enums;

namespace StatusTimers.Models;

public readonly struct StatusInfo(
    uint id,
    uint iconId,
    string name,
    float remainingSeconds,
    float maxSeconds,
    ulong gameObjectId,
    bool selfInflicted,
    uint stacks,
    byte partyPriority,
    bool isPermanent = false,
    string? actorName = null,
    char? enemyLetter = null,
    StatusCategory category = StatusCategory.Buff) {
    public uint Id { get; } = id;
    public uint IconId { get; } = iconId;
    public string Name { get; } = name;
    public float RemainingSeconds { get; } = remainingSeconds;
    public float MaxSeconds { get; } = maxSeconds;
    public bool IsPermanent { get; } = isPermanent;
    public ulong GameObjectId { get; } = gameObjectId;
    public bool SelfInflicted { get; } = selfInflicted;
    public uint Stacks { get; } = stacks;
    public byte PartyPriority { get; } = partyPriority;
    public string? ActorName { get; } = actorName;
    public char? EnemyLetter { get; } = enemyLetter;
    public StatusCategory StatusType { get; } = category;

    public StatusKey Key => new(GameObjectId, Id);
}
