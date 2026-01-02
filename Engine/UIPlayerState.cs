using System.Collections.Generic;

namespace ProjetDelta.UI
{
    public record UIArtefact(string Nom, string Description, string Rarity, string Color, int Count = 1);

    public record UIPlayerState(
        string Nom,
        string Background,
        string Classe,
        int Niveau,
        int Etage,
        string Difficulte,
        int Argent,
        float PvCourant,
        float PvMax,
        float Attaque,
        float Vitesse,
        int PointsNonDepenses,
        IReadOnlyList<UIArtefact> Artefacts,
        bool EnVie
    );
}
