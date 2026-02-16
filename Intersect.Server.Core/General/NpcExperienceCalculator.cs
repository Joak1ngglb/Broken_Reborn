using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Config;

namespace Intersect.Server.General;

/// <summary>
/// Calcula a experiência que um NPC deve dar baseado em seu nível.
/// Usa a mesma fórmula que você usava no editor, mas agora é calculado automaticamente pelo servidor.
/// </summary>
public static partial class NpcExperienceCalculator
{
    /// <summary>
    /// Calcula a experiência que um NPC deve dar baseado em seu nível.
    /// Fórmula: baseexp / (2.4 * level * fator)
    /// Onde baseexp = (50/3) * (level³ - 6*level² + 17*level - 12)
    /// </summary>
    /// <param name="level">Nível do NPC</param>
    /// <returns>Experiência calculada</returns>
    public static long CalculateExperience(int level)
    {
        var experienceOptions = Options.Instance.Npc.Experience;

        if (level <= 0)
        {
            return 0;
        }

        var monotonicRangeLimit = Math.Max(4, Math.Max(3, experienceOptions.LowLevelCapMaxLevel + 1));

        if (level <= monotonicRangeLimit)
        {
            var runningMaximum = CalculateExperienceWithoutMonotonicity(1, experienceOptions);

            if (level == 1)
            {
                return runningMaximum;
            }

            for (var currentLevel = 2; currentLevel <= level; currentLevel++)
            {
                var currentExperience = CalculateExperienceWithoutMonotonicity(currentLevel, experienceOptions);
                runningMaximum = Math.Max(runningMaximum, currentExperience);
            }

            return runningMaximum;
        }

        return CalculateExperienceWithoutMonotonicity(level, experienceOptions);
    }

    private static long CalculateExperienceWithoutMonotonicity(int level, NpcExperienceOptions experienceOptions)
    {
        if (level <= 0)
        {
            return 0;
        }

        var minimumExperience = Math.Max(1, experienceOptions.MinimumNpcExperience);

        if (level == 1)
        {
            return Math.Max(minimumExperience, 9);
        }

        if (level == 2)
        {
            return Math.Max(minimumExperience, 12);
        }

        if (level == 3)
        {
            return Math.Max(minimumExperience, 18);
        }

        var cubicExperience = CalculateCubicExperience(level, experienceOptions, minimumExperience);

        var lowLevelCapMaxLevel = Math.Max(0, experienceOptions.LowLevelCapMaxLevel);
        var lowLevelCapPerLevel = Math.Max(0, experienceOptions.LowLevelCapPerLevel);

        if (lowLevelCapMaxLevel <= 0 || lowLevelCapPerLevel <= 0 || level > lowLevelCapMaxLevel)
        {
            return cubicExperience;
        }

        var cappedExperience = Math.Min(cubicExperience, Math.Max(minimumExperience, level * lowLevelCapPerLevel));
        var transitionStartLevel = Math.Max(4, lowLevelCapMaxLevel - 1);

        if (level < transitionStartLevel)
        {
            return cappedExperience;
        }

        var transitionSteps = Math.Max(1, lowLevelCapMaxLevel - transitionStartLevel);
        var transitionProgress = Math.Clamp((double)(level - transitionStartLevel) / transitionSteps, 0d, 1d);
        var smoothProgress = transitionProgress * transitionProgress * (3d - 2d * transitionProgress); // SmoothStep

        var blendedExperience = cappedExperience + (cubicExperience - cappedExperience) * smoothProgress;

        return (long)Math.Max(minimumExperience, Math.Round(blendedExperience));
    }

    private static long CalculateCubicExperience(int level, NpcExperienceOptions experienceOptions, int minimumExperience)
    {
        // Cálculo da experiência base
        var levelCubed = Math.Pow(level, 3);
        var levelSquared = Math.Pow(level, 2);
        var baseExp = (50.0 / 3.0) * (levelCubed - 6 * levelSquared + 17 * level - 12);

        // Cálculo da XP final
        var experienceFactor = experienceOptions.ExperienceFactor;
        if (experienceFactor <= 0)
        {
            experienceFactor = 0.5;
        }

        var experience = baseExp / (2.4 * level * experienceFactor);

        // Garantir que a XP seja pelo menos 1
        return (long)Math.Max(minimumExperience, Math.Round(experience));
    }

    /// <summary>
    /// Calcula a experiência usando a fórmula automática baseada no nível do NPC.
    /// IMPORTANTE: Se AlwaysUseAutomaticCalculation = true, SEMPRE usa o cálculo automático,
    /// ignorando o valor configurado no editor.
    /// NPCs marcados como Boss recebem multiplicador de XP configurável.
    /// </summary>
    /// <param name="npcDescriptor">Descritor do NPC</param>
    /// <returns>Experiência que o NPC deve dar</returns>
    public static long GetNpcExperience(NPCDescriptor npcDescriptor)
    {
        var experienceOptions = Options.Instance.Npc.Experience;

        if (npcDescriptor == null)
        {
            return 0;
        }

        long experience;

        // Se configurado para SEMPRE usar cálculo automático
        if (experienceOptions.UseAutomaticNpcExperience)
        {
            experience = CalculateExperience(npcDescriptor.Level);
        }
        // Se a XP foi configurada manualmente (> 0), usar o valor manual
        else if (npcDescriptor.Experience > 0)
        {
            experience = npcDescriptor.Experience;
        }
        // Caso contrário, calcular automaticamente baseado no nível
        else
        {
            experience = CalculateExperience(npcDescriptor.Level);
        }

        // Aplicar multiplicador de Boss usando exclusivamente o flag IsBoss do descriptor.
        if (npcDescriptor.IsBoss)
        {
            experience *= Math.Max(1, experienceOptions.BossExperienceMultiplier);
        }

        return experience;
    }
}
