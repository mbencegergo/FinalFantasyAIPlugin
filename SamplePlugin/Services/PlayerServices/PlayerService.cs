using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using System;

namespace FinalFantasyAIPlugin.Services.PlayerService
{
    internal static class PlayerService
    {
        private static TimeSpan timeSinceLastUpdate = TimeSpan.Zero;
        private static readonly TimeSpan updateInterval = TimeSpan.FromSeconds(5);
        public static void Update()
        {
            if (!Plugin.ClientState.IsLoggedIn)
                return;

            timeSinceLastUpdate += Plugin.Framework.UpdateDelta;
            if (timeSinceLastUpdate < updateInterval)
                return;
            timeSinceLastUpdate = TimeSpan.Zero;

            var player = Plugin.ClientState.LocalPlayer;
            if (player != null)
            {
                Plugin.ChatGui.Print($"Name      : {player.Name}");
                Plugin.ChatGui.Print($"ClassJob  : {player.ClassJob.Value.Name}");
            }

            var territory = Plugin.DataManager
                .GetExcelSheet<TerritoryType>()?
                .GetRow(Plugin.ClientState.TerritoryType);
            var zoneName = territory?.PlaceName.Value.Name.ToString() ?? "Unknown";
            var subName = territory?.PlaceNameZone.Value.Name.ToString() ?? "Unknown";
            Plugin.ChatGui.Print($"Zone         : {zoneName}");
            Plugin.ChatGui.Print($"PlaceNameZone: {subName}");
            var region = territory?.PlaceNameRegion.Value.Name.ToString() ?? "Unknown";
            Plugin.ChatGui.Print($"Region       : {region}");

            unsafe
            {
                var weatherInfo = WeatherManager.Instance();
                int weatherId = weatherInfo != null ? weatherInfo->GetCurrentWeather() : 0;
                var weatherRow = Plugin.DataManager.GetExcelSheet<Weather>()?.GetRow((uint)weatherId);
                var weatherName = weatherRow?.Name.ToString() ?? "Unknown";
                Plugin.ChatGui.Print($"Weather   : {weatherName}");
            }
        }
    }
}
