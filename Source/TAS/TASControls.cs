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

        Hitboxes.Clear();
        Hitboxes.Add(Keys.B);

        SimplifiedGraphics.Clear();
        SimplifiedGraphics.Add(Keys.N);

        InvisiblePlayer.Clear();
        InvisiblePlayer.Add(Keys.I);

        Freecam.Clear();
        Freecam.Add(Keys.M);

        ToggleInfoGUI.Clear();
        ToggleInfoGUI.Add(Keys.F12);
    }

    private static readonly MethodInfo m_VirtualButton_Update = typeof(VirtualButton).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException();

    public static void Update()
    {
        m_VirtualButton_Update.Invoke(StartStop, []);
        m_VirtualButton_Update.Invoke(Restart, []);
        m_VirtualButton_Update.Invoke(FastForward, []);
        m_VirtualButton_Update.Invoke(FastForwardComment, []);
        m_VirtualButton_Update.Invoke(SlowForward, []);
        m_VirtualButton_Update.Invoke(FrameAdvance, []);
        m_VirtualButton_Update.Invoke(PauseResume, []);
        m_VirtualButton_Update.Invoke(Hitboxes, []);
        m_VirtualButton_Update.Invoke(TriggerHitboxes, []);
        m_VirtualButton_Update.Invoke(SimplifiedGraphics, []);
        m_VirtualButton_Update.Invoke(CenterCamera, []);
        m_VirtualButton_Update.Invoke(LockCamera, []);
        m_VirtualButton_Update.Invoke(SaveState, []);
        m_VirtualButton_Update.Invoke(ClearState, []);
        m_VirtualButton_Update.Invoke(InfoHud, []);
        m_VirtualButton_Update.Invoke(FreeCamera, []);
        m_VirtualButton_Update.Invoke(CameraUp, []);
        m_VirtualButton_Update.Invoke(CameraDown, []);
        m_VirtualButton_Update.Invoke(CameraLeft, []);
        m_VirtualButton_Update.Invoke(CameraRight, []);
        m_VirtualButton_Update.Invoke(CameraZoomIn, []);
        m_VirtualButton_Update.Invoke(CameraZoomOut, []);

        m_VirtualButton_Update.Invoke(InvisiblePlayer, []);
        m_VirtualButton_Update.Invoke(Freecam, []);
        m_VirtualButton_Update.Invoke(ToggleInfoGUI, []);
    }

    // public static bool IsTASControl(this VirtualButton self) =>
    //     self == StartStop ||
    //     self == Restart ||
    //     self == FastForward ||
    //     self == FastForwardComment ||
    //     self == SlowForward ||
    //     self == FrameAdvance ||
    //     self == PauseResume ||
    //     self == Hitboxes ||
    //     self == TriggerHitboxes ||
    //     self == SimplifiedGraphics ||
    //     self == CenterCamera ||
    //     self == LockCamera ||
    //     self == SaveState ||
    //     self == ClearState ||
    //     self == InfoHud ||
    //     self == FreeCamera ||
    //     self == CameraUp ||
    //     self == CameraDown ||
    //     self == CameraLeft ||
    //     self == CameraRight ||
    //     self == CameraZoomIn ||
    //     self == CameraZoomOut ||
    //     self == FreeCamera ||
    //     self == ToggleInfoGUI;
}
