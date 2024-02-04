using Celeste64.TAS.Input.Commands;
using Celeste64.TAS.Util;
using Sledge.Formats.Map.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Celeste64.TAS;

public static class CustomInfo {
    // private static readonly Regex LuaRegex = new(@"\[\[(.+?)\]\]", RegexOptions.Compiled);
    private static readonly Regex BraceRegex = new(@"\{(.+?)\}", RegexOptions.Compiled);
    private static readonly Regex TypeNameRegex = new(@"^([.\w=+<>]+)(\[(.+?)\])?(@([^.]*))?$", RegexOptions.Compiled);
    private static readonly Regex TypeNameSeparatorRegex = new(@"^[.+]", RegexOptions.Compiled);
    private static readonly Regex MethodRegex = new(@"^(.+)\((.*)\)$", RegexOptions.Compiled);
    private static readonly Dictionary<string, Type> AllTypes = new();
    private static readonly Dictionary<string, List<Type>> CachedParsedTypes = new();
    private static bool EnforceLegal => EnforceLegalCommand.EnabledWhenRunning; // && !AssertCommand.Running;

    public delegate bool HelperMethod(object obj, int decimals, out string formattedValue);

    // return true if obj is of expected parameter type. otherwise we call AutoFormatter
    private static readonly Dictionary<string, HelperMethod> HelperMethods = new();

    internal static void CollectAllTypeInfo() {
        AllTypes.Clear();
        CachedParsedTypes.Clear();

        // foreach (Type type in ModUtils.GetTypes()) {
        //     if (type.FullName is { } fullName) {
        //         string assemblyName = type.Assembly.GetName().Name;
        //         string modName = ConsoleEnhancements.GetModName(type);
        //         AllTypes[$"{fullName}@{assemblyName}"] = type;
        //         AllTypes[$"{fullName}@{modName}"] = type;
        //
        //         if (!fullName.StartsWith("Celeste.Mod.Everest+Events")) {
        //             string fullNameAlternative = fullName.Replace("+", ".");
        //             AllTypes[$"{fullNameAlternative}@{assemblyName}"] = type;
        //             AllTypes[$"{fullNameAlternative}@{modName}"] = type;
        //         }
        //     }
        // }

        foreach (var type in typeof(Game).Assembly.GetTypes())
        {
            if (type.FullName is not { } fullName) continue;

            string fullNameAlternative = fullName.Replace("+", ".");
            AllTypes[$"{fullName}"] = type;
            AllTypes[$"{fullNameAlternative}"] = type;
        }
    }

    internal static void InitializeHelperMethods() {
        HelperMethods.Add("toFrame()", HelperMethod_toFrame);
        // HelperMethods.Add("toPixelPerFrame()", HelperMethod_toPixelPerFrame);
    }

    public static string GetInfo(int? decimals = null) {
        decimals ??= Save.Instance.InfoHudDecimals;
        Dictionary<string, List<Actor>> cachedActors = new();

        return ParseTemplate(Save.Instance.InfoHudShowCustomTemplate, decimals.Value, cachedActors, false);
    }

