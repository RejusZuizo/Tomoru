using Xunit;

namespace Tomoshibi.Tests;

/// <summary>Serialises the tests that share <c>GradeScale.CustomBands</c>.
///
/// <para>Those bands are app-wide static state. One test sets them directly and
/// restores them afterwards; another builds a <c>SubjectsViewModel</c>, which
/// constructs a <c>GradeScaleViewModel</c>, whose constructor assigns the same
/// static from the state it was handed. Both are well behaved on their own —
/// but xUnit runs test classes in parallel, so the second could overwrite the
/// bands the first was midway through asserting on.</para>
///
/// <para>It failed roughly one run in eight, always on the same assertion, and
/// always reading a default band label where a custom one was expected. Sharing
/// a collection is what stops the two classes overlapping; it is not a fix for
/// the static itself, which is still there and still app-wide.</para></summary>
[CollectionDefinition(Name)]
public class GradeScaleStaticCollection
{
    public const string Name = "grade-scale static bands";
}
