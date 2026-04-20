using System;
using System.Globalization;

namespace Italbytz.Tcg.Abstractions;

public static class TcgClientMappingHelpers
{
    public static Uri? BuildImageUri(string? preferred, string? fallback = null)
    {
        var candidate = !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
        return Uri.TryCreate(candidate, UriKind.Absolute, out var imageUri) ? imageUri : null;
    }

    public static Uri? BuildHighResolutionImageUri(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return null;
        }

        var normalizedImage = image!;
        var imagePath = normalizedImage.EndsWith("/high.png", StringComparison.OrdinalIgnoreCase)
            ? normalizedImage
            : normalizedImage.TrimEnd('/') + "/high.png";

        return Uri.TryCreate(imagePath, UriKind.Absolute, out var imageUri) ? imageUri : null;
    }

    public static DateTimeOffset? ParseReleaseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var releaseDate)
            ? releaseDate
            : null;
    }
}