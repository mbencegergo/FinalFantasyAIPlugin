using Dalamud.Plugin.Services;
using ImGuiNET;
using System;
using System.Numerics;
using System.Text;

namespace FinalFantasyAIPlugin;

public static class ChatBubbleManager
{
    private static string? chatBubbleMessage;
    private static DateTime expirationTime;
    private static bool log;

    public static void Bubble(string message, bool log = true)
    {
        chatBubbleMessage = WrapText(message, 45); // wrap to 30 characters per line
        var duration = message.Length * 0.0504f; // ≈0.1 s per char
        expirationTime = DateTime.UtcNow.AddSeconds(duration);
        ChatBubbleManager.log = log;
    }

    public static void Bubble(string message, float timeInSeconds, bool log = true)
    {
        chatBubbleMessage = WrapText(message, 45); // wrap to 30 characters per line
        expirationTime = DateTime.UtcNow.AddSeconds(timeInSeconds);
        ChatBubbleManager.log = log;
    }

    public static void DrawBubble(IClientState clientState, IGameGui gameGui, IChatGui chatGui)
    {
        if (!clientState.IsLoggedIn)
            return;

        if (string.IsNullOrEmpty(chatBubbleMessage) || DateTime.UtcNow > expirationTime)
            return;

        if (log)
        {
            log = false;
            chatGui.Print("AI: " + chatBubbleMessage);
        }

        var player = clientState.LocalPlayer;
        if (player == null)
            return;

        var worldPos = player.Position + new Vector3(0, 1.8f, 0);
        if (!gameGui.WorldToScreen(worldPos, out var screenPos))
            return;

        ImGui.SetNextWindowPos(screenPos, ImGuiCond.Always, new Vector2(0.5f, 1.0f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0, 0, 0, 0.6f));
        ImGui.Begin("##ChatBubble", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize |
                                        ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav |
                                        ImGuiWindowFlags.NoInputs);
        ImGui.TextUnformatted(chatBubbleMessage);
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
    }

    private static string WrapText(string text, int maxCharsPerLine)
    {
        var words = text.Split(' ');
        var sb = new StringBuilder();
        var line = "";
        foreach (var w in words)
        {
            if ((line + w).Length > maxCharsPerLine)
            {
                sb.AppendLine(line.TrimEnd());
                line = w + " ";
            }
            else
            {
                line += w + " ";
            }
        }
        if (line.Length > 0) sb.Append(line.TrimEnd());
        return sb.ToString();
    }
}
