using KamiToolKit.Nodes;
using Newtonsoft.Json;
using StatusTimers.Config;
using StatusTimers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Helpers;

public class Util
{
    private static readonly Dictionary<string, TimeFormatTemplate> TemplateCache = new();

    public static string SerializeUIntSet(HashSet<uint> set)
        => string.Join(",", set.OrderBy(x => x));

    public static HashSet<uint> DeserializeUIntSet(string data)
        => data
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => uint.TryParse(s, out var val) ? val : (uint?)null)
            .Where(v => v.HasValue)
            .Select(v => v.Value)
            .ToHashSet();

    public static string CompressToBase64(string str)
    {
        var compressed = Dalamud.Utility.Util.CompressString(str);
        return Convert.ToBase64String(compressed);
    }

    public static string DecompressFromBase64(string base64)
    {
        var decompressed = Dalamud.Utility.Util.DecompressString(Convert.FromBase64String(base64));
        return decompressed;
    }

    public static string SerializeFilterList(HashSet<uint> filterList)
        => CompressToBase64(SerializeUIntSet(filterList));

    public static HashSet<uint> DeserializeFilterList(string input)
    {
        try
        {
            var decompressed = DecompressFromBase64(input);
            return DeserializeUIntSet(decompressed);
        }
        catch
        {
            return new HashSet<uint>();
        }
    }

    public static TKey? FindKeyByValue<TKey, TValue>(Dictionary<TKey, TValue> dict, TValue value)
        where TKey : struct {
        foreach (var kvp in dict.Where(kvp => EqualityComparer<TValue>.Default.Equals(kvp.Value, value)))
        {
            return kvp.Key;
        }

        return null;
    }

    public static string SerializeConfig(StatusTimerOverlayConfig config)
    {
        var json = JsonConvert.SerializeObject(config);
        var compressed = Dalamud.Utility.Util.CompressString(json);
        return Convert.ToBase64String(compressed);
    }

    public static StatusTimerOverlayConfig? DeserializeConfig(string input)
    {
        try
        {
            var json = Dalamud.Utility.Util.DecompressString(Convert.FromBase64String(input));
            return JsonConvert.DeserializeObject<StatusTimerOverlayConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    public static string FormatTime(double seconds, string template)
    {
        if (!TemplateCache.TryGetValue(template, out var fmt))
        {
            fmt = new TimeFormatTemplate(template);
            TemplateCache[template] = fmt;
        }
        return fmt.Format(seconds);
    }

    public static string SafeFormatTime(double seconds, string template)
    {
        try {
            return FormatTime(seconds, template);
        } catch {
            return FormatTime(seconds, "{S.0}s");
        }
    }

    public static void ApplyConfigProps(StatusTimerOverlayConfig config, SimpleComponentNode overlay)
    {
        if (config != null && overlay != null) {
            overlay.IsVisible = config.Enabled;
            overlay.Position = config.Position;
        }
    }

    public static void SaveOverlayProps(StatusTimerOverlayConfig config, SimpleComponentNode overlay)
    {
        if (config != null && overlay != null) {
            config.Enabled = overlay.IsVisible;
            config.Position = overlay.Position;
        }
    }

    public static void ResetConfig(StatusTimerOverlayConfig config, NodeKind nodeKind)
    {
        var defaults = new StatusTimerOverlayConfig(nodeKind);
        foreach (var prop in typeof(StatusTimerOverlayConfig).GetProperties().Where(p => p.CanRead && p.CanWrite)) {
            prop.SetValue(config, prop.GetValue(defaults));
        }
    }
}
