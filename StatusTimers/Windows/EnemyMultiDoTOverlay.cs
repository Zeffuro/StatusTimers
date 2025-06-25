using StatusTimers.Enums;
using StatusTimers.Models;
using StatusTimers.StatusSources;

namespace StatusTimers.Windows;

public class EnemyMultiDoTOverlay() : StatusTimerOverlay<StatusKey>(NodeKind.MultiDoT, new EnemyMultiDoTSource());
