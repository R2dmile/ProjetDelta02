using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Logger
{
    public interface ICombatLogger
    {
        void Log(CombatDataItem item);
    }
}
