using Celeste64.TAS.Input.Commands;
using Celeste64.TAS.Util;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Celeste64.TAS;

public class CustomInfo
{
    private static readonly Regex LuaRegex = new(@"\[\[(.+?)\]\]", RegexOptions.Compiled);
    private static readonly Regex BraceRegex = new(@"\{(.+?)\}", RegexOptions.Compiled);
    private static readonly Regex TypeNameRegex = new(@"^([.\w=+<>]+)$", RegexOptions.Compiled);
    private static readonly Regex TypeNameSeparatorRegex = new(@"^[.+]", RegexOptions.Compiled);
    private static readonly Regex MethodRegex = new(@"^(.+)\((.*)\)$", RegexOptions.Compiled);

    private static bool EnforceLegal => EnforceLegalCommand.EnabledWhenRunning; // && !AssertCommand.Running;

    private delegate bool HelperMethod(object obj, int decimals, [NotNullWhen(true)] out string? formattedValue);

    private static readonly Dictionary<string, HelperMethod> HelperMethods = new()
    {
        ["toFrame"] = static (object obj, int decimals, out string? formattedValue) =>
        {
            if (obj is float floatValue)
            {
                formattedValue = floatValue.ToFrames().ToString();
                return true;
            }

            formattedValue = null;
            return false;
        }
    };

    private static readonly Dictionary<string, Type> TypeCache = new();

    public static string GetInfo(int? decimals = null)
    {
        decimals ??= Save.Instance.InfoHudDecimals;
        return ParseTemplate(Save.Instance.InfoHudShowCustomTemplate, decimals.Value);
    }

    private static string ParseTemplate(string template, int decimals) =>
        BraceRegex.Replace(template, match =>
        {
            string matchText = match.Groups[1].Value;

            if (!TryParseMemberNames(matchText, out string? typeText, out string[]? memberNames, out string errorMessage))
                return errorMessage;
            if (!TryParseType(typeText, out var type, out errorMessage))
                return errorMessage;

            string lastMemberName = memberNames.Last();
            char lastCharacter = lastMemberName.Last();
            if (lastCharacter is ':' or '=')
            {
                lastMemberName = lastMemberName[..^1].Trim();
                memberNames[^1] = lastMemberName;
            }

            string? helperMethod = null;
            if (HelperMethods.ContainsKey(lastMemberName))
            {
                helperMethod = lastMemberName;
                memberNames = memberNames[..^1];
            }

            string prefix = lastCharacter switch
            {
                '=' => matchText,
                ':' => $"{matchText} ",
                _ => ""
            };

            return $"{prefix}{Format(type, memberNames, helperMethod, decimals)}";

            static string Format(Type? type, string[] memberNames, string? helperMethod, int decimals)
            {
                if (memberNames.IsNotEmpty() && (
                        type.GetGetMethod(memberNames.First()) is { IsStatic: true } ||
                        type.GetFieldInfo(memberNames.First()) is { IsStatic: true } ||
                        (MethodRegex.Match(memberNames.First()) is { Success: true } methodMatch && type.GetMethodInfo(methodMatch.Groups[1].Value) is { IsStatic: true })
                    ))
                {
                    object? obj = GetMemberValue(type, null, memberNames, out string? errMsg);
                    if (errMsg != null)
                        return errMsg;

                    return FormatValue(obj, helperMethod, decimals);
                }

                if (Game.Scene is World world)
                {
                    if (type.IsSameOrSubclassOf(typeof(Actor)))
                    {
                        if (type == typeof(World))
                        {
                            object? obj = GetMemberValue(type, world, memberNames, out string? errMsg);
                            if (errMsg != null)
                                return errMsg;

                            return FormatValue(obj, helperMethod, decimals);
                        }

                        var actors = world.All(type);
                        return string.Join("", actors.Select(actor =>
                        {
                            object? obj = GetMemberValue(type, actor, memberNames, out string? errMsg);
                            if (errMsg != null)
                                return errMsg;

                            string value = FormatValue(obj, helperMethod, decimals);

                            if (actors.Count > 1)
                                return $"\n{value}";
                            return value;
                        }));
                    }
                }

                return string.Empty;
            }
        });

