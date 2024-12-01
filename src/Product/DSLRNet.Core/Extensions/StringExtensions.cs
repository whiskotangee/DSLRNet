namespace DSLRNet.Core.Extensions;

public static class StringExtensions
{
    public static string WrapTextWithProperties(this string text, string? color = null, int? size = null)
    {
        if (color == null && size == null)
        {
            return text;
        }

        var colorAttribute = color != null ? $" color=\"{color}\"" : string.Empty;
        var sizeAttribute = size != null ? $" size=\"{size}\"" : string.Empty;

        var combinedAttributes = $"{colorAttribute}{sizeAttribute}";

        return $"<font{combinedAttributes}>{text}</font>";
    }

}
