using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace KefkaHelper.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly Plugin _plugin;


    public ConfigWindow(Plugin plugin) : base("Kefka Helper Configuration ###KefkaConfig")
    {
        _plugin = plugin;

        Size = new Vector2(300, 300);
        _configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var forsakenDisplayDebuffs = _configuration.ForsakenDisplayDebuffs;
        var forsakenPastFutureMessages = _configuration.ForsakenPastFutureMessages;
        var blackholeAmModeSelection = (int)_configuration.BlackHoleAutomarkerMode;
        var blackholeAmStagger = _configuration.BlackholeAutomarkerStagger;
        var blackholeAmDelay = _configuration.BlackholeAutomarkerDelay;


        ImGui.Text("Forsaken");
        if (ImGui.Checkbox("Show Debuff Window", ref forsakenDisplayDebuffs))
        {
            _configuration.ForsakenDisplayDebuffs = forsakenDisplayDebuffs;
            _configuration.Save();
        }
        if (ImGui.Checkbox("Past/Future messages", ref forsakenPastFutureMessages))
        {
            _configuration.ForsakenPastFutureMessages = forsakenPastFutureMessages;
            _configuration.Save();
        }
        ImGui.Spacing();
        if (ImGui.SmallButton("Toggle Preview"))
        {
            _plugin.ForsakenWindow.TogglePreview();
        }

        ImGui.Separator();
        ImGui.Text("Blackhole");
        if (ImGui.Combo("AM Mode", ref blackholeAmModeSelection, Enum.GetNames<Configuration.BlackholeAutomarkerMode>().ToList()))
        {
            _configuration.BlackHoleAutomarkerMode = (Configuration.BlackholeAutomarkerMode)blackholeAmModeSelection;
            _configuration.Save();
        }
        if (ImGui.SliderFloat("AM Stagger", ref blackholeAmStagger, 0.5f, 2.0f, "%0.1f s"))
        {
            _configuration.BlackholeAutomarkerStagger = blackholeAmStagger;
            _configuration.Save();
        }
        if (ImGui.SliderFloat("AM Delay", ref blackholeAmDelay, 0.0f, 5.0f, "%0.1f s"))
        {
            _configuration.BlackholeAutomarkerDelay = blackholeAmDelay;
            _configuration.Save();
        }
    }
}
