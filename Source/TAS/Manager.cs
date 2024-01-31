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
        Disabled,
        Running, Paused, FrameAdvance,
    }

    public static bool Running => CurrState != State.Disabled;
    private static State CurrState, NextState;
    private static bool FrameStepNextFrame = false; // Flag to frame advance 1 frame

    public static readonly InputController Controller = new();

    static Manager()
    {
        AttributeUtils.CollectMethods<EnableRunAttribute>();
        AttributeUtils.CollectMethods<DisableRunAttribute>();
    }

    public static void EnableRun()
    {
        Log.Info($"Starting TAS: {InputController.TasFilePath}");

        CurrState = State.Running;
        NextState = State.Running;
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
        NextState = State.Disabled;
        FrameStepNextFrame = false;
        AttributeUtils.Invoke<DisableRunAttribute>();
        Controller.Stop();
    }

    public static void Update()
    {
        CurrState = NextState;

        if (!Running) return;

        if (!IsPaused())
        {
            Controller.AdvanceFrame(out bool canPlayback);
            if (!canPlayback)
            {
                DisableRun();
            }
        }

        if (TASControls.PauseResume.Pressed)
        {
            if (CurrState == State.Running)
                NextState = State.Paused;
            else if (CurrState == State.Paused)
                NextState = State.Running;
        }

        if (CurrState == State.FrameAdvance)
        {
            NextState = State.Paused;
        }
        else if (TASControls.FrameAdvance.Pressed | TASControls.FrameAdvance.Repeated)
        {
            NextState = State.FrameAdvance;
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
        if (IsLoading()) return false;
        return CurrState == State.Paused;
    }
}
