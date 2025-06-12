using Dalamud.Plugin.Services;
using ImGuiNET;
using System;
using System.Numerics;

namespace SamplePlugin;

public static class ChatBubbleManager
{
    private static string? chatBubbleMessage;
    private static DateTime expirationTime;

    public static void Bubble(string message)
    {
        chatBubbleMessage = message;
        var duration = message.Length * 0.1008f; // ≈0.1 s per char
        expirationTime = DateTime.UtcNow.AddSeconds(duration);
    }

    public static void Bubble(string message, float timeInSeconds)
    {
        chatBubbleMessage = message;
        expirationTime = DateTime.UtcNow.AddSeconds(timeInSeconds);
    }

    public static void DrawBubble(IClientState clientState, IGameGui gameGui)
    {
        if (string.IsNullOrEmpty(chatBubbleMessage) || DateTime.UtcNow > expirationTime)
            return;

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
}
