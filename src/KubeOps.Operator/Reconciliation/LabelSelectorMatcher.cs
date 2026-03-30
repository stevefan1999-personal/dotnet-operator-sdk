// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace KubeOps.Operator.Reconciliation;

/// <summary>
/// Evaluates a Kubernetes label-selector expression against an entity's label dictionary.
/// Supports the set-based formats emitted by KubeOps.KubernetesClient label-selector types:
/// <list type="bullet">
///   <item><c>key in (v1,v2)</c> – EqualsSelector</item>
///   <item><c>key notin (v1,v2)</c> – NotEqualsSelector</item>
///   <item><c>key</c> – ExistsSelector</item>
///   <item><c>!key</c> – NotExistsSelector</item>
/// </list>
/// Multiple clauses joined by commas are evaluated as AND.
/// </summary>
internal static class LabelSelectorMatcher
{
    internal static bool Matches(string? selector, IReadOnlyDictionary<string, string>? entityLabels)
    {
        if (selector is null) return true;
        entityLabels ??= new Dictionary<string, string>();

        foreach (var clause in SplitTopLevel(selector))
        {
            if (!MatchClause(clause, entityLabels)) return false;
        }

        return true;
    }

    // Splits "key in (a,b),other notin (c)" at top-level commas only (ignores commas inside parens).
    private static IEnumerable<string> SplitTopLevel(string selector)
    {
        int depth = 0, start = 0;
        for (int i = 0; i < selector.Length; i++)
        {
            switch (selector[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    yield return selector[start..i].Trim();
                    start = i + 1;
                    break;
            }
        }

        if (start < selector.Length)
        {
            yield return selector[start..].Trim();
        }
    }

    private static bool MatchClause(string clause, IReadOnlyDictionary<string, string> labels)
    {
        const string inOp = " in (";
        const string notinOp = " notin (";

        // "key in (v1,v2)"
        int idx = clause.IndexOf(inOp, StringComparison.Ordinal);
        if (idx >= 0 && clause.EndsWith(')'))
        {
            var key = clause[..idx].Trim();
            var values = ParseValues(clause[(idx + inOp.Length)..^1]);
            return labels.TryGetValue(key, out var v) && values.Contains(v);
        }

        // "key notin (v1,v2)"
        idx = clause.IndexOf(notinOp, StringComparison.Ordinal);
        if (idx >= 0 && clause.EndsWith(')'))
        {
            var key = clause[..idx].Trim();
            var values = ParseValues(clause[(idx + notinOp.Length)..^1]);
            return !labels.TryGetValue(key, out var v) || !values.Contains(v);
        }

        // "!key"
        if (clause.StartsWith('!'))
        {
            return !labels.ContainsKey(clause[1..].Trim());
        }

        // "key"
        return labels.ContainsKey(clause);
    }

    private static HashSet<string> ParseValues(string csv) =>
        csv.Split(',').Select(v => v.Trim()).ToHashSet(StringComparer.Ordinal);
}
