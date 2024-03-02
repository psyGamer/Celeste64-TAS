using Celeste64.TAS.Util;

namespace Celeste64.TAS.Input.Commands;

public static class MetadataCommands
{
    [Command("RecordCount", Aliases = ["RecordCount:", "RecordCount："], CalcChecksum = false)]
    private static void RecordCountCommand() {
        // dummy
    }

    [Command("FileTime", Aliases = ["FileTime:", "FileTime："], CalcChecksum = false)]
    private static void FileTimeCommand()
    {
        string timerStr = (int) Save.CurrentRecord.Time.TotalHours > 0
            ? $"{((int) Save.CurrentRecord.Time.TotalHours):00}:{Save.CurrentRecord.Time.Minutes:00}:{Save.CurrentRecord.Time.Seconds:00}:{Save.CurrentRecord.Time.Milliseconds:000}"
            : $"{Save.CurrentRecord.Time.Minutes:00}:{Save.CurrentRecord.Time.Seconds:00}:{Save.CurrentRecord.Time.Milliseconds:000}";
        string timer = $"{timerStr}(({((float)Save.CurrentRecord.Time.TotalSeconds).ToFrames()}))";

        UpdateAllMetadata("FileTime",
                          _ => timer,
                          command => Manager.Controller.CurrentCommands is { } curr && curr.Contains(command));
    }

    public static void IncrementRecordCount() {
        Log.Info("incrementing");
        UpdateAllMetadata(
            "RecordCount",
            command => (int.Parse(command.Arguments.FirstOrDefault() ?? "0") + 1).ToString(),
            command => int.TryParse(command.Arguments.FirstOrDefault() ?? "0", out int _));
    }

    private static void UpdateAllMetadata(string commandName, Func<Command, string> getMetadata, Func<Command, bool>? predicate = null) {
        InputController inputController = Manager.Controller;
        string tasFilePath = InputController.TasFilePath;

        var metadataCommands = inputController.Commands.SelectMany(pair => pair.Value)
            .Where(command => command.Is(commandName) && command.FilePath == tasFilePath)
            .Where(predicate ?? (_ => true))
            .ToList();

        var updateLines = metadataCommands
            .Where(command => {
                string metadata = getMetadata(command);
                if (metadata.IsNullOrEmpty())
                    return false;
                if (command.Arguments.Length > 0 && command.Arguments[0] == metadata)
                    return false;
                return true;
            })
            .ToDictionary(command => command.StudioLineNumber, command => $"{command.Attribute.Name}: {getMetadata(command)}");

        if (updateLines.IsEmpty()) {
            return;
        }

        string[] allLines = File.ReadAllLines(tasFilePath);
        foreach (int lineNumber in updateLines.Keys) {
            allLines[lineNumber] = updateLines[lineNumber];
        }

        bool needsReload = Manager.Controller.NeedsReload;
        File.WriteAllLines(tasFilePath, allLines);
        Manager.Controller.NeedsReload = needsReload;
        // StudioCommunicationClient.Instance?.UpdateLines(updateLines);
    }
}
