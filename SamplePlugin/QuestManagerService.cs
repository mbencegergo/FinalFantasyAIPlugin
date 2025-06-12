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

    private static readonly HashSet<uint> activeQuests = new();

    private static TimeSpan timeSinceLastUpdate = TimeSpan.Zero;
    private static readonly TimeSpan updateInterval = TimeSpan.FromSeconds(5);

    public static IReadOnlyCollection<uint> ActiveQuestIds => activeQuests;

    public static List<QuestInfo> questInfos = new();
    public static event Action<List<QuestInfo>>? OnQuestsUpdated;
    public static event Action<uint>? OnQuestAdded;
    public static event Action<uint>? OnQuestRemoved;

    public static void UpdateActiveQuests(TimeSpan deltaTime, IDataManager dataManager, IDalamudPluginInterface pluginInterface, bool forced = false)
    {
        timeSinceLastUpdate += deltaTime;

        if (!forced && timeSinceLastUpdate < updateInterval)
            return;

        timeSinceLastUpdate = TimeSpan.Zero;

        var currentQuestIds = QuestMemoryReader.GetActiveQuestIds().ToHashSet();

        bool changed = false;

        // Detect removed quests
        foreach (var oldQuestId in activeQuests.ToArray())
        {
            if (!currentQuestIds.Contains(oldQuestId))
            {
                activeQuests.Remove(oldQuestId);
                OnQuestRemoved?.Invoke(oldQuestId);
                changed = true;
            }
        }

        // Detect added quests
        foreach (var newQuestId in currentQuestIds)
        {
            if (activeQuests.Add(newQuestId))
            {
                OnQuestAdded?.Invoke(newQuestId);
                changed = true;
            }
        }

        if (changed)
        {
            questInfos = QuestDataDumper.DumpActiveQuestData(dataManager, pluginInterface, activeQuests);
            Save();
            OnQuestsUpdated?.Invoke(questInfos);
        }
    }

    public static void AddQuest(uint questId)
    {
        if (activeQuests.Add(questId))
        {
            Save();
            OnQuestAdded?.Invoke(questId);
            OnQuestsUpdated?.Invoke(questInfos);
        }
    }

    public static void RemoveQuest(uint questId)
    {
        if (activeQuests.Remove(questId))
        {
            Save();
            OnQuestRemoved?.Invoke(questId);
            OnQuestsUpdated?.Invoke(questInfos);
        }
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
        var path = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "active_quests_data.json");
        if (!File.Exists(path))
            return false;

        try
        {
            var json = File.ReadAllText(path);
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

            var path = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "active_quests_data.json");
            File.WriteAllText(path, JsonSerializer.Serialize(questInfos, new JsonSerializerOptions { WriteIndented = true }));
            Plugin.Log.Information($"Active quest data exported to: {path}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to save quest data: {ex}");
        }
    }
}
