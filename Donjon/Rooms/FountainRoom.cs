using ProjetDelta.Player;
using ProjetDelta.UI;
using System.Collections.Generic;

namespace ProjetDelta.Donjon.Rooms
{
    /// <summary>
    /// Fontaine de soin : remet le joueur full PV. Pensee pour etre placee apres un embranchement.
    /// </summary>
    public class FountainRoom : Room
    {
        private readonly string _name;
        private readonly string _description;
        private Room _nextRoom;

        public override string Name => _name;

        public FountainRoom(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public override void SetNextRoomName(string name)
        {
            base.SetNextRoomName(name);
        }

        public override void ResolveLinks(Dictionary<string, Room> allRooms)
        {
            if (_nextRoomName != null && allRooms.TryGetValue(_nextRoomName, out var resolved))
                _nextRoom = resolved;
        }

        public override Room GetNextRoom() => _nextRoom;

        public override void ResolveRoom(IUI ui, Joueur joueur)
        {
            ui.Afficher($"Tu arrives a une fontaine de jus : {_name}");
            ui.Afficher(_description);
            joueur.Heal(joueur.PVMax); // heal complet
            ui.Afficher($"Tu recuperes tous tes PV ({joueur.PV}/{joueur.PVMax}).");
            ui.GoNext();
        }
    }
}
