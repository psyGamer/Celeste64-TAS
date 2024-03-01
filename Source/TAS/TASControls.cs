using System.Reflection;

namespace Celeste64.TAS;

public static class TASControls
{
    public static readonly VirtualButton StartStop = new("StartStop");
    public static readonly VirtualButton Restart = new("Restart");
    public static readonly VirtualButton FastForward = new("FastForward");
    public static readonly VirtualButton FastForwardComment = new("FastForwardComment");
    public static readonly VirtualButton SlowForward = new("SlowForward");
    public static readonly VirtualButton FrameAdvance = new("FrameAdvance");
    public static readonly VirtualButton PauseResume = new("PauseResume");
    public static readonly VirtualButton Hitboxes = new("Hitboxes");
    public static readonly VirtualButton TriggerHitboxes = new("TriggerHitboxes");
    public static readonly VirtualButton SimplifiedGraphics = new("SimplifiedGraphics");
    public static readonly VirtualButton CenterCamera = new("CenterCamera");
    public static readonly VirtualButton LockCamera = new("LockCamera");
    public static readonly VirtualButton SaveState = new("SaveState");
    public static readonly VirtualButton ClearState = new("ClearState");
    public static readonly VirtualButton InfoHud = new("InfoHud");
    public static readonly VirtualButton FreeCamera = new("FreeCamera");
    public static readonly VirtualButton CameraUp = new("CameraUp");
    public static readonly VirtualButton CameraDown = new("CameraDown");
    public static readonly VirtualButton CameraLeft = new("CameraLeft");
    public static readonly VirtualButton CameraRight = new("CameraRight");
    public static readonly VirtualButton CameraZoomIn = new("CameraZoomIn");
    public static readonly VirtualButton CameraZoomOut = new("CameraZoomOut");

    public static readonly VirtualButton InvisiblePlayer = new("InvisiblePlayer");
    public static readonly VirtualButton Freecam = new("Freecam");
    public static readonly VirtualButton ToggleInfoGUI = new("ToggleInfoGUI");

    public static readonly VirtualStick FreecamMove = new("FreeCamMove", VirtualAxis.Overlaps.TakeNewer, 0.35f);

    public static void Load()
    {
        StartStop.Clear();
        StartStop.Add(Keys.RightControl);

        Restart.Clear();
        Restart.Add(Keys.RightBracket);

        PauseResume.Clear();
        PauseResume.Add(Keys.P);

        FrameAdvance.Clear();
        FrameAdvance.RepeatDelay = 0.25f;
        FrameAdvance.RepeatInterval = 0.1f;
        FrameAdvance.Add(Keys.L);

        SlowForward.Clear();
        SlowForward.Add(Keys.RightShift);

        FastForward.Clear();
        FastForward.Add(Keys.RightShift);

        Hitboxes.Clear();
        Hitboxes.Add(Keys.B);

        SimplifiedGraphics.Clear();
        SimplifiedGraphics.Add(Keys.N);

        InvisiblePlayer.Clear();
        InvisiblePlayer.Add(Keys.I);

        Freecam.Clear();
        Freecam.Add(Keys.M);

        FreecamMove.Clear();
        FreecamMove.AddLeftJoystick(0);
        FreecamMove.AddDPad(0);
        FreecamMove.Add(Keys.A, Keys.D, Keys.W, Keys.S);

        ToggleInfoGUI.Clear();
        ToggleInfoGUI.Add(Keys.F12);
    }
}