    public static string ParseTemplate(string template, int decimals, Dictionary<string, List<Actor>> cachedActors, bool consoleCommand) {
        List<Actor> GetCachedOrFindActors(Type type, string actorId, Dictionary<string, List<Actor>> dictionary) {
            string actorText = $"{type.FullName}{actorId}";
            List<Actor> actors;
            if (dictionary.TryGetValue(actorText, out List<Actor>? value)) {
                actors = value;
            } else {
                actors = FindActors(type, actorId);
                dictionary[actorText] = actors;
            }

            return actors;
        }

        return BraceRegex.Replace(template, match => {
            string matchText = match.Groups[1].Value;

            if (!TryParseMemberNames(matchText, out string typeText, out List<string> memberNames, out string errorMessage)) {
                return errorMessage;
            }

            if (!TryParseTypes(typeText, out List<Type> types, out string actorId, out errorMessage)) {
                return errorMessage;
            }

            string lastMemberName = memberNames.Last();
            string lastCharacter = lastMemberName.Substring(lastMemberName.Length - 1, 1);
            if (lastCharacter is ":" or "=") {
                lastMemberName = lastMemberName.Substring(0, lastMemberName.Length - 1).Trim();
                memberNames[memberNames.Count - 1] = lastMemberName;
            }

            string helperMethod = "";
            if (HelperMethods.ContainsKey(lastMemberName)) {
                helperMethod = lastMemberName;
                memberNames = memberNames.SkipLast().ToList();
            }

            bool moreThanOneActor = types.Where(type => type.IsSameOrSubclassOf(typeof(Actor)))
                .SelectMany(type => GetCachedOrFindActors(type, actorId, cachedActors)).Count() > 1;

            bool foundValid = false;
            List<string> result = types.Select(type => {
                if (memberNames.IsNotEmpty() && (
                        type.GetGetMethod(memberNames.First()) is {IsStatic: true} ||
                        type.GetFieldInfo(memberNames.First()) is {IsStatic: true} ||
                        (MethodRegex.Match(memberNames.First()) is {Success: true} match &&
                         type.GetMethodInfo(match.Groups[1].Value) is {IsStatic: true})
                    )) {
                    foundValid = true;
                    return FormatValue(GetMemberValue(type, null, memberNames), helperMethod, decimals);
                }

                if (Game.Scene is World world) {
                    if (type.IsSameOrSubclassOf(typeof(Actor))) {
                        List<Actor> actors = GetCachedOrFindActors(type, actorId, cachedActors);

                        if (actors == null) {
                            return "Ignore NPE Warning";
                        }

                        foundValid = true;
                        return string.Join("", actors.Select(actor => {
                            string value = FormatValue(GetMemberValue(type, actor, memberNames), helperMethod, decimals);

                            if (moreThanOneActor) {
                                value = $"\n{value}";
                            }

                            return value;
                        }));
                    } else if (type == typeof(World)) {
                        foundValid = true;
                        return FormatValue(GetMemberValue(type, world, memberNames), helperMethod, decimals);
                    }
                }

                return string.Empty;
            }).Where(s => s.IsNotNullOrEmpty()).ToList();

            if (!foundValid) return $"No instance of {typeText} found";

            string prefix = lastCharacter switch {
                "=" => matchText,
                ":" => $"{matchText} ",
                _ => ""
            };

            string separator = types.First().IsSameOrSubclassOf(typeof(Actor)) ? "" : " ";
            if (consoleCommand && separator.IsEmpty() && result.IsNotEmpty()) {
                result[0] = result[0].TrimStart();
            }

            return $"{prefix}{string.Join(separator, result)}";
        });

        // return LuaRegex.Replace(result, match => {
        //     if (EnforceLegal) {
        //         return "Evaluate lua code is illegal when enforce legal";
        //     }
        //
        //     string code = match.Groups[1].Value;
        //     object[] objects = EvalLuaCommand.EvalLuaImpl(code);
        //     return objects == null ? "null" : string.Join(", ", objects.Select(o => o?.ToString() ?? "null"));
        // });
    }

    public static bool TryParseMemberNames(string matchText, out string typeText, out List<string> memberNames, out string errorMessage) {
        typeText = errorMessage = "";
        memberNames = new List<string>();

        List<string> splitText = matchText.Split('.').Select(s => s.Trim()).Where(s => s.IsNotEmpty()).ToList();
        if (splitText.Count <= 1) {
            errorMessage = "missing member";
            return false;
        }

        if (matchText.Contains("@")) {
            int assemblyIndex = splitText.FindIndex(s => s.Contains("@"));
            typeText = string.Join(".", splitText.Take(assemblyIndex + 1));
            memberNames = splitText.Skip(assemblyIndex + 1).ToList();
        } else {
            typeText = splitText[0];
            memberNames = splitText.Skip(1).ToList();
        }

        if (memberNames.Count <= 0) {
            errorMessage = "missing member";
            return false;
        }

        return true;
    }

    public static bool TryParseType(string text, out Type type, out string actorId, out string errorMessage) {
        TryParseTypes(text, out List<Type> types, out actorId, out errorMessage);

        if (types.IsEmpty()) {
            type = null;
            return false;
        } else {
            type = types.First();
            return true;
        }
    }

