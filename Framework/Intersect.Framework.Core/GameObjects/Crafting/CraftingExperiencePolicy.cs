namespace Intersect.Framework.Core.GameObjects.Crafting;

public static class CraftingExperiencePolicy
{
    public static long CalculateAwardedExperience(CraftingRecipeDescriptor recipe, int playerJobLevel)
    {
        if (recipe == null)
        {
            return 0;
        }

        if (recipe.ExperienceAmount > 0)
        {
            return recipe.ExperienceAmount;
        }

        return Calculate(
            ingredientCount: recipe.Ingredients?.Count ?? 0,
            recipeLevel: recipe.RecipeLevel,
            playerJobLevel: playerJobLevel
        );
    }

    public static long Calculate(int ingredientCount, int recipeLevel, int playerJobLevel)
    {
        var normalizedIngredientCount = Math.Max(1, ingredientCount);
        var normalizedRecipeLevel = Math.Max(1, recipeLevel);
        var normalizedPlayerLevel = Math.Max(1, playerJobLevel);

        var baseXp = normalizedIngredientCount * 6;
        var levelDelta = Math.Clamp(normalizedRecipeLevel - normalizedPlayerLevel, -10, 10);
        var multiplier = 1.0 + levelDelta * 0.08;

        var scaled = (long)Math.Round(baseXp * multiplier, MidpointRounding.AwayFromZero);

        var (minimum, maximum) = GetBracketCaps(normalizedRecipeLevel);

        return Math.Clamp(scaled, minimum, maximum);
    }

    public static (long minimum, long maximum) GetBracketCaps(int recipeLevel)
    {
        var normalizedRecipeLevel = Math.Max(1, recipeLevel);

        return normalizedRecipeLevel switch
        {
            <= 20 => (5, 40),
            <= 50 => (20, 120),
            _ => (60, 300),
        };
    }
}
