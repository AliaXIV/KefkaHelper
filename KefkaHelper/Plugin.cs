using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using KefkaHelper.Windows;

namespace KefkaHelper;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IPlayerState PlayerState { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IPartyList PartyList { get; private set; } = null!;

    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;
    
    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("KefkaHelper");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private const string CommandName = "/kefkahelper";

    public readonly StatusProcessor StatusProcessor = new();
    public readonly MarkerManager MarkerManager = new(); 
    
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand));

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        StatusProcessor.Dispose();
        MarkerManager.Dispose();
        
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();

    public void LogDebuffStatuses()
    {
        var localStatuses = GetLocalStatuses();
        foreach (var status in localStatuses)
        {
            Log.Info($"Self status: {status.GameData.Value.Name.ToString()} {status.StatusId}");
        }

        Log.Info($"Party list count: {PartyList.Length}");

        for (var i = 0; i < PartyList.Length; i++)
        {
            var entry = PartyList[i];
            if (entry == null)
            {
                continue;
            }

            foreach (var status in entry.Statuses)
            {
                Log.Info(
                    $"Party member {entry.Name} status: {status.GameData.Value.Name.ToString()} {status.StatusId}");
            }
        }
    }


    private unsafe StatusList GetLocalStatuses()
    {
        var character = Control.Instance()->LocalPlayer;
        return StatusList.CreateStatusListReference((nint)character->GetStatusManager())!;
    }
}
