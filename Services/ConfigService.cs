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

        public ConfigService(string configPath = "./Scripts/config.json")
        {
            _configPath = configPath;
        }

        public async Task<AppConfig> LoadConfigAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {_configPath}");
                }

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
