using StatusTimers.Enums;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Logic;

public sealed class StatusNodeActionService {
    public void Handle(
        uint statusId,
        ulong gameObjectToTargetId,
        NodeKind nodeKind,
        bool allowDismiss,
        bool allowTarget) {

        if (nodeKind is NodeKind.Combined or NodeKind.Buffs or NodeKind.Debuffs && allowDismiss) {
            StatusManager.ExecuteStatusOff(statusId);
        }

        if (nodeKind == NodeKind.MultiDoT && allowTarget) {
            GlobalServices.TargetManager.Target =
                GlobalServices.ObjectTable.FirstOrDefault(
                    obj => obj.GameObjectId == gameObjectToTargetId);
        }
    }
}
