using System.Numerics;
using StatusTimers.StatusSources;
using StatusTimers.Helpers;

namespace StatusTimers.Windows;

public class EnemyMultiDoTWindow() : StatusTimerWindow<StatusKey>(new EnemyMultiDoTSource());