// PathHelper.cs - Version 1.0
// Changelog :
//   1.0 - Extraction de ResolveRelativePath depuis ConfigService et PowerShellRunner

using System;
using System.IO;

namespace RunDeck.Helpers
{
    public static class PathHelper
    {
        /// <summary>
        /// Résout un chemin relatif par rapport au dossier de l'exécutable.
        /// Si le chemin est déjà absolu, il est retourné tel quel.
        /// </summary>
        public static string ResolveRelativePath(string path)
            => Path.IsPathRooted(path)
               ? path
               : Path.Combine(AppContext.BaseDirectory, path);
    }
}
