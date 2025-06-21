using StatusTimers.Helpers;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class EnemyMultiDoTOverlay() : StatusTimerOverlay<StatusKey>(new EnemyMultiDoTSource(), NodeKind.MultiDoT);
