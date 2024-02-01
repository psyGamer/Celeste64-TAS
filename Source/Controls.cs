
using Celeste64.TAS;
using Celeste64.TAS.Input;

namespace Celeste64;

public static class Controls
{
	public static VirtualStick Move = new("Move", VirtualAxis.Overlaps.TakeNewer, 0.35f);
	public static VirtualStick Menu = new("Menu", VirtualAxis.Overlaps.TakeNewer, 0.35f);
	public static VirtualStick Camera = new("Camera", VirtualAxis.Overlaps.TakeNewer, 0.35f);
	public static VirtualButton Jump = new("Jump", .1f);
	public static VirtualButton Dash = new("Dash", .1f);
	public static VirtualButton Climb = new("Climb");

	public static VirtualButton Confirm = new("Confirm");
	public static VirtualButton Cancel = new("Cancel");
	public static VirtualButton Pause = new("Pause");

    public static VirtualButton Freecam = new("Freecam");
    public static VirtualButton SimplifiedGraphics = new("SimplifedGraphics");


	public static void Load()
	{
		Move.Clear();
		Move.AddLeftJoystick(0);
		Move.AddDPad(0);
		// Move.AddArrowKeys();
        Move.Add(Keys.A, Keys.D, Keys.W, Keys.S);

		Camera.Clear();
		Camera.AddRightJoystick(0, 0.50f, 0.70f);
		// Camera.Add(Keys.A, Keys.D, Keys.W, Keys.S);
        Camera.AddArrowKeys();

		Jump.Clear();
		Jump.Add(0, Buttons.A, Buttons.Y);
		Jump.Add(Keys.C);

		Dash.Clear();
		Dash.Add(0, Buttons.X, Buttons.B);
		Dash.Add(Keys.X);

		Climb.Clear();
		Climb.Add(0, Buttons.LeftShoulder, Buttons.RightShoulder);
		Climb.Add(0, Axes.RightTrigger, 1, .4f);
		Climb.Add(0, Axes.LeftTrigger, 1, .4f);
		Climb.Add(Keys.Z, Keys.V, Keys.LeftShift, Keys.RightShift);

		Menu.Clear();
		Menu.AddLeftJoystick(0, 0.50f, 0.50f);
		Menu.AddDPad(0);
		Menu.AddArrowKeys();

		Confirm.Clear();
		Confirm.Add(0, Buttons.A);
		Confirm.Add(0, Keys.C);

		Cancel.Clear();
		Cancel.Add(0, Buttons.B);
		Cancel.Add(0, Keys.X);

		Pause.Clear();
		Pause.Add(0, Buttons.Start, Buttons.Select, Buttons.Back);
		Pause.Add(0, Keys.Enter, Keys.Escape);

        Freecam.Clear();
        Freecam.Add(Keys.M);

        SimplifiedGraphics.Clear();
        SimplifiedGraphics.Add(Keys.N);

        TASControls.Load();
        InputHelper.Load();
	}

	public static void Consume()
	{
		Move.Consume();
		Menu.Consume();
		Camera.Consume();
		Jump.Consume();
		Dash.Consume();
		Climb.Consume();
		Confirm.Consume();
		Cancel.Consume();
		Pause.Consume();

	}

	private static readonly Dictionary<Gamepads, Dictionary<string, string>> prompts = [];

	private static string GetControllerName(Gamepads pad) => pad switch
    {
        Gamepads.DualShock4 => "PlayStation 4",
        Gamepads.DualSense => "PlayStation 5",
        Gamepads.Nintendo => "Nintendo Switch",
        Gamepads.Xbox => "Xbox Series",
        _ => "Xbox Series",
    };

	private static string GetPromptLocation(string name)
	{
	        var gamepadPure = Input.Controllers[0];
	        var gamepad = gamepadPure.Gamepad;

		if (!prompts.TryGetValue(gamepad, out var list))
			prompts[gamepad] = list = new();

		if (!list.TryGetValue(name, out var lookup))
	            list[name] = lookup = !gamepadPure.Connected
	                    ? $"Controls/PC/{name}"
	                    : $"Controls/{GetControllerName(gamepad)}/{name}";

		return lookup;
	}

	public static string GetPromptLocation(VirtualButton button)
	{
		// TODO: instead, query the button's actual bindings and look up a
		// texture based on that! no time tho
		if (button == Confirm)
			return GetPromptLocation("confirm");
		else
			return GetPromptLocation("cancel");
	}

	public static Subtexture GetPrompt(VirtualButton button)
	{
		return Assets.Subtextures.GetValueOrDefault(GetPromptLocation(button));
	}
}
