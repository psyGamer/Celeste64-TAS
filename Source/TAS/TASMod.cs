using Celeste64.TAS.Input;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Celeste64.TAS;

public class TASMod
{
    private static ILHook? il_App_Tick;
    private static Hook? on_App_Tick_Update;
    private static Hook? on_Input_Step;

    public static void Initialize()
    {
        CommandAttribute.CollectMethods();

        il_App_Tick = new ILHook(typeof(App).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException(), IL_App_Tick);
        on_App_Tick_Update = new Hook(typeof(App).GetMethod("<Tick>g__Update|69_0", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException(), On_App_Tick_Update);
        on_Input_Step = new Hook(typeof(Foster.Framework.Input).GetMethod("Step", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException(), On_Input_Step);
    }

    public static void Deinitialize()
    {
        il_App_Tick?.Dispose();
        on_App_Tick_Update?.Dispose();
        on_Input_Step?.Dispose();
    }

    public static void Update()
    {
        if (TASControls.Freecam.ConsumePress())
        {
            Save.Instance.Freecam = Save.Instance.Freecam switch
            {
                Save.FreecamMode.Disabled => Save.FreecamMode.Orbit,
                Save.FreecamMode.Orbit => Save.FreecamMode.Free,
                Save.FreecamMode.Free => Save.FreecamMode.Disabled,
                _ => throw new ArgumentOutOfRangeException()
            };
            Save.Instance.SyncSettings();
        }

        if (TASControls.SimplifiedGraphics.ConsumePress())
        {
            Save.Instance.SimplifiedGraphics = !Save.Instance.SimplifiedGraphics;
            Save.Instance.SyncSettings();

            if (Game.Scene is World world)
            {
                world.Camera.FarPlane = Save.Instance.SimplifiedGraphics ? 8000 : 800;

                foreach (var actor in world.Actors)
                {
                    var fields = actor.GetType().GetFields()
                        .Where(f => f.FieldType.IsAssignableTo(typeof(Model)));

                    foreach (var field in fields)
                    {
                        var model = (Model) field.GetValue(actor)!;
                        foreach (var material in model.Materials)
                        {
                            if (material.Shader == null || !Assets.Shaders.ContainsKey(material.Shader.Name)) continue;
                            material.Simplified = Save.Instance.SimplifiedGraphics;
                        }
                    }
                }
            }
        }

        if (TASControls.Hitboxes.ConsumePress())
        {
            Save.Instance.Hitboxes = !Save.Instance.Hitboxes;
            Save.Instance.SyncSettings();
        }

        if (TASControls.InvisiblePlayer.ConsumePress())
        {
            Save.Instance.InvisiblePlayer = !Save.Instance.InvisiblePlayer;
            Save.Instance.SyncSettings();
        }

        Manager.Update();

        if (TASControls.StartStop.ConsumePress())
        {
            if (Manager.Running)
                Manager.DisableRun();
            else
                Manager.EnableRun();
        }

        InfoHUD.Update();
    }

    // Time.Duration is used for TAS inputs, so we need to keep track of this ourselves to do non-TAS inputs
    private static TimeSpan RealDuration;

    private static void IL_App_Tick(ILContext il)
    {
        var cur = new ILCursor(il);
        // Goto this if block:
        // if (App.accumulator > Time.FixedStepMaxElapsedTime)
        cur.GotoNext(MoveType.After,
            instr => instr.MatchLdsfld("Foster.Framework.App", "accumulator"),
            instr => instr.MatchLdsfld("Foster.Framework.Time", "FixedStepMaxElapsedTime"),
            instr => instr.MatchCall<TimeSpan>("op_GreaterThan"));
        // And append a '&& !Manager.Running' to the condition
        cur.EmitCall(typeof(Manager).GetProperty(nameof(Manager.Running), BindingFlags.Public | BindingFlags.Static)!.GetGetMethod()!);
        cur.EmitNot();
        cur.EmitAnd();
    }

    private delegate void orig_App_Tick_Update(TimeSpan delta);
    private static void On_App_Tick_Update(orig_App_Tick_Update orig, TimeSpan delta)
    {
        if (TASControls.ToggleInfoGUI.ConsumePress())
        {
            Game.Instance.imGuiEnabled = !Game.Instance.imGuiEnabled;
        }
        if (Game.Instance.imGuiEnabled)
        {
            Game.Instance.imGuiRenderer.Update();
        }

        // Always update real duration
        RealDuration += delta;

        // Don't do anything if TAS isn't running
        if (!Manager.Running)
        {
            orig(delta);
            Update();
            return;
        }

        // We split-up Input.Step() into 2 parts:
        // - Every real frame: Non-TAS inputs
        // - Every TAS frame: TAS inputs

        // Non-TAS inputs
        Foster.Framework.Input.LastState.Copy(Foster.Framework.Input.State);
        Foster.Framework.Input.State.Copy(Foster.Framework.Input.nextState);
        Foster.Framework.Input.nextState.Step();
        // Fake Time.Duration for binding update
        var tasDuration = Time.Duration;
        Time.Duration = RealDuration;
        for (int index = Foster.Framework.Input.virtualButtons.Count - 1; index >= 0; --index)
        {
            var button = Foster.Framework.Input.virtualButtons[index];
            if (button.TryGetTarget(out var target))
            {
                if (!target.IsTASHijacked())
                    target.Update();
            }
            else
            {
                Foster.Framework.Input.virtualButtons.RemoveAt(index);
            }
        }
        // Reset back
        Time.Duration = tasDuration;

        int loops = Manager.FrameLoops; // Copy to local variable, so it doesn't update while iterating
        for (int i = 0; i < loops; i++)
        {
            TASMod.Update();
            orig(delta);
        }
    }

    private delegate void orig_Input_Step();
    private static void On_Input_Step(orig_Input_Step orig)
    {
        // Default behaviour outside a TAS
        if (!Manager.Running)
            orig();

        // Updating hijacked input is done inside InputHelper.FeedInputs()
    }
}
