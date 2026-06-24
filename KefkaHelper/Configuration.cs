using Dalamud.Configuration;
using System;

namespace KefkaHelper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsDisplayForsakenDebuffs { get; set; } = true;

    public event Action Changed; 
    
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
        Changed?.Invoke();
    }
}
