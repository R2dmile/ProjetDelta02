using ProjetDelta.Data.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjetDelta.Data.DataLoader
{
    public class BasicLoaders
    {
        private static string ResolvePath(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Data", "DataStorage", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "Data", "DataStorage", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "Data", "DataStorage", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "DataStorage", fileName),
                Path.Combine(AppContext.BaseDirectory, "net8.0", "Data", "DataStorage", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "DataStorage", fileName),
                Path.Combine("..", "..", "..", "Data", "DataStorage", fileName)
            }
            .Select(p => Path.GetFullPath(p))
            .ToList();
            var found = candidates.FirstOrDefault(File.Exists);
            return found ?? Path.Combine("..", "..", "..", "Data", "DataStorage", fileName);
        }

        public static List<Classe> ChargerClasses()
        {
            string json = File.ReadAllText(ResolvePath("classes.json"));
            return JsonSerializer.Deserialize<List<Classe>>(json);
        }

        public static List<Background> ChargerBackgrounds(int flatBonus = 0)
        {
            string json = File.ReadAllText(ResolvePath("backgrounds.json"));
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
