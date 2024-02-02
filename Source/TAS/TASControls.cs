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

    public static readonly VirtualButton Freecam = new("Freecam");
    public static readonly VirtualButton ToggleInfoGUI = new("ToggleInfoGUI");

    public static void Load()
    {
        StartStop.Clear();
        StartStop.Add(Keys.RightControl);

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

        SimplifiedGraphics.Clear();
        SimplifiedGraphics.Add(Keys.N);

        Freecam.Clear();
        Freecam.Add(Keys.M);

        ToggleInfoGUI.Clear();
        ToggleInfoGUI.Add(Keys.F12);
    }

    public static bool IsTASControl(this VirtualButton self) =>
        self == StartStop ||
        self == Restart ||
        self == FastForward ||
        self == FastForwardComment ||
        self == SlowForward ||
        self == FrameAdvance ||
        self == PauseResume ||
        self == Hitboxes ||
        self == TriggerHitboxes ||
        self == SimplifiedGraphics ||
        self == CenterCamera ||
        self == LockCamera ||
        self == SaveState ||
        self == ClearState ||
        self == InfoHud ||
        self == FreeCamera ||
        self == CameraUp ||
        self == CameraDown ||
        self == CameraLeft ||
        self == CameraRight ||
        self == CameraZoomIn ||
        self == CameraZoomOut ||
        self == FreeCamera ||
        self == ToggleInfoGUI;
}
