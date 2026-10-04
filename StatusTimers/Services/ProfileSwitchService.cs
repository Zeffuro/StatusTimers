using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using StatusTimers.Windows;
using System;

namespace StatusTimers.Services;

public sealed class ProfileSwitchService {
    private uint _lastJobId;
    private int _lastHudLayout = -1;
    private bool _refresh = true;

    public void RequestRefresh() => _refresh = true;

    public void Update(OverlayManager manager) {
        uint jobId = Services.ObjectTable.LocalPlayer?.ClassJob.RowId ?? 0;
        int layout = manager.AutoSwitchHudLayouts && jobId != 0 ? GetCurrentHudLayout() : -1;
        if (!_refresh && jobId == _lastJobId && layout == _lastHudLayout) {
            return;
        }

        _refresh = false;
        _lastJobId = jobId;
        _lastHudLayout = layout;
        if (jobId == 0) {
            return;
        }

        string? profile = manager.AutoSwitchJobs ? manager.GetJobProfile(jobId) : null;
        if (profile == null && manager.AutoSwitchHudLayouts && layout >= 0) {
            profile = manager.GetHudLayoutProfile((uint)layout);
        }
        if (profile != null && !profile.Equals(manager.ActiveProfile, StringComparison.OrdinalIgnoreCase)) {
            manager.SwitchProfile(profile);
        }
    }

    public static unsafe int GetCurrentHudLayout() {
        var config = AddonConfig.Instance();
        if (config == null || !config->IsLoaded || config->ActiveDataSet == null) {
            return -1;
        }

        int layout = config->ActiveDataSet->CurrentHudLayout;
        return layout is >= 0 and < 4 ? layout : -1;
    }
}
