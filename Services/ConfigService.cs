// ConfigService.cs - Version 2.1
// Changelog : Utilisation de AppContext.BaseDirectory pour les chemins relatifs
//             évite les erreurs quand l'app est lancée depuis un raccourci ou bat

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService(string configPath = "Scripts/config.json")
        {
            // Toujours relatif au dossier de l'exe, peu importe d'où on lance l'app
            _configPath = Path.Combine(AppContext.BaseDirectory, configPath);
        }

        public async Task<AppConfig> LoadConfigAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                    throw new FileNotFoundException(
                        $"Configuration file not found: {_configPath}");

                var jsonContent = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                return config ?? new AppConfig();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading configuration: {ex.Message}", ex);
            }
        }
    }
}
