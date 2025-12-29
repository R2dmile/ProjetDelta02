namespace ProjetDelta.Data.DataClasses
{
    using System.Text.Json.Serialization;

    public class Stats
    {
        public float VieMax { get; private set; }
        public float VieCourante { get; private set; }
        public float Attaque { get; private set; }
        public float Vitesse { get; private set; }

        [JsonConstructor]
        public Stats(float VieMax, float VieCourante, float Attaque, float Vitesse)
        {
            this.VieMax = VieMax;
            this.VieCourante = VieCourante;
            this.Attaque = Attaque;
            this.Vitesse = Vitesse;
            if (VieCourante > VieMax) VieCourante = VieMax;
        }

        // Constructeur par défaut utilisé dans le code (si aucun paramètre n'est fourni)
        public Stats(int vie = 100, int attaque = 3, int vitesse = 5)
            : this(vie, vie, attaque, vitesse)
        {
        }
        public void SubirDegats(float degats)
        {
            VieCourante -= degats;
            if (VieCourante < 0) VieCourante = 0;
            if (VieCourante > VieMax) VieCourante = VieMax;

        }
        public bool EstVivant() => VieCourante > 0;

        public void AjusterParAttributs(Attributs attr)
        {
            // Mapping revise : Force/3 -> ATK, Dex/4 -> VIT, Constitution -> PV (1:1)
            VieMax += attr.GetConstitution() * 2; // buff constitution: 2 PV par point
            Attaque += attr.GetForce() / 3f;
            Vitesse += attr.GetDex() / 4f;
            // S'assurer que les PV sont pleins apres recalcul
            VieCourante = VieMax;
        }

        public void AjouterBonus(float pv, float attaque, float vitesse)
        {
            VieMax += pv;
            VieCourante += pv;
            Attaque += attaque;
            Vitesse += vitesse;
            if (VieCourante > VieMax) VieCourante = VieMax;
            if (VieCourante < 0) VieCourante = 0;
        }
    }
}
