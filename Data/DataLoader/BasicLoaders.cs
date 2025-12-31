using ProjetDelta.Data.DataClasses;
using System.Text.Json;

namespace ProjetDelta.Data.DataLoader
{
    public class BasicLoaders
    {
        public static List<Classe> ChargerClasses()
        {
            string json = DataFileHelper.ReadText("Data/DataStorage/classes.json");
            return JsonSerializer.Deserialize<List<Classe>>(json);
        }

        public static List<Background> ChargerBackgrounds(int flatBonus = 0)
        {
            string json = DataFileHelper.ReadText("Data/DataStorage/backgrounds.json");
            var backgrounds = JsonSerializer.Deserialize<List<Background>>(json) ?? new List<Background>();
            if (flatBonus > 0)
            {
                foreach (var bg in backgrounds)
                {
                    bg.Attributs.Force += flatBonus;
                    bg.Attributs.Constitution += flatBonus;
                    bg.Attributs.Dex += flatBonus;
                    bg.Attributs.Intel += flatBonus;
                }
            }
            return backgrounds;
        }
    }
}
