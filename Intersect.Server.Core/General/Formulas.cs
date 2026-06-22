using System;
using System.Collections.Generic;
using Intersect;
using Intersect.Enums;
using Intersect.Server.Core;
using Intersect.Server.Entities;
using Intersect.Server.Localization;
using Intersect.Utilities;
using NCalc;
using Newtonsoft.Json;

namespace Intersect.Server.General;

public partial class Formulas
{
    private readonly record struct CombatRequest(
        Entity Attacker,
        Entity Defender,
        long BasePower,
        DamageType DamageType,
        Stat ScalingStat,
        int ScalingPercent,
        double CriticalMultiplier,
        bool IsHeal,
        int? AttackerLevel = null
    );

    private readonly record struct CombatResolverOptions(
        bool AllowHealingCriticals,
        bool IgnoreHealingDefense,
        bool PreventReflectChaining
    );

    private const string DefaultFormulaMagicDamage = "Random(((BaseDamage + (ScalingStat * ScaleFactor))) * CritMultiplier * .975, ((BaseDamage + (ScalingStat * ScaleFactor))) * CritMultiplier * 1.025) * (100 / (100 + V_MagicResist))";
    private const string DefaultFormulaPhysicalDamage = "Random(((BaseDamage + (ScalingStat * ScaleFactor))) * CritMultiplier * .975, ((BaseDamage + (ScalingStat * ScaleFactor))) * CritMultiplier * 1.025) * (100 / (100 + V_Defense))";
    private const string DefaultFormulaTrueDamage = "Random(((BaseDamage + (ScalingStat * ScaleFactor))) * CritMultiplier * .975, ((BaseDamage + (ScalingStat * ScaleFactor))) * CritMultiplier * 1.025)";

    private static readonly string FormulasFile = Path.Combine(ServerContext.ResourceDirectory, "formulas.json");
    private static Formulas? _formulas;

    public Formula ExpFormula = new("BaseExp * Power(Gain, Level)");

    public string MagicDamage = DefaultFormulaMagicDamage;
    public string PhysicalDamage = DefaultFormulaPhysicalDamage;
    public string TrueDamage = DefaultFormulaTrueDamage;

    public static void LoadFormulas()
    {
        Console.WriteLine("Loading formulae...");

        try
        {
            _formulas = new Formulas();
            if (File.Exists(FormulasFile))
            {
                _formulas = JsonConvert.DeserializeObject<Formulas>(File.ReadAllText(FormulasFile)) ?? _formulas;
            }

            File.WriteAllText(FormulasFile, JsonConvert.SerializeObject(_formulas, Formatting.Indented));

            Expression.CacheEnabled = false;
        }
        catch (Exception ex)
        {
            throw new Exception(Strings.Formulas.Missing, ex);
        }
    }

    public static long CalculateDamage(
        long baseDamage,
        DamageType damageType,
        Stat scalingStat,
        int scaling,
        double critMultiplier,
        Entity attacker,
        Entity victim,
        int? attackerLevel = null,
        IReadOnlyDictionary<string, object>? parameterOverrides = null
    )
    {
        if (_formulas == null)
        {
            throw new InvalidOperationException("Formulas not yet initialized");
        }

        ArgumentNullException.ThrowIfNull(attacker, nameof(attacker));
        ArgumentNullException.ThrowIfNull(victim);

        if (attacker.Stat == null)
        {
            throw new ArgumentException(
                $@"{nameof(attacker)}.{nameof(attacker.Stat)} is null",
                nameof(attacker)
            );
        }

        if (victim.Stat == null)
        {
            throw new ArgumentException($@"{nameof(victim)}.{nameof(victim.Stat)} is null", nameof(victim));
        }

        var expressionString = damageType switch
        {
            DamageType.Physical => _formulas.PhysicalDamage,
            DamageType.Magic => _formulas.MagicDamage,
            DamageType.True => _formulas.TrueDamage,
            _ => _formulas.TrueDamage,
        };

        var request = new CombatRequest(
            attacker,
            victim,
            baseDamage,
            damageType,
            scalingStat,
            scaling,
            critMultiplier,
            baseDamage < 0
        );

        var resolverOptions = new CombatResolverOptions(
            Options.Instance.Combat.HealingCanCrit,
            Options.Instance.Combat.HealingIgnoresDefense,
            Options.Instance.Combat.PreventReflectChaining
        );

        return ResolveDamage(request, expressionString, resolverOptions, parameterOverrides);
    }

