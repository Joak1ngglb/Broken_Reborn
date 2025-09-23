using Intersect.Enums;

namespace Intersect.Server.Database.PlayerData.Migrations;

public static class PlayerPetGenderMigration
{
    public static void Run(PlayerContext context)
    {
        foreach (var pet in context.Player_Pets)
        {
            if (pet.Gender is PetGender.Male or PetGender.Female)
            {
                continue;
            }

            pet.Gender = PetGender.Unspecified;
        }

        context.ChangeTracker.DetectChanges();
        context.SaveChanges();
    }
}
