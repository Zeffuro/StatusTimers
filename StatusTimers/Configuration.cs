using Dalamud.Configuration;
using System;
using Newtonsoft.Json;

namespace StatusTimers;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    [JsonProperty] public bool ConfigOption = true;

    public void Save()
    {
        Services.PluginInterface.SavePluginConfig(this);
    }
}