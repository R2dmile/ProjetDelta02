using ProjetDelta.Data.DataClasses;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.Player;
using System.Runtime.CompilerServices;

namespace ProjetDelta.Artifacts
{
    public static class ArtefactFactory
    {
        private static readonly ConditionalWeakTable<Combattant, HashSet<string>> DamageMultHandlers = new();
        private static readonly ConditionalWeakTable<Combattant, HashSet<string>> PoisonHandlers = new();
        private static readonly ConditionalWeakTable<Combattant, HashSet<string>> ThornsHandlers = new();
        private static readonly ConditionalWeakTable<Combattant, Dictionary<string, float>> SpeedPctApplied = new();
        private static readonly ConditionalWeakTable<Combattant, Dictionary<string, float>> XpBonusApplied = new();
        private static readonly ConditionalWeakTable<Combattant, Dictionary<string, float>> GoldBonusApplied = new();
        private static readonly ConditionalWeakTable<Combattant, HashSet<string>> XpFlatHandlers = new();
        private static readonly ConditionalWeakTable<Combattant, HashSet<string>> ShieldPctHandlers = new();
        private static readonly ConditionalWeakTable<Combattant, HashSet<string>> SpecialShredHandlers = new();
        private class PrudentTalismanState
        {
            public bool Attached;
            public int ChargesConfigured;
            public int ChargesLeft;
        }
        private static readonly ConditionalWeakTable<Combattant, PrudentTalismanState> PrudentStates = new();
        private static readonly HashSet<string> SpecialArtefactNames = BuildSpecialArtefactNames();

        public static IArtefact FromDto(ArtefactDto dto)
        {
            var type = dto.Type?.Trim().ToLowerInvariant() ?? "";
            var nom = string.IsNullOrWhiteSpace(dto.Nom) ? dto.Type : dto.Nom;
            var desc = dto.Description;
            var v1 = dto.Valeur ?? 0;
            var v2 = dto.Valeur2 ?? 0;
            var color = ResolveColor(dto);

            var artefact = type switch
            {
                "poison" => BuildPoison(nom, desc, (int)v1),
                "heal_on_attack" or "soin_attaque" => BuildSoinSurAttaque(nom, desc, (int)v1),
                "regen_on_hit" or "regeneration" => BuildRegeneration(nom, desc, (int)v1),
                "armure_plate" or "armor_flat" => BuildArmure(nom, desc, (int)v1),
                "bouclier_pct" or "shield_pct" => BuildBouclier(nom, desc, v1),
                "vol_de_vie" or "lifesteal" => BuildVolDeVie(nom, desc, v1),
                "epines" or "thorns" => BuildEpines(nom, desc, v1, v2),
                "vitesse_flat" => BuildSpeedFlat(nom, desc, v1),
                "vitesse_pct" => BuildSpeedPct(nom, desc, v1),
                "dmg_mult" => BuildDamageMultiplier(nom, desc, v1),
                "first_strike" => BuildFirstStrike(nom, desc, v1, v2),
                "charge" => BuildCharge(nom, desc, v1, v2),
                "stat_bonus" => BuildStatBonus(nom, desc, v1, v2),
                "force_mul" => BuildForceMultiplier(nom, desc, v1),
                "dex_mul" => BuildDexMultiplier(nom, desc, v1),
                "const_mul" => BuildConstMultiplier(nom, desc, v1),
                "esquive_pct" or "dodge_pct" => BuildEsquive(nom, desc, v1),
                "loot_bonus" or "drop_bonus" => BuildLootBonus(nom, desc, v1),
                "luck_stack" or "clover_stack" => BuildLuckStack(nom, desc, v1),
                "gold_bonus" or "gold_pct" => BuildGoldBonus(nom, desc, v1),
                "pyro_fury" => BuildPyroFury(nom, desc, v1),
                "xp_bonus" => BuildXpBonus(nom, desc, v1, v2),
                "combo_dmg_life" => BuildComboDmgLife(nom, desc, v1, v2),
                "combo_armor_thorns" => BuildComboArmorThorns(nom, desc, v1, v2),
                "extra_attack" => BuildExtraAttack(nom, desc),
                "rage_speed" => BuildRageSpeed(nom, desc, v1),
                "evasion_eye" => BuildEvasionEye(nom, desc, v1, v2),
                "charge_crit" => BuildChargeCrit(nom, desc, v1, v2),
                "inertie_stone" => BuildInertieStone(nom, desc, v1, v2),
                "prudent_talisman" => BuildPrudentTalisman(nom, desc, v1, v2),
                "magic_missile" => BuildMagicMissile(nom, desc, v1, v2),
                "burn_on_hit" => BuildBurnOnHit(nom, desc, v1),
                "burn_reflect" => BuildBurnReflect(nom, desc, v1),
                "burn_amp" => BuildBurnAmp(nom, desc, v1),
                "burn_extra_tick" => BuildBurnExtraTick(nom, desc),
                "burn_heal" => BuildBurnHeal(nom, desc, v1),
                "rage_on_hit" => BuildRageOnHit(nom, desc, v1, v2),
                "persistence_maxhp" => BuildPersistenceMaxHp(nom, desc, v1),
                "stat_per_fight" => BuildStatPerFight(nom, desc, v1, v2),
                "last_stand" => BuildLastStand(nom, desc),
                "epic_clone_once" => BuildEpicCloneOnce(nom, desc),
                "rare_clone_once" => BuildRareCloneOnce(nom, desc),
                "special_clone_once" => BuildSpecialCloneOnce(nom, desc),
                "special_shred" => BuildSpecialShred(nom, desc),
                "grant_special" => BuildGrantSpecial(nom, desc),
                "shadow_dagger" => BuildShadowDagger(nom, desc),
                "const_armor" => BuildConstArmor(nom, desc),
                "aura_catalyst" => BuildAuraCatalyst(nom, desc, v1),
                "hermes_pair" => BuildHermesPair(nom, desc, v1, v2),
                "rage_reactor" => BuildRageReactor(nom, desc, v1, v2),
                "loot_sword" => BuildLootSword(nom, desc, v1, v2),
                "ice_hammer" => BuildIceHammer(nom, desc, v1, v2),
                "common_doubler" => BuildCommonDoubler(nom, desc),
                "treasure_overload" => BuildTreasureOverload(nom, desc),
                "int_bonus" or "intel_bonus" => BuildIntelBonus(nom, desc, v1),
                "stat_custom" => BuildStatCustom(nom, desc, dto),
                _ => BuildAutre(nom, desc)
            };

            return ApplyColor(artefact, color);
        }

