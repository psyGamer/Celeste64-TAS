using Celeste64.TAS.Input;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Celeste64.TAS;

public class TASMod
{
    private static ILHook? il_App_Tick;
    private static Hook? on_App_Tick_Update;
    private static Hook? on_Time_Advance;
    private static Hook? on_VirtualButton_Update;

    public static void Initialize()
    {
        CommandAttribute.CollectMethods();
        CustomInfo.CollectAllTypeInfo();
        CustomInfo.InitializeHelperMethods();

        il_App_Tick = new ILHook(typeof(App).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException(), IL_App_Tick);
        on_App_Tick_Update = new Hook(typeof(App).GetMethod("<Tick>g__Update|69_0", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException(), On_App_Tick_Update);
        on_Time_Advance = new Hook(typeof(Time).GetMethod(nameof(Time.Advance), BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException(), On_Time_Advance);
        on_VirtualButton_Update = new Hook(typeof(VirtualButton).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException(), On_VirtualButton_Update);
    }

    public static void Deinitialize()
    {
        il_App_Tick?.Dispose();
        on_App_Tick_Update?.Dispose();
        on_Time_Advance?.Dispose();
        on_VirtualButton_Update?.Dispose();
    }

    public static void Update()
    {
        if (TASControls.Freecam.Pressed)
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

        if (TASControls.SimplifiedGraphics.Pressed)
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

        Manager.Update();

        if (TASControls.StartStop.Pressed)
        {
            if (Manager.Running)
                Manager.DisableRun();
            else
                Manager.EnableRun();
        }

        InfoHUD.Update();
    }

    private static TimeSpan ActualDuration;

    private delegate void orig_Time_Advance(TimeSpan delta);
    private static void On_Time_Advance(orig_Time_Advance orig, TimeSpan delta)
    {
        // Don't advance time while paused
        // However, we still need to advance it for ourselves, so that TASControls still work
        if (Manager.IsPaused())
        {
            ActualDuration += delta;
            return;
        }

        orig(delta);
        ActualDuration = Time.Duration;
    }

    private static readonly MethodInfo m_VirtualButton_set_Repeated = typeof(VirtualButton).GetProperty(nameof(VirtualButton.Repeated))?.GetSetMethod(nonPublic: true) ?? throw new InvalidOperationException();
    private delegate void orig_VirtualButton_Update(VirtualButton self);
    private static void On_VirtualButton_Update(orig_VirtualButton_Update orig, VirtualButton self)
    {
        orig(self);

        // If this is one of our controls, re-run the "repeated"-check with the ActualDuration
        if (!self.IsTASControl()) return;

        if (self.Down && (ActualDuration - self.PressTimestamp).TotalSeconds > self.RepeatDelay)
        {
            if (Time.OnInterval(
                    (ActualDuration - self.PressTimestamp).TotalSeconds - self.RepeatDelay,
                    Time.Delta,
                    self.RepeatInterval, 0))
            {
                m_VirtualButton_set_Repeated.Invoke(self, [true]);
            }
        }
    }

    private static void IL_App_Tick(ILContext il)
    {
        var cur = new ILCursor(il);
        // Goto this if block:
        // if (App.accumulator > Time.FixedStepMaxElapsedTime)
        cur.GotoNext(MoveType.After,
            instr => instr.MatchLdsfld("Foster.Framework.App", "accumulator"),
            instr => instr.MatchLdsfld("Foster.Framework.Time", "FixedStepMaxElapsedTime"),
            instr => instr.MatchCall<TimeSpan>("op_GreaterThan"));
        // And append a '&& !Manager.Running' to the conditionn
        cur.EmitCall(typeof(Manager).GetProperty(nameof(Manager.Running), BindingFlags.Public | BindingFlags.Static)!.GetGetMethod()!);
        cur.EmitNot();
        cur.EmitAnd();
    }

    private delegate void orig_App_Tick_Update(TimeSpan delta);
    private static void On_App_Tick_Update(orig_App_Tick_Update orig, TimeSpan delta)
    {
        if (TASControls.ToggleInfoGUI.Pressed)
        {
            Game.Instance.imGuiEnabled = !Game.Instance.imGuiEnabled;
        }

        if (Game.Instance.imGuiEnabled)
        {
            Game.Instance.imGuiRenderer.Update();
        }

        int loops = Manager.FrameLoops; // Copy to local variable, so it doesn't update while iterating
        for (int i = 0; i < loops; i++)
        {
            orig(delta);
        }
    }
}
