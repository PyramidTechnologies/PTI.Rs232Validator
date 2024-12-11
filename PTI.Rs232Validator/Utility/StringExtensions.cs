using System.Text.RegularExpressions;

namespace PTI.Rs232Validator.Utility;

/// <summary>
/// A container of extension methods for <see cref="string"/>.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Adds spaces between words in the specified camelCase or PascalCase string.
    /// </summary>
    /// <param name="input">The string to mutate.</param>
    /// <returns>The mutated string.</returns>
    public static string AddSpacesToCamelCase(this string input)
    {
        return BetweenCamelCaseWordsRegex().Replace(input, " ");
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z0-9])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex BetweenCamelCaseWordsRegex();
}