using ProjetDelta.Data.DataClasses;
using System.Text.Json;

namespace ProjetDelta.Data.DataLoader.ProjetDelta.Narration
{
    public static class SectionLoader
    {
        public static List<Section> ChargerSections()
        {
            string filePath = Path.Combine("..", "..", "..", "Data", "DataStorage", "sections3.json");
            try
            {
                string json = File.ReadAllText(filePath);
                var sectionsData = JsonSerializer.Deserialize<Section.SectionsData>(json);
                return sectionsData?.Sections ?? new List<Section>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des sections : {ex.Message}");
                return new List<Section>();
            }
        }
    }
}
