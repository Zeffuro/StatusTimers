using FFXIVClientStructs.FFXIV.Component.GUI;
using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Extensions;
using StatusTimers.Helpers;
using StatusTimers.Interfaces;
using StatusTimers.Logic;
using StatusTimers.Models;
using StatusTimers.Services;
using StatusTimers.StatusSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using GlobalServices = StatusTimers.Services.Services;

namespace StatusTimers.Windows;

public class OverlayManager : IAsyncDisposable {
    private static readonly TimeSpan FrameworkSetupTimeout = TimeSpan.FromSeconds(15);

    private bool _isDisposed;
    private ConfigurationWindow? _configurationWindow;
    private StatusTimerOverlayNode<StatusKey>? _playerCombinedOverlay;
    private StatusTimerOverlayNode<StatusKey>? _enemyMultiDoTOverlay;
    private StatusTimerOverlayNode<StatusKey>? _playerBuffsOverlay;
    private StatusTimerOverlayNode<StatusKey>? _playerDebuffsOverlay;
    private StatusNodeActionService? _statusActionService;
    private ColorPickerAddon? _colorPickerAddon;

    public StatusTimerOverlayNode<StatusKey>? PlayerCombinedOverlayInstance => _playerCombinedOverlay;
    public StatusTimerOverlayNode<StatusKey>? EnemyMultiDoTOverlayInstance => _enemyMultiDoTOverlay;
    public StatusTimerOverlayNode<StatusKey>? PlayerBuffsOverlayInstance => _playerBuffsOverlay;
    public StatusTimerOverlayNode<StatusKey>? PlayerDebuffsOverlayInstance => _playerDebuffsOverlay;
    public ColorPickerAddon? ColorPickerInstance => _colorPickerAddon;

    public async ValueTask DisposeAsync() {
        if (_isDisposed) {
            return;
        }
        await GlobalServices.Framework.RunSafely(FlushProfileChanges);
        _isDisposed = true;

        await DetachAndDisposeAllAsync();
    }

    public async Task SetupAsync(CancellationToken cancellationToken) {
        await DetachAndDisposeAllAsync();

        await GlobalServices.Framework.RunSafelyWithTimeout(CreateAndAttachOverlays, cancellationToken, FrameworkSetupTimeout);

        _colorPickerAddon = new ColorPickerAddon {
            InternalName = "StatusTimerColorPicker",
            Title = "Pick a color",
            Size = new Vector2(400, 540)
        };

        _configurationWindow = new ConfigurationWindow(this) {
            InternalName = "StatusTimersConfiguration",
            Title = "StatusTimers Configuration",
            Size = new Vector2(640, 512)
        };
    }

    private async ValueTask DetachAndDisposeAllAsync() {
        var colorPickerAddon = _colorPickerAddon;
        var configurationWindow = _configurationWindow;

        _colorPickerAddon = null;
        _configurationWindow = null;

        await GlobalServices.Framework.RunSafely(() => {
            colorPickerAddon?.CloseSilently();

            if (_playerCombinedOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_playerCombinedOverlay);
                _playerCombinedOverlay = null;
            }

            if (_enemyMultiDoTOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_enemyMultiDoTOverlay);
                _enemyMultiDoTOverlay = null;
            }

            if (_playerBuffsOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_playerBuffsOverlay);
                _playerBuffsOverlay = null;
            }

