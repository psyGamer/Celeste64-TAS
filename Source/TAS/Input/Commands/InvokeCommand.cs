using Celeste64.TAS.Util;

namespace Celeste64.TAS.Input.Commands;

public class InvokeCommand
{
    private const string LogPrefix = "Invoke Command Failed: ";
    private static readonly object nonReturnObject = new();

    // Invoke, World.Method, Parameters...
    // Invoke, Actor.Method, Parameters...
    // Invoke, Type.StaticMethod, Parameters...
    [Command("Invoke", LegalInFullGame = false)]
    private static void Invoke(string[] args) {
        if (args.Length < 1) {
            return;
        }

        try
        {
            string[] parameters = args.Skip(1).ToArray();

            if (CustomInfo.TryParseMemberNames(args[0], out string? typeText, out string[]? memberNames, out string errorMessage)
             && CustomInfo.TryParseType(typeText, out Type? type, out errorMessage))
            {
                SetCommand.ObjectInfo info = new()
                {
                    Type = type,
                    MemberNames = memberNames,
                    Values = parameters
                };

                if (info.MemberNames.IsEmpty())
                {
                    Log.Warning($"{LogPrefix}No member names specified");
                    return;
                }

                SetCommand.FindObject(ref info);
                InvokeObjectMethod(info);
            }
            else
            {
                Log.Warning($"{LogPrefix}{errorMessage}");
            }
        }
        catch (Exception e)
        {
            Log.Warning($"{LogPrefix}{e}");
        }
    }

    private static object InvokeObjectMethod(SetCommand.ObjectInfo info) {
        if (info.Type.IsSameOrSubclassOf(typeof(Actor)) && info.Obj is List<object> objects) {
            List<object> result = new();
            foreach (object o in objects) {
                if (TryInvokeMethod(o, out object r)) {
                    r ??= "null";
                    result.Add(r);
                }
            }

            return result.IsEmpty() ? nonReturnObject : string.Join("\n", result);
        } else {
            if (TryInvokeMethod(info.Obj, out object r)) {
                return r;
            } else {
                return nonReturnObject;
            }
        }

        bool TryInvokeMethod(object @object, out object? returnObject) {
            if (info.ObjType.GetMethodInfo(info.LastMemberName) is { } methodInfo) {
                var parameterInfos = methodInfo.GetParameters().ToList();
                object?[] parameters = new object[parameterInfos.Count];

                for (int i = 0; i < parameterInfos.Count; i++) {
                    object? convertedObj;
                    var parameterInfo = parameterInfos[i];
                    var parameterType = parameterInfo.ParameterType;

                    if (info.Values.IsEmpty()) {
                        parameters[i] = parameterInfo.HasDefaultValue ? parameterInfo.DefaultValue : SetCommand.Convert(null, parameterType);
                        continue;
                    }

                    if (parameterType == typeof(Vec2)) {
                        string[] array = info.Values[..2];
                        float.TryParse(array.GetValueOrDefault(0), out float x);
                        float.TryParse(array.GetValueOrDefault(1), out float y);
                        convertedObj = new Vector2(x, y);
                        info.Values = info.Values[2..];
                    } else if (parameterType.IsSameOrSubclassOf(typeof(Actor))) {
                        if (CustomInfo.TryParseType(info.Values[0], out Type? entityType, out string errorMessage)) {
                            convertedObj = ((List<Actor>) SetCommand.FindSpecialObject(entityType)!).FirstOrDefault()!;
                        } else {
                            Log.Warning($"{LogPrefix}{errorMessage}");
                            convertedObj = null;
                        }
                    } else if (parameterType == typeof(World)) {
                        convertedObj = Game.Scene as World;
                    } else {
                        convertedObj = SetCommand.Convert(parameters.FirstOrDefault(), parameterType);
                        parameters = parameters[1..];
                    }

                    parameters[i] = convertedObj;
                }

                returnObject = methodInfo.Invoke(@object, parameters);
                return methodInfo.ReturnType != typeof(void);
            } else {
                Log.Warning($"{LogPrefix}{info.ObjType.FullName}.{info.LastMemberName} member not found");
                returnObject = nonReturnObject;
                return false;
            }
        }
    }
}
