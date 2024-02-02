using Celeste64.TAS.Util;
using ImGuiNET;

namespace Celeste64.TAS;

public static class InfoHUD
{
    public static void RenderGUI()
    {
        ImGui.Begin("InfoHUD");

        const int Decimals = 3;

        if (Game.Scene is World world)
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

                ImGui.Text(string.Join("  ", statues));

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
