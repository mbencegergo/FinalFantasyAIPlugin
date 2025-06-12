using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace SamplePlugin;

public static class QuestManagerService
{
    private static readonly string SaveFilePath = Path.Combine(
        Plugin.PluginInterface.ConfigDirectory.FullName, "active_quests.json");
    private static readonly string SaveDataPath = Path.Combine(
        Plugin.PluginInterface.ConfigDirectory.FullName, "active_quests_data.json");

    private static readonly HashSet<uint> activeQuests = new();

    private static TimeSpan timeSinceLastUpdate = TimeSpan.Zero;
    private static readonly TimeSpan updateInterval = TimeSpan.FromSeconds(5);

    public static IReadOnlyCollection<uint> ActiveQuestIds => activeQuests;

    public static List<QuestInfo> questInfos = new();
    public static event Action<List<QuestInfo>>? OnQuestsUpdated;
    public static event Action<QuestInfo>? OnQuestAdded;
    public static event Action<QuestInfo>? OnQuestRemoved;

    private static bool questsInitialized = false;

    public static void UpdateActiveQuests(TimeSpan deltaTime, IDataManager dataManager, IDalamudPluginInterface pluginInterface, bool forced = false)
    {
        timeSinceLastUpdate += deltaTime;

        if (!forced && timeSinceLastUpdate < updateInterval)
            return;

        timeSinceLastUpdate = TimeSpan.Zero;

        var currentQuestIds = QuestMemoryReader.GetActiveQuestIds().ToHashSet();

        if (activeQuests.SetEquals(currentQuestIds))
            return;

        bool changed = false;

        var removedQuests = activeQuests.Except(currentQuestIds).ToList();
        var addedQuests = currentQuestIds.Except(activeQuests).ToList();

        foreach (var oldQuestId in removedQuests)
        {
            var oldQuestInfo = questInfos.FirstOrDefault(x => x.Id == oldQuestId);
            activeQuests.Remove(oldQuestId);
            if (oldQuestInfo != null && questsInitialized)
                OnQuestRemoved?.Invoke(oldQuestInfo);
            changed = true;
        }

        foreach (var newQuestId in addedQuests)
        {
            activeQuests.Add(newQuestId);
            changed = true;
        }

        if (changed)
        {
            questInfos = QuestDataDumper.DumpActiveQuestData(dataManager, pluginInterface, activeQuests);

            foreach (var newQuestId in addedQuests)
            {
                var newQuestInfo = questInfos.FirstOrDefault(x => x.Id == newQuestId);
                if (newQuestInfo != null && questsInitialized)
                    OnQuestAdded?.Invoke(newQuestInfo);
            }

            Save();
            OnQuestsUpdated?.Invoke(questInfos);
        }

        questsInitialized = true;
    }

    public static void Load()
    {
        TryLoadIds();
        TryLoadQuestInfos();
    }

    private static bool TryLoadIds()
    {
        if (!File.Exists(SaveFilePath))
            return false;

        try
        {
            var json = File.ReadAllText(SaveFilePath);
            var ids = JsonSerializer.Deserialize<List<uint>>(json);
            if (ids != null)
            {
                activeQuests.Clear();
                foreach (var id in ids)
                    activeQuests.Add(id);
            }
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to load quest ids: {ex}");
            return false;
        }
    }

    private static bool TryLoadQuestInfos()
    {
        if (!File.Exists(SaveDataPath))
            return false;

        try
        {
            var json = File.ReadAllText(SaveDataPath);
            var infos = JsonSerializer.Deserialize<List<QuestInfo>>(json);
            if (infos != null)
            {
                questInfos.Clear();
                foreach (var info in infos)
                    questInfos.Add(info);
            }
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to load quest data: {ex}");
            return false;
        }
    }

    private static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(activeQuests);
            File.WriteAllText(SaveFilePath, json);

            File.WriteAllText(SaveDataPath, JsonSerializer.Serialize(questInfos, new JsonSerializerOptions { WriteIndented = true }));
            Plugin.Log.Information($"Active quest data exported to: {SaveDataPath}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to save quest data: {ex}");
        }
    }
}
