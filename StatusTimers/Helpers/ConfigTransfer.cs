using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StatusTimers.Config;
using StatusTimers.Enums;
using System;
using System.Linq;

namespace StatusTimers.Helpers;

public static class ConfigTransfer {
    public static StatusTimerOverlayConfig CloneOverlay(NodeKind kind, StatusTimerOverlayConfig config)
        => ReadOverlay(JObject.FromObject(config), kind);

    public static OverlaySetup CloneSetup(OverlaySetup setup) => new() {
        Overlays = setup.Overlays.ToDictionary(pair => pair.Key, pair => CloneOverlay(pair.Key, pair.Value))
    };

    public static string ExportSetup(OverlaySetup setup)
        => Util.CompressToBase64(JsonConvert.SerializeObject(setup));

    public static string ExportOverlay(NodeKind kind, StatusTimerOverlayConfig config)
        => Util.SerializeConfig(config);

    public static bool TryImportSetup(string input, out OverlaySetup? setup, out string error) {
        setup = null;
        error = string.Empty;
        try {
            var root = Parse(input);
            if (root["Overlays"] is not JObject overlays) {
                throw new ArgumentException("Import this configuration in its overlay tab.");
            }

            var result = new OverlaySetup();
            foreach (var kind in Enum.GetValues<NodeKind>()) {
                if (overlays[kind.ToString()] is not JObject data) {
                    throw new ArgumentException($"{kind} settings are missing.");
                }

                result.Overlays.Add(kind, ReadOverlay(data, kind));
            }
            setup = result;
            return true;
        }
        catch (Exception ex) {
            error = ex is ArgumentException ? ex.Message : "Clipboard data was invalid or could not be imported.";
            return false;
        }
    }

    public static bool TryImportOverlay(string input, NodeKind kind,
        out StatusTimerOverlayConfig? config, out string error) {
        config = null;
        error = string.Empty;
        try {
            var data = Parse(input);
            if (data["Overlays"] != null) {
                throw new ArgumentException("Import this profile in the Profiles tab.");
            }

            if (!data.Properties().Any(property => typeof(StatusTimerOverlayConfig).GetProperty(property.Name) != null)) {
                throw new ArgumentException("Clipboard data was invalid or could not be imported.");
            }

            config = ReadOverlay(data, kind);
            return true;
        }
        catch (Exception ex) {
            error = ex is ArgumentException ? ex.Message : "Clipboard data was invalid or could not be imported.";
            return false;
        }
    }

    private static JObject Parse(string input) {
        if (string.IsNullOrWhiteSpace(input)) {
            throw new ArgumentException("Clipboard is empty.");
        }

        var text = input.Trim();
        return JObject.Parse(text.StartsWith('{') ? text : Util.DecompressFromBase64(text));
    }

    private static StatusTimerOverlayConfig ReadOverlay(JObject data, NodeKind kind) {
        var config = new StatusTimerOverlayConfig(kind);
        JsonConvert.PopulateObject(data.ToString(), config);
        var defaults = new StatusTimerOverlayConfig(kind);
        config.Icon ??= defaults.Icon;
        config.Name ??= defaults.Name;
        config.Timer ??= defaults.Timer;
        config.Actor ??= defaults.Actor;
        config.Progress ??= defaults.Progress;
        config.Background ??= defaults.Background;
        StatusTimerOverlayConfig.NodePartConfig[] parts =
            [config.Icon, config.Name, config.Timer, config.Actor, config.Progress, config.Background];
        StatusTimerOverlayConfig.NodePartConfig[] defaultParts =
            [defaults.Icon, defaults.Name, defaults.Timer, defaults.Actor, defaults.Progress, defaults.Background];
        for (var i = 0; i < parts.Length; i++) {
            parts[i].Anchor ??= defaultParts[i].Anchor;
            parts[i].StyleKind = defaultParts[i].StyleKind;
            parts[i].Style ??= defaultParts[i].Style?.Clone();
            parts[i].StyleBar ??= defaultParts[i].StyleBar?.Clone();
            parts[i].EnsureStyleConsistency();
        }
        config.FilterList ??= [];
        config.TimerFormat ??= defaults.TimerFormat;
        config.ItemsPerLine = Math.Clamp(config.ItemsPerLine, 1, 30);
        config.MaxStatuses = Math.Clamp(config.MaxStatuses, 1, 30);
        return config;
    }
}
