using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataClasses.Rooms
{
    public class RawRoom
    {
        public string type { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string treasure { get; set; } = string.Empty;
        public List<string> items { get; set; } = new();
        public List<string> monsters { get; set; } = new();
        public string? next { get; set; }
        public Dictionary<string, string> choices { get; set; } = new();
        public string attribute { get; set; } = string.Empty;
        public int difficulty { get; set; }
        public string success { get; set; } = string.Empty;
        public string fail { get; set; } = string.Empty;
        public int rewardGold { get; set; }
        public int failDamage { get; set; }
        public bool boss { get; set; }
        public bool elite { get; set; }
        public int tier { get; set; }
    }
    public class RawRoomList
    {
        public List<RawRoom> rooms { get; set; } = new();
    }
}
