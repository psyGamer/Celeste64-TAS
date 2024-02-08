using Celeste64.TAS.Util;
using System.Globalization;
using System.Reflection;

namespace Celeste64.TAS.Input.Commands;

public static class SetCommand
{
    internal struct ObjectInfo()
    {
        public required Type Type;
        public required string[] MemberNames = [];
        public required string[] Values = [];

        internal object? Obj = null;
        internal Type ObjType;

        internal object? StructObj = null;
        internal string LastMemberName = string.Empty;

        internal ObjectInfo Clone() {
            return new ObjectInfo
            {
                Type = Type,
                MemberNames = MemberNames,
                Values = Values,

                Obj = Obj,
                ObjType = ObjType,

                StructObj = StructObj,
                LastMemberName = LastMemberName,
            };
        }
    }

    private const string LogPrefix = "Set Command Failed: ";

    // Set, Actor.Field, Value
    // Set, Class.Static.Instance, Value
    [Command("Set", LegalInFullGame = false)]
    private static void Set(string[] args)
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
                ObjectInfo info = new()
                {
                    Type = type,
                    MemberNames = memberNames,
                    Values = parameters
                };

                // FindObject(type, memberNames, parameters, out var obj);
                FindObject(ref info);
                SetObjectMember(info);
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

    // internal static bool FindObject(Type? type, string[] memberNames, string[] values, out ObjectInfo info, object? structObj = null)
    internal static bool FindObject(ref ObjectInfo info)
    {
        if (info.MemberNames.IsEmpty() || info.Values.IsEmpty() && info.StructObj == null)
        {
            return false;
        }

        info.LastMemberName = info.MemberNames[^1];
        info.MemberNames = info.MemberNames[..^1];

        if (info.MemberNames.IsEmpty() &&
            (info.Type.GetGetMethod(info.LastMemberName) is { IsStatic: true } ||
             info.Type.GetFieldInfo(info.LastMemberName) is { IsStatic: true }))
        {
            info.ObjType = info.Type;
        }
        else if (info.MemberNames.IsNotEmpty() &&
                 (info.Type.GetGetMethod(info.MemberNames.First()) is { IsStatic: true } ||
                  info.Type.GetFieldInfo(info.MemberNames.First()) is { IsStatic: true }))
        {
            info.Obj = CustomInfo.GetMemberValue(info.Type, null, info.MemberNames, out string? errorMessage);
            if (info.Obj == null)
            {
                Log.Warning($"{LogPrefix}{info.Type.FullName} member value is null");
                return false;
            }

            if (errorMessage != null)
            {
                Log.Warning($"{LogPrefix}{errorMessage}");
                return false;
            }

            info.ObjType = info.Obj.GetType();
        }
        else
        {
            info.Obj = FindSpecialObject(info.Type);
            if (info.Obj == null)
            {
                Log.Warning($"{LogPrefix}{info.Type.FullName} object is not found");
                return false;
            }

            if (info.Type.IsSameOrSubclassOf(typeof(Actor)) && info.Obj is List<Actor> actors)
            {
                if (actors.IsEmpty())
                {
                    Log.Warning($"{LogPrefix}{info.Type.FullName} actor is not found");
                    return false;
                }

                List<object> memberValues = [];
                foreach (var actor in actors)
                {
                    object? memberValue = CustomInfo.GetMemberValue(info.Type, actor, info.MemberNames, out string? errorMessage);
                    if (errorMessage != null)
                    {
                        Log.Warning($"{LogPrefix}{errorMessage}");
                        return false;
                    }

                    if (memberValue != null)
                    {
                        memberValues.Add(memberValue);
                    }
                }

                if (memberValues.IsEmpty())
                {
                    return false;
                }

                info.Obj = memberValues;
                info.ObjType = memberValues.First().GetType();
            }
            else
            {
                info.Obj = CustomInfo.GetMemberValue(info.Type, info.Obj, info.MemberNames, out string? errorMessage);
                if (info.Obj == null)
                {
                    Log.Warning($"{LogPrefix}{info.Type.FullName} member value is null");
                    return false;
                }

                if (errorMessage != null)
                {
                    Log.Warning($"{LogPrefix}{errorMessage}");
                    return false;
                }

                info.ObjType = info.Obj.GetType();
            }
        }

        return true;
    }