    private static long ResolveDamage(
        CombatRequest request,
        string expressionString,
        CombatResolverOptions resolverOptions,
        IReadOnlyDictionary<string, object>? parameterOverrides = null
    )
    {
        ArgumentNullException.ThrowIfNull(request.Attacker, nameof(request.Attacker));
        ArgumentNullException.ThrowIfNull(request.Defender, nameof(request.Defender));

        if (request.Attacker.Stat == null)
        {
            throw new ArgumentException(
                $@"{nameof(request.Attacker)}.{nameof(request.Attacker.Stat)} is null",
                nameof(request)
            );
        }

        if (request.Defender.Stat == null)
        {
            throw new ArgumentException(
                $@"{nameof(request.Defender)}.{nameof(request.Defender.Stat)} is null",
                nameof(request)
            );
        }

        var expression = new Expression(expressionString);
        var baseDamage = request.BasePower;
        var isHeal = request.IsHeal || baseDamage < 0;

        if (baseDamage < 0)
        {
            baseDamage = Math.Abs(baseDamage);
        }

        var negate = isHeal;

        if (expression.Parameters == null)
        {
            throw new ArgumentNullException(nameof(expression.Parameters));
        }

        try
        {
            var critMultiplier = request.CriticalMultiplier;
            if (negate && !resolverOptions.AllowHealingCriticals)
            {
                critMultiplier = 1;
            }

            expression.Parameters["BaseDamage"] = baseDamage;
            expression.Parameters["ScalingStat"] = request.Attacker.Stat[(int)request.ScalingStat].Value();
            expression.Parameters["ScaleFactor"] = request.ScalingPercent / 100f;
            expression.Parameters["CritMultiplier"] = critMultiplier;
            expression.Parameters["A_Attack"] = request.Attacker.Stat[(int)Stat.Attack].Value();
            expression.Parameters["A_Defense"] = request.Attacker.Stat[(int)Stat.Defense].Value();
            expression.Parameters["A_Speed"] = request.Attacker.Stat[(int)Stat.Agility].Value();
            expression.Parameters["A_AbilityPwr"] = request.Attacker.Stat[(int)Stat.Intelligence].Value();
            expression.Parameters["A_MagicResist"] = request.Attacker.Stat[(int)Stat.Willpower].Value();
            expression.Parameters["A_Level"] = request.AttackerLevel ?? request.Attacker.Level;
            expression.Parameters["V_Attack"] = request.Defender.Stat[(int)Stat.Attack].Value();

            var defenderDefense = request.Defender.Stat[(int)Stat.Defense].Value();
            var defenderResist = request.Defender.Stat[(int)Stat.Willpower].Value();

            if (negate && resolverOptions.IgnoreHealingDefense)
            {
                defenderDefense = 0;
                defenderResist = 0;
            }

            expression.Parameters["V_Defense"] = defenderDefense;
            expression.Parameters["V_MagicResist"] = defenderResist;
            expression.Parameters["V_Speed"] = request.Defender.Stat[(int)Stat.Agility].Value();
            expression.Parameters["V_AbilityPwr"] = request.Defender.Stat[(int)Stat.Intelligence].Value();
            expression.Parameters["V_Level"] = request.Defender.Level;
            expression.Parameters["IsHeal"] = isHeal;
            expression.Parameters["PreventReflectChaining"] = resolverOptions.PreventReflectChaining;

            if (parameterOverrides != null)
            {
                foreach (var parameterOverride in parameterOverrides)
                {
                    expression.Parameters[parameterOverride.Key] = parameterOverride.Value;
                }
            }

            expression.EvaluateFunction += delegate(string name, FunctionArgs args)
            {
                ArgumentNullException.ThrowIfNull(args);

                if (name == "Random")
                {
                    args.Result = Random(args);
                }
            };

            var result = Convert.ToDouble(expression.Evaluate());
            if (negate)
            {
                result = -result;
            }

            return (long)Math.Round(result);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to evaluate damage formula", ex);
        }
    }

    private static int Random(FunctionArgs args)
    {
        if (args.Parameters == null)
        {
            throw new ArgumentNullException(nameof(args.Parameters));
        }

        var parameters = args.EvaluateParameters() ??
                         throw new NullReferenceException($"{nameof(args.EvaluateParameters)}() returned null.");

        if (parameters.Length < 2)
        {
            throw new ArgumentException($"{nameof(Random)}() requires 2 numerical parameters.");
        }

        var min = (int)Math.Round(
            (double)(parameters[0] ?? throw new NullReferenceException("First parameter is null."))
        );

        var max = (int)Math.Round(
            (double)(parameters[1] ?? throw new NullReferenceException("Second parameter is null."))
        );

        return min >= max ? min : Randomization.Next(min, max + 1);
    }
}
