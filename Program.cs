using Valour.Sdk.Client;
using Valour.Sdk.Models;
using Valour.Shared.Models;

namespace ArgonautDiceBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var token = Environment.GetEnvironmentVariable("VALOUR_BOT_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Bot token missing.");
                return;
            }

            var client = new ValourClient(token);
            var channelCache = new Dictionary<long, Channel>();
            var initializedPlanets = new HashSet<long>();

            var sessionManager = new SessionManager();
            var diceService = new DiceService();
            var commandHandler = new CommandHandler(sessionManager, diceService);

            var login = await client.LoginAsync();
            if (!login.Success)
            {
                Console.WriteLine($"Login failed: {login.Message}");
                return;
            }

            Console.WriteLine($"Logged in as {client.Me.Name}");

            // Initial planet load
            await Utils.InitializePlanetsAsync(client, channelCache, initializedPlanets);

            // Re-run when planets update
            client.PlanetService.JoinedPlanetsUpdated += async () =>
            {
                await Utils.InitializePlanetsAsync(client, channelCache, initializedPlanets);
            };

            // Message listener
            client.MessageService.MessageReceived += async (message) =>
            {
                if (!channelCache.TryGetValue(message.ChannelId, out var channel))
                    return;

                await commandHandler.HandleAsync(client, channel, message);
            };

            await Task.Delay(Timeout.Infinite);
        }
    }
}
