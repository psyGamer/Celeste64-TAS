using Celeste64.TAS.Input;
using Celeste64.TAS.Util;
using ImGuiNET;

namespace Celeste64.TAS;

public static class InfoHUD
{
    public static void RenderGUI()
    {
        ImGui.SetNextWindowSizeConstraints(new Vec2(200f, 200f), new Vec2(float.PositiveInfinity, float.PositiveInfinity));
        ImGui.Begin("Info HUD", ImGuiWindowFlags.MenuBar);

        const int Decimals = 3;

        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("Settings"))
            {
                bool showInputs = Save.Instance.InfoHudShowInputs;
                bool showWorld = Save.Instance.InfoHudShowWorld;

                ImGui.Checkbox("Show Input Display", ref showInputs);
                ImGui.Checkbox("Show World Information", ref showWorld);

                Save.Instance.InfoHudShowInputs = showInputs;
                Save.Instance.InfoHudShowWorld = showWorld;
                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }

        if (Save.Instance.InfoHudShowInputs)
        {
            var controller = Manager.Controller;
            var inputs = controller.Inputs;
            if (Manager.Running && controller.CurrentFrameInTas >= 0 && controller.CurrentFrameInTas < inputs.Count)
            {
                InputFrame? current = controller.Current;
                if (controller.CurrentFrameInTas >= 1 && current != controller.Previous) {
                    current = controller.Previous;
                }

                var previous = current!.Prev;
                var next = current.Next;

                int maxLine = Math.Max(current.Line, Math.Max(previous?.Line ?? 0, next?.Line ?? 0)) + 1;
                int linePadLeft = maxLine.ToString().Length;

                int maxFrames = Math.Max(current.Frames, Math.Max(previous?.Frames ?? 0, next?.Frames ?? 0));
                int framesPadLeft = maxFrames.ToString().Length;

                string FormatInputFrame(InputFrame inputFrame) => $"{(inputFrame.Line + 1).ToString().PadLeft(linePadLeft)}: {string.Empty.PadLeft(framesPadLeft - inputFrame.Frames.ToString().Length)}{inputFrame}";

                if (previous != null) ImGui.Text(FormatInputFrame(previous));

                string currentStr = FormatInputFrame(current);
                int currentFrameLength = controller.CurrentFrameInInput.ToString().Length;
                int inputWidth = currentStr.Length + currentFrameLength + 2;
                inputWidth = Math.Max(inputWidth, 20);
                ImGui.Text( $"{currentStr.PadRight(inputWidth - currentFrameLength)}{controller.CurrentFrameInInputForHud}");

                if (next != null) ImGui.Text(FormatInputFrame(next));

                ImGui.Text(string.Empty);
            }
        }

        if (Save.Instance.InfoHudShowWorld && Game.Scene is World world)
        {
            var player = world.Get<Player>();
            if (player != null)
            {
                ImGui.Text($"Pos: {player.Position.X.ToFormattedString(Decimals)} {player.Position.Y.ToFormattedString(Decimals)} {player.Position.Z.ToFormattedString(Decimals)}");
                ImGui.Text($"Vel: {player.Velocity.X.ToFormattedString(Decimals)} {player.Velocity.Y.ToFormattedString(Decimals)} {player.Velocity.Z.ToFormattedString(Decimals)}");
                ImGui.Text(string.Empty);

                List<string> statues = new();
                if (player.tCoyote > 0)
                    statues.Add($"Coyote({player.tCoyote.ToFrames()})@{player.coyoteZ.ToFormattedString(Decimals)}");
                if (player.tClimbCooldown > 0)
                    statues.Add($"ClimbCD({player.tClimbCooldown.ToFrames()})");
                if (player.tDashCooldown > 0)
                    statues.Add($"DashCD({player.tDashCooldown.ToFrames()})");
                if (player.tNoDashJump > 0)
                    statues.Add($"DashJumpCD({player.tNoDashJump.ToFrames()})");
                if (player.TryClimb())
                    statues.Add($"CanClimb");
                if (world.SolidWallCheckClosestToNormal(player.SolidWaistTestPos, Player.ClimbCheckDist, -new Vec3(player.targetFacing, 0), out _))
                    statues.Add($"CanWallJump");
                statues.Add($"St{player.stateMachine.State?.ToString() ?? string.Empty}");

                ImGui.TextWrapped(string.Join("  ", statues));

                string timerStr = (int) Save.CurrentRecord.Time.TotalHours > 0
                    ? $"{((int) Save.CurrentRecord.Time.TotalHours):00}:{Save.CurrentRecord.Time.Minutes:00}:{Save.CurrentRecord.Time.Seconds:00}:{Save.CurrentRecord.Time.Milliseconds:000}"
                    : $"{Save.CurrentRecord.Time.Minutes:00}:{Save.CurrentRecord.Time.Seconds:00}:{Save.CurrentRecord.Time.Milliseconds:000}";
                ImGui.Text($"[{Save.Instance.LevelID}] Timer: {timerStr}");
            }
        }

        ImGui.End();
    }

    private static int ToFrames(this float seconds)
    {
        return (int) Math.Ceiling(seconds / Time.Delta);
    }
}
