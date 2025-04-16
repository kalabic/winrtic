using System.Text.Json.Nodes;
using System.Text.Json;
using OpenRTIC.BasicDevices;

namespace OpenRTIC.Config;

public class ValueParser
{
    static public int AssertNodeParamIsNullOrArray(JsonNode node, string paramName)
    {
        var paramNode = node![paramName]!;
        if ((paramNode is null) || (paramNode.GetValueKind() == System.Text.Json.JsonValueKind.Undefined))
        {
            return 0;
        }

        if ((paramNode is not null) && (paramNode.GetValueKind() == JsonValueKind.Array))
        {
            return 1;
        }

        DeviceNotifications.Error($" * Error: Value '{paramName}' not an array.");
        return -1;
    }

    static public int AssertNodeParamIsNullOrObject(JsonNode node, string paramName)
    {
        var paramNode = node![paramName]!;
        if ((paramNode is null) || (paramNode.GetValueKind() == System.Text.Json.JsonValueKind.Undefined))
        {
            return 0;
        }

        if ((paramNode is not null) && (paramNode.GetValueKind() == JsonValueKind.Object))
        {
            return 1;
        }

        DeviceNotifications.Error($" * Error: Value '{paramName}' not an object.");
        return -1;
    }

    static public int AssertNodeParamIsNullOrString(JsonNode node, string paramName)
    {
        var paramNode = node![paramName]!;
        if ((paramNode is null) || (paramNode.GetValueKind() == System.Text.Json.JsonValueKind.Undefined))
        {
            return 0;
        }

        if ((paramNode is not null) && (paramNode.GetValueKind() == JsonValueKind.String))
        {
            return 1;
        }

        DeviceNotifications.Error($" * Error: Value '{paramName}' not a string.");
        return -1;
    }

    static public int AssertReadNodeStringParam(JsonNode node, string paramName, Action<string> reader)
    {
        int assert = AssertNodeParamIsNullOrString(node, paramName);
        if (assert == 1)
        {
            reader(node![paramName]!.GetValue<string>());
        }
        return assert;
    }

    static public int AssertReadNodeStringParam(JsonNode node, string paramName, Func<string, int> reader)
    {
        int assert = AssertNodeParamIsNullOrString(node, paramName);
        if (assert == 1)
        {
            return reader(node![paramName]!.GetValue<string>());
        }
        return assert;
    }

    static public int AssertNodeParamIsNullOrBool(JsonNode node, string paramName)
    {
        var paramNode = node![paramName]!;
        if ((paramNode is null) || (paramNode.GetValueKind() == System.Text.Json.JsonValueKind.Undefined))
        {
            return 0;
        }

        if ((paramNode is not null) && ((paramNode.GetValueKind() == JsonValueKind.False) || (paramNode.GetValueKind() == JsonValueKind.True)))
        {
            return 1;
        }

        DeviceNotifications.Error($" * Error: Value '{paramName}' not a bool.");
        return -1;
    }

    static public int AssertReadNodeBoolParam(JsonNode node, string paramName, Action<bool> reader)
    {
        Func<bool, int> defaultAction = (value) => { reader(value); return 1; };
        return AssertReadNodeBoolParam(node, paramName, defaultAction);
    }

    static public int AssertReadNodeBoolParam(JsonNode node, string paramName, Func<bool, int> reader)
    {
        int assert = AssertNodeParamIsNullOrBool(node, paramName);
        if (assert == 1)
        {
            return reader(node![paramName]!.GetValue<bool>());
        }
        return assert;
    }

    static public int AssertNodeParamIsNullOrNumber(JsonNode node, string paramName)
    {
        var paramNode = node![paramName]!;
        if ((paramNode is null) || (paramNode.GetValueKind() == System.Text.Json.JsonValueKind.Undefined))
        {
            return 0;
        }

        if ((paramNode is not null) && (paramNode.GetValueKind() == JsonValueKind.Number))
        {
            return 1;
        }

        DeviceNotifications.Error($" * Error: Value '{paramName}' not a number.");
        return -1;
    }

    static public int AssertReadNodeFloatParamInRange(JsonNode node, string paramName, float minValue, float maxValue, Action<float> reader)
    {
        Func<float, int> defaultAction = (value) => { reader(value); return 1; };
        return AssertReadNodeFloatParamInRange(node, paramName, minValue, maxValue, defaultAction);
    }

    static public int AssertReadNodeFloatParamInRange(JsonNode node, string paramName, float minValue, float maxValue, Func<float, int> reader)
    {
        int assert = AssertNodeParamIsNullOrNumber(node, paramName);
        if (assert == 1)
        {
            float value = 0.0f;
            string stringValue = node![paramName]!.ToString();
            if (float.TryParse(stringValue, out value))
            {
                if (value >= minValue && value <= maxValue)
                {
                    return reader(value);
                }
                else
                {
                    DeviceNotifications.Error($" * Error: Value '{paramName}' not in range: {stringValue}");
                    return -1;
                }
            }

            DeviceNotifications.Error($" * Error: Value '{paramName}' could not be parsed into a float: {stringValue}");
            return -1;
        }
        return assert;
    }

    static public int AssertReadNodeIntParamInRange(JsonNode node, string paramName, int minValue, int maxValue, Action<int> reader)
    {
        Func<int, int> defaultAction = (value) => { reader(value); return 1; };
        return AssertReadNodeIntParamInRange(node, paramName, minValue, maxValue, defaultAction);
    }

    static public int AssertReadNodeIntParamInRange(JsonNode node, string paramName, int minValue, int maxValue, Func<int, int> reader)
    {
        int assert = AssertNodeParamIsNullOrNumber(node, paramName);
        if (assert == 1)
        {
            int value = 0;
            string stringValue = node![paramName]!.ToString();
            if (int.TryParse(stringValue, out value))
            {
                if (value >= minValue && value <= maxValue)
                {
                    return reader(value);
                }
                else
                {
                    DeviceNotifications.Error($" * Error: Value '{paramName}' not in range: {stringValue}");
                    return -1;
                }
            }

            DeviceNotifications.Error($" * Error: Value '{paramName}' could not be parsed into an int: {stringValue}");
            return -1;
        }
        return assert;
    }
}
