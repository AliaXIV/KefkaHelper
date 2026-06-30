using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Linq;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using KefkaHelper.Windows;
using KefkaHelper.Services;

namespace KefkaHelper;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService]
    internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService]
    public static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService]
    internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;
    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;
    [PluginService]
    internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService]
    internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;
    
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("KefkaHelper");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public ForsakenWindow ForsakenWindow { get; init; }

    private const string CommandName = "/kefkahelper";

    public readonly KefkaProcessor KefkaProcessor;
    public readonly MarkerManager MarkerManager = new();
    public readonly BattleProcessor BattleProcessor;

    public static List<IPartyMember> OrderedPartyList => GetOrderedPartyList();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        BattleProcessor = new BattleProcessor();
        KefkaProcessor = new KefkaProcessor(this);

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        ForsakenWindow = new ForsakenWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ForsakenWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens main window of the plugin, as of now it contains blackhole data"
        });

    PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Configuration.OnChanged += ConfigurationOnChanged;
        ConfigurationOnChanged();
        
    }

    private void ConfigurationOnChanged()
    {
        KefkaProcessor.SetForsakenEnabled(Configuration.ForsakenDisplayDebuffs);
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        ForsakenWindow.Dispose();

        KefkaProcessor.Dispose();
        MarkerManager.Dispose();
        BattleProcessor.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
    

    private static List<IPartyMember> GetOrderedPartyList()
    {
        return PartyListResolver.GetPartyListSnapshot()
                                .OrderBy(e => e.Slot)
                                .Select(e => e.Member)
                                .ToList();
    }

    private static unsafe StatusList GetLocalStatuses()
    {
        var character = Control.Instance()->LocalPlayer;
        return StatusList.CreateStatusListReference((nint)character->GetStatusManager())!;
    }
}