    public static bool TryParseTypes(string text, out List<Type> types) {
        return TryParseTypes(text, out types, out _, out _);
    }

    public static bool TryParseTypes(string text, out List<Type> types, out string actorId) {
        return TryParseTypes(text, out types, out actorId, out _);
    }

    public static bool TryParseTypes(string text, out List<Type> types, out string actorId, out string errorMessage) {
        types = new List<Type>();
        actorId = "";
        errorMessage = "";

        if (!TryParseTypeName(text, out string typeNameMatched, out string typeNameWithAssembly, out actorId)) {
            errorMessage = "parsing type name failed";
            return false;
        }

        if (CachedParsedTypes.Keys.Contains(typeNameMatched)) {
            types = CachedParsedTypes[typeNameMatched];
        } else {
            // find the full type name
            List<string> matchTypeNames = AllTypes.Keys.Where(name => name.StartsWith(typeNameMatched)).ToList();

            string typeName = TypeNameSeparatorRegex.Replace(typeNameMatched, "");
            if (matchTypeNames.IsEmpty()) {
                // find the part of type name
                matchTypeNames = AllTypes.Keys.Where(name => name.Contains($".{typeName}")).ToList();
            }

            if (matchTypeNames.IsEmpty()) {
                // find the nested type name
                matchTypeNames = AllTypes.Keys.Where(name => name.Contains($"+{typeName}")).ToList();
            }

            // one type can correspond to two keys (..@assemblyName and ..@modName), so we need Distinct<Type>()
            types = matchTypeNames.Select(name => AllTypes[name]).Distinct<Type>().ToList();
            CachedParsedTypes[typeNameMatched] = types;
        }

        if (types.IsEmpty()) {
            errorMessage = $"{typeNameMatched} not found";
            return false;
        } else {
            return true;
        }
    }

    private static bool TryParseTypeName(string text, out string typeNameMatched, out string typeNameWithAssembly, out string actorId) {
        typeNameMatched = "";
        typeNameWithAssembly = "";
        actorId = "";
        if (TypeNameRegex.Match(text) is {Success: true} match) {
            typeNameMatched = match.Groups[1].Value;
            typeNameWithAssembly = $"{typeNameMatched}@{match.Groups[5].Value}";
            typeNameWithAssembly = typeNameWithAssembly switch {
                "Theo@" => "TheoCrystal@",
                "Jellyfish@" => "Glider@",
                _ => typeNameWithAssembly
            };
            actorId = match.Groups[3].Value;
            return true;
        } else {
            return false;
        }
    }

    public static object GetMemberValue(Type type, object obj, List<string> memberNames, bool setCommand = false) {
        foreach (string memberName in memberNames) {
            if (type.GetGetMethod(memberName) is { } getMethodInfo) {
                if (getMethodInfo.IsStatic) {
                    obj = getMethodInfo.Invoke(null, null);
                } else if (obj != null) {
                    obj = getMethodInfo.Invoke(obj, null);
                }
            } else if (type.GetFieldInfo(memberName) is { } fieldInfo) {
                if (fieldInfo.IsStatic) {
                    obj = fieldInfo.GetValue(null);
                } else if (obj != null) {
                    obj = fieldInfo.GetValue(obj);
                }
            } else if (MethodRegex.Match(memberName) is {Success: true} match && type.GetMethodInfo(match.Groups[1].Value) is { } methodInfo) {
                if (EnforceLegal) {
                    return $"{memberName}: Calling methods is illegal when enforce legal";
                } else if (match.Groups[2].Value.IsNotNullOrWhiteSpace() || methodInfo.GetParameters().Length > 0) {
                    return $"{memberName}: Only method without parameters is supported";
                } else if (methodInfo.ReturnType == typeof(void)) {
                    return $"{memberName}: Method return void is not supported";
                } else if (methodInfo.IsStatic) {
                    obj = methodInfo.Invoke(null, null);
                } else if (obj != null) {
                    obj = methodInfo.Invoke(obj, null);
                }
            } else {
                if (obj == null) {
                    return $"{type.FullName}.{memberName} member not found";
                } else {
                    return $"{obj.GetType().FullName}.{memberName} member not found";
                }
            }

            if (obj == null) {
                return null;
            }

            type = obj.GetType();
        }

