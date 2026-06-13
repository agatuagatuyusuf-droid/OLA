namespace SkyAuto.Core.Variables;

public static class VariableMasker
{
    public static string MaskSecret(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= 2)
            return "**";

        if (value.Length <= 6)
            return $"{value[0]}***{value[^1]}";

        return $"{value[..2]}******{value[^2..]}";
    }

    public static string MaskTextByDefinitions(string text, IEnumerable<VariableDefinition> definitions, VariableContext context)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;
        foreach (var def in definitions.Where(d => d.IsSecret))
        {
            var rawValue = context.GetValue(def.Key)?.ToString();
            if (string.IsNullOrEmpty(rawValue))
                continue;

            var masked = MaskSecret(rawValue);
            result = result.Replace(rawValue, masked);
        }

        return result;
    }
}
