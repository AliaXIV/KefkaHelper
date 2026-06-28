using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Linq;
using System.Text;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using KefkaHelper.Windows;
using KefkaHelper.Services;
using Action = Lumina.Excel.Sheets.Action;

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

    private static readonly Dictionary<string, int> PartyOrderSymbolMapping = new()
    {
        {"E090", 1},
        {"E091", 2},
        {"E092", 3},
        {"E093", 4},
        {"E094", 5},
        {"E095", 6},
        {"E096", 7},
        {"E097", 8},
        {"E0E0", 1},
        {"E0E1", 2},
        {"E0E2", 3},
        {"E0E3", 4},
        {"E0E4", 5},
        {"E0E5", 6},
        {"E0E6", 7},
        {"E0E7", 8},
    };

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

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand));

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Configuration.OnChanged += ConfigurationOnChanged;
        ConfigurationOnChanged();
    }

    private void ConfigurationOnChanged()
    {
        KefkaProcessor.SetForsakenEnabled(Configuration.IsDisplayForsakenDebuffs);
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
