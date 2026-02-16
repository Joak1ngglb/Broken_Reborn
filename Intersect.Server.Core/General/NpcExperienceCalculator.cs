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
        var minimumExperience = Math.Max(1, experienceOptions.MinimumNpcExperience);

        // Garantir que a XP seja pelo menos 1
        return (long)Math.Max(minimumExperience, Math.Round(experience));
    }

    /// <summary>
    /// Verifica se o NPC é um Boss baseado no nome
    /// </summary>
    /// <param name="npcName">Nome do NPC</param>
    /// <returns>True se o NPC tem a tag [BOSS] no nome</returns>
    private static bool IsBoss(string npcName)
    {
        var bossTag = Options.Instance.Npc.Experience.BossTag;

        if (string.IsNullOrWhiteSpace(npcName))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(bossTag))
        {
            return false;
        }

        return npcName.Contains(bossTag, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calcula a experiência usando a fórmula automática baseada no nível do NPC.
    /// IMPORTANTE: Se AlwaysUseAutomaticCalculation = true, SEMPRE usa o cálculo automático,
    /// ignorando o valor configurado no editor.
    /// NPCs com [BOSS] no nome recebem multiplicador de XP x30.
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

        // Aplicar multiplicador de Boss se o NPC tiver [BOSS] no nome
        if (IsBoss(npcDescriptor.Name))
        {
            experience *= Math.Max(1, experienceOptions.BossExperienceMultiplier);
        }

        return experience;
    }
}
