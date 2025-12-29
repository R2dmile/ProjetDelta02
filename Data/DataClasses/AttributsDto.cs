using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataClasses
{
    public class AttributsDto
    {
        // Public properties for JSON deserialization
        public float MulIntel { get; set; }
        public float MulForce { get; set; }
        public float MulConst { get; set; }
        public float MulDex { get; set; }

        public float Intel { get; set; }
        public float Force { get; set; }
        public float Constitution { get; set; }
        public float Dex { get; set; }
        public int Darkness { get; set; }

        public AttributsDto(float intel, float force, float constitution, float dex, int darkness)
        {
            Intel = intel;
            Force = force;
            Constitution = constitution;
            Dex = dex;
            Darkness = darkness;
        }

        // Parameterless constructor for deserialization
        public AttributsDto() { }
        public Attributs ToAttributs() {
            return new Attributs().AddAttributes(this).ModifyMultipliers(this);
        }
    }

}
