using Newtonsoft.Json;
using StatusTimers.Config;
using System;
using System.Collections.Generic;
using System.IO;

namespace StatusTimers.Helpers;

public sealed class ProfileRepository(string directory) {
    private readonly string path = Path.Combine(directory, "Profiles.json");
    public bool Exists => File.Exists(path);

    public ProfileStore Load() {
        var store = JsonConvert.DeserializeObject<ProfileStore>(File.ReadAllText(path))
            ?? throw new InvalidDataException("The profile file is empty.");
        if (store.Version != 1) {
            throw new InvalidDataException("Unsupported profile file version.");
        }

        if (store.Profiles == null || !store.Profiles.ContainsKey(store.ActiveProfile)) {
            throw new InvalidDataException("The active profile is missing.");
        }

        store.Profiles = new Dictionary<string, string>(store.Profiles, StringComparer.OrdinalIgnoreCase);
        store.JobProfiles ??= [];
        store.HudLayoutProfiles ??= [];
        return store;
    }

    public void Save(ProfileStore store) {
        var json = JsonConvert.SerializeObject(store, Formatting.Indented);
        Directory.CreateDirectory(directory);
        var temporaryPath = path + ".tmp";
        try {
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(path)) {
                File.Replace(temporaryPath, path, path + ".bak");
            }
            else {
                File.Move(temporaryPath, path);
            }
        }
        finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }

    public static ProfileStore Clone(ProfileStore store) => new() {
        Version = store.Version,
        ActiveProfile = store.ActiveProfile,
        Profiles = new Dictionary<string, string>(store.Profiles, StringComparer.OrdinalIgnoreCase),
        JobProfiles = new Dictionary<uint, string>(store.JobProfiles),
        AutoSwitchJobs = store.AutoSwitchJobs,
        HudLayoutProfiles = new Dictionary<uint, string>(store.HudLayoutProfiles),
        AutoSwitchHudLayouts = store.AutoSwitchHudLayouts
    };

    public static string ValidateName(string name) {
        name = name.Trim();
        if (name.Length is 0 or > 64) {
            throw new ArgumentException("Profile names must contain 1 to 64 characters.");
        }

        return name;
    }
}
