using Celeste64.TAS.Input;
using Celeste64.TAS.Util;

namespace Celeste64.TAS;

[AttributeUsage(AttributeTargets.Method)]
internal class EnableRunAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
internal class DisableRunAttribute : Attribute;

public static class Manager
{
    public enum State
    {
        Disabled, Enabled, FrameStepping,
    }

    public static bool Running => CurrState is State.Enabled or State.FrameStepping;
    private static State CurrState;
    public static bool FrameStepNextFrame = false; // Flag to frame advance 1 frame

    public static readonly InputController Controller = new();

    static Manager()
    {
        AttributeUtils.CollectMethods<EnableRunAttribute>();
        AttributeUtils.CollectMethods<DisableRunAttribute>();
    }

    public static void EnableRun()
    {
        Log.Info($"Starting TAS: {InputController.TasFilePath}");

        CurrState = State.Enabled;
        AttributeUtils.Invoke<EnableRunAttribute>();
        Controller.Stop();
        Controller.Clear();
        Controller.RefreshInputs();

        // Controller.Inputs.ForEach(i => Log.Info($"input {i}"));
        // Controller.Commands.ForEach(p => Log.Info($"command {p.Value}@{p.Key}"));
    }

    public static void DisableRun()
    {
        Log.Info("Stopping TAS");

        CurrState = State.Disabled;
        FrameStepNextFrame = false;
        AttributeUtils.Invoke<DisableRunAttribute>();
        Controller.Stop();
    }

    public static void Update()
    {
        if (!Running) return;

        if (!IsPaused())
        {
            Controller.AdvanceFrame(out bool canPlayback);
            Log.Info($"Current Frame ({Controller.CurrentFrameInTas}/{Controller.Inputs.Count}): {Controller.Current} ~~ {CurrState}");
            if (!canPlayback)
            {
                DisableRun();
            }
        }

        if (TASControls.PauseResume.Pressed)
        {
            if (CurrState == State.Enabled)
                CurrState = State.FrameStepping;
            else if (CurrState == State.FrameStepping)
                CurrState = State.Enabled;
        }

        FrameStepNextFrame = false;
        if (TASControls.FrameAdvance.Pressed | TASControls.FrameAdvance.Repeated)
        {
            // Will be reset above on the next frame
            FrameStepNextFrame = true;
        }
    }

    public static void AbortTas(string message)
    {
        Log.Error(message);
        DisableRun();
    }

    public static bool IsLoading()
    {
        return !(Game.Instance.transitionStep == Game.TransitionStep.FadeIn ||
                 Game.Instance.transitionStep == Game.TransitionStep.None);
    }

    public static bool IsPaused()
    {
        if (CurrState != State.FrameStepping || IsLoading()) return false;

        if (FrameStepNextFrame)
            return false;

        return true;
    }
}
