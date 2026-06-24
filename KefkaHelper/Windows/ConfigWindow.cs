using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace KefkaHelper.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin _plugin;

    public ConfigWindow(Plugin plugin) : base("Kefka Helper Configuration ###KefkaConfig")
    {
        _plugin = plugin;

        Size = new Vector2(300, 100);
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var configValue = configuration.IsDisplayForsakenDebuffs;
        if (ImGui.Checkbox("Forsaken Debuff Monitoring", ref configValue))
        {
            configuration.IsDisplayForsakenDebuffs = configValue;
            configuration.Save();
        }

        ImGui.Spacing();
        if (ImGui.SmallButton("Toggle Forsaken Preview"))
        {
            if (_plugin.ForsakenWindow.IsOpen)
            {
                _plugin.ForsakenWindow.IsOpen = false;
                _plugin.ForsakenWindow.IsPreview = false;
            }
            else
            {
                _plugin.ForsakenWindow.IsOpen = true;
                _plugin.ForsakenWindow.IsPreview = true;
            }
        }
    }
}
