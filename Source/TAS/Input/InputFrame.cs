namespace Celeste64.TAS.Input;

public struct InputFrame
{
    public readonly int Line;
    public readonly int Frames;

    public InputFrame(int frames)
    {
        Frames = frames;
    }

    public static bool TryParse(string line, int studioLine, InputFrame prevInputFrame, out InputFrame inputFrame, int repeatIndex = 0, int repeatCount = 0, int frameOffset = 0)
    {
        Log.Info($"Parsing InputFrame '{line}' at {studioLine}");

        inputFrame = new(0);
        return true;
    }
}
