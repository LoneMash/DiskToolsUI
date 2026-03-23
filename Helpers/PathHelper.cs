// ═══════════════════════════════════════════════════════════════════
// PathHelper.cs — Résolution de chemins relatifs et dossier racine
// ═══════════════════════════════════════════════════════════════════
// Rôle : Fournit le dossier de l'exécutable (compatible single-file
//        publish), résout les chemins relatifs, et configure PSHOME
//        pour le moteur PowerShell embarqué si nécessaire.
// Couche : Helpers
// Consommé par : ConfigService, PowerShellRunner, App.xaml.cs
// ═══════════════════════════════════════════════════════════════════

using System;
using System.IO;

namespace RunDeck.Helpers
{
    public static class PathHelper
    {
        /// <summary>
        /// Dossier de l'exécutable, fiable en mode single-file publish.
        /// </summary>
        public static string AppDirectory { get; } = ResolveAppDirectory();

        /// <summary>
        /// Configure PSHOME si besoin (doit être appelé AVANT tout accès
        /// à System.Management.Automation pour éviter le cache statique).
        /// En mode single-file publish, les DLLs PS sont exclues du bundle
        /// et le dossier Modules/ est copié à la racine du publish.
        /// PSHOME pointe donc sur le dossier de l'exécutable.
        /// </summary>
        public static void EnsurePSHome()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PSHOME")))
                return;

            // Ne configurer PSHOME que si un dossier Modules/ existe à côté de l'exe
            // (= mode single-file publish). En mode normal (dotnet run / VS),
            // le SDK résout tout seul via Assembly.Location.
            var modulesDir = Path.Combine(AppDirectory, "Modules");
            if (Directory.Exists(modulesDir))
                Environment.SetEnvironmentVariable("PSHOME", AppDirectory);
        }

        /// <summary>
        /// Résout un chemin relatif par rapport au dossier de l'exécutable.
        /// Si le chemin est déjà absolu, il est retourné tel quel.
        /// </summary>
        public static string ResolveRelativePath(string path)
            => Path.IsPathRooted(path)
               ? path
               : Path.Combine(AppDirectory, path);

        private static string ResolveAppDirectory()
        {
            // En mode single-file, AppContext.BaseDirectory peut pointer vers
            // un dossier d'extraction temporaire. Environment.ProcessPath
            // pointe toujours vers le vrai exe.
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                var dir = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }

            // Fallback classique
            return AppContext.BaseDirectory;
        }
    }
}
