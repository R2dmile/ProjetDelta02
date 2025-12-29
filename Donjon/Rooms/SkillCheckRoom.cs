using ProjetDelta.Player;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Donjon.Rooms
{
    public class SkillCheckRoom : Room
    {
        private string _name;
        private string _description;
        private string _attribute;
        private int _difficulty;
        private int _rewardGold;
        private int _failDamage;
        private Room _successRoom;
        private Room _failRoom;
        private string _successName;
        private string _failName;
        private Room _nextRoom;

        public override string Name => _name;

        public SkillCheckRoom(string name, string description, string attribute, int difficulty, int rewardGold, int failDamage, string successName, string failName)
        {
            _name = name;
            _description = description;
            _attribute = attribute?.ToLowerInvariant() ?? "force";
            _difficulty = difficulty;
            _rewardGold = rewardGold;
            _failDamage = failDamage;
            _successName = successName;
            _failName = failName;
        }

        public override void ResolveLinks(Dictionary<string, Room> allRooms)
        {
            if (!string.IsNullOrWhiteSpace(_successName) && allRooms.TryGetValue(_successName, out var ok))
                _successRoom = ok;
            if (!string.IsNullOrWhiteSpace(_failName) && allRooms.TryGetValue(_failName, out var ko))
                _failRoom = ko;

            // Fallback lineaire si success/fail non renseignes (utile pour le mode random)
            if (_successRoom == null && !string.IsNullOrWhiteSpace(_nextRoomName) && allRooms.TryGetValue(_nextRoomName, out var next))
                _successRoom = next;
            if (_failRoom == null)
                _failRoom = _successRoom;
        }

        public override Room GetNextRoom() => _nextRoom;

        public override void ResolveRoom(IUI ui, Joueur joueur)
        {
            ui.Afficher($"Salle Skill Check : {_name}");
            ui.Afficher(_description);

            float valeur = _attribute switch
            {
                "intel" or "intelligence" => joueur.Intelligence,
                "dex" or "dexterite" => joueur.Dexterite,
                "const" or "constitution" => joueur.Constitution,
                "force" => joueur.Force,
                "darkness" => joueur.Darkness,
                _ => joueur.Force
            };

            ui.Afficher($"Test de {_attribute} (difficulte: {_difficulty}) - valeur du joueur: {valeur}");

            bool reussite = valeur >= _difficulty;
            if (reussite)
            {
                ui.Afficher("Reussite !");
                if (_rewardGold > 0)
                {
                    joueur.AjouterArgent(_rewardGold);
                    ui.Afficher($"Tu gagnes {_rewardGold} or (total: {joueur.Argent}).");
                }
                _nextRoom = _successRoom;
            }
            else
            {
                ui.Afficher("Echec du test.");
                if (_failDamage > 0)
                {
                    var log = new ProjetDelta.Logger.CombatDataItem { NomRecevant = joueur.Nom };
                    joueur.SubirDegats(_failDamage, log);
                    ui.Afficher($"Tu subis {_failDamage} degats en echec.");
                }
                _nextRoom = _failRoom ?? _successRoom;
            }
        }
    }
}
