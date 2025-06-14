using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FinalFantasyAIPlugin.Windows;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Application.Network;
using ImGuiNET;
using System.Numerics;
using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Hooking;
using System.Text.Json;
using FinalFantasyAIPlugin.Services.QuestServices;
using FinalFantasyAIPlugin.Services.PlayerService;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace FinalFantasyAIPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    private const string CommandName = "/ai";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FinalFantasyAIPlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;


        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"Final Fantasy AI plugin is running...");

        // 🟢 This sends a chat message when the plugin loads
        //PlayEmote(1);

        QuestDataDumper.InitializeQuestCache(DataManager, PluginInterface);
        QuestService.Initialize();
        QuestService.OnQuestAdded += OnQuestAdded;
        QuestService.OnQuestRemoved += OnQuestRemoved;

        _ = AIIntegrationManager.SendEventAsync("quests", JsonSerializer.Serialize(QuestService.questInfos));

        PlayerService.OnPlayerInfoUpdated += InitializePlayerInfo;

        Framework.Update += OnFrameworkUpdate;
    }

    private void InitializePlayerInfo(PlayerInfo playerInfo)
    {
        SendPlayerInfo("player_info", playerInfo);
        PlayerService.OnPlayerInfoUpdated -= InitializePlayerInfo;

        playerInfo.OnNameChanged += _ => SendPlayerInfo("player_name", playerInfo);
        playerInfo.OnLevelChanged += _ => SendPlayerInfo("player_level", playerInfo);
        playerInfo.OnClassJobChanged += _ => SendPlayerInfo("player_class_job", playerInfo);
        playerInfo.OnRegionChanged += _ => SendPlayerInfo("player_region", playerInfo);
        playerInfo.OnZoneChanged += _ => SendPlayerInfo("player_zone", playerInfo);
        playerInfo.OnPlaceZoneChanged += _ => SendPlayerInfo("player_place_zone", playerInfo);
        playerInfo.OnPlaceChanged += _ => SendPlayerInfo("player_place", playerInfo);
        playerInfo.OnSubPlaceChanged += _ => SendPlayerInfo("player_sub_place", playerInfo);
        playerInfo.OnWeatherChanged += _ => SendPlayerInfo("player_weather", playerInfo);
    }

    private void SendPlayerInfo(string eventName, PlayerInfo info)
    {
        _ = AIIntegrationManager.SendEventAsync(eventName, JsonSerializer.Serialize(info));
    }

    private void OnQuestAdded(QuestInfo quest)
    {
        _ = AIIntegrationManager.SendEventAsync("quest_added", JsonSerializer.Serialize(quest));
    }

    private void OnQuestRemoved(QuestInfo quest)
    {
        _ = AIIntegrationManager.SendEventAsync("quest_completed", JsonSerializer.Serialize(quest));
    }

    public unsafe void PlayEmote(ushort emoteId)
    {
        var emoteManager = EmoteManager.Instance();
        if (emoteManager == null)
        {
            ChatGui.PrintError("EmoteManager instance not found.");
            return;
        }

        if (!emoteManager->CanExecuteEmote(emoteId))
        {
            ChatGui.PrintError($"Cannot execute emote ID {emoteId}.");
            return;
        }

        var result = emoteManager->ExecuteEmote(emoteId, null);
        ChatGui.Print($"Emote ID {emoteId} executed: {result}");
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }
    private void OnFrameworkUpdate(IFramework framework)
    {
        QuestService.UpdateActiveQuests(framework.UpdateDelta, DataManager, PluginInterface); 
        AIIntegrationManager.Update(framework.UpdateDelta);
        PlayerService.Update();
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI()
    {
        WindowSystem.Draw();

        ChatBubbleManager.DrawBubble(ClientState, GameGui, ChatGui);
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
