namespace Celeste64.TAS.Input.Commands;

public class LoadCommand
{
    [Command("Load", LegalInFullGame = false)]
    private static void LoadCmd(string[] args)
    {
        var entry = new World.EntryInfo()
        {
            Map = args.Length <= 0 ? "1" : args[0],
            CheckPoint = args.Length <= 1 ? "Start" : args[1],
            Submap = false, // TODO
            Reason = World.EntryReasons.Entered,
        };

        // We need to change the current scene to the main menu,
        // since otherwise an input on the first frame would get consumed by the previous scene.
        if (Game.Instance.scenes.TryPop(out _))
        {
            Game.Instance.scenes.Push(new Overworld(true));
        }

        Game.Instance.Goto(new Transition()
        {
            Mode = Transition.Modes.Replace,
            Scene = () => new World(entry),
            ToBlack = null,
            FromBlack = new SpotlightWipe(),
        });
    }

    [Command("LoadStrawberry", LegalInFullGame = false)]
    private static void LoadStrawbCmd(string[] args)
    {
        if (args.Length != 3)
        {
            Manager.AbortTas("Level, Strawberry IDs and Facing direction are required for the LoadStrawberry command");
            return;
        }

        if (!int.TryParse(args[1], out int strawbIdx))
        {
            Manager.AbortTas($"Couldn't parse strawberry ID '{args[1]}'");
            return;
        }
        string strawbID = $"{args[0]}/{strawbIdx}";

        if (!float.TryParse(args[2], out float targetFacing))
        {
            Manager.AbortTas($"Couldn't parse facing direction '{args[3]}'");
            return;
        }

        var entry = new World.EntryInfo()
        {
            Map = args[0],
            CheckPoint = "Start", // Doesnt matter anyway
            Submap = false, // TODO
            Reason = World.EntryReasons.Entered,
        };

        // We need to change the current scene to the main menu,
        // since otherwise an input on the first frame would get consumed by the previous scene.
        if (Game.Instance.scenes.TryPop(out _))
        {
            Game.Instance.scenes.Push(new Overworld(true));
        }

        Game.Instance.Goto(new Transition()
        {
            Mode = Transition.Modes.Replace,
            Scene = () =>
            {
                var world = new World(entry);

                // We need to search through 'adding', since they'll only get properly added on the first update call.
                if (!world.adding.TryFirst(actor => actor is Player, out var playerActor) || playerActor is not Player player)
                {
                    Manager.AbortTas($"Couldn't find player");
                    return world;
                }
                if (!world.adding.TryFirst(actor => actor is Strawberry s && s.ID == strawbID, out var strawb))
                {
                    Manager.AbortTas($"Couldn't parse strawberry ID '{strawbID}'");
                    return world;
                }

                player.Position = strawb.Position + Vec3.UnitZ * -3;
                player.Facing = player.targetFacing = new Vec2(MathF.Sin(targetFacing * Calc.DegToRad), -MathF.Cos(targetFacing * Calc.DegToRad));
                Manager.TASLevelRecord.Strawberries.Add(strawbID);

                return world;
            },
            ToBlack = null,
            FromBlack = new SpotlightWipe(),
        });
    }
}
