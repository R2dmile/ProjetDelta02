using ProjetDelta.Player;
using ProjetDelta.UI;

namespace ProjetDelta.Donjon.Rooms
{
    public abstract class Room
    {
        public abstract string Name { get; }
        public abstract void ResolveRoom(IUI ui, Joueur joueur);

        protected string _nextRoomName;
        public virtual void SetNextRoomName(string name) => _nextRoomName = name;

        public virtual void ResolveLinks(Dictionary<string, Room> allRooms) { }

        public virtual Room GetNextRoom() => null;
    }
}
