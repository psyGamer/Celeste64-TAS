using Celeste64.TAS.Util;
using System.Text;
using TAS.Utils;

namespace Celeste64.TAS.Input;

[AttributeUsage(AttributeTargets.Method)]
internal class ClearInputsAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
internal class ParseFileEndAttribute : Attribute;

public class InputController
{
    public readonly List<InputFrame> Inputs = new();
    public readonly SortedDictionary<int, List<Command>> Commands = new();

    private static readonly Dictionary<string, FileSystemWatcher> watchers = new();
    private readonly HashSet<string> usedFiles = new();

    public bool NeedsReload = true;

    private int initializationFrameCount;

    public int CurrentFrameInInput { get; private set; } // Starts at 1
    // public int CurrentFrameInInputForHud { get; private set; } // Starts at 1
    public int CurrentFrameInTas { get; private set; } // Starts at 0

    public InputFrame Previous => Inputs.GetValueOrDefault(CurrentFrameInTas - 1);
    public InputFrame Current => Inputs.GetValueOrDefault(CurrentFrameInTas);
    public InputFrame Next => Inputs.GetValueOrDefault(CurrentFrameInTas + 1);
    public List<Command>? CurrentCommands => Commands.GetValueOrDefault(CurrentFrameInTas);

    public bool CanPlayback => CurrentFrameInTas < Inputs.Count;
    public bool NeedsToWait => false; // TODO Manager.IsLoading();

    private static readonly string DefaultTasFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Celeste64.tas");

    private static string studioTasFilePath = string.Empty;
    public static string StudioTasFilePath {
        get => studioTasFilePath;
        set {
            if (studioTasFilePath == value) return;

            studioTasFilePath = string.IsNullOrEmpty(value) ? value : Path.GetFullPath(value);

            try {
                if (!File.Exists(TasFilePath)) {
                    File.WriteAllText(TasFilePath, string.Empty);
                }
            } catch {
                studioTasFilePath = DefaultTasFilePath;
            }

            if (Manager.Running) {
                Manager.DisableRun();
            }

            // Preload tas file
            Manager.Controller.Clear();
            Manager.Controller.Stop();
            Manager.Controller.RefreshInputs();
        }
    }

    public static string TasFilePath => string.IsNullOrEmpty(StudioTasFilePath) ? DefaultTasFilePath : StudioTasFilePath;

    private string checksum = string.Empty;
    private string Checksum => string.IsNullOrEmpty(checksum) ? checksum = CalcChecksum(/*Inputs.Count - 1*/0) : checksum;

    public void RefreshInputs() {
        if (!NeedsReload) return;

        string lastChecksum = Checksum;
        bool firstRun = usedFiles.IsEmpty();

        Clear();
        int tryCount = 5;
        while (tryCount > 0) {
            if (ReadFile(TasFilePath)) {
                // if (Manager.NextStates.Has(States.Disable)) {
                //     Clear();
                //     Manager.DisableRun();
                // } else {
                    NeedsReload = false;
                    ParseFileEnd();
                    // if (!firstRun && lastChecksum != Checksum) {
                    //     MetadataCommands.UpdateRecordCount(this);
                    // }
                // }

                break;
            } else {
                System.Threading.Thread.Sleep(50);
                tryCount--;
                Clear();
            }
        }

        // CurrentFrameInTas = Math.Min(Inputs.Count, CurrentFrameInTas);
    }

    public void AdvanceFrame(out bool canPlayback) {
        RefreshInputs();

        canPlayback = CanPlayback;

        if (NeedsToWait) {
            return;
        }

        if (CurrentCommands != null) {
            foreach (var command in CurrentCommands) {
                if (command.Attribute.ExecuteTiming.Has(ExecuteTiming.Runtime) /*&&
                    (!EnforceLegalCommand.EnabledWhenRunning || command.Attribute.LegalInMainGame)*/) {
                    command.Invoke();
                }
            }
        }

        if (!CanPlayback) {
            return;
        }

        // ExportGameInfo.ExportInfo();
        // StunPauseCommand.UpdateSimulateSkipInput();
        InputHelper.FeedInputs(Current);

        if (CurrentFrameInInput == 0 || Current.Line == Previous.Line) {
            CurrentFrameInInput++;
        } else {
            CurrentFrameInInput = 1;
        }

        // if (CurrentFrameInInputForHud == 0 || Current == Previous) {
        //     CurrentFrameInInputForHud++;
        // } else {
        //     CurrentFrameInInputForHud = 1;
        // }

        CurrentFrameInTas++;
    }

    // studioLine starts at 0, startLine starts at 1;
    public bool ReadFile(string filePath, int startLine = 0, int endLine = int.MaxValue, int studioLine = 0, int repeatIndex = 0, int repeatCount = 0) {
        try {
            if (!File.Exists(filePath)) {
                return false;
            }

            usedFiles.Add(filePath);
            var lines = File.ReadLines(filePath).Take(endLine);
            ReadLines(lines, filePath, startLine, studioLine, repeatIndex, repeatCount);
            return true;
        } catch (Exception e) {
            Log.Warning(e.ToString());
            return false;
        }
    }