            if (_playerDebuffsOverlay != null) {
                GlobalServices.OverlayController.RemoveNode(_playerDebuffsOverlay);
                _playerDebuffsOverlay = null;
            }
        });


        await Task.WhenAll(
            DisposeAddonAsync(configurationWindow).AsTask(),
            DisposeAddonAsync(colorPickerAddon).AsTask());
    }

    private static async ValueTask DisposeAddonAsync(IAsyncDisposable? addon) {
        if (addon == null) {
            return;
        }

        await Task.Run(async () => await addon.DisposeAsync());
    }

    private void CreateAndAttachOverlays() {
        _playerCombinedOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.Combined);
        _enemyMultiDoTOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.MultiDoT);
        _playerBuffsOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.Buffs);
        _playerDebuffsOverlay = new StatusTimerOverlayNode<StatusKey>(NodeKind.Debuffs);
        _statusActionService = new StatusNodeActionService();

        foreach (var kind in Enum.GetValues<NodeKind>()) {
            var overlay = GetOverlay(kind)!;
            overlay.Initialize();
            IStatusSource<StatusKey> source = kind switch {
                NodeKind.MultiDoT => new EnemyMultiDoTSource(),
                NodeKind.Buffs => new StatusCategoryFilteredSource<StatusKey>(new PlayerCombinedStatusesSource(), StatusCategory.Buff),
                NodeKind.Debuffs => new StatusCategoryFilteredSource<StatusKey>(new PlayerCombinedStatusesSource(), StatusCategory.Debuff),
                _ => new PlayerCombinedStatusesSource()
            };
            var dataSource = new StatusDataSourceManager<StatusKey>(source, kind,
                () => overlay.IsPreviewEnabled,
                () => overlay.OverlayConfig.ShowPermaIcons,
                () => overlay.OverlayConfig.MaxStatuses,
                () => overlay.OverlayConfig.ItemsPerLine);
            overlay.SetStatusProvider(() => dataSource.FetchAndProcessStatuses(overlay.OverlayConfig));
            overlay.SetNodeActionHandler(_statusActionService.Handle);
            overlay.OverlayConfig.OnPropertyChanged += OnOverlayConfigChanged;
            GlobalServices.OverlayController.AddNode(overlay);
        }
        InitializeProfiles();
    }

    public void ToggleConfig() {
        if (_isDisposed) {
            return;
        }

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_isDisposed) {
                return;
            }

            CloseColorPicker();
            _configurationWindow?.Toggle();
        });
    }

    public void OpenConfig() {
        if (_isDisposed) {
            return;
        }

        GlobalServices.Framework.RunOnFrameworkThread(() => {
            if (_isDisposed) {
                return;
            }

            CloseColorPicker();
            _configurationWindow?.Open();
        });
    }

    internal void CloseColorPicker() {
        _colorPickerAddon?.CloseSilently();
    }

    public StatusTimerOverlayNode<StatusKey>? GetOverlay(NodeKind kind) => kind switch {
        NodeKind.Combined => _playerCombinedOverlay,
        NodeKind.MultiDoT => _enemyMultiDoTOverlay,
        NodeKind.Buffs => _playerBuffsOverlay,
        NodeKind.Debuffs => _playerDebuffsOverlay,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public OverlaySetup CaptureSetup() => new() {
        Overlays = Enum.GetValues<NodeKind>().ToDictionary(kind => kind,
            kind => ConfigTransfer.CloneOverlay(kind, GetOverlay(kind)!.OverlayConfig))
    };

    private string CaptureProfileSnapshot() => ConfigTransfer.ExportSetup(new OverlaySetup {
        Overlays = Enum.GetValues<NodeKind>().ToDictionary(kind => kind, kind => GetOverlay(kind)!.OverlayConfig)
    });

    private void ReplaceSetup(OverlaySetup setup) {
        var current = Enum.GetValues<NodeKind>().ToDictionary(kind => kind,
            kind => (Overlay: GetOverlay(kind)!, Config: GetOverlay(kind)!.OverlayConfig,
                Preview: GetOverlay(kind)!.IsPreviewEnabled, Locked: GetOverlay(kind)!.IsLocked));
        CloseColorPicker();
        try {
            foreach (var (kind, previous) in current) {
                previous.Config.OnPropertyChanged -= OnOverlayConfigChanged;
                previous.Overlay.ReplaceConfiguration(setup.Overlays[kind]);
                previous.Overlay.OverlayConfig.OnPropertyChanged += OnOverlayConfigChanged;
            }
        }
        catch {
            foreach (var previous in current.Values) {
                previous.Overlay.OverlayConfig.OnPropertyChanged -= OnOverlayConfigChanged;
                previous.Overlay.ReplaceConfiguration(previous.Config);
                previous.Config.OnPropertyChanged += OnOverlayConfigChanged;
                previous.Overlay.IsPreviewEnabled = previous.Preview;
                previous.Overlay.IsLocked = previous.Locked;
            }
            throw;
        }
        foreach (var previous in current.Values) {
            previous.Overlay.SaveConfiguration();
        }
        _configurationWindow?.RequestRebuild(overlaysChanged: true);
    }

    private static void FitSetupToScreen(OverlaySetup setup) {
        foreach (var config in setup.Overlays.Values) {
            FitOverlayToScreen(config);
        }
    }

    private static unsafe void FitOverlayToScreen(StatusTimerOverlayConfig config) {
        var stage = AtkStage.Instance();
        if (stage == null) {
            return;
        }

        var screen = new Vector2(stage->ScreenSize.Width, stage->ScreenSize.Height);
        var size = OverlayLayoutHelper.CalculateOverlaySize(config) * (config.ScaleInt * 0.01f);
        config.Position = new Vector2(
            Math.Clamp(config.Position.X, 0, Math.Max(0, screen.X - size.X)),
            Math.Clamp(config.Position.Y, 0, Math.Max(0, screen.Y - size.Y)));
    }

    public string ImportOverlay(NodeKind kind, string input) {
        if (!ConfigTransfer.TryImportOverlay(input, kind, out var config, out var error)) {
            throw new ArgumentException(error);
        }
        var setup = CaptureSetup();
        FitOverlayToScreen(config!);
        setup.Overlays[kind] = config!;
        ApplySetup(setup);
        return "Configuration imported from clipboard.";
    }

    public string ResetOverlay(NodeKind kind) {
        var setup = CaptureSetup();
        setup.Overlays[kind] = new StatusTimerOverlayConfig(kind);
        ApplySetup(setup);
        return "Configuration reset to default.";
    }

    public string ExportOverlay(NodeKind kind) => ConfigTransfer.ExportOverlay(kind, GetOverlay(kind)!.OverlayConfig);

    private ProfileRepository? _profileRepository;
    private ProfileStore? _profiles;
    private DateTime? _profileSaveAt;
    private readonly ProfileSwitchService _profileSwitcher = new();

    public string ActiveProfile => _profiles?.ActiveProfile ?? "Unavailable";
    public string ProfileError { get; private set; } = string.Empty;
    public bool ProfilesAvailable => _profiles != null;
    public bool AutoSwitchJobs => _profiles?.AutoSwitchJobs ?? false;
    public bool AutoSwitchHudLayouts => _profiles?.AutoSwitchHudLayouts ?? false;
    public IReadOnlyList<string> ProfileNames => _profiles?.Profiles.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
    public string? GetJobProfile(uint jobId) => _profiles?.JobProfiles.GetValueOrDefault(jobId);
    public string? GetHudLayoutProfile(uint layout) => _profiles?.HudLayoutProfiles.GetValueOrDefault(layout);

    private void InitializeProfiles() {
        _profileRepository = new ProfileRepository(GlobalServices.PluginInterface.GetPluginConfigDirectory());
        try {
            if (_profileRepository.Exists) {
                var store = _profileRepository.Load();
                var setup = ReadProfile(store, store.ActiveProfile);
                ReplaceSetup(setup);
                _profiles = store;
            }
            else {
                var store = new ProfileStore();
                store.Profiles.Add(store.ActiveProfile, CaptureProfileSnapshot());
                _profileRepository.Save(store);
                _profiles = store;
            }
        }
        catch (Exception ex) {
            ProfileError = $"Failed to load profiles: {ex.Message}";
            GlobalServices.Logger.Error(ex, "Failed to load profiles.");
        }
        _profileSaveAt = null;
    }

    private void OnOverlayConfigChanged(string propertyName, bool updateNodes) {
        if (!_isDisposed && _profiles != null) {
            _profileSaveAt = DateTime.UtcNow.AddMilliseconds(500);
        }
    }

    public void UpdateProfiles() {
        if (_isDisposed || _profiles == null) {
            return;
        }

        if (_profileSaveAt <= DateTime.UtcNow) {
            FlushProfileChanges();
        }
        try {
            _profileSwitcher.Update(this);
        }
        catch (Exception ex) {
            RecordProfileError(ex);
        }
    }

    private void FlushProfileChanges() {
        if (_profileSaveAt == null || _profiles == null) {
            return;
        }

        try {
            var next = CaptureCurrentProfile();
            _profileRepository!.Save(next);
            _profiles = next;
            _profileSaveAt = null;
            ProfileError = string.Empty;
        }
        catch (Exception ex) {
            _profileSaveAt = DateTime.UtcNow.AddSeconds(5);
            RecordProfileError(ex);
        }
    }

    private void RecordProfileError(Exception ex) {
        var error = $"Failed to update profile: {ex.Message}";
        if (ProfileError != error) {
            GlobalServices.Logger.Error(ex, error);
        }

        ProfileError = error;
    }

    private ProfileStore CaptureCurrentProfile() {
        EnsureProfilesAvailable();
        var next = ProfileRepository.Clone(_profiles!);
        next.Profiles[next.ActiveProfile] = CaptureProfileSnapshot();
        return next;
    }

    private void EnsureProfilesAvailable() {
        if (_profiles == null) {
            throw new InvalidOperationException(string.IsNullOrEmpty(ProfileError) ? "Profiles are unavailable." : ProfileError);
        }
    }

    private static OverlaySetup ReadProfile(ProfileStore store, string name) {
        if (!store.Profiles.TryGetValue(name, out var snapshot)) {
            throw new ArgumentException($"Profile '{name}' does not exist.");
        }
        if (!ConfigTransfer.TryImportSetup(snapshot, out var setup, out var error)) {
            throw new ArgumentException(error);
        }
        return setup!;
    }

    private void CommitProfileChange(ProfileStore next, OverlaySetup? setup = null, bool rebuild = true) {
        EnsureProfilesAvailable();
        var previous = _profiles!;
        _profileRepository!.Save(next);
        try {
            if (setup != null) {
                ReplaceSetup(setup);
            }
        }
        catch {
            try { _profileRepository.Save(previous); }
            catch (Exception rollbackError) { RecordProfileError(rollbackError); }
            throw;
        }
        _profiles = next;
        _profileSaveAt = null;
        ProfileError = string.Empty;
        if (rebuild) {
            _configurationWindow?.RequestRebuild();
        }
    }

    public string SaveProfile(string name) {
        name = ProfileRepository.ValidateName(name);
        var next = CaptureCurrentProfile();
        if (next.Profiles.ContainsKey(name)) {
            throw new ArgumentException("A profile with this name already exists.");
        }
        next.Profiles.Add(name, next.Profiles[next.ActiveProfile]);
        next.ActiveProfile = name;
        CommitProfileChange(next);
        return $"Profile '{name}' saved.";
    }

    public string SwitchProfile(string name) {
        name = ProfileRepository.ValidateName(name);
        EnsureProfilesAvailable();
        name = _profiles!.Profiles.Keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Profile '{name}' does not exist.");
        if (name.Equals(ActiveProfile, StringComparison.OrdinalIgnoreCase)) {
            return $"Profile '{name}' is already loaded.";
        }
        var next = CaptureCurrentProfile();
        var setup = ReadProfile(next, name);
        next.ActiveProfile = name;
        CommitProfileChange(next, setup);
        return $"Profile '{name}' loaded.";
    }

    public string RenameProfile(string name, string newName) {
        newName = ProfileRepository.ValidateName(newName);
        var next = CaptureCurrentProfile();
        name = next.Profiles.Keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Select an existing profile.");
        if (!name.Equals(newName, StringComparison.OrdinalIgnoreCase) && next.Profiles.ContainsKey(newName)) {
            throw new ArgumentException("That profile name is already in use.");
        }
        var snapshot = next.Profiles[name];
        next.Profiles.Remove(name);
        next.Profiles.Add(newName, snapshot);
        if (next.ActiveProfile.Equals(name, StringComparison.OrdinalIgnoreCase)) {
            next.ActiveProfile = newName;
        }

        foreach (var assignments in new[] { next.JobProfiles, next.HudLayoutProfiles }) {
            foreach (var key in assignments.Keys.ToArray()) {
                if (assignments[key].Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    assignments[key] = newName;
                }
            }
        }
        CommitProfileChange(next);
        return $"Profile renamed to '{newName}'.";
    }

    public string DeleteProfile(string name) {
        var next = CaptureCurrentProfile();
        if (next.ActiveProfile.Equals(name, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Load another profile before deleting the active one.");
        }
        if (!next.Profiles.Remove(name)) {
            throw new ArgumentException("Select an existing profile.");
        }

        foreach (var assignments in new[] { next.JobProfiles, next.HudLayoutProfiles }) {
            foreach (var key in assignments.Keys.ToArray()) {
                if (assignments[key].Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    assignments.Remove(key);
                }
            }
        }
        CommitProfileChange(next);
        return $"Profile '{name}' deleted.";
    }

    public string ApplyPreset(string id) {
        var preset = BuiltInPresetCatalog.All.FirstOrDefault(item => item.Id == id)
            ?? throw new ArgumentException("Unknown preset.");
        var current = CaptureSetup();
        var setup = BuiltInPresetCatalog.Create(id, current);
        PlaceNewPresetOverlays(current, setup);
        var next = CaptureCurrentProfile();
        var name = UniqueProfileName(next, preset.Name);
        next.Profiles.Add(name, ConfigTransfer.ExportSetup(setup));
        next.ActiveProfile = name;
        CommitProfileChange(next, setup);
        foreach (var kind in Enum.GetValues<NodeKind>()) {
            var overlay = GetOverlay(kind)!;
            if (!overlay.OverlayConfig.Enabled) {
                continue;
            }

            overlay.IsPreviewEnabled = true;
            overlay.IsLocked = false;
        }
        return $"Profile '{name}' created.";
    }

    private static unsafe void PlaceNewPresetOverlays(OverlaySetup current, OverlaySetup setup) {
        const float spacing = 24;
        var overlays = new[] { NodeKind.Combined, NodeKind.Buffs, NodeKind.Debuffs, NodeKind.MultiDoT }
            .Select(kind => setup.Overlays[kind])
            .Where(config => config.Enabled)
            .Select(config => (Config: config, Size: OverlayLayoutHelper.CalculateOverlaySize(config) * (config.ScaleInt * 0.01f)))
            .ToArray();
        if (overlays.Length == 0) {
            return;
        }

        var origin = current.Overlays[NodeKind.Combined].Position;
        if (origin == Vector2.Zero) {
            origin = new Vector2(40, 100);
        }

        var stage = AtkStage.Instance();
        if (stage != null) {
            var width = overlays.Sum(overlay => overlay.Size.X) + spacing * (overlays.Length - 1);
            var height = overlays.Max(overlay => overlay.Size.Y);
            origin = new Vector2(
                Math.Clamp(origin.X, 0, Math.Max(0, stage->ScreenSize.Width - width)),
                Math.Clamp(origin.Y, 0, Math.Max(0, stage->ScreenSize.Height - height)));
        }

        var offset = 0f;
        foreach (var overlay in overlays) {
            overlay.Config.Position = origin + new Vector2(offset, 0);
            offset += overlay.Size.X + spacing;
        }
    }

    private static string UniqueProfileName(ProfileStore store, string prefix) {
        for (int suffix = 1; ; suffix++) {
            var name = suffix == 1 ? prefix : $"{prefix} {suffix}";
            if (!store.Profiles.ContainsKey(name)) {
                return name;
            }
        }
    }

    private void ApplySetup(OverlaySetup setup) {
        var next = CaptureCurrentProfile();
        next.Profiles[next.ActiveProfile] = ConfigTransfer.ExportSetup(setup);
        CommitProfileChange(next, setup);
    }

    public string ImportProfile(string input) {
        if (!ConfigTransfer.TryImportSetup(input, out var setup, out var error)) {
            throw new ArgumentException(error);
        }

        FitSetupToScreen(setup!);
        var next = CaptureCurrentProfile();
        var name = UniqueProfileName(next, "Imported");
        next.Profiles.Add(name, ConfigTransfer.ExportSetup(setup!));
        next.ActiveProfile = name;
        CommitProfileChange(next, setup);
        return $"Profile '{name}' imported from clipboard.";
    }

    public string ExportProfile() => CaptureProfileSnapshot();

    public string SetAutoSwitchJobs(bool enabled) {
        var next = CaptureCurrentProfile();
        next.AutoSwitchJobs = enabled;
        CommitProfileChange(next, rebuild: false);
        _profileSwitcher.RequestRefresh();
        return enabled ? "Automatic job profile switching enabled." : "Automatic job profile switching disabled.";
    }

    public string AssignJobProfile(uint jobId, string? name) {
        var next = CaptureCurrentProfile();
        if (name == null) {
            next.JobProfiles.Remove(jobId);
        }
        else {
            if (!next.Profiles.ContainsKey(name)) {
                throw new ArgumentException("Select an existing profile.");
            }

            next.JobProfiles[jobId] = name;
        }
        CommitProfileChange(next, rebuild: false);
        _profileSwitcher.RequestRefresh();
        return name == null ? "Job profile assignment cleared." : $"Job assigned to '{name}'.";
    }

    public string SetAutoSwitchHudLayouts(bool enabled) {
        var next = CaptureCurrentProfile();
        next.AutoSwitchHudLayouts = enabled;
        CommitProfileChange(next, rebuild: false);
        _profileSwitcher.RequestRefresh();
        return enabled ? "HUD layout profile switching enabled." : "HUD layout profile switching disabled.";
    }

    public string AssignHudLayoutProfile(uint layout, string? name) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>(layout, 3);

        var next = CaptureCurrentProfile();
        if (name == null) {
            next.HudLayoutProfiles.Remove(layout);
        }
        else {
            if (!next.Profiles.ContainsKey(name)) {
                throw new ArgumentException("Select an existing profile.");
            }

            next.HudLayoutProfiles[layout] = name;
        }
        CommitProfileChange(next, rebuild: false);
        _profileSwitcher.RequestRefresh();
        return name == null ? "HUD layout assignment cleared." : $"HUD Layout {layout + 1} assigned to '{name}'.";
    }

}
