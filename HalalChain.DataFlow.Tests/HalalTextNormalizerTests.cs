using HalalChain.DataFlow.Transform;
using Xunit;

namespace HalalChain.DataFlow.Tests;

public sealed class HalalTextNormalizerTests
{
    [Theory]
    [InlineData("  Acme   Foods  ", "Acme Foods")]
    [InlineData("Acme\tFoods", "Acme Foods")]
    [InlineData("Acme\nFoods", "Acme Foods")]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("", null)]
    public void CollapseWhitespace_NormalizesRuns(string? input, string? expected) =>
        Assert.Equal(expected, HalalTextNormalizer.CollapseWhitespace(input));

    [Theory]
    [InlineData("Acme Foods", "Acme-Foods")]
    [InlineData("ACME  FOODS", "Acme Foods")]
    [InlineData("acme foods ltd.", "ACME FOODS LTD")]
    [InlineData("Acme_Foods", "Acme.Foods")]
    public void ComparisonKey_IsStableAcrossSpellingVariants(string a, string b) =>
        Assert.Equal(
            HalalTextNormalizer.ComparisonKey(a),
            HalalTextNormalizer.ComparisonKey(b));

    [Fact]
    public void ComparisonKey_DistinguishesDiacritics()
    {
        // Transliteration is a business decision, not a normalisation
        // side effect: collapsing "ç" to "c" would merge genuinely
        // different organization names.
        Assert.NotEqual(
            HalalTextNormalizer.ComparisonKey("Açme Foods"),
            HalalTextNormalizer.ComparisonKey("Acme Foods"));
    }

    [Fact]
    public void ComparisonKey_OfEmpty_IsEmpty() =>
        Assert.Equal(string.Empty, HalalTextNormalizer.ComparisonKey("   "));

    [Theory]
    [InlineData("my", "MY")]
    [InlineData("  my  ", "MY")]
    [InlineData("My", "MY")]
    [InlineData(null, null)]
    public void CanonicalizeIdentifier_UpperCases(string? input, string? expected) =>
        Assert.Equal(expected, HalalTextNormalizer.CanonicalizeIdentifier(input));

    [Theory]
    [InlineData("my", "MY")]
    [InlineData("  my ", "MY")]
    [InlineData("USA", null)]
    [InlineData("M", null)]
    [InlineData("1Y", null)]
    [InlineData(null, null)]
    public void NormalizeCountryCode_OnlyAcceptsAlpha2(string? input, string? expected) =>
        Assert.Equal(expected, HalalTextNormalizer.NormalizeCountryCode(input));

    [Fact]
    public void NormalizeDate_ParsesToIso8601Utc()
    {
        var result = HalalTextNormalizer.NormalizeDate("2026-01-15");

        Assert.NotNull(result);
        Assert.Equal("2026-01-15T00:00:00.0000000Z", result);
    }

    [Fact]
    public void NormalizeDate_ConvertsOffsetToUtc()
    {
        var result = HalalTextNormalizer.NormalizeDate("2026-01-15T08:00:00+08:00");

        Assert.Equal("2026-01-15T00:00:00.0000000Z", result);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("")]
    [InlineData(null)]
    public void NormalizeDate_ReturnsNullRatherThanGuessing(string? input) =>
        Assert.Null(HalalTextNormalizer.NormalizeDate(input));

    [Theory]
    [InlineData("1234.56", 1234.56)]
    [InlineData("0.99", 0.99)]
    [InlineData("  10  ", 10)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("12,345.6", 12345.6)]
    public void TryParseDecimal_HandlesInvariantFormat(string input, double expected)
    {
        Assert.True(HalalTextNormalizer.TryParseDecimal(input, out var value));
        Assert.Equal(expected, (double)value, 3);
    }

    [Theory]
    [InlineData("1234,56", 1234.56)]
    [InlineData("0,99", 0.99)]
    [InlineData("1,5", 1.5)]
    public void TryParseDecimal_TreatsTrailingCommaGroupAsDecimalSeparator(string input, double expected)
    {
        // Under plain invariant rules "1234,56" parses as 123456. Reading a
        // quantity a thousandfold too large is a data-integrity failure, so
        // the comma-decimal form must win over the grouping interpretation.
        Assert.True(HalalTextNormalizer.TryParseDecimal(input, out var value));
        Assert.Equal(expected, (double)value, 3);
    }

    [Fact]
    public void TryParseDecimal_DoesNotMistakeGroupingForDecimal()
    {
        // "1,234" is unambiguously a grouped thousands value: the group is
        // three digits, so it is not a fractional part.
        Assert.True(HalalTextNormalizer.TryParseDecimal("1,234", out var value));
        Assert.Equal(1234m, value);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("12,34,56")]
    [InlineData("1.2.3")]
    [InlineData("USD 5")]
    public void TryParseDecimal_RejectsUnparseableInput(string? input) =>
        Assert.False(HalalTextNormalizer.TryParseDecimal(input, out _));

    [Fact]
    public void NormalizeStatus_MatchesIgnoringCaseAndSeparators()
    {
        string[] known = ["InProgress", "Completed", "Failed"];

        Assert.Equal("InProgress", HalalTextNormalizer.NormalizeStatus("in progress", known));
        Assert.Equal("InProgress", HalalTextNormalizer.NormalizeStatus("IN-PROGRESS", known));
        Assert.Equal("Completed", HalalTextNormalizer.NormalizeStatus("  completed ", known));
    }

    [Fact]
    public void NormalizeStatus_ReturnsNullForUnknownRatherThanGuessing()
    {
        string[] known = ["Active", "Suspended"];

        // Guessing a status could silently change compliance behaviour, so
        // an unrecognised value must surface as null.
        Assert.Null(HalalTextNormalizer.NormalizeStatus("Terminated", known));
        Assert.Null(HalalTextNormalizer.NormalizeStatus(null, known));
    }
}
