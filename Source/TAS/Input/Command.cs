using Celeste64.TAS.Util;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Celeste64.TAS.Input;

public class Command
{
    public readonly string[] Arguments;
    public readonly CommandAttribute Attribute;

    public readonly string FilePath;
    public readonly int Frame;
    public readonly int StudioLineNumber; // From 0

    public string LineText => Arguments.Length == 0 ? Attribute.Name : $"{Attribute.Name}, {string.Join(", ", Arguments)}";

    public static bool Parsing { get; private set; }

    private readonly Action? commandCall;

    public void Invoke() => commandCall?.Invoke();
    public bool Is(string commandName) => Attribute.IsName(commandName);

    private Command(CommandAttribute attribute, int frame, Action commandCall, string[] args, string filePath, int studioLineNumber) {
        Arguments = args;
        Attribute = attribute;
        FilePath = filePath;
        Frame = frame;
        StudioLineNumber = studioLineNumber;
        this.commandCall = commandCall;
    }

    public static bool TryParse(InputController inputController, string filePath, int fileLine, string lineText, int frame, int studioLine, [NotNullWhen(true)] out Command? command)
    {
        Log.Info($"Parsing Command '{lineText}' at line {fileLine}/{studioLine} at frame {frame}");

        command = null;

        try
        {
            if (string.IsNullOrEmpty(lineText) || !char.IsLetter(lineText[0])) return false;

            string[] args = Split(lineText);
            string commandName = args[0];

            (CommandAttribute? attribute, MethodInfo? method) = CommandAttribute.FindMethod(commandName);
            if (attribute == null || method == null)
            {
                Log.Error($"Failed to parse command \"{lineText.Trim()}\" at line {fileLine} of the file \"{filePath}\"");
                return false;
            }

            string[] commandArgs = args.Skip(1).ToArray();

            var parameterTypes = method.GetParameters().Select(info => info.ParameterType).ToArray();
            object[] parameters = parameterTypes.Length switch
            {
                4 => [commandArgs, studioLine, filePath, fileLine],
                3 => [commandArgs, studioLine, filePath],
                2 when parameterTypes[1] == typeof(int) => [commandArgs, studioLine],
                2 when parameterTypes[1] == typeof(string) => [commandArgs, lineText.Trim()],
                1 => [commandArgs],
                0 => [],
                _ => throw new ArgumentException()
            };

            Action commandCall = () => method.Invoke(null, parameters);
            command = new Command(attribute, frame, commandCall, commandArgs, filePath, studioLine);

            if (attribute.ExecuteTiming.Has(ExecuteTiming.Parse))
            {
                Parsing = true;
                commandCall.Invoke();
                Parsing = false;
            }

            if (!inputController.Commands.TryGetValue(frame, out var commands))
                inputController.Commands[frame] = commands = [];
            commands.Add(command);

            return true;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            Log.Error($"Failed to parse command \"{lineText.Trim()}\" at line {fileLine} of the file \"{filePath}\"");
            return false;
        }
    }

    private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SpaceSeparatorRegex = new(@"^[^,]+?\s+[^,\s]", RegexOptions.Compiled);


    private static string[] Split(string line)
    {
        string trimLine = line.Trim();
        // Determined by the first separator
        string[] args = SpaceSeparatorRegex.IsMatch(trimLine) ? SpaceRegex.Split(trimLine) : trimLine.Split(',');
        return args.Select(text => text.Trim()).ToArray();
    }
}

/* Additional commands can be added by giving them the Command attribute and naming them (CommandName)Command.
 * The execute at start field indicates whether a command should be executed while building the input list (read, play)
 * or when playing the file (console).
 * The args field should list formats the command takes. This is not currently used but may be implemented into Studio in the future.
 * Commands that execute can be:
 * - void Command(string[], InputController, int)
 * - void Command(string[])
 * - void Command()
 */
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CommandAttribute(string name) : Attribute
{
    public string Name = name;
    public bool LegalInFullGame = true;
    public ExecuteTiming ExecuteTiming = ExecuteTiming.Runtime;
    public string[] Aliases = [];

    private static readonly Dictionary<CommandAttribute, MethodInfo> MethodInfos = new();

    public bool IsName(string name)
    {
        return Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) ||
               Aliases.Any(alias => alias.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }

    public static KeyValuePair<CommandAttribute, MethodInfo> FindMethod(string commandName)
    {
        return MethodInfos.FirstOrDefault(pair => pair.Key.IsName(commandName));
    }

    [RequiresUnreferencedCode("Calls System.Reflection.Assembly.GetTypes()")]
    internal static void CollectMethods()
    {
        MethodInfos.Clear();
        var methodInfos = typeof(CommandAttribute).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(info => info.GetCustomAttributes<CommandAttribute>().IsNotEmpty());

        foreach (var methodInfo in methodInfos)
        {
            var commandAttributes = methodInfo.GetCustomAttributes<CommandAttribute>();
            foreach (var commandAttribute in commandAttributes)
            {
                MethodInfos[commandAttribute] = methodInfo;
            }
        }
    }
}

[Flags]
public enum ExecuteTiming
{
    Parse = 1,
    Runtime = 2
}
