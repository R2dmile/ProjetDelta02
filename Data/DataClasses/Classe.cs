using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataClasses
{
    public class Classe : BaseOption
    {
        public override void AdjustAttributes(Attributs attributs)
        {
            attributs.ModifyMultipliers(Attributs);
        }
    }
}
