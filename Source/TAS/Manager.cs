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
        FastForward,
    }

    public static bool Running => CurrState != State.Disabled;
    public static int FrameLoops => CurrState == State.FastForward ? 3 : 1;

    public static State CurrState, NextState;

    public static readonly InputController Controller = new();

    internal static Save.LevelRecord TASLevelRecord = new();

    public const float FreecamRotateSpeed = 0.5f;
    public const float FreecamZoomSpeed = 5.0f;
    public const float FreecamMoveSpeed = 3.0f;

    public static Vec3 FreecamPosition;
    public static Vec2 FreecamRotation;
    public static float FreecamDistance = 50f;

    public static Vec2 MouseDelta => Foster.Framework.Input.Mouse.Position - Foster.Framework.Input.LastState.Mouse.Position;

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

        TASLevelRecord.ID = string.Empty;
    }

    public static void DisableRun()
    {
        Log.Info("Stopping TAS");

        CurrState = State.Disabled;
        NextState = State.Disabled;
        AttributeUtils.Invoke<DisableRunAttribute>();
        Controller.Stop();
    }

    public static void Update()
    {
        CurrState = NextState;
        if (!Running) return;

        if (Save.Instance.LevelID != TASLevelRecord.ID)
            TASLevelRecord = new Save.LevelRecord { ID = Save.Instance.LevelID };

        if (!IsPaused())
        {
            Controller.AdvanceFrame(out bool canPlayback);

            if (!canPlayback)
            {
                DisableRun();
            }
        }

        switch (CurrState)
        {
        case State.Running:
            if (TASControls.PauseResume.ConsumePress())
                NextState = State.Paused;
            else
                NextState = TASControls.FastForward.Down ? State.FastForward : State.Running;
            break;
        case State.FastForward:
            NextState = TASControls.FastForward.Down ? State.FastForward : State.Running;
            break;
        case State.FrameAdvance:
            NextState = State.Paused;
            break;
        case State.Paused:
            if (TASControls.PauseResume.ConsumePress())
                NextState = State.Running;
            else if (TASControls.SlowForward.Down || TASControls.FrameAdvance.ConsumePress() || TASControls.FrameAdvance.Repeated)
                NextState = State.FrameAdvance;

            break;
        case State.Disabled:
        default:
            break;
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
