using Celeste64.TAS.StudioCommunication;
using System.Reflection;

namespace Celeste64.TAS.Input;

public static class InputHelper
{
    private static readonly Dictionary<string, VirtualStick> originalSticks = new();
    private static readonly Dictionary<string, VirtualButton> originalButtons = new();
    private static readonly List<TASButtonBinding> allButtonBindings = new();

    // Duplicates of the controls, for the TAS
    private static readonly VirtualStick Move = new("Move", VirtualAxis.Overlaps.TakeNewer, 0.35f);
    private static readonly VirtualStick Menu = new("Menu", VirtualAxis.Overlaps.TakeNewer, 0.35f);
    private static readonly VirtualStick Camera = new("Camera", VirtualAxis.Overlaps.TakeNewer, 0.35f);
    private static readonly VirtualButton Jump = new("Jump", .1f);
    private static readonly VirtualButton Dash = new("Dash", .1f);
    private static readonly VirtualButton Climb = new("Climb");

    private static readonly VirtualButton Confirm = new("Confirm");
    private static readonly VirtualButton Cancel = new("Cancel");
    private static readonly VirtualButton Pause = new("Pause");

    public static void Load()
    {
        originalSticks["Move"] = Controls.Move;

        originalButtons["Jump"] = Controls.Jump;
        originalButtons["Dash"] = Controls.Dash;
        originalButtons["Climb"] = Controls.Climb;
        originalButtons["Confirm"] = Controls.Confirm;
        originalButtons["Pause"] = Controls.Pause;

        Move.Clear();
        Move.Horizontal.Positive.Bindings.Add(new TASAxisBinding());
        Move.Horizontal.Negative.Bindings.Add(new TASAxisBinding());
        Move.Vertical.Positive.Bindings.Add(new TASAxisBinding());
        Move.Vertical.Negative.Bindings.Add(new TASAxisBinding());

        Jump.Clear();
        Jump.Bindings.Add(new TASButtonBinding(Actions.Jump));
        Jump.Bindings.Add(new TASButtonBinding(Actions.Jump2));

        Dash.Clear();
        Dash.Bindings.Add(new TASButtonBinding(Actions.Dash));
        Dash.Bindings.Add(new TASButtonBinding(Actions.Dash2));

        Climb.Clear();
        Climb.Bindings.Add(new TASButtonBinding(Actions.Climb));

        Confirm.Clear();
        Confirm.Bindings.Add(new TASButtonBinding(Actions.Confirm));

        Pause.Clear();
        Pause.Bindings.Add(new TASButtonBinding(Actions.Pause));

        allButtonBindings.Clear();
        allButtonBindings.AddRange(Jump.Bindings.Cast<TASButtonBinding>());
        allButtonBindings.AddRange(Dash.Bindings.Cast<TASButtonBinding>());
        allButtonBindings.AddRange(Climb.Bindings.Cast<TASButtonBinding>());
        allButtonBindings.AddRange(Confirm.Bindings.Cast<TASButtonBinding>());
        allButtonBindings.AddRange(Pause.Bindings.Cast<TASButtonBinding>());
    }

    [EnableRun]
    private static void Start()
    {
        // Replace controls with our buttons
        Controls.Move = Move;

        Controls.Jump = Jump;
        Controls.Dash = Dash;
        Controls.Climb = Climb;
        Controls.Confirm = Confirm;
        Controls.Pause = Pause;
    }

    [DisableRun]
    private static void Stop()
    {
        // Reset back to original buttons
        Controls.Move = originalSticks["Move"];

        Controls.Jump = originalButtons["Jump"];
        Controls.Dash = originalButtons["Dash"];
        Controls.Climb = originalButtons["Climb"];
        Controls.Confirm = originalButtons["Confirm"];
        Controls.Pause = originalButtons["Pause"];
    }

    public static void FeedInputs(InputFrame? input)
    {
        // Update binding state
        foreach (var binding in allButtonBindings)
        {
            if (input.Actions.HasFlag(binding.action))
            {
                if (binding is { IsPressed: false, IsDown: false })
                    binding.IsPressed = true; // 1st frame
                else if (binding.IsPressed)
                    binding.IsPressed = false; // 2nd+ frame
                binding.IsReleased = false;
            }
            else
            {
                if (binding is { IsReleased: false, IsDown: true })
                    binding.IsReleased = true; // 1st frame
                else if (binding.IsReleased)
                    binding.IsReleased = false; // 2nd+ frame
                binding.IsPressed = false;
            }

            binding.IsDown = input.Actions.HasFlag(binding.action);
        }

        {
            var left = Move.Horizontal.Positive.Bindings[0] as TASAxisBinding;
            var right = Move.Horizontal.Negative.Bindings[0] as TASAxisBinding;
            var up = Move.Vertical.Negative.Bindings[0] as TASAxisBinding;
            var down = Move.Vertical.Positive.Bindings[0] as TASAxisBinding;

            left!.LastValue = left.Value;
            right!.LastValue = right.Value;
            up!.LastValue = up.Value;
            down!.LastValue = down.Value;

            var moveVector = input.MoveVector;
            left.Value = MathF.Max(0.0f, moveVector.X);
            right.Value = MathF.Max(0.0f, -moveVector.X);
            up.Value = MathF.Max(0.0f, moveVector.Y);
            down.Value = MathF.Max(0.0f, -moveVector.Y);
        }

        // Update all buttons/sticks, to forward the updated binding state
        Jump.Update();
        Dash.Update();
        Climb.Update();
        Confirm.Update();
        Cancel.Update();
        Pause.Update();
        Move.Horizontal.Positive.Update();
        Move.Horizontal.Negative.Update();
        Move.Vertical.Positive.Update();
        Move.Vertical.Negative.Update();
    }

    private record TASButtonBinding(Actions action) : VirtualButton.IBinding
    {
        public bool IsPressed { get; set; }
        public bool IsDown { get; set; }
        public bool IsReleased { get; set; }

        public float Value => IsDown ? 1.0f : 0.0f;
        public float ValueNoDeadzone => IsDown ? 1.0f : 0.0f;

        public VirtualButton.ConditionFn? Enabled { get; set; }
    }

    private record TASAxisBinding : VirtualButton.IBinding
    {
        public bool IsPressed => Value > 0 && LastValue <= 0;
        public bool IsDown => Value > 0;
        public bool IsReleased => Value <= 0 && LastValue > 0;

        public float Value { get; set; }
        public float LastValue { get; set; }

        public float ValueNoDeadzone => Value;

        public VirtualButton.ConditionFn? Enabled { get; set; }
    }

    /// A button which is hijacked by the TAS inputs
    public static bool IsTASHijacked(this VirtualButton self) => self.Bindings.Any(bind => bind is TASButtonBinding or TASAxisBinding);
}
