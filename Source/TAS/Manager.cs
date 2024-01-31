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
        Controller.Clear();
        Controller.RefreshInputs();

        // Controller.Inputs.ForEach(i => Log.Info($"input {i}"));
        // Controller.Commands.ForEach(p => Log.Info($"command {p.Value}@{p.Key}"));
    }

    public static void DisableRun() {
        Log.Info("Stopping TAS");

        Running = false;
        AttributeUtils.Invoke<DisableRunAttribute>();
        Controller.Stop();
    }

    public static void Update()
    {
        if (Running)
        {
            Controller.AdvanceFrame(out bool canPlayback);
            Log.Info($"Current Frame ({Controller.CurrentFrameInTas}/{Controller.Inputs.Count}): {Controller.Current}");
            Log.Info(Game.Instance.transitionStep);
            if (!canPlayback) {
                DisableRun();
            }
        }
    }

    public static void AbortTas(string message) {
        Log.Error(message);
        DisableRun();
    }

    public static bool IsLoading()
    {
        return !(Game.Instance.transitionStep == Game.TransitionStep.FadeIn ||
                 Game.Instance.transitionStep == Game.TransitionStep.None);
    }
}
