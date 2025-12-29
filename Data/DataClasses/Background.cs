using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataClasses
{
    public class Background : BaseOption
    {
        public override void AdjustAttributes(Attributs attributs)
        {
            attributs.AddAttributes(Attributs);
        }
    }
}
