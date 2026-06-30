using Dalamud.Configuration;
using System;

namespace KefkaHelper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public enum BlackholeAutomarkerMode
    {
        Disabled, 
        AccretionOnly,
        All
    }
    
    // base
    public int Version { get; set; } = 0;

    // forsaken
    public bool ForsakenDisplayDebuffs { get; set; } = true;
    public bool ForsakenPastFutureMessages { get; set; } = true;
    
    // blackhole
    public BlackholeAutomarkerMode BlackHoleAutomarkerMode { get; set; } = BlackholeAutomarkerMode.Disabled;
    public float BlackholeAutomarkerStagger { get; set; } = 0.5f;
    public float BlackholeAutomarkerDelay { get; set; } = 2.0f;
    
    // lifetime
    public event Action? OnChanged;
    
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
        OnChanged?.Invoke();
    }
}
