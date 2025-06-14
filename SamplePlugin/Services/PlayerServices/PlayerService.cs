using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Dalamud.Plugin.Services;
using static System.Net.Mime.MediaTypeNames;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Text.Json;
using FinalFantasyAIPlugin.Services.QuestServices;

namespace FinalFantasyAIPlugin.Services.PlayerService
{
    internal unsafe static class PlayerService
    {
        public static PlayerInfo PlayerInfo = new();
        public static Action<PlayerInfo> OnPlayerInfoUpdated;
        private static TimeSpan remainingTime = TimeSpan.Zero;
        private static readonly TimeSpan updateInterval = TimeSpan.FromSeconds(5);

        public static void Update(bool forced = false)
        {
            if (!Plugin.ClientState.IsLoggedIn)
                return;

            if (!UpdateIntervalReady(Plugin.Framework.UpdateDelta, forced))
                return;

            var player = Plugin.ClientState.LocalPlayer;
            if (player != null)
            {
                PlayerInfo.SetName(player.Name.ToString());
                PlayerInfo.SetLevel(player.Level.ToString());
                PlayerInfo.SetClassJob(player.ClassJob.Value.Name.ToString() ?? "");
            }

            try
            {
                var territoryInfo = TerritoryInfo.Instance();
                if (territoryInfo != null)
                {
                    var area = Plugin.DataManager.GetExcelSheet<PlaceName>()?.GetRow(territoryInfo->AreaPlaceNameId).Name.ToString();
                    var subarea = Plugin.DataManager.GetExcelSheet<PlaceName>()?.GetRow(territoryInfo->SubAreaPlaceNameId).Name.ToString();
                    PlayerInfo.SetPlace(area ?? "");
                    PlayerInfo.SetSubPlace(subarea ?? "");
                }

                var territory = Plugin.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(Plugin.ClientState.TerritoryType);
                PlayerInfo.SetRegion(territory?.PlaceNameRegion.Value.Name.ToString() ?? "");
                PlayerInfo.SetZone(territory?.PlaceName.Value.Name.ToString() ?? "");
                PlayerInfo.SetPlaceZone(territory?.PlaceNameZone.Value.Name.ToString() ?? "");

                var weatherInfo = WeatherManager.Instance();
                if (weatherInfo != null)
                {
                    int weatherId = weatherInfo->GetCurrentWeather();
                    var weatherRow = Plugin.DataManager.GetExcelSheet<Weather>()?.GetRow((uint)weatherId);
                    PlayerInfo.SetWeather(weatherRow?.Name.ToString() ?? "");
                }
            }
            catch (Exception ex)
            {
                Plugin.ChatGui.Print($"[PlayerService] Error: " + ex.Message);
            }

            OnPlayerInfoUpdated?.Invoke(PlayerInfo);
        }

        private static bool UpdateIntervalReady(TimeSpan deltaTime, bool forced = false)
        {
            remainingTime -= deltaTime;
            if (!forced && remainingTime > TimeSpan.Zero)
                return false;

            remainingTime = updateInterval;
            return true;
        }
    }

    public class PlayerInfo
    {
        public string Name { get; private set; }
        public Action<string> OnNameChanged;
        public void SetName(string name, bool notify = true)
        {
            if (string.Equals(name, Name)) return;
            Name = name;
            if (notify) OnNameChanged?.Invoke(Name);
        }

        public string Level { get; private set; }
        public Action<string> OnLevelChanged;
        public void SetLevel(string level, bool notify = true)
        {
            if (string.Equals(level, Level)) return;
            Level = level;
            if (notify) OnLevelChanged?.Invoke(Level);
        }

        public string ClassJob { get; private set; }
        public Action<string> OnClassJobChanged;
        public void SetClassJob(string job, bool notify = true)
        {
            if (string.Equals(job, ClassJob)) return;
            ClassJob = job;
            if (notify) OnClassJobChanged?.Invoke(ClassJob);
        }

        public string Region { get; private set; }
        public Action<string> OnRegionChanged;
        public void SetRegion(string region, bool notify = true)
        {
            if (string.Equals(region, Region)) return;
            Region = region;
            if (notify) OnRegionChanged?.Invoke(Region);
        }

        public string Zone { get; private set; }
        public Action<string> OnZoneChanged;
        public void SetZone(string zone, bool notify = true)
        {
            if (string.Equals(zone, Zone)) return;
            Zone = zone;
            if (notify) OnZoneChanged?.Invoke(Zone);
        }

        public string PlaceZone { get; private set; }
        public Action<string> OnPlaceZoneChanged;
        public void SetPlaceZone(string placeZone, bool notify = true)
        {
            if (string.Equals(placeZone, PlaceZone)) return;
            PlaceZone = placeZone;
            if (notify) OnPlaceZoneChanged?.Invoke(PlaceZone);
        }


        public string Place { get; private set; }
        public Action<string> OnPlaceChanged;
        public void SetPlace(string place, bool notify = true)
        {
            if (string.Equals(place, Place)) return;
            Place = place;
            if (notify)
            {
                OnPlaceChanged?.Invoke(Place);
                Plugin.ChatGui.Print("Subplace Modified : " + Place);
            }
        }

        public string SubPlace { get; private set; }
        public Action<string> OnSubPlaceChanged;
        public void SetSubPlace(string subPlace, bool notify = true)
        {
            if (string.Equals(subPlace, SubPlace)) return;
            SubPlace = subPlace;
            if (notify)
            {
                OnSubPlaceChanged?.Invoke(SubPlace);
                Plugin.ChatGui.Print("Subplace Modified : " + SubPlace);
            }
        }

        public string Weather { get; private set; }
        public Action<string> OnWeatherChanged;
        public void SetWeather(string weather, bool notify = true)
        {
            if (string.Equals(weather, Weather)) return;
            Weather = weather;
            if (notify) OnWeatherChanged?.Invoke(Weather);
        }
    }
}
