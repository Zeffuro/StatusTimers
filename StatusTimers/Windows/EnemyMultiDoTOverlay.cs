using StatusTimers.Helpers;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class EnemyMultiDoTOverlay : StatusTimerOverlay<StatusKey> {
    public EnemyMultiDoTOverlay() : base(NodeKind.MultiDoT) {
        Source = new EnemyMultiDoTSource(this);
    }
}
