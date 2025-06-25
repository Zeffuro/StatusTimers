using StatusTimers.Enums;

namespace StatusTimers.Models;

public record DummyStatusTemplate(
    uint Id,
    uint IconId,
    string Name,
    float MaxSeconds,
    StatusCategory StatusType,
    bool IsPermanent = false
);