        private static IArtefact BuildPoison(string nom, string? desc, int bonus)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{bonus} degats plats sur chaque attaque.")
                .OnEquip(bearer =>
                {
                    var attached = PoisonHandlers.GetOrCreateValue(bearer);
                    if (attached.Contains(nom)) return;
                    attached.Add(nom);

                    bearer.OnAttack += (Combattant cible, ref float degats) =>
                    {
                        var count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        if (count <= 0) return;
                        var avant = degats;
                        var scaled = bonus * count * bearer.ArtefactDamageMult;
                        degats += scaled;
                        var suffix = count > 1 ? $" x{count}" : string.Empty;
                        bearer.LogArtefact(nom, $"{nom}{suffix} sur {cible.Nom} ajoute +{MathF.Round(scaled,2)} degats ({MathF.Round(avant,2)} -> {MathF.Round(degats,2)})", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildSoinSurAttaque(string nom, string? desc, int soin)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Soigne {soin} PV quand tu attaques (si tu n'es pas full PV).")
                .OnAttack((Combattant source, Combattant cible, ref float degats) =>
                {
                    if (source.PV < source.PVMax)
                    {
                        var avant = source.PV;
                        source.Heal(soin);
                        var apres = source.PV;
                        source.LogArtefact(nom, $"{nom} soigne {source.Nom} de {soin} pv ({avant}->{apres})", soin, false);
                    }
                })
                .Build();
        }

        private static IArtefact BuildRegeneration(string nom, string? desc, int soin)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Soigne {soin} PV au debut de ton tour (pas au-dessus du max).")
                .OnEquip(bearer =>
                {
                    bearer.OnTurnStart += () =>
                    {
                        if (!bearer.EstVivant()) return;
                        if (bearer.PV < bearer.PVMax)
                        {
                            var avant = bearer.PV;
                            bearer.Heal(soin);
                            var apres = bearer.PV;
                            bearer.LogArtefact(nom, $"{nom} soigne {bearer.Nom} de {soin} pv ({avant}/{bearer.PVMax}->{apres}/{bearer.PVMax})", soin, false);
                        }
                    };
                })
                .Build();
        }

        private static IArtefact BuildArmure(string nom, string? desc, int reduction)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"-{reduction} degats sur chaque coup recu.")
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    var avant = degats;
                    var apres = avant - reduction;
                    if (apres < 0) apres = 0;
                    degats = apres;
                    if (avant > 0 && apres < avant)
                        bearer.LogArtefact(nom, $"{nom} ({bearer.Nom}) reduit les degats de {avant} a {apres}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildBouclier(string nom, string? desc, float pct)
        {
            // Interprete la valeur comme des points de resistance (res) : reduction = res / (100 + res)
            // Pour compat desc anciennes < 1 (ex 0.1 = 10%), on convertit en points *100 => 10 pts ~ 9.1% DR
            float pointsParArtefact = pct <= 1 ? pct * 100f : pct;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Bouclier : {pointsParArtefact:0.#} points de resistance (DR = res/(100+res), lineaire sur les copies).")
                .OnEquip(bearer =>
                {
                    var attached = ShieldPctHandlers.GetOrCreateValue(bearer);
                    if (attached.Contains(nom)) return;
                    attached.Add(nom);

                    bearer.OnDamageTaken += (ref float degats) =>
                    {
                        var count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        if (count <= 0) return;
                        float totalPoints = pointsParArtefact * count;
                        float reductionPct = totalPoints / (100f + totalPoints);
                        var reduction = degats * reductionPct;
                        degats -= reduction;
                        if (reduction > 0)
                        {
                            var suffix = count > 1 ? $" (x{count})" : string.Empty;
                            bearer.LogArtefact(nom, $"{nom}{suffix} absorbe {MathF.Round(reduction, 2)} degats (res {totalPoints:0.#}, DR {reductionPct*100:0.#}%)", 0, false);
                        }
                    };
                })
                .Build();
        }

        private static IArtefact BuildVolDeVie(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Soigne {MathF.Round(pct * 100, 0)}% des degats reels infliges.")
                .OnAttack((Combattant source, Combattant cible, ref float degats) =>
                {
                    Action<float, Combattant?>? handler = null;
                    handler = (degatsSubis, attaquant) =>
                    {
                        cible.OnTakeDamage -= handler;
                        if (degatsSubis <= 0) return;
                        var soin = MathF.Round(degatsSubis * pct, 2);
                        if (soin <= 0) return;
                        if (source.PV < source.PVMax)
                        {
                            var avant = source.PV;
                            source.Heal(soin);
                            var apres = source.PV;
                            source.LogArtefact(nom, $"{nom} soigne {source.Nom} de {soin} pv ({avant}->{apres})", soin, false);
                        }
                    };
                    cible.OnTakeDamage += handler;
                })
                .Build();
        }

        private static IArtefact BuildEpines(string nom, string? desc, float pct, float flat)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Renvoie {MathF.Round(pct*100,0)}% + {flat} degats a l'attaquant quand tu es touche.")
                .OnEquip(bearer =>
                {
                    var attached = ThornsHandlers.GetOrCreateValue(bearer);
                    if (attached.Contains(nom)) return;
                    attached.Add(nom);

                    bearer.OnTakeDamage += (degatsRecus, attaquant) =>
                    {
                        var cible = attaquant;
                        if (cible == null || !cible.EstVivant()) return;
                        var count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        if (count <= 0) return;
                        var renvoi = ((flat * count) + degatsRecus * pct * count) * bearer.ArtefactDamageMult;
                        renvoi = MathF.Round(renvoi, 2);
                        if (renvoi <= 0) return;

                        var suffix = count > 1 ? $" (x{count})" : string.Empty;
                        bearer.LogArtefact(nom, $"{nom}{suffix} renvoie {renvoi} degats imparables a {cible.Nom}", 0, false);

                        // Degats directs sans declencher d'autres handlers.
                        cible.SubirDegatsSansEffets(renvoi);
                    };
                })
                .Build();
        }

        private static IArtefact BuildSpeedFlat(string nom, string? desc, float flat)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{flat} vitesse (SPD)")
                .OnCombatStart(bearer =>
                {
                    bearer.AddSpeedBonus(flat, 0);
                    bearer.LogArtefact(nom, $"{nom} ({bearer.Nom}) augmente la vitesse de +{flat}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildSpeedPct(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,0)}% vitesse (SPD) par copie (effet lineaire)")
                .OnEquip(bearer =>
                {
                    var applied = SpeedPctApplied.GetOrCreateValue(bearer);
                    applied.TryGetValue(nom, out var prev);
                    // inclure la copie en cours d'equipement
                    int count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)) + 1;
                    float desired = pct * count;
                    float delta = desired - prev;
                    if (MathF.Abs(delta) > 0.0001f)
                    {
                        bearer.AddSpeedBonus(0, delta);
                        applied[nom] = desired;
                        var suffix = count > 1 ? $" (x{count})" : string.Empty;
                        bearer.LogArtefact(nom, $"{nom}{suffix} porte la vitesse totale a +{MathF.Round(desired*100,0)}% SPD", 0, false);
                    }
                })
                .Build();
        }

        private static IArtefact BuildDamageMultiplier(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,0)}% degats sur les attaques.")
                .OnEquip(bearer =>
                {
                    var attached = DamageMultHandlers.GetOrCreateValue(bearer);
                    if (attached.Contains(nom)) return;
                    attached.Add(nom);

                    bearer.OnAttack += (Combattant cible, ref float degats) =>
                    {
                        var count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        if (count <= 0) return;
                        var avant = degats;
                        var bonusPct = pct * count;
                        degats *= (1 + bonusPct);
                        var suffix = count > 1 ? $" x{count}" : string.Empty;
                        bearer.LogArtefact(nom, $"{nom}{suffix} augmente les degats ({MathF.Round(avant,2)} -> {MathF.Round(degats,2)})", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildFirstStrike(string nom, string? desc, float bonusDegats, float bonusPct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Premier coup du combat : +{bonusDegats} degats ou +{MathF.Round(bonusPct*100,0)}%")
                .OnEquip(bearer =>
                {
                    bool first = true;
                    bearer.OnCombatStart += () => first = true;
                    bearer.OnAttack += (Combattant cible, ref float degats) =>
                    {
                        if (!first) return;
                        first = false;
                        var avant = degats;
                        degats += bonusDegats;
                        degats *= (1 + bonusPct);
                        bearer.LogArtefact(nom, $"{nom} renforce le premier coup ({MathF.Round(avant,2)} -> {MathF.Round(degats,2)})", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildCharge(string nom, string? desc, float bonusVitesse, float malusVitesse)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Un tour rapide (+{bonusVitesse} SPD), un tour lent (-{malusVitesse} SPD) en alternance.")
                .OnEquip(bearer =>
                {
                    bool rapide = true;
                    float applique = 0;
                    bearer.OnTurnStart += () =>
                    {
                        // retirer l'ancien effet
                        bearer.AddSpeedBonus(-applique, 0);
                        // appliquer le nouveau
                        applique = rapide ? bonusVitesse : -MathF.Abs(malusVitesse);
                        bearer.AddSpeedBonus(applique, 0);
                        bearer.LogArtefact(nom, rapide
                            ? $"{nom} accelere ce tour (+{bonusVitesse} SPD)"
                            : $"{nom} ralentit ce tour (-{MathF.Abs(malusVitesse)} SPD)", 0, false);
                        rapide = !rapide;
                    };
                })
                .Build();
        }

        private static IArtefact BuildStatBonus(string nom, string? desc, float hpBonus, float atkBonus)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{hpBonus} PV max et +{atkBonus} attaque")
                .OnEquip(bearer =>
                {
                    bearer.AddStatBonus(hpBonus, atkBonus, 0);
                    bearer.LogArtefact(nom, $"{nom} octroie +{hpBonus} PV et +{atkBonus} attaque", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildForceMultiplier(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,0)}% d'attaque")
                .OnEquip(bearer =>
                {
                    var bonus = bearer.Force * pct;
                    bearer.AddStatBonus(0, bonus, 0);
                    bearer.LogArtefact(nom, $"{nom} augmente l'attaque de +{MathF.Round(bonus,2)}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildDexMultiplier(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,0)}% de vitesse")
                .OnEquip(bearer =>
                {
                    var bonus = bearer.Dexterite * 0.5f * pct;
                    bearer.AddStatBonus(0, 0, bonus);
                    bearer.LogArtefact(nom, $"{nom} augmente la vitesse de +{MathF.Round(bonus,2)}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildConstMultiplier(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,0)}% de PV max")
                .OnEquip(bearer =>
                {
                    var bonus = bearer.Constitution * 5f * pct;
                    bearer.AddStatBonus(bonus, 0, 0);
                    bearer.LogArtefact(nom, $"{nom} augmente les PV de +{MathF.Round(bonus,2)}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildEsquive(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"{MathF.Round(pct*100,1)}% de chance d'esquiver un coup (annule les degats)")
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    var rnd = new Random();
                    var chance = MathF.Min(1f, MathF.Max(0f, pct * Combattant.GlobalDodgeMultiplier));
                    if (rnd.NextDouble() < chance)
                    {
                        degats = 0;
                        bearer.LogArtefact(nom, $"{nom} permet d'esquiver le coup !", 0, false);
                    }
                })
                .Build();
        }

        private static IArtefact BuildLootBonus(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,1)}% chance de trouver un artefact apres un combat")
                .OnEquip(bearer =>
                {
                    bearer.AddLootDropBonus(pct);
                    var total = bearer.LootDropBonusPct * 100f;
                    bearer.LogArtefact(nom, $"{nom} augmente la chance de loot a {MathF.Round(total,1)}%", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildGoldBonus(string nom, string? desc, float pct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,1)}% d'or gagne sur les recompenses par copie (effet lineaire)")
                .OnEquip(bearer =>
                {
                    var applied = GoldBonusApplied.GetOrCreateValue(bearer);
                    applied.TryGetValue(nom, out var prev);
                    int count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)) + 1;
                    float desired = pct * count;
                    float delta = desired - prev;
                    if (MathF.Abs(delta) > 0.0001f)
                    {
                        bearer.AddGoldBonus(delta);
                        applied[nom] = desired;
                        var suffix = count > 1 ? $" (x{count})" : string.Empty;
                        bearer.LogArtefact(nom, $"{nom}{suffix} porte le bonus or a +{MathF.Round(desired*100,1)}%", 0, false);
                    }
                })
                .Build();
        }

        private static IArtefact BuildLuckStack(string nom, string? desc, float pct)
        {
            float inc = pct <= 0 ? 0.03f : pct;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(inc*100,1)}% chance d'artefact supplémentaire apres chaque combat (cumulatif).")
                .OnEquip(bearer =>
                {
                    bearer.OnCombatEnd += () =>
                    {
                        bearer.AddLootDropBonus(inc);
                        bearer.LogArtefact(nom, $"{nom} augmente ta chance de butin de +{MathF.Round(inc*100,1)}% (cumulatif)", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildPyroFury(string nom, string? desc, float burnStacks)
        {
            float stacks = burnStacks <= 0 ? 2f : burnStacks;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Au debut de ton tour ET quand tu es touche, applique {stacks} brulures a ta cible/assaillant.")
                .OnEquip(bearer =>
                {
                    // Debut de tour : cible actuelle
                    bearer.OnTurnStart += () =>
                    {
                        var cible = bearer.CurrentTarget;
                        if (cible == null || !cible.EstVivant()) return;
                        float amount = stacks * bearer.ArtefactDamageMult;
                        cible.AddBurn(amount, bearer);
                        bearer.LogArtefact(nom, $"{nom} enflamme {cible.Nom} (+{MathF.Round(amount,2)} brulure)", 0, false);
                    };
                    // Quand on est attaque : appliquer au frappeur
                    bearer.OnTakeDamage += (degatsRecus, attaquant) =>
                    {
                        if (attaquant == null || !attaquant.EstVivant()) return;
                        float amount = stacks * bearer.ArtefactDamageMult;
                        attaquant.AddBurn(amount, bearer);
                        bearer.LogArtefact(nom, $"{nom} riposte en enflammant {attaquant.Nom} (+{MathF.Round(amount,2)} brulure)", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildXpBonus(string nom, string? desc, float pct, float flatOnWin)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(pct*100,1)}% d'XP gagnee par copie" + (flatOnWin > 0 ? $" et +{flatOnWin} XP apres chaque combat (lineaire)" : ""))
                .OnEquip(bearer =>
                {
                    var applied = XpBonusApplied.GetOrCreateValue(bearer);
                    applied.TryGetValue(nom, out var prev);
                    int count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)) + 1;
                    float desired = pct * count;
                    float delta = desired - prev;
                    if (MathF.Abs(delta) > 0.0001f)
                    {
                        bearer.AddXpBonus(delta);
                        applied[nom] = desired;
                        var suffix = count > 1 ? $" (x{count})" : string.Empty;
                        bearer.LogArtefact(nom, $"{nom}{suffix} porte le bonus XP a +{MathF.Round(desired*100,1)}%", 0, false);
                    }

                    if (flatOnWin > 0)
                    {
                        var hooks = XpFlatHandlers.GetOrCreateValue(bearer);
                        if (hooks.Contains(nom)) return;
                        hooks.Add(nom);
                        bearer.OnCombatEnd += () =>
                        {
                            if (bearer is Joueur j)
                            {
                                int c = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                                if (c <= 0) return;
                                var gain = (int)(flatOnWin * c);
                                j.GagnerExperience(gain);
                                j.LogArtefact(nom, $"{nom} ajoute +{gain} XP apres le combat (x{c})", 0, false);
                            }
                        };
                    }
                })
                .Build();
        }

        private static IArtefact BuildComboDmgLife(string nom, string? desc, float dmgPct, float lifePct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{MathF.Round(dmgPct*100,0)}% degats et {MathF.Round(lifePct*100,0)}% vol de vie sur tes attaques.")
                .OnAttack((Combattant source, Combattant cible, ref float degats) =>
                {
                    var avant = degats;
                    degats *= (1 + dmgPct);
                    source.LogArtefact(nom, $"{nom} augmente les degats ({MathF.Round(avant,2)} -> {MathF.Round(degats,2)})", 0, false);

                    Action<float, Combattant?>? handler = null;
                    handler = (degatsSubis, attaquant) =>
                    {
                        cible.OnTakeDamage -= handler;
                        if (degatsSubis <= 0) return;
                        var soin = MathF.Round(degatsSubis * lifePct, 2);
                        if (soin <= 0) return;
                        if (source.PV < source.PVMax)
                        {
                            var avantSoin = source.PV;
                            source.Heal(soin);
                            var apresSoin = source.PV;
                            source.LogArtefact(nom, $"{nom} vole {soin} pv ({avantSoin}->{apresSoin})", soin, false);
                        }
                    };
                    cible.OnTakeDamage += handler;
                })
                .Build();
        }

        private static IArtefact BuildComboArmorThorns(string nom, string? desc, float reduction, float thornsPct)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Reduit les degats de {reduction} et renvoie {MathF.Round(thornsPct*100,0)}% a l'attaquant.")
                .OnEquip(bearer =>
                {
                    bearer.OnDamageTaken += (ref float degats) =>
                    {
                        var avant = degats;
                        degats -= reduction;
                        if (degats < 0) degats = 0;
                        if (avant != degats)
                            bearer.LogArtefact(nom, $"{nom} reduit les degats ({avant}->{degats})", 0, false);
                    };

                    bearer.OnTakeDamage += (degatsRecus, attaquant) =>
                    {
                        if (attaquant == null || !attaquant.EstVivant()) return;
                        var renvoi = MathF.Round(degatsRecus * thornsPct, 2);
                        if (renvoi <= 0) return;
                        renvoi *= bearer.ArtefactDamageMult;
                        renvoi = MathF.Round(renvoi, 2);
                        var pvAvant = attaquant.PV;
                        attaquant.SubirDegatsSansEffets(renvoi);
                        var pvApres = attaquant.PV;
                        bearer.LogArtefact(nom, $"{nom} renvoie {renvoi} degats a {attaquant.Nom} ({pvAvant}->{pvApres})", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildAutre(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom ?? "Artefact inconnu")
                .AvecDescription(desc ?? "Effet inconnu.")
                .Build();
        }

        private static IArtefact BuildExtraAttack(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Octroie une attaque supplementaire par tour.")
                .OnEquip(bearer =>
                {
                    bool bonusReady = false;

                    // On remplit le "chargeur" a chaque debut de tour
                    bearer.OnTurnStart += () => bonusReady = true;

                    bearer.OnAttack += (Combattant cible, ref float degats) =>
                    {
                        if (!bonusReady) return;
                        if (cible == null) return;
                        bonusReady = false;

                        // Declencher l'attaque bonus juste apres l'application des degats de l'attaque courante
                        Action<float, Combattant?>? afterDamage = null;
                        afterDamage = (float dmg, Combattant? attaquant) =>
                        {
                            cible.OnTakeDamage -= afterDamage;
                            if (!bearer.EstVivant()) return;

                            var bonusTarget = cible.EstVivant() ? cible : (bearer.CurrentTarget ?? cible);
                            if (bonusTarget == null || !bonusTarget.EstVivant()) return;

                            bearer.Attaquer(bonusTarget, 1f);
                            bearer.LogArtefact(nom, $"{nom} permet une attaque supplementaire sur {bonusTarget.Nom}", 0, false);
                        };
                        cible.OnTakeDamage += afterDamage;
                    };
                })
                .Build();
        }

        private static IArtefact BuildRageSpeed(string nom, string? desc, float flatGain)
        {
            float bonusAccumule = 0f;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{flatGain} SPD a chaque attaque (temporaire jusqu'a la fin du combat)")
                .OnEquip(bearer =>
                {
                    bearer.OnAttack += (Combattant cible, ref float degats) =>
                    {
                        int count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        float inc = flatGain * MathF.Max(1, count);
                        bonusAccumule += inc;
                        bearer.AddSpeedBonus(inc, 0);
                        var suffix = count > 1 ? $" (x{count})" : string.Empty;
                        bearer.LogArtefact(nom, $"{nom}{suffix} ajoute +{inc} SPD ce combat (total +{bonusAccumule})", 0, false);
                    };
                    bearer.OnCombatEnd += () =>
                    {
                        if (bonusAccumule != 0)
                        {
                            bearer.AddSpeedBonus(-bonusAccumule, 0);
                            bonusAccumule = 0;
                        }
                    };
                })
                .Build();
        }

        private static IArtefact BuildEvasionEye(string nom, string? desc, float pct, float unused)
        {
            // pct attendu : 0.20 (20% d'esquive)
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Tant que tu es sous 30% PV, tu as 20% de chances d'esquiver chaque coup.")
                .OnEquip(bearer =>
                {
                    bearer.OnDamageTaken += (ref float degats) =>
                    {
                        if (bearer.PV <= bearer.PVMax * 0.3f)
                        {
                            var chance = MathF.Min(1f, MathF.Max(0f, pct * Combattant.GlobalDodgeMultiplier));
                            if (new Random().NextDouble() < chance)
                            {
                                degats = 0;
                                bearer.LogArtefact(nom, $"{nom} permet d'esquiver le coup !", 0, false);
                            }
                        }
                    };
                })
                .Build();
        }

        private static IArtefact BuildChargeCrit(string nom, string? desc, float unused, float unused2)
        {
            // Toutes les 3 attaques, la suivante fait +50% degats
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Toutes les 3 attaques, la suivante inflige +50% degats.")
                .OnEquip(bearer =>
                {
                    int compteur = 0;
                    bearer.OnAttack += (Combattant cible, ref float degats) =>
                    {
                        compteur++;
                        if (compteur >= 3)
                        {
                            compteur = 0;
                            var avant = degats;
                            degats *= 1.5f;
                            bearer.LogArtefact(nom, $"{nom} declenche un coup charge (+50%): {MathF.Round(avant,2)} -> {MathF.Round(degats,2)}", 0, false);
                        }
                    };
                    bearer.OnCombatEnd += () => compteur = 0;
                })
                .Build();
        }

        private static IArtefact BuildInertieStone(string nom, string? desc, float dmgRedPct, float spdMalusPct)
        {
            // dmgRedPct attendu : 0.10, spdMalusPct attendu : 0.05
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "-10% degats recus, -5% SPD.")
                .OnEquip(bearer =>
                {
                    bearer.OnDamageTaken += (ref float degats) =>
                    {
                        var avant = degats;
                        degats *= MathF.Max(0, 1 - dmgRedPct);
                        bearer.LogArtefact(nom, $"{nom} reduit les degats ({MathF.Round(avant,2)} -> {MathF.Round(degats,2)})", 0, false);
                    };
                    bearer.AddSpeedBonus(0, -spdMalusPct);
                })
                .Build();
        }

        private static IArtefact BuildPrudentTalisman(string nom, string? desc, float unused, float unused2)
        {
            // Premiere fois qu'un coup est recu, il est reduit de 50%
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "La premiere fois qu'un coup te touche dans un combat, il est reduit de 50%.")
                .OnEquip(bearer =>
                {
                    var state = PrudentStates.GetOrCreateValue(bearer);
                    // chaque copie ajoute une charge de mitigation
                    state.ChargesConfigured = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)) + 1;
                    state.ChargesLeft = state.ChargesConfigured;

                    if (!state.Attached)
                    {
                        state.Attached = true;
                        void ResetCharges()
                        {
                            state.ChargesLeft = state.ChargesConfigured;
                        }

                        bearer.OnDamageTaken += (ref float degats) =>
                        {
                            if (state.ChargesLeft <= 0) return;
                            state.ChargesLeft--;
                            var avant = degats;
                            degats *= 0.5f;
                            var suffix = state.ChargesLeft > 0 ? $" (charges restantes : {state.ChargesLeft})" : string.Empty;
                            bearer.LogArtefact(nom, $"{nom} amortit un coup ({MathF.Round(avant,2)} -> {MathF.Round(degats,2)}){suffix}", 0, false);
                        };
                        bearer.OnCombatStart += ResetCharges;
                        bearer.OnCombatEnd += ResetCharges;
                    }

                    bearer.LogArtefact(nom, $"{nom} prepare {state.ChargesConfigured} blocage(s) a 50% pour ce combat", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildBurnOnHit(string nom, string? desc, float burn)
        {
            float b = burn <= 0 ? 1f : burn;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Applique {b} de brulure a chaque attaque (1 degat par tour).")
                .OnAttack((Combattant source, Combattant cible, ref float degats) =>
                {
                    cible.AddBurn(b * source.ArtefactDamageMult, source);
                    source.LogArtefact(nom, $"{nom} applique {MathF.Round(b * source.ArtefactDamageMult,2)} brulure a {cible.Nom}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildBurnReflect(string nom, string? desc, float ratio)
        {
            float r = ratio <= 0 ? 0.33f : ratio;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Quand tu recois de la brulure, renvoie {MathF.Round(r*100,0)}% de cette brulure a l'ennemi (pas de boucle infinie).")
                .OnEquip(bearer =>
                {
                    bearer.OnBurnApplied += (amount, source) =>
                    {
                        if (source == null || source == bearer) return;
                        // eviter boucle : ne pas reagir si la source possede aussi cet artefact
                        bool sourceHasSame = source.Artefacts.Any(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        if (sourceHasSame) return;
                        var reflected = amount * r;
                        if (reflected > 0 && source != null)
                        {
                            source.AddBurn(reflected, bearer);
                            bearer.LogArtefact(nom, $"{nom} renvoie {MathF.Round(reflected,2)} brulure a {source.Nom}", 0, false);
                        }
                    };
                })
                .Build();
        }

        private static IArtefact BuildBurnAmp(string nom, string? desc, float mult)
        {
            float add = mult <= 0 ? 1f : mult; // +100% = 1f
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Double les brulures appliquees (+100%). Cumule lineairement.")
                .OnEquip(bearer =>
                {
                    bearer.AddBurnMultiplier(add);
                    int count = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)) + 1;
                    var suffix = count > 1 ? $" (x{count})" : string.Empty;
                    bearer.LogArtefact(nom, $"{nom}{suffix} amplifie les brulures : +{MathF.Round(add*100,0)}% multiplicateur applique", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildBurnExtraTick(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Quand un ennemi t'attaque, sa brulure se declenche une fois de plus a la fin de son tour.")
                .OnEquip(bearer =>
                {
                    bearer.OnTakeDamage += (degatsRecus, attaquant) =>
                    {
                        if (attaquant == null || !attaquant.EstVivant()) return;
                        var burn = attaquant.BurnStacks;
                        if (burn <= 0) return;
                        // infliger un tick supplementaire de brulure immediatement
                        attaquant.SubirDegatsSansEffets(burn);
                        bearer.LogArtefact(nom, $"{nom} declenche une brulure supplementaire sur {attaquant.Nom} (-{MathF.Round(burn,2)})", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildBurnHeal(string nom, string? desc, float pct)
        {
            float ratio = pct <= 0 ? 0.3f : pct;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Quand tu subis des degats de brulure, tu te soignes de {MathF.Round(ratio*100,1)}% des degats subis.")
                .OnEquip(bearer =>
                {
                    bearer.OnBurnDamage += dmg =>
                    {
                        if (dmg <= 0) return;
                        var soin = MathF.Round(dmg * ratio, 2);
                        if (soin <= 0) return;
                        var avant = bearer.PV;
                        bearer.Heal(soin);
                        var apres = bearer.PV;
                        bearer.LogArtefact(nom, $"{nom} convertit la brulure en soin +{soin} ({avant}->{apres})", soin, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildPersistenceMaxHp(string nom, string? desc, float gain)
        {
            float inc = gain <= 0 ? 3f : gain;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Apres chaque combat gagne : +{inc} PV max (permanent).")
                .OnEquip(bearer =>
                {
                    bearer.OnCombatEnd += () =>
                    {
                        if (bearer is Joueur j)
                        {
                            j.AddStatBonus(inc, 0, 0);
                            j.LogArtefact(nom, $"{nom} renforce ton corps : +{inc} PV max permanents", inc, false);
                        }
                    };
                })
                .Build();
        }

        private static IArtefact BuildStatPerFight(string nom, string? desc, float atkGain, float hpGain)
        {
            float atkInc = atkGain < 0 ? 0f : atkGain;
            float hpInc = hpGain < 0 ? 0f : hpGain;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Apres chaque combat gagne : +{atkInc} ATK et +{hpInc} PV max permanents.")
                .OnEquip(bearer =>
                {
                    bearer.OnCombatEnd += () =>
                    {
                        if (bearer is Joueur j)
                        {
                            j.AddStatBonus(hpInc, atkInc, 0);
                            j.LogArtefact(nom, $"{nom} renforce tes stats : +{hpInc} PV max, +{atkInc} ATK permanents", hpInc, false);
                        }
                    };
                })
                .Build();
        }

        private static IArtefact BuildLastStand(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Prochaine mort annulee : reste a 1 PV et attaque immediatement 2 fois. L'artefact se detruit apres usage.")
                .OnEquip(bearer =>
                {
                    bool used = false;
                    Combattant? lastAttacker = null;

                    bearer.OnTakeDamage += (degatsRecus, attaquant) =>
                    {
                        if (attaquant != null) lastAttacker = attaquant;
                    };

                    DamageTakenHandler? handler = null;
                    handler = (ref float degats) =>
                    {
                        if (used) return;
                        // Anticiper la mort : si le degat ferait tomber a 0 ou moins
                        if (bearer.PV - degats > 0) return;
                        used = true;
                        // Annule le degat fatal
                        degats = 0;
                        // Remonter a 1 PV
                        if (bearer.PV > 1)
                        {
                            bearer.SubirDegatsSansEffets(bearer.PV - 1);
                        }
                        else if (bearer.PV <= 0)
                        {
                            bearer.SubirDegatsSansEffets(bearer.PV - 1); // fixe PV=1 meme si negatif
                        }
                        bearer.LogArtefact(nom, $"{nom} te sauve : tu restes a 1 PV et contre-attaques !", 0, true);

                        var cible = lastAttacker ?? bearer.CurrentTarget;
                        if (cible != null && cible.EstVivant())
                        {
                            bearer.Attaquer(cible);
                            if (cible.EstVivant())
                                bearer.Attaquer(cible);
                        }

                        // Detruire l'artefact et detacher le handler
                        bearer.Artefacts.RemoveAll(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        bearer.OnDamageTaken -= handler!;
                    };

                    bearer.OnDamageTaken += handler;
                })
                .Build();
        }

        private static IArtefact BuildEpicCloneOnce(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Le prochain artefact epique obtenu est duplique 2 fois, puis cet artefact est detruit.")
                .OnEquip(bearer =>
                {
                    // Hook sur OnEquip pour surveiller le prochain artefact
                    Action? handler = null;
                    bool used = false;
                    handler = () =>
                    {
                        if (used) return;
                        if (bearer.Artefacts.Count == 0) return;
                        var last = bearer.Artefacts.Last();
                        // Si l'artefact qui vient d'etre ajoute est ce cloneur, ignorer
                        if (ReferenceEquals(last, bearer.Artefacts.FirstOrDefault(a => a.Nom == nom))) return;

                        // Verifier rarete epique
                        var dto = ArtefactLoader.GetDtos().FirstOrDefault(d =>
                            string.Equals(d.Nom, last.Nom, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(d.Id, last.Nom, StringComparison.OrdinalIgnoreCase));
                        var isEpic = dto != null && dto.Rarity != null && dto.Rarity.Equals("epic", StringComparison.OrdinalIgnoreCase);
                        if (!isEpic) return;

                        used = true;
                        // Desabonner pour eviter toute reentrance pendant les equips suivants
                        bearer.OnEquip -= handler!;

                        // Dupliquer 2 fois
                        var cloneDtoId = dto.Id ?? dto.Nom ?? "";
                        var clone1 = ArtefactLoader.CreerInstance(cloneDtoId);
                        var clone2 = ArtefactLoader.CreerInstance(cloneDtoId);
                        if (clone1 != null && bearer is Joueur j1)
                        {
                            j1.EquipArtefact(clone1);
                            j1.LogArtefact(nom, $"{nom} duplique {clone1.Nom}", 0, false);
                        }
                        if (clone2 != null && bearer is Joueur j2)
                        {
                            j2.EquipArtefact(clone2);
                            j2.LogArtefact(nom, $"{nom} duplique {clone2.Nom}", 0, false);
                        }

                        // Retirer le cloneur
                        bearer.Artefacts.RemoveAll(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                    };
                    bearer.OnEquip += handler;
                })
                .Build();
        }

        private static IArtefact BuildRareCloneOnce(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Le prochain artefact rare obtenu est duplique deux fois, puis cet artefact se brise.")
                .OnEquip(bearer =>
                {
                    Action? handler = null;
                    bool used = false;
                    handler = () =>
                    {
                        if (used) return;
                        if (bearer.Artefacts.Count == 0) return;
                        var last = bearer.Artefacts.Last();
                        if (last.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)) return;

                        var dto = ArtefactLoader.GetDtos().FirstOrDefault(d =>
                            string.Equals(d.Nom, last.Nom, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(d.Id, last.Nom, StringComparison.OrdinalIgnoreCase));
                        if (dto == null || !string.Equals(dto.Rarity, "rare", StringComparison.OrdinalIgnoreCase))
                            return;

                        used = true;
                        bearer.OnEquip -= handler!;

                        // se detruire d'abord pour eviter des boucles/eventuels empilements
                        bearer.Artefacts.RemoveAll(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));

                        // cloner deux fois
                        var targetId = dto.Id ?? dto.Nom ?? "";
                        var clone1 = ArtefactLoader.CreerInstance(targetId);
                        var clone2 = ArtefactLoader.CreerInstance(targetId);
                        if (clone1 != null)
                        {
                            bearer.EquipArtefact(clone1);
                            bearer.LogArtefact(nom, $"{nom} duplique {clone1.Nom}", 0, false);
                        }
                        if (clone2 != null)
                        {
                            bearer.EquipArtefact(clone2);
                            bearer.LogArtefact(nom, $"{nom} duplique {clone2.Nom} une seconde fois", 0, false);
                        }
                    };
                    bearer.OnEquip += handler;
                })
                .Build();
        }

        private static IArtefact BuildSpecialCloneOnce(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Duplique deux fois un artefact special deja possede, puis se brise (ou attend le prochain special acquis).")
                .OnEquip(bearer =>
                {
                    Action? handler = null;
                    bool used = false;
                    handler = () =>
                    {
                        if (used) return;
                        var target = bearer.Artefacts.FirstOrDefault(a =>
                            !a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase) &&
                            ArtefactLoader.GetDtos().Any(d =>
                                (d.Id ?? d.Nom ?? "").Equals(a.Nom, StringComparison.OrdinalIgnoreCase) &&
                                d.Rarity != null && d.Rarity.Equals("special", StringComparison.OrdinalIgnoreCase)));
                        if (target == null) return;

                        var dto = ArtefactLoader.GetDtos().FirstOrDefault(d =>
                            (d.Id ?? d.Nom ?? "").Equals(target.Nom, StringComparison.OrdinalIgnoreCase));
                        if (dto == null) return;

                        used = true;
                        bearer.OnEquip -= handler!;

                        var clone1 = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                        var clone2 = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                        if (clone1 != null)
                        {
                            bearer.EquipArtefact(clone1);
                            bearer.LogArtefact(nom, $"{nom} duplique {clone1.Nom}", 0, false);
                        }
                        if (clone2 != null)
                        {
                            bearer.EquipArtefact(clone2);
                            bearer.LogArtefact(nom, $"{nom} duplique {clone2.Nom} une seconde fois", 0, false);
                        }

                        bearer.Artefacts.RemoveAll(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                    };
                    // tenter immediatement
                    handler();
                    // sinon, ecouter le prochain equip si rien a dupliquer encore
                    bearer.OnEquip += handler;
                })
                .Build();
        }

        private static IArtefact BuildSpecialShred(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "A chaque attaque, inflige 1 degat imparable par artefact special possede.")
                .OnEquip(bearer =>
                {
                    var attached = SpecialShredHandlers.GetOrCreateValue(bearer);
                    if (attached.Contains(nom)) return;
                    attached.Add(nom);

                    var specialsNow = bearer.Artefacts.Count(a => IsSpecialArtefact(a.Nom));
                    bearer.LogArtefact(nom, $"{nom} s'active : {specialsNow} artefact(s) speciaux detects", 0, false);

                    bearer.OnAttack += (Combattant cible, ref float degats) =>
                    {
                        int countCachet = bearer.Artefacts.Count(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                        if (countCachet <= 0) return;
                        int countSpecial = bearer.Artefacts.Count(a => IsSpecialArtefact(a.Nom));
                        if (countSpecial <= 0) return;
                        var dmg = countSpecial * countCachet * bearer.ArtefactDamageMult;
                        dmg = MathF.Round(dmg, 2);
                        var suffix = countCachet > 1 ? $" (x{countCachet})" : string.Empty;
                        cible.SubirDegatsSansEffets(dmg);
                        bearer.LogArtefact(nom, $"{nom}{suffix} transperce {cible.Nom} pour {dmg} degats imparables (specials:{countSpecial})", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildShadowDagger(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Chance d'esquive selon la DEX (1% par 50 DEX, max 75%).")
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    float chance = MathF.Min(0.75f, (bearer.Dexterite / 50f) * 0.01f);
                    if (chance <= 0) return;
                    var rnd = new Random();
                    var chanceEff = MathF.Min(1f, MathF.Max(0f, chance * Combattant.GlobalDodgeMultiplier));
                    if (rnd.NextDouble() < chanceEff)
                    {
                        degats = 0;
                        bearer.LogArtefact(nom, $"{nom} te fait disparaitre dans l'ombre : attaque esquivee", 0, false);
                    }
                })
                .Build();
        }

        private static IArtefact BuildConstArmor(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Reduit les degats finaux de (CON/15) apres autres reductions.")
                .OnEquip(bearer =>
                {
                    bearer.OnDamageTaken += (ref float degats) =>
                    {
                        var reduction = MathF.Floor(bearer.Constitution / 15f);
                        if (reduction <= 0) return;
                        var avant = degats;
                        degats -= reduction;
                        if (degats < 0) degats = 0;
                        if (avant > degats)
                            bearer.LogArtefact(nom, $"{nom} amortit ({avant}->{degats}) grace a la CON", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildGrantSpecial(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Octroie 3 artefacts speciaux uniques puis se detruit.")
                .OnEquip(bearer =>
                {
                    var ids = new[] { "special_guerrier", "special_mage", "special_berserker", "special_voleur" };
                    int count = 0;
                    foreach (var id in ids)
                    {
                        if (count >= 3) break;
                        var art = ArtefactLoader.CreerInstance(id);
                        if (art != null)
                        {
                            bearer.EquipArtefact(art);
                            bearer.LogArtefact(nom, $"{nom} t'offre {art.Nom}", 0, false);
                            count++;
                        }
                    }
                    bearer.Artefacts.RemoveAll(a => a.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase));
                })
                .Build();
        }

        private static IArtefact BuildMagicMissile(string nom, string? desc, float dmg, float shieldFlat)
        {
            float missileDmg = dmg == 0 ? 3.5f : dmg; // buff +0.5
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Special : A chaque attaque, lance un missile magique de {missileDmg} degats (multi-cast +1 par 25 INT) et erige un bouclier qui scale avec l'INT.")
                .OnAttack((Combattant source, Combattant cible, ref float degats) =>
                {
                    int casts = Math.Max(1, 1 + (int)MathF.Floor(source.Intelligence / 25f));
                    source.LogRepeatCount = casts;
                    source.LogRepeatUsed = false;
                    var extra = missileDmg * source.ArtefactDamageMult;
                    for (int i = 0; i < casts; i++)
                    {
                        cible.SubirDegatsSansEffets(extra);
                        source.LogArtefact(nom, $"{nom} frappe {cible.Nom} pour {MathF.Round(extra,2)} degats magiques", 0, false);
                    }
                    source.LogRepeatCount = 1;
                    source.LogRepeatUsed = false;
                })
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    // Bouclier plat qui scale avec l'INT (+2% par INT)
                    float baseShield = shieldFlat <= 0 ? 1f : shieldFlat;
                    float absorb = baseShield * bearer.ArtefactDamageMult;
                    var avant = degats;
                    degats -= absorb;
                    if (degats < 0) degats = 0;
                    if (avant > degats && absorb > 0)
                        bearer.LogArtefact(nom, $"{nom} absorbe {MathF.Round(avant - degats,2)} degats (bouclier INT)", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildAuraCatalyst(string nom, string? desc, float bonusPct)
        {
            // bonusPct attendu : 0.05 (5%)
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "+5% d'efficacite sur tes artefacts defensifs (degats recus -5%, soins +5%).")
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    var avant = degats;
                    degats *= MathF.Max(0, 1 - bonusPct);
                    bearer.LogArtefact(nom, $"{nom} amortit (+{bonusPct*100:0}% defense) {MathF.Round(avant,2)} -> {MathF.Round(degats,2)}", 0, false);
                })
                .OnEquip(bearer =>
                {
                    // petit boost aux soins entrants en reutilisant AddLootBonus? Non -> via OnTakeDamage hook impossible.
                    // On applique un bonus global de 5% sur les soins via AddStatBonus sur HP max pour simuler une meilleure sustain.
                    var bonus = bearer.PVMax * 0.05f;
                    bearer.AddStatBonus(bonus, 0, 0);
                    bearer.LogArtefact(nom, $"{nom} renforce ta resi : +{MathF.Round(bonus,2)} PV max (bonus defensif)", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildHermesPair(string nom, string? desc, float unused, float unused2)
        {
            // Pour chaque paire d'artefacts possedes, +1 DEX (~+0.25 SPD)
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Pour chaque paire d'artefacts possedes, +1 DEX (bonus de vitesse).")
                .OnEquip(bearer =>
                {
                    float currentBonus = 0f;
                    Action recalcul = () =>
                    {
                        // retirer l'ancien bonus
                        if (currentBonus != 0) bearer.AddSpeedBonus(-currentBonus, 0);
                        int paires = (bearer.Artefacts.Count) / 2;
                        currentBonus = paires * 0.25f; // 1 dex -> 0.25 SPD
                        if (currentBonus != 0)
                        {
                            bearer.AddSpeedBonus(currentBonus, 0);
                            bearer.LogArtefact(nom, $"{nom} accorde +{currentBonus:0.00} SPD grace aux paires d'artefacts", 0, false);
                        }
                    };
                    // initial
                    recalcul();
                    // se mettre a jour a chaque equip
                    bearer.OnEquip += recalcul;
                })
                .Build();
        }

        private static IArtefact BuildRageReactor(string nom, string? desc, float unused, float unused2)
        {
            // Chaque fois qu'on attaque, on simule une reaction aux bonus de SPD : +DEX SPD et +5% degats sur ce coup
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "A chaque bonus de vitesse (simule sur chaque attaque), +DEX SPD et +5% degats sur l'attaque.")
                .OnAttack((Combattant bearer, Combattant cible, ref float degats) =>
                {
                    bearer.AddSpeedBonus(bearer.Dexterite, 0);
                    var avant = degats;
                    degats *= 1.05f;
                    bearer.LogArtefact(nom, $"{nom} amplifie la rage: SPD +{bearer.Dexterite:0.##}, degats {MathF.Round(avant,2)} -> {MathF.Round(degats,2)}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildRageOnHit(string nom, string? desc, float atkGain, float spdGain)
        {
            float atk = atkGain <= 0 ? 1f : atkGain;
            float spd = spdGain <= 0 ? 0.25f : spdGain;
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"Chaque degat subi octroie +{atk} ATK et +{spd} SPD permanents.")
                .OnEquip(bearer =>
                {
                    bearer.OnTakeDamage += (degatsRecus, attaquant) =>
                    {
                        if (degatsRecus <= 0) return;
                        bearer.AddStatBonus(0, atk, spd);
                        bearer.LogArtefact(nom, $"{nom} renforce ta rage : +{atk} ATK, +{spd} SPD permanents", 0, false);
                    };
                })
                .Build();
        }

        private static IArtefact BuildLootSword(string nom, string? desc, float unused, float unused2)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Garanti un artefact commun apres chaque combat gagne.")
                .OnCombatEnd(bearer =>
                {
                    if (bearer is Joueur j)
                    {
                        var pool = ArtefactLoader.GetDtos()
                            .Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (pool.Count == 0) return;
                        var rand = new Random();
                        var dto = pool[rand.Next(pool.Count)];
                        var art = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                        if (art != null)
                        {
                            j.EquipArtefact(art);
                            var rarity = dto.Rarity ?? "common";
                            var color = rarity.Equals("epic", StringComparison.OrdinalIgnoreCase) ? "magenta"
                                        : rarity.Equals("rare", StringComparison.OrdinalIgnoreCase) ? "blue"
                                        : rarity.Equals("special", StringComparison.OrdinalIgnoreCase) ? "cyan"
                                        : "white";
                            j.LogArtefact(nom, $"{nom} depouille l'ennemi : {art.Nom} obtenu.", 0, false);
                            // message supplementaire pour le chat
                            j.LogArtefact(nom, $"[Loot Sword] Artefact commun garanti : {art.Nom}", 0, false);
                        }
                    }
                })
                .Build();
        }

        private static IArtefact BuildIceHammer(string nom, string? desc, float unused, float unused2)
        {
            // Glass canon : +30% degats infliges, +20% degats recus
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Marteau de glace : +30% degats infliges, mais +20% degats recus.")
                .OnAttack((Combattant bearer, Combattant cible, ref float degats) =>
                {
                    var avant = degats;
                    degats *= 1.3f;
                    var apres = degats;
                    // Log immédiatement (avant le log de degats finaux) pour l'ordre attendu
                    bearer.LogArtefact(nom, $"{nom} frappe fort : {MathF.Round(avant,2)} -> {MathF.Round(apres,2)}", 0, false);
                })
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    var avant = degats;
                    degats *= 1.2f;
                    bearer.LogArtefact(nom, $"{nom} fragilise le porteur : {MathF.Round(avant,2)} -> {MathF.Round(degats,2)}", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildCommonDoubler(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Double les artefacts communs quand ils entrent dans l'inventaire (copie instantanee).")
                  .OnEquip(bearer =>
                  {
                      bool handling = false;
                      bearer.OnEquip += () =>
                      {
                          if (handling) return;
                          handling = true;
                          try
                          {

                              // sécuriser sur l'artefact le plus récemment ajouté
                              var recents = bearer.Artefacts.LastOrDefault();
                              if (recents == null) return;
                              // ne pas dupliquer le CommonDoubler lui-même
                              if (recents.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase)) return;

                              var dto = ArtefactLoader.GetDtos().FirstOrDefault(d =>
                                  string.Equals(d.Nom, recents.Nom, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(d.Id, recents.Nom, StringComparison.OrdinalIgnoreCase));
                              var isCommon = dto == null || string.IsNullOrWhiteSpace(dto.Rarity) || dto.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase);
                              if (!isCommon) return;

                              var clone = ArtefactLoader.CreerInstance(dto?.Id ?? recents.Nom);
                              if (clone != null && bearer is Joueur j)
                              {
                                  j.EquipArtefact(clone);
                                  j.LogArtefact(nom, $"{nom} duplique l'artefact commun : {clone.Nom}", 0, false);
                              }
                          }
                          finally
                          {
                              handling = false;
                          }
                      };
                  })
                  .Build();
          }

        private static IArtefact BuildTreasureOverload(string nom, string? desc)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? "Ajoute un roll supplementaire d'or ET d'artefact dans chaque salle tresor.")
                .OnEquip(bearer =>
                {
                    bearer.AddTreasureOverload(1);
                    bearer.LogArtefact(nom, $"{nom} surcharge les coffres : roll tresor +1 (total {bearer.TreasureOverloadStacks})", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildIntelBonus(string nom, string? desc, float val)
        {
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? $"+{val} Intelligence (booste les effets d'artefacts et la rarete).")
                .OnEquip(bearer =>
                {
                    bearer.AddAttributeBonus(new AttributsDto { Intel = val });
                    bearer.LogArtefact(nom, $"{nom} octroie +{val} INT", 0, false);
                })
                .Build();
        }

        private static IArtefact BuildStatCustom(string nom, string? desc, ArtefactDto dto)
        {
            var bonus = new AttributsDto
            {
                Force = dto.Force ?? 0,
                Dex = dto.Dex ?? 0,
                Constitution = dto.Constitution ?? 0,
                Intel = dto.Intel ?? 0
            };
            string defaultDesc = $"Bonus stats : +{bonus.Force} FOR, +{bonus.Dex} DEX, +{bonus.Constitution} CON, +{bonus.Intel} INT.";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription(desc ?? defaultDesc)
                .OnEquip(bearer =>
                {
                    bearer.AddAttributeBonus(bonus);
                    bearer.LogArtefact(nom, $"{nom} offre +{bonus.Force} FOR, +{bonus.Dex} DEX, +{bonus.Constitution} CON, +{bonus.Intel} INT", 0, false);
                })
                .Build();
        }

        private static string ResolveColor(ArtefactDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Color))
                return dto.Color.Trim().ToLowerInvariant();

            var type = dto.Type?.Trim().ToLowerInvariant() ?? "";

            var rarity = dto.Rarity?.Trim().ToLowerInvariant();
            if (rarity == "legendary") return "gold";
            if (type.Contains("burn") || type.Contains("pyro") || type.Contains("fire"))
                return "orange";
            if (type.Contains("heal") || type.Contains("regen"))
                return "green";
            if (type.Contains("poison"))
                return "lime";
            if (type.Contains("lifesteal") || type.Contains("vol_de_vie"))
                return "magenta";
            if (type.Contains("armor") || type.Contains("armure") || type.Contains("bouclier") || type.Contains("shield"))
                return "blue";
            if (type.Contains("ice"))
                return "blue";
            if (type.Contains("speed") || type.Contains("vitesse") || type.Contains("hermes"))
                return "yellow";
            if (type.Contains("loot") || type.Contains("gold") || type.Contains("luck") || type.Contains("treasure"))
                return "gold";
            if (type.Contains("xp"))
                return "cyan";
            if (type.Contains("dodge") || type.Contains("esquive") || type.Contains("shadow"))
                return "teal";
            if (type.Contains("rage") || type.Contains("dmg") || type.Contains("charge"))
                return "red";
            if (type.Contains("stat") || type.Contains("bonus") || type.Contains("force") || type.Contains("dex") || type.Contains("const") || type.Contains("intel"))
                return "lime";
            if (type.Contains("clone"))
                return "purple";
            if (type.Contains("magic") || type.Contains("aura") || type.Contains("special"))
                return "cyan";

            return "green";
        }

        private static HashSet<string> BuildSpecialArtefactNames()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in ArtefactLoader.GetDtos())
            {
                if (!string.Equals(d.Rarity, "special", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(d.Id))
                    set.Add(d.Id.Trim());
                if (!string.IsNullOrWhiteSpace(d.Nom))
                    set.Add(d.Nom.Trim());
            }
            return set;
        }

        private static bool IsSpecialArtefact(string nom)
        {
            if (string.IsNullOrWhiteSpace(nom)) return false;
            if (SpecialArtefactNames.Contains(nom)) return true;

            var dto = ArtefactLoader.GetDtos().FirstOrDefault(d =>
                string.Equals(d.Id ?? string.Empty, nom, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.Nom ?? string.Empty, nom, StringComparison.OrdinalIgnoreCase));
            return dto != null && string.Equals(dto.Rarity, "special", StringComparison.OrdinalIgnoreCase);
        }

        private static IArtefact ApplyColor(IArtefact artefact, string color)
        {
            if (artefact is TriggerArtefact trigger)
            {
                trigger.SetColor(color);
            }
            return artefact;
        }
    }
}


