using FFXIVClientStructs.FFXIV.Client.Game;
using System.Collections.Generic;

public static class QuestMemoryReader
{
    public static List<uint> GetActiveQuestIds()
    {
        var questIds = new List<uint>();

        unsafe
        {
            var questManager = QuestManager.Instance();
            if (questManager == null)
                return questIds;

            var quests = questManager->NormalQuests;
            foreach (var quest in quests)
            {
                if (quest.QuestId != 0)
                    questIds.Add(quest.QuestId);
            }
        }

        return questIds;
    }
}
