using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SamplePlugin;

public static class QuestDataDumper
{
    private static readonly Dictionary<uint, Quest> _questIdMap = new();
    private static List<QuestData> questData;

    public static void InitializeQuestCache(IDataManager dataManager, IDalamudPluginInterface pluginInterface)
    {
        var questSheet = dataManager.GetExcelSheet<Quest>();
        if (questSheet == null) return;

        _questIdMap.Clear();
        foreach (var quest in questSheet)
        {
            var idStr = quest.Id.ToString();
            var parts = idStr.Split('_');
            if (parts.Length == 2 && uint.TryParse(parts[1], out var parsedId))
                _questIdMap[parsedId] = quest;
        }

        try
        {
            var dataPath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName ?? "", "quests.json");

            if (File.Exists(dataPath))
            {
                var json = File.ReadAllText(dataPath);
                questData = JsonSerializer.Deserialize<List<QuestData>>(json) ?? new List<QuestData>();
                Plugin.Log.Information($"Loaded {questData.Count} entries from quests.json.");
            }
            else
            {
                Plugin.Log.Warning($"quests.json not found at path: {dataPath}");
                questData = new List<QuestData>();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Failed to load quests.json: {ex}");
            questData = new List<QuestData>();
        }
    }

    public static List<QuestInfo> DumpActiveQuestData(
        IDataManager dataManager,
        IDalamudPluginInterface pluginInterface,
        IReadOnlyCollection<uint> activeQuestIds)
    {
        if (_questIdMap.Count == 0 || activeQuestIds.Count == 0)
        {
            Plugin.Log.Warning("Quest data dump skipped: no quest cache or active quests.");
            return null;
        }

        var npcSheet = dataManager.GetExcelSheet<ENpcResident>();
        var jobSheet = dataManager.GetExcelSheet<ClassJob>();
        var journalSheet = dataManager.GetExcelSheet<JournalGenre>();

        var questInfos = new List<QuestInfo>();

        foreach (var questId in activeQuestIds)
        {
            if (!_questIdMap.TryGetValue(questId, out var row) || row.RowId == 0 || row.Name.IsEmpty)
            {
                Plugin.Log.Warning($"Quest ID {questId} not found or invalid.");
                continue;
            }

            var description = row.TodoParams.Count > 0 ? SafeToString(() => row.TodoParams[0].ToString()) : "";
            var name = row.Name.ToString();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var startNpc = GetNpcName(row.IssuerStart.RowId, npcSheet);
            var endNpc = GetNpcName(row.TargetEnd.RowId, npcSheet);
            var genre = GetNameSafe(row.JournalGenre.RowId, journalSheet);

            var qData = questData.FirstOrDefault(x => x.id == questId);

            questInfos.Add(new QuestInfo
            {
                Id = questId,
                Name = name,
                JournalGenre = genre,
                StartNpc = startNpc,
                EndNpc = endNpc,
                Sequence = qData.sequence,
                Objectives = qData.objectives
            });
        }

        return questInfos;
    }

    private static string GetNpcName(uint rowId, ExcelSheet<ENpcResident> sheet)
    {
        if (rowId == 0)
            return "";

        var npc = sheet.GetRow(rowId);
        return npc.RowId != 0 && !npc.Singular.IsEmpty ? npc.Singular.ToString() : "";
    }

    private static string GetNameSafe(uint rowId, ExcelSheet<JournalGenre>? sheet)
    {
        if (sheet == null)
            return "";

        var row = sheet.GetRow(rowId);
        return row.RowId != 0 && !row.Name.IsEmpty ? row.Name.ToString() : "";
    }

    private static string SafeToString(Func<string> func)
    {
        try { return func(); }
        catch (Exception ex)
        {
            Plugin.Log.Warning($"Exception during string conversion: {ex.Message}");
            return "";
        }
    }
}

public class QuestInfo
{
    public uint Id { get; set; }
    public string Name { get; set; } = "";
    public string JournalGenre { get; set; } = "";
    public string StartNpc { get; set; } = "";
    public string EndNpc { get; set; } = "";
    public List<string> Sequence { get; set; }
    public List<string> Objectives { get; set; }
}

public class QuestData
{
    public int id { get; set; }
    public List<string> sequence { get; set; } = new();
    public List<string> objectives { get; set; } = new();
}
