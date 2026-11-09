using PCL.Core.App.Localization;

namespace PCL.Core.Utils.Validate;

internal static class ValidationMessage
{
    public static string Text(string key, params object?[] args)
    {
        var text = Lang.Text(key, args);
        return string.IsNullOrWhiteSpace(text) ? "Invalid input." : text;
    }
}
