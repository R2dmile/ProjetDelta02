using ProjetDelta.Artifacts;
using ProjetDelta.Data.DataClasses;
using ProjetDelta.Logger;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Player
{

    public delegate void AttackHandler(Combattant cible, ref float degats);
    public delegate void DamageTakenHandler(ref float degats);
    public delegate void AfterAttackHandler(Combattant cible, float baseDegats, float finalDegats, float degatsInfliges);
    public abstract class Combattant
    {
        // Multiplicateurs globaux ajustes pendant les combats (sudden death)
        internal static float GlobalDamageMultiplier { get; set; } = 1f;
        internal static float GlobalHealMultiplier { get; set; } = 1f;
        internal static float GlobalDodgeMultiplier { get; set; } = 1f;
        protected Attributs _attributs = new Attributs();
        protected Stats _stats = new Stats();
        protected string _nom = string.Empty;
        protected string _couleur = "white";
        private float _spdFlatBonus = 0f;
        private float _spdPctBonus = 0f;

        public virtual string Nom => _nom;
        public virtual float PV => _stats.VieCourante;
        public virtual float PVMax => _stats.VieMax;
        public virtual float SPD => (_stats.Vitesse * (1 + _spdPctBonus)) + _spdFlatBonus;
        public virtual float Attaque => _stats.Attaque;
        public virtual string Couleur => _couleur;
        public float Force => _attributs?.GetForce() ?? 0;
        public float Dexterite => _attributs?.GetDex() ?? 0;
        public float Constitution => _attributs?.GetConstitution() ?? 0;
        public float Intelligence => _attributs?.GetIntel() ?? 0;
        public int IntelligenceStacks => (int)MathF.Floor(MathF.Max(0, Intelligence) / 100f); // +1 cast / 100 INT
        public int Darkness => _attributs?.GetDarkness() ?? 0;
        public float ArtefactDamageMult => 1f + MathF.Max(0, Intelligence) * 0.02f; // +2% par point d'INT (poison/epines/etc.)
        public float RareFindBonusPct => MathF.Max(0, Intelligence) * 0.005f; // +0.5% par INT (buff)
        public float LootDropBonusPct => _lootDropBonusPct;
        public float GoldBonusPct => _goldBonusPct;
        public float XpBonusPct => _xpBonusPct;
        internal int LogRepeatCount { get; set; } = 1;
        internal bool LogRepeatUsed { get; set; } = false;
        public float BurnStacks => _burnStacks;

        public event Action<float, Combattant?> OnTakeDamage;

        public event AttackHandler? OnAttack;
        public event DamageTakenHandler? OnDamageTaken;
        public event Action? OnCombatStart;
        public event Action? OnCombatEnd;
        public event Action? OnEquip;
        public event Action? OnTurnStart;
        public event AfterAttackHandler? OnAfterAttack;
        public List<IArtefact> Artefacts { get; private set; } = new();

        protected ICombatLogger _logger = null!;
        private float _lootDropBonusPct = 0f;
        private float _goldBonusPct = 0f;
        private float _xpBonusPct = 0f;
        private int _treasureOverloadStacks = 0;
        private float _burnStacks = 0f;
        private float _burnMultiplier = 1f;
        public event Action<float>? OnBurnDamage;
        internal Combattant? CurrentTarget { get; private set; }
        public int TreasureOverloadStacks => _treasureOverloadStacks;
        public event Action<float, Combattant?>? OnBurnApplied;
        public void DebutCombat()
        {
            OnCombatStart?.Invoke();
        }

        internal void SetCurrentTarget(Combattant? cible)
        {
            CurrentTarget = cible;
        }

        public void FinCombat()
        {
            OnCombatEnd?.Invoke();
            // reset brulures en fin de combat
            _burnStacks = 0f;
        }
        public virtual void SetLogger(ICombatLogger logger)
        {
            _logger = logger;
            // injecter un logger dans les artefacts déjà équipés pour les affichages de load/resume
            foreach (var art in Artefacts)
            {
                art.Register(this);
            }
        }

        public virtual void EquipArtefact(IArtefact artefact, bool applyOnEquip = true)
        {
            // Toujours appliquer l'effet propre a l'artefact (Register/EquipTo) pour reinitialiser ses hooks,
            // meme lors d'un chargement (applyOnEquip=false evite seulement de declencher les listeners globaux).
            artefact.EquipTo(this);
            artefact.Register(this);
            Artefacts.Add(artefact);
            if (applyOnEquip)
            {
                OnEquip?.Invoke();
            }
        }

        public void DebutTour()
        {
            OnTurnStart?.Invoke();
            if (_burnStacks > 0)
            {
                var dmg = _burnStacks;
                SubirDegatsSansEffets(dmg);
                OnBurnDamage?.Invoke(dmg);
                LogArtefact("Brulure", $"{Nom} subit {MathF.Round(dmg,2)} degats de brulure", dmg, true);
            }
        }

        public virtual void Attaquer(Combattant cible, float multiplicateurDegats = 1f)
        {
            float degatsBase = _stats.Attaque * multiplicateurDegats * GlobalDamageMultiplier;
            var attaqueLog = new CombatDataItem
            {
                Event = TypeEvenement.Attaque,
                NomAttaquant = _nom,
                NomRecevant = cible.Nom,
                CouleurAtt = Couleur,
                CouleurDef = cible.Couleur,
                DegatInflige = degatsBase // log l'attaque avant les modifs d'artefacts
            };
            _logger?.Log(attaqueLog);

            float degats = degatsBase;
            // L'attaquant peut modifier les degats via ses artefacts avant l'application
            OnAttack?.Invoke(cible, ref degats);
            degats = SanitizeDamage(degats);

            var degatsLog = new CombatDataItem
            {
                Event = TypeEvenement.Degats,
                NomAttaquant = _nom,
                NomRecevant = cible.Nom,
                CouleurAtt = Couleur,
                CouleurDef = cible.Couleur
            };

            cible.SubirDegats(degats, degatsLog, this);
            OnAfterAttack?.Invoke(cible, degatsBase, degats, degatsLog.DegatSubi);
        }

        public virtual void SubirDegats(float degats, CombatDataItem damageLog, Combattant? attaquant = null)
        {
            degats = SanitizeDamage(degats);
            // Les artefacts defensifs peuvent reagir/ajuster les degats recus
            OnDamageTaken?.Invoke(ref degats);
            degats = SanitizeDamage(degats);

            // Si aucun degat final, on ne loggue rien et on ne declenche pas les effets qui repondent aux degats (epines, etc.)
            if (degats <= 0)
            {
                return;
            }

            _stats.SubirDegats(degats);

            damageLog.Event = TypeEvenement.Degats;
            damageLog.CouleurDef = Couleur;
            damageLog.DegatSubi = degats;
            damageLog.NomRecevant = Nom;
            damageLog.HpCourant = _stats.VieCourante.ToString("0.##");
            damageLog.HpTotal = _stats.VieMax.ToString("0.##");

            _logger?.Log(damageLog);

            OnTakeDamage?.Invoke(degats, attaquant);
        }

        // Degats sans declencher les evenements/artefacts defensifs.
        public void SubirDegatsSansEffets(float degats)
        {
            degats = SanitizeDamage(degats);
            _stats.SubirDegats(degats);
        }

        public virtual bool EstVivant() => _stats.EstVivant();

        public virtual void Heal(float amount)
        {
            // Un soin est equivalent a infliger des degats negatifs.
            amount *= GlobalHealMultiplier;
            if (_stats.VieCourante >= _stats.VieMax) return;
            _stats.SubirDegats(-amount);
        }

        public void LogArtefact(string artefactNom, string message, float deltaPv = 0, bool includeHp = true)
        {
            // Aggreger les multicasts INT : log une seule fois avec xN
            if (LogRepeatCount > 1)
            {
                if (LogRepeatUsed) return;
                message = $"{message} x{LogRepeatCount}";
            LogRepeatUsed = true;
        }
        string hpCourant = includeHp ? _stats.VieCourante.ToString() : string.Empty;
        string hpTotal = includeHp ? _stats.VieMax.ToString() : string.Empty;
            var color = Artefacts.FirstOrDefault(a => a.Nom.Equals(artefactNom, StringComparison.OrdinalIgnoreCase))?.Color;
            if (string.IsNullOrWhiteSpace(color)) color = artefactNom.ToLower().Contains("brul") ? "orange" : "green";
            _logger?.Log(new CombatDataItem
            {
                Event = TypeEvenement.Artefact,
                NomAttaquant = artefactNom,
                NomRecevant = Nom,
                Message = message,
                DegatSubi = -deltaPv,
                HpCourant = hpCourant,
                HpTotal = hpTotal,
                Type = color,
                CouleurAtt = color,
                CouleurDef = Couleur,
            });
        }

        public void AddSpeedBonus(float flat = 0f, float pct = 0f)
        {
            _spdFlatBonus += flat;
            _spdPctBonus += pct;
        }

        public void AddLootBonus(float pct)
        {
            _lootDropBonusPct += pct;
        }

        public void AddXpBonus(float pct)
        {
            _xpBonusPct += pct;
        }

        public void AddStatBonus(float pv = 0f, float attaque = 0f, float vitesse = 0f)
        {
            _stats.AjouterBonus(pv, attaque, vitesse);
        }

        public void AddLootDropBonus(float pct)
        {
            _lootDropBonusPct += pct;
            if (_lootDropBonusPct < 0) _lootDropBonusPct = 0;
        }

        public void AddGoldBonus(float pct)
        {
            _goldBonusPct += pct;
            if (_goldBonusPct < 0) _goldBonusPct = 0;
        }

        public void AddTreasureOverload(int stacks = 1)
        {
            _treasureOverloadStacks += stacks;
            if (_treasureOverloadStacks < 0) _treasureOverloadStacks = 0;
        }

        public void AddBurn(float amount, Combattant? source = null)
        {
            if (amount <= 0) return;
            var applied = amount * MathF.Max(0, _burnMultiplier);
            if (applied <= 0) return;
            _burnStacks += applied;
            OnBurnApplied?.Invoke(applied, source);
        }

        public void AddBurnMultiplier(float add)
        {
            _burnMultiplier += add;
            if (_burnMultiplier < 0.1f) _burnMultiplier = 0.1f;
        }

        public void SetMetaBonuses(float goldPct, float lootPct)
        {
            _goldBonusPct = MathF.Max(0, goldPct);
            _lootDropBonusPct = MathF.Max(0, lootPct);
        }

        public void AddAttributeBonus(AttributsDto dto)
        {
            _attributs.AddAttributes(dto);
        }

        private static float SanitizeDamage(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return float.PositiveInfinity;
            }
            return value;
        }
    }
}
