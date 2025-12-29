using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataClasses
{
    public abstract class BaseOption
    {
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AttributsDto Attributs { get; set; } = new AttributsDto();
        public string? ArtefactId { get; set; }
        public int DifficultyUnlock { get; set; } = 0; // 0 = toujours dispo, sinon niveau de difficulte requis

        public abstract void AdjustAttributes(Attributs attributs);
    }
}
