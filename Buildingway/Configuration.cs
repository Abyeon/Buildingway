using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace Buildingway;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool SpawnWithCollision { get; set; }
    public Dictionary<string, string> PathDictionary { get; set; } = new();

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
