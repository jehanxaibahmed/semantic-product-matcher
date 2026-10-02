using System.Globalization;
using System.Text.RegularExpressions;

namespace ProductMatcher.Application.Matching;

/// <summary>
/// Strips order phrasing that says nothing about which product is meant, for example
/// "2 boxes of the" or "please". Pack sizes after the product name ("5kg") are kept because they
/// help tell variants apart.
/// </summary>
public static partial class QueryNormalizer
{
    public static string Normalize(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var text = query.ToLower(CultureInfo.InvariantCulture);
        text = Punctuation().Replace(text, " ");
        text = LeadingQuantity().Replace(text, string.Empty);
        text = Filler().Replace(text, " ");
        text = Whitespace().Replace(text, " ").Trim();

        // Never normalise a query away entirely; fall back to the cleaned original.
        return text.Length > 0 ? text : Whitespace().Replace(query, " ").Trim().ToLower(CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"[^\p{L}\p{N}%.\s-]")]
    private static partial Regex Punctuation();

    // e.g. "2 boxes of the", "a case of", "3 x", "two bags of", "1x"
    [GeneratedRegex(
        @"^\s*(?:(?:\d+(?:\.\d+)?|a|an|one|two|three|four|five|six|seven|eight|nine|ten|dozen|couple|few)\s*(?:x\b)?\s*)+" +
        @"(?:(?:boxes|box|cases|case|bags|bag|sacks|sack|packs|pack|trays|tray|tubs|tub|tins|tin|cans|can|bottles|bottle|" +
        @"punnets|punnet|bunches|bunch|jars|jar|rolls|roll|loaves|loaf|of|the|our|usual|lots)\b\s*)*")]
    private static partial Regex LeadingQuantity();

    [GeneratedRegex(@"\b(?:please|pls|plz|thanks|thank you|cheers|can i get|can we get|could we have|we need|i need|some|our usual|the usual)\b")]
    private static partial Regex Filler();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
