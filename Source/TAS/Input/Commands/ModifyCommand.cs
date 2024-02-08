using Celeste64.TAS.Util;
using System.Globalization;

namespace Celeste64.TAS.Input.Commands;

public class ModifyCommand
{
    private const string LogPrefix = "Modify Command Failed: ";

    // Modify, World.Field, Value
    // Modify, Actor.Field, Value
    // Modify, Type.Static.Instance, Value
    [Command("Modify", LegalInFullGame = false)]
    private static void Modify(string[] args)
    {
        if (args.Length < 2)
        {
            return;
        }

        try
        {
            string[] parameters = args.Skip(1).ToArray();

            if (CustomInfo.TryParseMemberNames(args[0], out string? typeText, out string[]? memberNames, out string errorMessage)
             && CustomInfo.TryParseType(typeText, out Type? type, out errorMessage))
            {
                SetCommand.ObjectInfo info = new() { Type = type, MemberNames = memberNames, Values = parameters };

                if (info.MemberNames.IsEmpty() || info.Values.IsEmpty() && info.StructObj == null)
                {
                    Log.Warning($"{LogPrefix}No member names or values specified");
                    return;
                }

                SetCommand.FindObject(ref info);
                ModifyObjectMember(info);
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

    private static bool ModifyObjectMember(SetCommand.ObjectInfo info)
    {
        if (info.Type.IsSameOrSubclassOf(typeof(Actor)) && info.Obj is List<object> objects)
        {
            bool success = false;
            objects.ForEach(o => success |= ModifyMember(o));
            return success;
        }

        return ModifyMember(info.Obj);

        bool ModifyMember(object? @object)
        {
            if (!TryModifyMember(info, @object))
            {
                Log.Warning($"{LogPrefix}{info.ObjType.FullName}.{info.LastMemberName} member not found");
                return false;
            }

            // After modifying the struct, we also need to update the object owning the struct
            if (info.MemberNames.IsNotEmpty() && info.ObjType.IsStructType())
            {
                string[] vecValues = @object switch
                {
                    Vec2 vec => [vec.X.ToString(CultureInfo.InvariantCulture), vec.Y.ToString(CultureInfo.InvariantCulture)],
                    Vec3 vec => [vec.X.ToString(CultureInfo.InvariantCulture), vec.Y.ToString(CultureInfo.InvariantCulture), vec.Z.ToString(CultureInfo.InvariantCulture)],
                    _ => []
                };

                var newInfo = info.Clone() with { Values = vecValues, StructObj = vecValues.IsEmpty() ? @object : null };
                if (!SetCommand.FindObject(ref newInfo)) return false;

                return ModifyObjectMember(newInfo);
            }

            return true;
        }
    }

    private static bool TryModifyMember(SetCommand.ObjectInfo info, object? obj)
    {
        if (obj is Actor actor)
        {
            switch (info.LastMemberName)
            {
            case "X" or "Y" or "Z" when info.Values.Length == 1:
            {
                if (!float.TryParse(info.Values[0], out float value)) return false;
                actor.Position = info.LastMemberName switch
                {
                    "X" => actor.Position with { X = actor.Position.X + value },
                    "Y" => actor.Position with { Y = actor.Position.Y + value },
                    "Z" => actor.Position with { Z = actor.Position.Z + value },
                    _ => actor.Position
                };
                return true;
            }
            case "XY" or "XZ" or "YZ" when info.Values.Length == 2:
            {
                if (!float.TryParse(info.Values[0], out float value1)) return false;
                if (!float.TryParse(info.Values[1], out float value2)) return false;
                actor.Position = info.LastMemberName switch
                {
                    "XY" => actor.Position with { X = actor.Position.X + value1, Y = actor.Position.Y + value2 },
                    "XZ" => actor.Position with { Y = actor.Position.Y + value1, Z = actor.Position.Z + value2 },
                    "YZ" => actor.Position with { Y = actor.Position.Y + value1, Z = actor.Position.Z + value2 },
                    _ => actor.Position
                };
                return true;
            }
            case "XYZ" when info.Values.Length == 3:
            {
                if (!float.TryParse(info.Values[0], out float value1)) return false;
                if (!float.TryParse(info.Values[1], out float value2)) return false;
                if (!float.TryParse(info.Values[2], out float value3)) return false;
                actor.Position += new Vec3(value1, value2, value3);
                return true;
            }
            }
        }
        else if (obj is Vec3 vec)
        {
            var f_X = typeof(Vec3).GetField(nameof(Vec3.X))!;
            var f_Y = typeof(Vec3).GetField(nameof(Vec3.Y))!;
            var f_Z = typeof(Vec3).GetField(nameof(Vec3.Z))!;

            switch (info.LastMemberName)
            {
            case "X" or "Y" or "Z" when info.Values.Length == 1:
            {
                if (!float.TryParse(info.Values[0], out float value)) return false;
                switch (info.LastMemberName)
                {
                case "X":
                    f_X.SetValue(obj, vec.X + value);
                    break;
                case "Y":
                    f_Y.SetValue(obj, vec.Y + value);
                    break;
                case "Z":
                    f_Z.SetValue(obj, vec.Z + value);
                    break;
                }

                return true;
            }
            case "XY" or "XZ" or "YZ" when info.Values.Length == 2:
            {
                if (!float.TryParse(info.Values[0], out float value1)) return false;
                if (!float.TryParse(info.Values[1], out float value2)) return false;
                switch (info.LastMemberName)
                {
                case "XY":
                    f_X.SetValue(obj, vec.X + value1);
                    f_Y.SetValue(obj, vec.Y + value2);
                    break;
                case "XZ":
                    f_X.SetValue(obj, vec.X + value1);
                    f_Z.SetValue(obj, vec.Z + value2);
                    break;
                case "YZ":
                    f_Y.SetValue(obj, vec.Y + value1);
                    f_Z.SetValue(obj, vec.Z + value2);
                    break;
                }

                return true;
            }
            case "XYZ" when info.Values.Length == 3:
            {
                if (!float.TryParse(info.Values[0], out float value1)) return false;
                if (!float.TryParse(info.Values[1], out float value2)) return false;
                if (!float.TryParse(info.Values[2], out float value3)) return false;
                f_X.SetValue(obj, vec.X + value1);
                f_Y.SetValue(obj, vec.Y + value2);
                f_Z.SetValue(obj, vec.Z + value3);
                return true;
            }
            }
        }

        if (info.ObjType.GetPropertyInfo(info.LastMemberName) is { } property && property.GetSetMethod(true) is { } setMethod && property.GetGetMethod(true) is { } getMethod)
        {
            object? value = info.StructObj ?? SetCommand.ConvertType(info.Values, property.PropertyType);
            if (property.PropertyType.IsStructType() && Nullable.GetUnderlyingType(property.PropertyType) == null && value == null)
            {
                Log.Warning($"{LogPrefix}{property.PropertyType.FullName} member value is null");
                return false;
            }

            object? currValue = getMethod.Invoke(obj, []);
            switch (value)
            {
            case int valueInt:
                setMethod.Invoke(obj, [(int)currValue! + valueInt]);
                break;
            case float valueFloat:
                setMethod.Invoke(obj, [(float)currValue! + valueFloat]);
                break;
            case double valueDouble:
                setMethod.Invoke(obj, [(double)currValue! + valueDouble]);
                break;
            default:
            {
                if (property.PropertyType.GetMethod("op_Addition") is { } addMethod)
                    setMethod.Invoke(obj, [addMethod.Invoke(value, [currValue])]);
                else
                    return false;
                break;
            }
            }
        }
        else if (info.ObjType.GetFieldInfo(info.LastMemberName) is { } field)
        {
            object? value = info.StructObj ?? SetCommand.ConvertType(info.Values, field.FieldType);
            if (field.FieldType.IsStructType() && Nullable.GetUnderlyingType(field.FieldType) == null && value == null)
            {
                Log.Warning($"{LogPrefix}{field.FieldType.FullName} member value is null");
                return false;
            }

            object? currValue = field.GetValue(obj);
            switch (value)
            {
            case int valueInt:
                field.SetValue(obj, (int)currValue! + valueInt);
                break;
            case float valueFloat:
                field.SetValue(obj, (float)currValue! + valueFloat);
                break;
            case double valueDouble:
                field.SetValue(obj, (double)currValue! + valueDouble);
                break;
            default:
            {
                if (field.FieldType.GetMethod("op_Addition") is { } addMethod)
                    field.SetValue(obj, addMethod.Invoke(value, [currValue]));
                else
                    return false;
                break;
            }
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}
