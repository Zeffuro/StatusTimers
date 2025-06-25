using Dalamud.Configuration;
using Newtonsoft.Json;
using System;

namespace StatusTimers;

[Serializable]
public class Configuration : IPluginConfiguration {
    [JsonProperty] public bool ConfigOption = true;
    public int Version { get; set; } = 0;

    public void Save() {
        Services.Services.PluginInterface.SavePluginConfig(this);
    }
}
