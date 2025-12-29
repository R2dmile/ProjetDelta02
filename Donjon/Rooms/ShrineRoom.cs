using ProjetDelta.Player;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using ProjetDelta.Data.DataLoader;
using ProjetDelta.Artifacts;

namespace ProjetDelta.Donjon.Rooms
{
    /// <summary>
    /// Salle sanctuaire : offre une petite benediction aleatoire (or, soin, artefact commun).
    /// Remplace les anciens skill checks.
    /// </summary>
    public class ShrineRoom : Room
    {
        private readonly string _name;
        private readonly string _description;
        private Room _nextRoom;

        public override string Name => _name;

        public ShrineRoom(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public override void ResolveLinks(Dictionary<string, Room> allRooms)
        {
            if (_nextRoomName != null && allRooms.TryGetValue(_nextRoomName, out var resolved))
                _nextRoom = resolved;
        }

        public override Room GetNextRoom() => _nextRoom;

        public override void ResolveRoom(IUI ui, Joueur joueur)
        {
            ui.Afficher($"Sanctuaire : {_name}");
            ui.Afficher(_description);

            var rand = new Random();
            double roll = rand.NextDouble();
            if (roll < 0.4)
            {
                int gold = rand.Next(10, 26);
                joueur.AjouterArgent(gold);
                ui.Afficher($"Une aura bienveillante te donne {gold} or. (Argent total : {joueur.Argent})");
            }
            else if (roll < 0.7)
            {
                float heal = MathF.Max(5, joueur.PVMax * 0.2f);
                var avant = joueur.PV;
                joueur.Heal(heal);
                ui.Afficher($"Une energie curative te restaure ({avant}->{joueur.PV}) PV.");
            }
            else
            {
                var dtos = ArtefactLoader.GetDtos();
                var commons = dtos.Where(d => string.IsNullOrWhiteSpace(d.Rarity) || d.Rarity.Equals("common", StringComparison.OrdinalIgnoreCase)).ToList();
                if (commons.Count > 0)
                {
                    var dto = commons[rand.Next(commons.Count)];
                    var art = ArtefactLoader.CreerInstance(dto.Id ?? dto.Nom ?? "");
                    if (art != null)
                    {
                        joueur.EquipArtefact(art);
                        ui.Afficher($"Le sanctuaire offre un artefact : {art.Nom}");
                        if (!string.IsNullOrWhiteSpace(dto.Description))
                            ui.Afficher($"Description : {dto.Description}");
                    }
                }
                else
                {
                    ui.Afficher("Le sanctuaire est silencieux... aucun artefact a offrir.");
                }
            }
        }
    }
}
