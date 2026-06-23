using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Linq;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
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
    public static ISigScanner SigScanner { get; private set; }
    
    [PluginService]
    internal static IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    
    [PluginService]
    internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("KefkaHelper");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private const string CommandName = "/kefkahelper";

    public readonly StatusProcessor StatusProcessor = new();
    public readonly MarkerManager MarkerManager = new();
    
    public static List<IPartyMember> OrderedPartyList => GetOrderedPartyList();
    
    public Plugin()
    {

        GameInteropProvider.InitializeFromAttributes(new CommandExecutor());

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

        var partyList = OrderedPartyList;

        Log.Info($"Party list count: {partyList.Count}");
        
        foreach (var entry in partyList)
        {
            foreach (var status in entry.Statuses)
            {
                Log.Info(
                    $"Party member {entry.Name} status: {status.GameData.Value.Name.ToString()} {status.StatusId}");
            }
        }
    }

    private static List<IPartyMember> GetOrderedPartyList()
    {
        return PartyList
               .OrderBy(e => GetPartySlot(e.Name.ToString()))
               .ToList();
    }

    private static unsafe StatusList GetLocalStatuses()
    {
        var character = Control.Instance()->LocalPlayer;
        return StatusList.CreateStatusListReference((nint)character->GetStatusManager())!;
    }

    private static unsafe int GetPartySlot(string playerName)
    {
        var addon = GameGui.GetAddonByName<AddonPartyList>("_PartyList");
        if (addon == null || !addon->IsVisible)
        {
            Log.Info($"Addon not visible");
            return 0;
        }

        for (var i = 0; i < addon->PartyMembers.Length; i++)
        {
            var partyMember = addon->PartyMembers[i];
            var name = partyMember.Name->NodeText.ExtractText();
            Log.Info($"Name: '{name}' '{playerName}'");
            if (name.Contains(playerName))
            {
                return i + 1;
            }
        }
        return 0;
    }
    
    
}
