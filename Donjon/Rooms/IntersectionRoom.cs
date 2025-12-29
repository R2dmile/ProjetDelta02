using ProjetDelta.Player;
using ProjetDelta.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Donjon.Rooms
{
    public class IntersectionRoom : Room
    {
        private string _name;
        private string _description;
        private Dictionary<string, string> _choixVers; // Texte affiché -> nom de salle
        private List<Room> _possibleRooms = new();
        private List<string> _choixAffiches = new();
        private Room _nextRoom;

        public override string Name => _name;

        public IntersectionRoom(string name, string description, Dictionary<string, string> choixVers)
        {
            _name = name;
            _description = description;
            _choixVers = choixVers;
        }

        /// <summary>
        /// Appelée après que toutes les salles sont créées
        /// </summary>
        /// <param name="roomMap"></param>
        public override void ResolveLinks(Dictionary<string, Room> roomMap)
        {
            _possibleRooms = _choixVers
            .Where(kvp => roomMap.ContainsKey(kvp.Value))
            .Select(kvp => roomMap[kvp.Value])
            .ToList();
        }

        public override void ResolveRoom(IUI ui, Joueur joueur)
        {
            _nextRoom = ui.ChoisirOption(
         "  Où veux-tu aller ?",
             _possibleRooms,
             r => r.Name
         );
        }

        public override Room GetNextRoom()
        {
            return _nextRoom;
        }
    }



}
