using Celeste64.TAS.Input;
using Celeste64.TAS.Util;

namespace Celeste64.TAS;

[AttributeUsage(AttributeTargets.Method)]
internal class EnableRunAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
internal class DisableRunAttribute : Attribute;

public class Manager
{
    public static bool Running;
    public static readonly InputController Controller = new();

    static Manager() {
        AttributeUtils.CollectMethods<EnableRunAttribute>();
        AttributeUtils.CollectMethods<DisableRunAttribute>();
    }

    public static void EnableRun() {
        Log.Info($"Starting TAS: {InputController.TasFilePath}");

        Running = true;
        AttributeUtils.Invoke<EnableRunAttribute>();
        Controller.Stop();
        Controller.RefreshInputs();
    }

    public static void DisableRun() {
        Log.Info("Stopping TAS");

        Running = false;
        AttributeUtils.Invoke<DisableRunAttribute>();
        Controller.Stop();
    }
}
