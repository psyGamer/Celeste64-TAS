namespace Celeste64.TAS.Input.Commands;

public class LoadCommand
{
    [Command("Load", LegalInFullGame = false)]
    private static void Load(string[] args)
    {
        var entry = new World.EntryInfo()
        {
            Map = args.Length <= 0 ? "1" : args[0],
            CheckPoint = args.Length <= 1 ? "Start" : args[1],
            Submap = false, // TODO
            Reason = World.EntryReasons.Entered,
        } ;

        Game.Instance.Goto(new Transition()
        {
            Mode = Transition.Modes.Replace,
            Scene = () => new World(entry),
            ToPause = true,
            ToBlack = new AngledWipe(),
            PerformAssetReload = true
        });
    }
}
