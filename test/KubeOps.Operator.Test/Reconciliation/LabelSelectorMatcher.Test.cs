// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

using KubeOps.Operator.Reconciliation;

namespace KubeOps.Operator.Test.Reconciliation;

public sealed class LabelSelectorMatcherTest
{
    // ── null selector ────────────────────────────────────────────────────────

    [Fact]
    public void Matches_NullSelector_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["env"] = "prod" };
        LabelSelectorMatcher.Matches(null, labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_NullSelector_EmptyLabels_ReturnsTrue()
    {
        LabelSelectorMatcher.Matches(null, new Dictionary<string, string>()).Should().BeTrue();
    }

    [Fact]
    public void Matches_NullSelector_NullLabels_ReturnsTrue()
    {
        LabelSelectorMatcher.Matches(null, null).Should().BeTrue();
    }

    // ── "key in (v1,v2)" ─────────────────────────────────────────────────────

    [Fact]
    public void Matches_InOperator_KeyPresentWithMatchingValue_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["env"] = "prod" };
        LabelSelectorMatcher.Matches("env in (prod)", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_InOperator_KeyPresentAmongMultipleValues_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["env"] = "staging" };
        LabelSelectorMatcher.Matches("env in (prod,staging,dev)", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_InOperator_KeyPresentButWrongValue_ReturnsFalse()
    {
        var labels = new Dictionary<string, string> { ["env"] = "dev" };
        LabelSelectorMatcher.Matches("env in (prod,staging)", labels).Should().BeFalse();
    }

    [Fact]
    public void Matches_InOperator_KeyAbsent_ReturnsFalse()
    {
        var labels = new Dictionary<string, string> { ["tier"] = "frontend" };
        LabelSelectorMatcher.Matches("env in (prod)", labels).Should().BeFalse();
    }

    // ── "key notin (v1,v2)" ──────────────────────────────────────────────────

    [Fact]
    public void Matches_NotInOperator_KeyAbsent_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["tier"] = "frontend" };
        LabelSelectorMatcher.Matches("env notin (prod)", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_NotInOperator_KeyPresentButNotInValues_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["env"] = "dev" };
        LabelSelectorMatcher.Matches("env notin (prod,staging)", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_NotInOperator_KeyPresentAndInValues_ReturnsFalse()
    {
        var labels = new Dictionary<string, string> { ["env"] = "prod" };
        LabelSelectorMatcher.Matches("env notin (prod,staging)", labels).Should().BeFalse();
    }

    // ── "key" (exists) ────────────────────────────────────────────────────────

    [Fact]
    public void Matches_ExistsOperator_KeyPresent_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["managed"] = "true" };
        LabelSelectorMatcher.Matches("managed", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_ExistsOperator_KeyAbsent_ReturnsFalse()
    {
        var labels = new Dictionary<string, string> { ["env"] = "prod" };
        LabelSelectorMatcher.Matches("managed", labels).Should().BeFalse();
    }

    // ── "!key" (not exists) ───────────────────────────────────────────────────

    [Fact]
    public void Matches_NotExistsOperator_KeyAbsent_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["env"] = "prod" };
        LabelSelectorMatcher.Matches("!managed", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_NotExistsOperator_KeyPresent_ReturnsFalse()
    {
        var labels = new Dictionary<string, string> { ["managed"] = "true" };
        LabelSelectorMatcher.Matches("!managed", labels).Should().BeFalse();
    }

    // ── multi-clause (AND semantics) ─────────────────────────────────────────

    [Fact]
    public void Matches_MultiClause_AllMatch_ReturnsTrue()
    {
        var labels = new Dictionary<string, string>
        {
            ["env"] = "prod",
            ["managed"] = "true",
        };
        LabelSelectorMatcher.Matches("env in (prod),managed in (true)", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_MultiClause_OneClauseDoesNotMatch_ReturnsFalse()
    {
        var labels = new Dictionary<string, string>
        {
            ["env"] = "prod",
            ["managed"] = "false",
        };
        LabelSelectorMatcher.Matches("env in (prod),managed in (true)", labels).Should().BeFalse();
    }

    [Fact]
    public void Matches_MultiClause_InAndNotIn_ReturnsTrue()
    {
        var labels = new Dictionary<string, string>
        {
            ["env"] = "prod",
        };
        LabelSelectorMatcher.Matches("env in (prod),!managed", labels).Should().BeTrue();
    }

    // ── commas inside parentheses must NOT be treated as clause separators ───

    [Fact]
    public void Matches_InOperatorWithCommaInValues_DoesNotSplitAtInnerComma()
    {
        // "env in (prod,staging)" has a comma inside parens — must be treated as ONE clause
        var labels = new Dictionary<string, string> { ["env"] = "staging" };
        LabelSelectorMatcher.Matches("env in (prod,staging)", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_ComplexMultiClauseWithCommaInsideParens_CorrectlyEvaluated()
    {
        var labels = new Dictionary<string, string>
        {
            ["env"] = "prod",
            ["region"] = "eu-west",
        };
        LabelSelectorMatcher.Matches(
            "env in (prod,staging),region notin (us-east,us-west)",
            labels).Should().BeTrue();
    }

    // ── empty labels ─────────────────────────────────────────────────────────

    [Fact]
    public void Matches_ExistsClause_EmptyLabels_ReturnsFalse()
    {
        LabelSelectorMatcher.Matches("env", new Dictionary<string, string>()).Should().BeFalse();
    }

    [Fact]
    public void Matches_NotExistsClause_EmptyLabels_ReturnsTrue()
    {
        LabelSelectorMatcher.Matches("!env", new Dictionary<string, string>()).Should().BeTrue();
    }

    [Fact]
    public void Matches_NotInClause_EmptyLabels_ReturnsTrue()
    {
        // key absent → not in any set → true
        LabelSelectorMatcher.Matches("env notin (prod)", new Dictionary<string, string>()).Should().BeTrue();
    }

    // ── key=value equality ────────────────────────────────────────────────────

    [Fact]
    public void Matches_EqualityOperator_Matches_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["env"] = "prod" };
        LabelSelectorMatcher.Matches("env=prod", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_EqualityOperator_WrongValue_ReturnsFalse()
    {
        var labels = new Dictionary<string, string> { ["env"] = "staging" };
        LabelSelectorMatcher.Matches("env=prod", labels).Should().BeFalse();
    }

    [Fact]
    public void Matches_EqualityOperator_KeyAbsent_ReturnsFalse()
    {
        LabelSelectorMatcher.Matches("env=prod", new Dictionary<string, string>()).Should().BeFalse();
    }

    // ── key!=value inequality ─────────────────────────────────────────────────

    [Fact]
    public void Matches_InequalityOperator_DifferentValue_ReturnsTrue()
    {
        var labels = new Dictionary<string, string> { ["env"] = "staging" };
        LabelSelectorMatcher.Matches("env!=prod", labels).Should().BeTrue();
    }

    [Fact]
    public void Matches_InequalityOperator_SameValue_ReturnsFalse()
    {
        var labels = new Dictionary<string, string> { ["env"] = "prod" };
        LabelSelectorMatcher.Matches("env!=prod", labels).Should().BeFalse();
    }

    [Fact]
    public void Matches_InequalityOperator_KeyAbsent_ReturnsTrue()
    {
        // key absent → not equal → true
        LabelSelectorMatcher.Matches("env!=prod", new Dictionary<string, string>()).Should().BeTrue();
    }

    [Fact]
    public void Matches_MixedEqualityAndSetBased_AllMatch_ReturnsTrue()
    {
        var labels = new Dictionary<string, string>
        {
            ["env"] = "prod",
            ["region"] = "eu-west",
        };
        LabelSelectorMatcher.Matches("env=prod,region in (eu-west,us-east)", labels).Should().BeTrue();
    }
}
