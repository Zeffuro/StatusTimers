using StatusTimers.Helpers;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class EnemyMultiDoTWindow() : StatusTimerWindow<StatusKey>(new EnemyMultiDoTSource(), NodeKind.MultiDoT);