    public void ReadLines(IEnumerable<string> lines, string filePath, int startLine, int studioLine, int repeatIndex, int repeatCount, bool lockStudioLine = false) {
        int subLine = 0;
        foreach (string readLine in lines) {
            subLine++;
            if (subLine < startLine) {
                continue;
            }

            string lineText = readLine.Trim();

            if (lineText.StartsWith('#')) continue; // Comment

            if (Command.TryParse(this, filePath, subLine, lineText, initializationFrameCount, studioLine, out var _))
            {
                continue;
            }
            // if (Command.TryParse(this, filePath, subLine, lineText, initializationFrameCount, studioLine, out Command command) &&
            //     command.Is("Play")) {
            //     // workaround for the play command
            //     // the play command needs to stop reading the current file when it's done to prevent recursion
            //     return;
            // }

            // if (lineText.StartsWith("***")) {
            //     FastForward fastForward = new(initializationFrameCount, lineText.Substring(3), studioLine);
            //     if (FastForwards.TryGetValue(initializationFrameCount, out FastForward oldFastForward) && oldFastForward.SaveState &&
            //         !fastForward.SaveState) {
            //         // ignore
            //     } else {
            //         FastForwards[initializationFrameCount] = fastForward;
            //     }
            // } else if (lineText.StartsWith("#")) {
            //     FastForwardComments[initializationFrameCount] = new FastForward(initializationFrameCount, "", studioLine);
            //     if (!Comments.TryGetValue(filePath, out var comments)) {
            //         Comments[filePath] = comments = new List<Comment>();
            //     }
            //
            //     comments.Add(new Comment(filePath, initializationFrameCount, subLine, lineText));
            // } else if (!AutoInputCommand.TryInsert(filePath, lineText, studioLine, repeatIndex, repeatCount)) {
                AddFrames(lineText, studioLine, repeatIndex, repeatCount);
            // }

            if (filePath == TasFilePath && !lockStudioLine) {
                studioLine++;
            }
        }

        // if (filePath == TasFilePath) {
        //     FastForwardComments[initializationFrameCount] = new FastForward(initializationFrameCount, "", studioLine);
        // }
    }

    public void AddFrames(string line, int studioLine, int repeatIndex = 0, int repeatCount = 0, int frameOffset = 0) {
        if (!InputFrame.TryParse(line, studioLine, Inputs.LastOrDefault(), out InputFrame? inputFrame, repeatIndex, repeatCount, frameOffset)) {
            return;
        }

        // Console.WriteLine($"Input {inputFrame}");

        for (int i = 0; i < inputFrame.Value.Frames; i++) {
            Inputs.Add(inputFrame.Value);
        }

        // LibTasHelper.WriteLibTasFrame(inputFrame);
        initializationFrameCount += inputFrame.Value.Frames;
    }

    public void Stop() {
        CurrentFrameInInput = 0;
        // CurrentFrameInInputForHud = 0;
        CurrentFrameInTas = 0;
        // NextCommentFastForward = null;
    }

    public void Clear() {
        initializationFrameCount = 0;
        checksum = string.Empty;
        // savestateChecksum = string.Empty;
        Inputs.Clear();
        Commands.Clear();
        // FastForwards.Clear();
        // FastForwardComments.Clear();
        // Comments.Clear();
        usedFiles.Clear();
        NeedsReload = true;
        StopWatchers();
        AttributeUtils.Invoke<ClearInputsAttribute>();
    }

    private void StartWatchers() {
        foreach (string filePath in usedFiles) {
            string? fullFilePath = Path.GetFullPath(filePath);

            // Watch TAS file
            CreateWatcher(fullFilePath);

            // Watch parent folder, since watched folder's change is not detected
            while (fullFilePath != null && Directory.GetParent(fullFilePath) != null) {
                CreateWatcher(Path.GetDirectoryName(fullFilePath)!);
                fullFilePath = Directory.GetParent(fullFilePath)?.FullName;
            }
        }

        return;

        void CreateWatcher(string filePath) {
            if (watchers.ContainsKey(filePath)) {
                return;
            }

            FileSystemWatcher watcher;
            if (File.GetAttributes(filePath).Has(FileAttributes.Directory)) {
                if (Directory.GetParent(filePath) is { } parentDir) {
                    watcher = new FileSystemWatcher();
                    watcher.Path = parentDir.FullName;
                    watcher.Filter = new DirectoryInfo(filePath).Name;
                    watcher.NotifyFilter = NotifyFilters.DirectoryName;
                } else {
                    return;
                }
            } else {
                watcher = new FileSystemWatcher();
                watcher.Path = Path.GetDirectoryName(filePath)!;
                watcher.Filter = Path.GetFileName(filePath);
            }

            watcher.Changed += OnTasFileChanged;
            watcher.Created += OnTasFileChanged;
            watcher.Deleted += OnTasFileChanged;
            watcher.Renamed += OnTasFileChanged;

            try {
                watcher.EnableRaisingEvents = true;
            } catch (Exception e) {
                Log.Error(e.ToString());
                Log.Error($"Failed watching folder: {watcher.Path}, filter: {watcher.Filter}");
                watcher.Dispose();
                return;
            }

            watchers[filePath] = watcher;
        }

        void OnTasFileChanged(object sender, FileSystemEventArgs e) {
            Log.Info("File changed");
            NeedsReload = true;
        }
    }

    private void StopWatchers() {
        foreach (var watcher in watchers.Values) {
            watcher.Dispose();
        }
        watchers.Clear();
    }

    private void ParseFileEnd() {
        StartWatchers();
        AttributeUtils.Invoke<ParseFileEndAttribute>();
    }

    private string CalcChecksum(int toInputFrame) {
        StringBuilder result = new(TasFilePath);
        result.AppendLine();

        // int checkInputFrame = 0;
        //
        // while (checkInputFrame < toInputFrame) {
        //     InputFrame currentInput = Inputs[checkInputFrame];
        //     result.AppendLine(currentInput.ToActionsString());
        //
        //     if (Commands.GetValueOrDefault(checkInputFrame) is { } commands) {
        //         foreach (Command command in commands.Where(command => command.Attribute.CalcChecksum)) {
        //             result.AppendLine(command.LineText);
        //         }
        //     }
        //
        //     checkInputFrame++;
        // }

        return HashHelper.ComputeHash(result.ToString());
    }
}
