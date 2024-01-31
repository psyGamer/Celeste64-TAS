using Celeste64.TAS.Util;

namespace Celeste64.TAS.Input.Commands;

public class CameraModeCommand
{
    public enum CameraMode
    {
        Dependent, Independent
    }

    public static CameraMode Mode { get; private set; } = CameraMode.Independent;

    [Command("CameraMode")]
    private static void ModeCmd(string[] args, int _, string __, int line)
    {
        if (args.IsEmpty() || !Enum.TryParse(args[0], true, out CameraMode mode)) {
            Manager.AbortTas($"CameraMode command failed at line {line}\nMode must be Dependent or Independent");
            return;
        }

        Mode = mode;
    }
}