        return obj;
    }

    public static List<Actor> FindActors(Type type, string actorId) {
        if (Game.Scene is not World world) return [];

        if (!world.tracked.TryGetValue(type, out var list))
        {
            world.tracked[type] = list = new();
            foreach (var actor in world.Actors)
                if (actor.GetType() == type)
                    list.Add(actor);
            world.trackedTypes.Add(type);
        }
        return list;
    }

    public static string FormatValue(object obj, string helperMethodName, int decimals) {
        if (obj == null) {
            return string.Empty;
        }

        bool invalidParameter = false;
        if (HelperMethods.TryGetValue(helperMethodName, out HelperMethod method)) {
            if (method(obj, decimals, out string formattedValue)) {
                return formattedValue;
            } else {
                invalidParameter = true;
            }
        }

        return $"{AutoFormatter(obj, decimals)}{(invalidParameter ? $",\n not a valid parameter of {helperMethodName}" : "")}";
    }

    public static bool HelperMethod_toFrame(object obj, int decimals, out string formattedValue) {
        if (obj is float floatValue) {
            formattedValue = floatValue.ToFrames().ToString();
            return true;
        }

        formattedValue = "";
        return false;
    }

    // public static bool HelperMethod_toPixelPerFrame(object obj, int decimals, out string formattedValue) {
    //     if (obj is float floatValue) {
    //         formattedValue = GameInfo.ConvertSpeedUnit(floatValue, SpeedUnit.PixelPerFrame).ToString(CultureInfo.InvariantCulture);
    //         return true;
    //     }
    //
    //     if (obj is Vector2 vector2) {
    //         formattedValue = GameInfo.ConvertSpeedUnit(vector2, SpeedUnit.PixelPerFrame).ToSimpleString(decimals);
    //         return true;
    //     }
    //
    //     formattedValue = "";
    //     return false;
    // }

    public static string AutoFormatter(object obj, int decimals) {
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

        if (obj is IEnumerable enumerable and not IEnumerable<char>) {
            bool compressed = enumerable is IEnumerable<Component> or IEnumerable<Actor>;
            string separator = compressed ? ",\n " : ", ";
            return IEnumerableToString(enumerable, separator, compressed);
        }

        // if (obj is Collider collider) {
        //     return ColliderToString(collider);
        // }

        return $"{obj}";
    }

    public static string IEnumerableToString(IEnumerable enumerable, string separator, bool compressed) {
        StringBuilder sb = new();
        if (!compressed) {
            foreach (object o in enumerable) {
                if (sb.Length > 0) {
                    sb.Append(separator);
                }

                sb.Append(o);
            }

            return sb.ToString();
        }

        Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
        foreach (object obj in enumerable) {
            string str = obj.ToString();
            if (keyValuePairs.ContainsKey(str)) {
                keyValuePairs[str]++;
            } else {
                keyValuePairs.Add(str, 1);
            }
        }

        foreach (string key in keyValuePairs.Keys) {
            if (sb.Length > 0) {
                sb.Append(separator);
            }

            if (keyValuePairs[key] == 1) {
                sb.Append(key);
            } else {
                sb.Append($"{key} * {keyValuePairs[key]}");
            }
        }

        return sb.ToString();
    }

    // public static string ColliderToString(Collider collider, int iterationHeight = 1) {
    //     if (collider is Hitbox hitbox) {
    //         return $"Hitbox=[{hitbox.Left},{hitbox.Right}]×[{hitbox.Top},{hitbox.Bottom}]";
    //     }
    //
    //     if (collider is Circle circle) {
    //         if (circle.Position == Vector2.Zero) {
    //             return $"Circle=radius {circle.Radius}";
    //         } else {
    //             return $"Circle=radius {circle.Radius}, offset {circle.Position}";
    //         }
    //     }
    //
    //     if (collider is ColliderList list && iterationHeight > 0) {
    //         return "ColliderList: {" + string.Join("; ", list.colliders.Select(s => ColliderToString(s, iterationHeight - 1))) + "}";
    //     }
    //
    //     return collider.ToString();
    // }
}
