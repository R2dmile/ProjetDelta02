using ProjetDelta.Artifacts;
using ProjetDelta.Data.DataClasses;
using System.Text.Json;

namespace ProjetDelta.Data.DataLoader
{
    public static class ArtefactLoader
    {
        private static List<ArtefactDto> _cacheDtos;
        private static Dictionary<string, ArtefactDto> _cacheById;
        private static string _cachePath;

        private static List<ArtefactDto> ChargerDtos(string path)
        {
            var json = DataFileHelper.ReadText(path);
            return JsonSerializer.Deserialize<List<ArtefactDto>>(json) ?? new List<ArtefactDto>();
        }

        private static void EnsureLoad(string relativePath)
        {
            if (_cacheDtos != null && string.Equals(_cachePath, relativePath, StringComparison.OrdinalIgnoreCase))
                return;

            _cachePath = relativePath;
            _cacheDtos = ChargerDtos(relativePath);
            _cacheById = _cacheDtos
                .Where(d => !string.IsNullOrWhiteSpace(d.Id))
                .ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, IArtefact> ChargerDepuisJson(string relativePath = "Data\\DataStorage\\artefacts.json")
        {
            EnsureLoad(relativePath);
            return _cacheDtos
                .Where(d => !string.IsNullOrWhiteSpace(d.Id))
                .ToDictionary(d => d.Id, d => ArtefactFactory.FromDto(d), StringComparer.OrdinalIgnoreCase);
        }

        public static IArtefact? CreerInstance(string idOuNom, string relativePath = "Data\\DataStorage\\artefacts.json")
        {
            EnsureLoad(relativePath);
            if (_cacheById.TryGetValue(idOuNom, out var dto))
                return ArtefactFactory.FromDto(dto);

            var matchNom = _cacheDtos.FirstOrDefault(d => string.Equals(d.Nom, idOuNom, StringComparison.OrdinalIgnoreCase));
            if (matchNom != null)
                return ArtefactFactory.FromDto(matchNom);

            return null;
        }

        public static IReadOnlyList<ArtefactDto> GetDtos(string relativePath = "Data\\DataStorage\\artefacts.json")
        {
            EnsureLoad(relativePath);
            return _cacheDtos;
        }
    }
}
