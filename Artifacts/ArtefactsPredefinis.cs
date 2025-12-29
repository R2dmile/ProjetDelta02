using ProjetDelta.Player;

namespace ProjetDelta.Artifacts
{
    public static class ArtefactsPredefinis
    {
        /// <summary>
        /// Ajoute un bonus de degats plats sur chaque attaque.
        /// </summary>
        public static IArtefact Poison(int bonusDegats = 2)
        {
            const string nom = "Poison";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription($"+{bonusDegats} degats plats sur chaque attaque.")
                .OnAttack((Combattant source, Combattant cible, ref float degats) =>
                {
                    var avant = degats;
                    degats += bonusDegats;
                    source.LogArtefact(nom, $"{nom} sur {cible.Nom} ajoute +{bonusDegats} degats ({avant} -> {degats})", 0, false);
                })
                .Build();
        }

        /// <summary>
        /// Rend des PV a chaque attaque effectuee.
        /// </summary>
        public static IArtefact SoinSurAttaque(int soin = 1)
        {
            const string nom = "Soin sur attaque";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription($"Soigne {soin} PV quand tu attaques (si tu n'es pas full PV).")
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

        /// <summary>
        /// Soigne le porteur a chaque fois qu'il subit des degats (apres l'application du coup).
        /// </summary>
        public static IArtefact RegenerationSurCoup(int soin = 2)
        {
            const string nom = "Regeneration";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription($"Soigne {soin} PV au debut de ton tour (pas au-dessus du max).")
                .OnEquip(bearer =>
                {
                    bearer.OnTurnStart += () =>
                    {
                        if (!bearer.EstVivant()) return; // ne pas ressusciter une cible morte
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

        /// <summary>
        /// Reduit les degats recus de maniere lineaire.
        /// </summary>
        public static IArtefact ArmurePlate(int reduction = 1)
        {
            const string nom = "Armure plate";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription($"Reduit chaque coup recu de {reduction} degats.")
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    var avant = degats;
                    var apres = avant - reduction;
                    if (apres < 0) apres = 0;
                    degats = apres;
                    bearer.LogArtefact(nom, $"{nom} ({bearer.Nom}) reduit les degats de {avant} a {apres}", 0, false);
                })
                .Build();
        }

        /// <summary>
        /// Reduit les degats recus d'un pourcentage et log l'absorption.
        /// </summary>
        public static IArtefact BouclierPourcentage(float pourcentageReduction = 0.2f)
        {
            const string nom = "Bouclier";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription($"Absorbe {MathF.Round(pourcentageReduction * 100, 0)}% des degats recus.")
                .OnDamageTaken((Combattant bearer, ref float degats) =>
                {
                    var reduction = degats * pourcentageReduction;
                    degats -= reduction;
                    if (reduction > 0)
                        bearer.LogArtefact(nom, $"{nom} ({bearer.Nom}) absorbe {MathF.Round(reduction,2)} degats", 0);
                })
                .Build();
        }

        /// <summary>
        /// Vol de vie : soigne un pourcentage des degats reels infliges a la cible.
        /// </summary>
        public static IArtefact VolDeVie(float pourcentage = 0.3f)
        {
            const string nom = "Vol de vie";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription($"Soigne {MathF.Round(pourcentage * 100, 0)}% des degats reels infliges.")
                .OnAttack((Combattant source, Combattant cible, ref float degats) =>
                {
                    Action<float, Combattant?>? handler = null;
                    handler = (degatsSubis, attaquant) =>
                    {
                        cible.OnTakeDamage -= handler;
                        if (degatsSubis <= 0) return;
                        var soin = MathF.Round(degatsSubis * pourcentage, 2);
                        if (soin <= 0) return;
                        if (source.PV < source.PVMax)
                        {
                            var avant = source.PV;
                            source.Heal(soin);
                            var apres = source.PV;
                            source.LogArtefact(nom, $"{nom} soigne {source.Nom} de {soin} pv ({avant}->{apres})", soin, false);
                        }
                    };
                    // On se branche sur le prochain degat recu par la cible (celui de cette attaque).
                    cible.OnTakeDamage += handler;
                })
                .Build();
        }

        /// <summary>
        /// Epines : renvoie une partie des degats recus a l'attaquant (flat + pourcentage).
        /// </summary>
        public static IArtefact Epines(float pourcentage = 0.2f, float flat = 0f)
        {
            const string nom = "Epines";
            return new ArtefactBuilder()
                .AvecNom(nom)
                .AvecDescription($"Renvoie {MathF.Round(pourcentage * 100, 0)}% + {flat} degats a l'attaquant quand tu es touche.")
                .OnEquip(bearer =>
                {
                    bearer.OnTakeDamage += (degatsRecus, attaquant) =>
                    {
                        if (attaquant == null || !attaquant.EstVivant()) return;

                        var renvoi = flat + degatsRecus * pourcentage;
                        renvoi = MathF.Round(renvoi, 2);
                        if (renvoi <= 0) return;

                        var pvAvant = attaquant.PV;
                        // Degats directs sans declencher les autres artefacts/handlers.
                        attaquant.SubirDegatsSansEffets(renvoi);
                        var pvApres = attaquant.PV;

                        bearer.LogArtefact(nom, $"{nom} renvoie {renvoi} degats a {attaquant.Nom} ({pvAvant}->{pvApres})", 0, false);
                    };
                })
                .Build();
        }
    }
}
