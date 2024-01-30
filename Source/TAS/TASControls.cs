namespace Celeste64.TAS;

public static class TASControls {

    public static readonly VirtualButton StartStop = new("StartStop");
    public static readonly VirtualButton Restart = new("Restart");
    public static readonly VirtualButton FastForward = new("FastForward");
    public static readonly VirtualButton FastForwardComment = new("FastForwardComment");
    public static readonly VirtualButton SlowForward = new("SlowForward");
    public static readonly VirtualButton FrameAdvance = new("FrameAdvance");
    public static readonly VirtualButton PauseResume = new("PauseResume");
    public static readonly VirtualButton Hitboxes = new("Hitboxes");
    public static readonly VirtualButton TriggerHitboxes = new("TriggerHitboxes");
    public static readonly VirtualButton SimplifiedGraphic = new("SimplifiedGraphic");
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

    public static void Load()
    {
        StartStop.Clear();
        StartStop.Add(Keys.RightControl);
    }
}