    private static bool SetObjectMember(ObjectInfo info)
    {
        if (info.Type.IsSameOrSubclassOf(typeof(Actor)) && info.Obj is List<object> objects)
        {
            bool success = false;
            objects.ForEach(o => success |= SetMember(o));
            return success;
        }

        return SetMember(info.Obj);

        bool SetMember(object? @object)
        {
            if (!TrySetMember(info, @object))
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

                var newInfo = info.Clone() with
                {
                    Values = vecValues,
                    StructObj = vecValues.IsEmpty() ? @object : null
                };
                if (!FindObject(ref newInfo)) return false;

                return SetObjectMember(newInfo);
            }

            return true;
        }
    }

    private static bool TrySetMember(ObjectInfo info, object? obj)
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
                    "X" => actor.Position with { X = value },
                    "Y" => actor.Position with { Y = value },
                    "Z" => actor.Position with { Z = value },
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
                    "XY" => actor.Position with { X = value1, Y = value2 },
                    "XZ" => actor.Position with { Y = value1, Z = value2 },
                    "YZ" => actor.Position with { Y = value1, Z = value2 },
                    _ => actor.Position
                };
                return true;
            }
            case "XYZ" when info.Values.Length == 3:
            {
                if (!float.TryParse(info.Values[0], out float value1)) return false;
                if (!float.TryParse(info.Values[1], out float value2)) return false;
                if (!float.TryParse(info.Values[2], out float value3)) return false;
                actor.Position = new Vec3(value1, value2, value3);
                return true;
            }
            }
        }
        else if (obj is Vec3)
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
                    f_X.SetValue(obj, value);
                    break;
                case "Y":
                    f_Y.SetValue(obj, value);
                    break;
                case "Z":
                    f_Z.SetValue(obj, value);
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
                    f_X.SetValue(obj, value1);
                    f_Y.SetValue(obj, value2);
                    break;
                    case "XZ":
                    f_X.SetValue(obj, value1);
                    f_Z.SetValue(obj, value2);
                    break;
                    case "YZ":
                    f_Y.SetValue(obj, value1);
                    f_Z.SetValue(obj, value2);
                        break;
                }
                return true;
            }
            case "XYZ" when info.Values.Length == 3:
            {
                if (!float.TryParse(info.Values[0], out float value1)) return false;
                if (!float.TryParse(info.Values[1], out float value2)) return false;
                if (!float.TryParse(info.Values[2], out float value3)) return false;
                f_X.SetValue(obj, value1);
                f_Y.SetValue(obj, value2);
                f_Z.SetValue(obj, value3);
                return true;
            }
            }
        }

        if (info.ObjType.GetPropertyInfo(info.LastMemberName) is { } property && property.GetSetMethod(true) is { } setMethod)
        {
            object? value = info.StructObj ?? ConvertType(info.Values, property.PropertyType);
            if (property.PropertyType.IsStructType() && Nullable.GetUnderlyingType(property.PropertyType) == null && value == null)
            {
                Log.Warning($"{LogPrefix}{property.PropertyType.FullName} member value is null");
                return false;
            }
            setMethod.Invoke(obj, [value]);
        }
        else if (info.ObjType.GetFieldInfo(info.LastMemberName) is { } field)
        {
            object? value = info.StructObj ?? ConvertType(info.Values, field.FieldType);
            if (field.FieldType.IsStructType() && Nullable.GetUnderlyingType(field.FieldType) == null && value == null)
            {
                Log.Warning($"{LogPrefix}{field.FieldType.FullName} member value is null");
                return false;
            }
            field.SetValue(obj, value);
        }
        else
        {
            return false;
        }

        return true;
    }

    internal static object? FindSpecialObject(Type type)
    {
        if (type.IsSameOrSubclassOf(typeof(Actor)))
        {
            if (Game.Scene is World world)
                return world.All(type);
            return (List<Actor>) [];
        }
        else if (type == typeof(World) && Game.Scene is World world)
        {
            return world;
        }
        else
        {
            return null;
        }
    }

    internal static object? Convert(object? value, Type type)
    {
        try
        {
            if (value is null or (string and ("" or "null")))
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            if (type == typeof(string) && value is "\"\"")
            {
                return string.Empty;
            }

            return type.IsEnum ? Enum.Parse(type, (string)value, true) : System.Convert.ChangeType(value, type);
        }
        catch
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }

    private static object? ConvertType(string[] values, Type type)
    {
        Type nullableType = type;
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
        {
            // TODO: Fix string parameters
            return string.Join(" ", values);
        }

        if (values.Length == 2 && type == typeof(Vec2))
        {
            if (!float.TryParse(values[0], out float x)) return null;
            if (!float.TryParse(values[1], out float y)) return null;
            return new Vec2(x, y);
        }

        if (values.Length == 3 && type == typeof(Vec3))
        {
            if (!float.TryParse(values[0], out float x)) return null;
            if (!float.TryParse(values[1], out float y)) return null;
            if (!float.TryParse(values[2], out float z)) return null;
            return new Vec3(x, y, z);
        }

        if (values.Length == 1)
        {
            if (type == typeof(Random))
            {
                if (!int.TryParse(values[0], out int seed)) return null;
                return new Random(seed);
            }

            return Convert(values[0], nullableType);
        }

        if (values.Length >= 2)
        {
            object? instance = Activator.CreateInstance(type);
            var members = type.GetMembers().Where(info => (info.MemberType & (MemberTypes.Field | MemberTypes.Property)) != 0).ToArray();

            for (int i = 0; i < members.Length && i < values.Length; i++)
            {
                string memberName = members[i].Name;
                if (type.GetField(memberName) is { } fieldInfo)
                {
                    fieldInfo.SetValue(instance, Convert(values[i], fieldInfo.FieldType));
                }
                else if (type.GetProperty(memberName) is { } propertyInfo)
                {
                    propertyInfo.SetValue(instance, Convert(values[i], propertyInfo.PropertyType));
                }
            }

            return instance;
        }

        return default;
    }
}
