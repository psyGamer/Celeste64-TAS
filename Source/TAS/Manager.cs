using Celeste64.TAS.Input;
using Celeste64.TAS.Util;

namespace Celeste64.TAS;

[AttributeUsage(AttributeTargets.Method)]
internal class EnableRunAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
internal class DisableRunAttribute : Attribute;

public static class Manager
{
	public static World? world;

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

		switch (CurrState)
		{
			case State.Running:
				if (TASControls.PauseResume.Pressed)
					NextState = State.Paused;
				else
					NextState = TASControls.FastForward.Down ? State.FastForward : State.Running;
				break;
			case State.FastForward:
				NextState = TASControls.FastForward.Down ? State.FastForward : State.Running;
				//Reset Camera to Dependent
				Save.Instance.Freecam = Save.FreecamMode.Disabled;
				break;
			case State.FrameAdvance:
				NextState = State.Paused;
				//Reset Camera to Dependent
				Save.Instance.Freecam = Save.FreecamMode.Disabled;
				break;
			case State.Paused:
				if (TASControls.PauseResume.Pressed)
				{
					NextState = State.Running;
					//Reset Camera to Dependent
					Save.Instance.Freecam = Save.FreecamMode.Disabled;
				}
				else if (TASControls.SlowForward.Down || TASControls.FrameAdvance.Pressed | TASControls.FrameAdvance.Repeated)
				{
					NextState = State.FrameAdvance;
					//Reset Camera to Dependent
					Save.Instance.Freecam = Save.FreecamMode.Disabled;
				}
				//Reset Camera to Dependent
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
