using System.Reflection;

namespace ProjetDelta.Data.DataLoader
{
    /// <summary>
    /// Fournit une lecture unifiée des fichiers de données JSON soit depuis le disque (dev/publish),
    /// soit depuis les ressources embarquées dans l'exécutable (single-file).
    /// </summary>
    public static class DataFileHelper
    {
        public static string ReadText(string relativePathOrFileName)
        {
            var fileName = Path.GetFileName(relativePathOrFileName);
            string? TryReadResource(Assembly asm)
            {
                try
                {
                    var resName = asm.GetManifestResourceNames()
                        .FirstOrDefault(n => n.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase));
                    if (resName == null) return null;
                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream == null) return null;
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch
                {
                    return null;
                }
            }

            // 1) Dossier à côté de l'exe (publish) ou chemin relatif fourni
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, relativePathOrFileName),
                Path.Combine(AppContext.BaseDirectory, "Data", "DataStorage", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), relativePathOrFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "DataStorage", fileName)
            }
            .Select(Path.GetFullPath)
            .Distinct()
            .ToList();

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }

            // 2) Ressource embarquée : chercher dans EntryAssembly, l'assembly courant puis tous les assemblies chargés
            var searched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblies = new List<Assembly>();
            if (Assembly.GetEntryAssembly() is Assembly entryAsm) assemblies.Add(entryAsm);
            var exec = Assembly.GetExecutingAssembly();
            if (!assemblies.Contains(exec)) assemblies.Add(exec);
            var call = Assembly.GetCallingAssembly();
            if (!assemblies.Contains(call)) assemblies.Add(call);
            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

            foreach (var asm in assemblies)
            {
                if (asm == null) continue;
                var name = asm.FullName ?? "";
                if (!searched.Add(name)) continue;
                var text = TryReadResource(asm);
                if (text != null) return text;
            }

            throw new FileNotFoundException($"Impossible de trouver {fileName} (cherché sur disque et ressources embarquées).");
        }
    }
}
