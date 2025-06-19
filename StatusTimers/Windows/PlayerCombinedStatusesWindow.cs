using System.Numerics;
using StatusTimers.StatusSources;
using StatusTimers.Helpers;

namespace StatusTimers.Windows;

public class PlayerCombinedStatusesWindow() : StatusTimerWindow<StatusKey>(new PlayerCombinedStatusesSource());