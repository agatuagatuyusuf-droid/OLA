using System.Text.Json;
using System.Text.RegularExpressions;

namespace SkyAuto.Core.Variables;

public partial class VariableResolver
{
    private static readonly Regex TemplateRegex = TemplatePattern();

    [GeneratedRegex(@"{{\s*([^{}]+)\s*}}")]
    private static partial Regex TemplatePattern();

    public VariableResolveResult Resolve(string input, VariableContext context)
    {
        var result = new VariableResolveResult
        {
            OriginalText = input,
            ResolvedText = input,
            Success = true
        };

        if (string.IsNullOrEmpty(input))
            return result;

        try
        {
            var matches = TemplateRegex.Matches(input);

            if (matches.Count == 0)
                return result;

            var resolved = input;
            var missingKeys = new List<string>();
            var usedKeys = new List<string>();

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value.Trim();
                var placeholder = match.Value;

                object? value = null;

            if (key.StartsWith("step_"))
            {
                if (key.Contains('.'))
                    value = ResolveStepOutput(key, context);
                else
                    value = context.GetValue(key);
            }
            else
            {
                value = context.GetValue(key);

                if (value == null && key.Contains('.'))
                {
                    value = ResolveNestedProperty(key, context);
                }
            }

                if (value == null)
                {
                    missingKeys.Add(key);
                    result.Success = false;
                }
                else
                {
                    usedKeys.Add(key);
                    var strValue = ConvertToString(value);
                    resolved = resolved.Replace(placeholder, strValue);
                }
            }

            result.ResolvedText = resolved;
            result.MissingKeys = missingKeys;
            result.UsedKeys = usedKeys;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ResolvedText = input;
            return result;
        }
    }

    private static object? ResolveStepOutput(string key, VariableContext context)
    {
        var parts = key.Split('.');
        if (parts.Length < 2) return null;

        var stepKey = parts[0];
        var output = context.GetStepOutput(stepKey);
        if (output == null) return null;

        return ResolveProperty(output, parts.Skip(1).ToList());
    }

    private static object? ResolveNestedProperty(string key, VariableContext context)
    {
        var parts = key.Split('.');
        if (parts.Length < 2) return null;

        var baseKey = parts[0];
        var baseValue = context.GetValue(baseKey);
        if (baseValue == null) return null;

        return ResolveProperty(baseValue, parts.Skip(1).ToList());
    }

    private static object? ResolveProperty(object? value, List<string> propertyPath)
    {
        if (value == null || propertyPath.Count == 0)
            return value;

        var current = value;
        foreach (var prop in propertyPath)
        {
            if (current == null) return null;

            if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object &&
                    jsonElement.TryGetProperty(prop, out var propValue))
                {
                    current = propValue;
                    continue;
                }
                return null;
            }

            if (current is Dictionary<string, object?> dict)
            {
                if (dict.TryGetValue(prop, out var dictValue))
                {
                    current = dictValue;
                    continue;
                }
                return null;
            }

            return null;
        }

        return current;
    }

    private static string ConvertToString(object? value)
    {
        if (value == null) return string.Empty;

        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString() ?? string.Empty,
                JsonValueKind.Number => je.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Object => je.GetRawText(),
                JsonValueKind.Array => je.GetRawText(),
                _ => je.GetRawText()
            };
        }

        return value.ToString() ?? string.Empty;
    }
}
