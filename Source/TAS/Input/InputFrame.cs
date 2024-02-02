using Celeste64.TAS.StudioCommunication;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Celeste64.TAS.Input;

public record InputFrame
{
    private const int MaxFrames = 9999;
    private const int MaxFrameDigits = 4;

    public int Line { get; init; }
    public int Frames { get; init; }
    public int FrameOffset { get; init; }

    public Actions Actions;
    public float? Angle { get; init; }
    public float Magnitude { get; init; }

    public InputFrame? Prev { get; private set; }
    public InputFrame? Next { get; private set; }

    public Vec2 MoveVector => Angle == null
        ? Vec2.Zero
        : new Vec2(MathF.Sin(Angle.Value * Calc.DegToRad) * Magnitude, MathF.Cos(Angle.Value * Calc.DegToRad) * Magnitude);

    public static bool TryParse(string line, int studioLine, InputFrame? prevInputFrame, [NotNullWhen(true)] out InputFrame? inputFrame, int repeatIndex = 0, int repeatCount = 0, int frameOffset = 0)
    {
        Log.Info($"Parsing InputFrame '{line}' at {studioLine}");

        int frameSeparatorIdx = line.IndexOf(",", StringComparison.Ordinal);
        string framesStr;
        if (frameSeparatorIdx == -1) {
            framesStr = line;
        } else {
            framesStr = line.Substring(0, frameSeparatorIdx);
        }

        if (!int.TryParse(framesStr, out int frames) || frames <= 0) {
            inputFrame = null;
            return false;
        }

        if (frameSeparatorIdx == -1) {
            // No actions
            inputFrame = new InputFrame {
                Line = studioLine,
                Frames = frames,
                FrameOffset = frameOffset,
                Actions = Actions.None,
            };;
            return true;
        }

        string[] actionStrs = line.Substring(frameSeparatorIdx + 1).Split(',');

        Actions actions = Actions.None;
        float? angle = null;
        float? magnitude = null;

        foreach (string actionStr in actionStrs)
        {
            if (actionStr.Length == 1 && ActionsUtils.TryParse(actionStr[0], out var action))
            {
                actions |= action;
                continue;
            }

            if (float.TryParse(actionStr, CultureInfo.InvariantCulture, out float value))
            {
                if (angle == null)
                {
                    angle = value;
                    continue;
                }

                if (magnitude == null)
                {
                    magnitude = value;
                    continue;
                }

                Log.Warning($"Invalid float action: '{actionStr}' inside '{line}' @ {studioLine}");
            }
        }

        inputFrame = new InputFrame {
            Line = studioLine,
            Frames = frames,
            FrameOffset = frameOffset,

            Actions = actions,
            Angle = angle,
            Magnitude = magnitude ?? 1.0f,
        };

        if (prevInputFrame != null)
        {
            prevInputFrame.Next = inputFrame;
            inputFrame.Prev = prevInputFrame;
        }

        return true;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append(Frames.ToString().PadLeft(MaxFrameDigits));
        if (Angle != null)
            sb.Append($",{Angle.Value.ToString(CultureInfo.InvariantCulture)}");
        if (Math.Abs(Magnitude - 1.0f) > 1e-10)
            sb.Append($",{Magnitude.ToString(CultureInfo.InvariantCulture)}");

        foreach ((char chr, Actions action) in ActionsUtils.Chars)
        {
            if (Actions.HasFlag(action))
                sb.Append($",{chr}");
        }

        return sb.ToString();
    }
}
