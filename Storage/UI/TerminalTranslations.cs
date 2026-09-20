using System.Collections.Generic;
using Jotunn.Managers;

namespace SmartCraftStorage.Storage.UI
{
    internal static class TerminalTranslations
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["title"] = "Storage terminal", ["description"] = "Manage nearby chests with a shared network name. Their slots form one finite store.",
            ["open"] = "Manage storage", ["network"] = "Network", ["unnamed"] = "Not linked", ["name"] = "Network name",
            ["save"] = "Link", ["rename"] = "Name storage network", ["capacity"] = "$1 chests · $2 / $3 slots occupied",
            ["search"] = "Search stored items…", ["stored"] = "NETWORK STORAGE", ["carried"] = "YOUR INVENTORY",
            ["withdraw"] = "Withdraw", ["deposit"] = "Deposit selected", ["deposit_all"] = "Deposit all unlocked",
            ["organize"] = "Organize network", ["quantity"] = "Quantity", ["sort_name"] = "Sort: name", ["sort_amount"] = "Sort: quantity",
            ["select"] = "Select an item to move", ["empty"] = "No matching items", ["locked"] = "Equipped or locked",
            ["help"] = "Give nearby chests the same network name using their naming shortcut. Organize combines stacks and frees chests.",
            ["naming_help"] = "Use the terminal's name to link this chest. Leave empty to unlink it.",
            ["no_network"] = "Name this terminal and nearby chests to begin.", ["ready"] = "Ready",
            ["pending"] = "Moving items…", ["recovering"] = "Waiting to confirm the transfer…", ["busy"] = "Storage is busy. Try again shortly.",
            ["done"] = "$1 moved · $2 remaining", ["organized"] = "Network organized", ["rejected"] = "Operation declined",
            ["unavailable"] = "Storage unavailable", ["denied"] = "You do not have access to this storage.",
            ["quality"] = "Quality $1", ["variant"] = "Variant $1", ["close"] = "Close", ["slots"] = "Physical chest capacity",
            ["compact_help"] = "Combine compatible stacks and pack the contents into fewer chests. Items may change chest.",
            ["linked"] = "Network name saved", ["cancel"] = "Cancel",
            ["name_pending"] = "Waiting to confirm the network name…",
            ["stored_total"] = "Stored in network: $1",
            ["incompatible_slot"] = "Choose an empty slot or a compatible stack."
        };

        public static void Setup()
        {
            var localization = LocalizationManager.Instance.GetLocalization();
            var en = new Dictionary<string, string>();
            foreach (var pair in English) en["scs_terminal_" + pair.Key] = pair.Value;
            localization.AddTranslation("English", en);
            var es = new Dictionary<string, string>
            {
                ["title"] = "Terminal de almacenamiento", ["description"] = "Administra cofres cercanos con el mismo nombre de red. Sus espacios forman un almacén de capacidad limitada.",
                ["open"] = "Administrar almacén", ["network"] = "Red", ["unnamed"] = "Sin vincular", ["name"] = "Nombre de la red",
                ["save"] = "Vincular", ["rename"] = "Nombrar red de almacenamiento", ["capacity"] = "$1 cofres · $2 / $3 espacios ocupados",
                ["search"] = "Buscar objetos almacenados…", ["stored"] = "ALMACENAMIENTO DE LA RED", ["carried"] = "TU INVENTARIO",
                ["withdraw"] = "Retirar", ["deposit"] = "Depositar seleccionado", ["deposit_all"] = "Depositar sin bloqueados",
                ["organize"] = "Organizar red", ["quantity"] = "Cantidad", ["sort_name"] = "Orden: nombre", ["sort_amount"] = "Orden: cantidad",
                ["select"] = "Selecciona un objeto para moverlo", ["empty"] = "No hay objetos que coincidan", ["locked"] = "Equipado o bloqueado",
                ["help"] = "Asigna el mismo nombre de red a los cofres cercanos con su atajo para nombrar. Organizar combina pilas y libera cofres.",
                ["naming_help"] = "Usa el nombre de la terminal para vincular este cofre. Déjalo vacío para desvincularlo.",
                ["no_network"] = "Nombra esta terminal y los cofres cercanos para empezar.", ["ready"] = "Listo",
                ["pending"] = "Moviendo objetos…", ["recovering"] = "Esperando confirmar la transferencia…", ["busy"] = "El almacén está ocupado. Intenta de nuevo en un momento.",
                ["done"] = "$1 movidos · $2 restantes", ["organized"] = "Red organizada", ["rejected"] = "Operación rechazada",
                ["unavailable"] = "Almacén no disponible", ["denied"] = "No tienes acceso a este almacén.",
                ["quality"] = "Calidad $1", ["variant"] = "Variante $1", ["close"] = "Cerrar", ["slots"] = "Capacidad de los cofres físicos",
                ["compact_help"] = "Combina pilas compatibles y concentra el contenido en menos cofres. Los objetos pueden cambiar de cofre.",
                ["linked"] = "Nombre de red guardado", ["cancel"] = "Cancelar",
                ["name_pending"] = "Esperando confirmar el nombre de la red…",
                ["stored_total"] = "Almacenados en la red: $1",
                ["incompatible_slot"] = "Elige un espacio vacío o una pila compatible."
            };
            var translated = new Dictionary<string, string>();
            foreach (var pair in es) translated["scs_terminal_" + pair.Key] = pair.Value;
            localization.AddTranslation("Spanish", translated);
        }

        public static string Text(string key, params string[] values)
        {
            string token = "$scs_terminal_" + key;
            string text = Localization.instance != null ? Localization.instance.Localize(token, values) : token;
            if (text == token || text == "[scs_terminal_" + key + "]")
            {
                text = English.TryGetValue(key, out var fallback) ? fallback : key;
                for (int i = 0; i < values.Length; i++) text = text.Replace("$" + (i + 1), values[i]);
            }
            return text;
        }
    }
}
