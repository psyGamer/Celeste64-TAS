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
}
