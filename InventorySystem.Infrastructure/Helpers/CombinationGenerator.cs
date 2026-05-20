using InventorySystem.Domain.Entities;

namespace InventorySystem.Infrastructure.Helpers;

public static class CombinationGenerator
{
    public static IReadOnlyList<GeneratedCombination> Generate(IEnumerable<ProductVariant> variants)
    {
        var orderedVariants = variants
            .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Name, StringComparer.Ordinal)
            .Select(v => new
            {
                Name = v.Name.Trim(),
                Values = v.Values
                    .Select(value => value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(value => value, StringComparer.Ordinal)
                    .ToList()
            })
            .Where(v => !string.IsNullOrWhiteSpace(v.Name) && v.Values.Count > 0)
            .ToList();

        if (orderedVariants.Count == 0)
        {
            return [];
        }

        var combinations = new List<GeneratedCombination>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Build(index: 0, selected: []);

        return combinations;

        void Build(int index, List<VariantValue> selected)
        {
            if (index == orderedVariants.Count)
            {
                var key = string.Join(
                    "|",
                    selected.Select(item => $"{NormalizeKeyPart(item.VariantName)}:{NormalizeKeyPart(item.Value)}"));

                if (!seenKeys.Add(key))
                {
                    return;
                }

                combinations.Add(new GeneratedCombination(
                    CombinationKey: key,
                    DisplayLabel: string.Join(" / ", selected.Select(item => item.Value)),
                    RawCombination: string.Join(",", selected.Select(item => $"{item.VariantName}:{item.Value}"))));

                return;
            }

            var variant = orderedVariants[index];

            foreach (var value in variant.Values)
            {
                selected.Add(new VariantValue(variant.Name, value));
                Build(index + 1, selected);
                selected.RemoveAt(selected.Count - 1);
            }
        }
    }

    private static string NormalizeKeyPart(string value)
    {
        return value.Trim().ToUpperInvariant().Replace(" ", "_");
    }

    private sealed record VariantValue(string VariantName, string Value);
}

public sealed record GeneratedCombination(
    string CombinationKey,
    string DisplayLabel,
    string RawCombination);