    internal static bool TryParseMemberNames(string matchText, [NotNullWhen(true)] out string? typeText, [NotNullWhen(true)] out string[]? memberNames, out string errorMessage)
    {
        List<string> splitText = matchText
            .Split('.')
            .Select(s => s.Trim())
            .Where(s => s.IsNotEmpty())
            .ToList();

        if (splitText.Count <= 1)
        {
            errorMessage = "Missing member";
            memberNames = null;
            typeText = null;
            return false;
        }

        if (matchText.Contains('@'))
        {
            int assemblyIndex = splitText.FindIndex(s => s.Contains("@"));
            typeText = string.Join(".", splitText.Take(assemblyIndex + 1));
            memberNames = splitText.Skip(assemblyIndex + 1).ToArray();
        }
        else
        {
            typeText = splitText[0];
            memberNames = splitText.Skip(1).ToArray();
        }

        if (memberNames.Length <= 0)
        {
            errorMessage = "Missing member";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    internal static bool TryParseType(string text, [NotNullWhen(true)] out Type? type, out string errorMessage)
    {
        if (TypeNameRegex.Match(text) is not { Success: true } match)
        {
            type = null;
            errorMessage = "Parsing type name failed";
            return false;
        }

        string typeNameMatched = match.Groups[1].Value;

        if (TypeCache.TryGetValue(typeNameMatched, out type))
        {
            errorMessage = string.Empty;
            return true;
        }

        var types = typeof(Game).Assembly.GetTypes();

        // 1. Search for exactly full name
        var matchedTypes = types.Where(t => t.FullName == text).ToArray();
        // 2. Search for exact type name
        if (matchedTypes.IsEmpty())
            matchedTypes = types.Where(t => t.Name == text).ToArray();

        if (matchedTypes.IsEmpty())
        {
            type = null;
            errorMessage = "Parsing type not found";
            return false;
        }

        type = matchedTypes.First();
        TypeCache[typeNameMatched] = type;

        errorMessage = string.Empty;
        return true;
    }

    internal static object? GetMemberValue(Type? type, object? obj, IEnumerable<string> memberNames, out string? errorMessage)
    {
        foreach (string memberName in memberNames)
        {
            if (type.GetGetMethod(memberName) is { } getMethodInfo)
            {
                if (getMethodInfo.IsStatic)
                    obj = getMethodInfo.Invoke(null, null);
                else if (obj != null)
                    obj = getMethodInfo.Invoke(obj, null);
            }
            else if (type.GetFieldInfo(memberName) is { } fieldInfo)
            {
                if (fieldInfo.IsStatic)
                    obj = fieldInfo.GetValue(null);
                else if (obj != null)
                    obj = fieldInfo.GetValue(obj);
            }
            else if (MethodRegex.Match(memberName) is { Success: true } match && type.GetMethodInfo(match.Groups[1].Value) is { } methodInfo)
            {
                if (EnforceLegal)
                {
                    errorMessage = $"{memberName}: Calling methods is illegal when enforce legal";
                    return null;
                }

                if (match.Groups[2].Value.IsNotNullOrWhiteSpace() || methodInfo.GetParameters().Length > 0)
                {
                    errorMessage = $"{memberName}: Only method without parameters is supported";
                    return null;
                }

                if (methodInfo.ReturnType == typeof(void))
                {
                    errorMessage = $"{memberName}: Method return void is not supported";
                    return null;
                }

                if (methodInfo.IsStatic)
                    obj = methodInfo.Invoke(null, null);
                else if (obj != null)
                    obj = methodInfo.Invoke(obj, null);
            }
            else
            {
                if (obj == null)
                {
                    errorMessage = $"{type.FullName}.{memberName} member not found";
                    return null;
                }
                else
                {
                    errorMessage = $"{obj.GetType().FullName}.{memberName} member not found";
                    return null;
                }
            }

            if (obj == null)
            {
                errorMessage = null;
                return null;
            }

            type = obj.GetType();
        }

        errorMessage = null;
        return obj;
    }

    private static string FormatValue(object? obj, string? helperMethodName, int decimals)
    {
        if (obj == null)
        {
            return string.Empty;
        }

        bool invalidParameter = false;
        if (helperMethodName != null && HelperMethods.TryGetValue(helperMethodName, out var method))
        {
            if (method(obj, decimals, out string? formattedValue))
                return formattedValue;

            invalidParameter = true;
        }

        return $"{AutoFormatter(obj, decimals)}{(invalidParameter ? $",\n not a valid parameter of {helperMethodName}" : "")}";
    }

    private static string AutoFormatter(object obj, int decimals)
    {
        switch (obj)
        {
        case Vec2 vec2:
            return vec2.ToSimpleString(decimals);
        case float floatValue:
            return floatValue.ToFormattedString(decimals);
        case Scene scene:
            return $"{scene}";
        case Actor actor:
            return $"{actor}";
        }

        if (obj is IEnumerable enumerable and not IEnumerable<char>)
        {
            bool compressed = enumerable is IEnumerable<Actor>;
            string separator = compressed ? ",\n " : ", ";
            return IEnumerableToString(enumerable, separator, compressed);
        }

        return $"{obj}";
    }

    private static string IEnumerableToString(IEnumerable enumerable, string separator, bool compressed)
    {
        StringBuilder sb = new();
        if (!compressed)
        {
            foreach (object obj in enumerable)
            {
                if (sb.Length > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(obj);
            }

            return sb.ToString();
        }

        Dictionary<string, int> keyValuePairs = new();
        foreach (object obj in enumerable)
        {
            string? str = obj.ToString();
            if (str != null && !keyValuePairs.TryAdd(str, 1))
            {
                keyValuePairs[str]++;
            }
        }

        foreach (string key in keyValuePairs.Keys)
        {
            if (sb.Length > 0)
            {
                sb.Append(separator);
            }

            if (keyValuePairs[key] == 1)
            {
                sb.Append(key);
            }
            else
            {
                sb.Append($"{key} * {keyValuePairs[key]}");
            }
        }

        return sb.ToString();
    }
}
