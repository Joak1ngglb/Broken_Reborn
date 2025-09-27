namespace Intersect.Server.Database.GameData.Migrations;

public static class PetSpriteGenderMigration
{
    public static void Run(GameContext context)
    {
        foreach (var pet in context.Pets)
        {
            var baseSprite = pet.Sprite ?? string.Empty;

            if (string.IsNullOrWhiteSpace(pet.MaleSprite))
            {
                pet.MaleSprite = baseSprite;
            }

            if (string.IsNullOrWhiteSpace(pet.FemaleSprite))
            {
                pet.FemaleSprite = baseSprite;
            }
        }

        context.ChangeTracker.DetectChanges();
        context.SaveChanges();
    }
}
