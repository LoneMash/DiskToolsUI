// ═══════════════════════════════════════════════════════════════════
// IConfigService.cs — Contrat de chargement de la configuration
// ═══════════════════════════════════════════════════════════════════
// Rôle : Définit le chargement de la config globale (AppConfig) et
//        de la liste des actions déclarées dans actions.json.
// Couche : Interfaces
// Consommé par : ConfigService, MainWindowViewModel (initialisation)
// ═══════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using System.Threading.Tasks;
using RunDeck.Models;

namespace RunDeck.Interfaces
{
    public interface IConfigService
    {
        Task<AppConfig> LoadConfigAsync();
        Task<List<ActionDefinition>> LoadActionsAsync(string actionsFilePath);
    }
}
