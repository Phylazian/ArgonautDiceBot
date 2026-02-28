using Valour.Sdk.Client;
using Valour.Sdk.Models;
using Valour.Shared.Models;

namespace ArgonautDiceBot
{
    public static class Utils
    {
        public static async Task InitializePlanetsAsync(
            ValourClient client,
            Dictionary<long, Channel> channelCache,
            HashSet<long> initializedPlanets)
        {
            foreach (var planet in client.PlanetService.JoinedPlanets)
            {
                if (initializedPlanets.Contains(planet.Id))
                    continue;

                Console.WriteLine($"Initializing planet: {planet.Name}");

                await planet.EnsureReadyAsync();
                await planet.FetchInitialDataAsync();

                foreach (var ch in planet.Channels)
                {
                    channelCache[ch.Id] = ch;

                    if (ch.ChannelType == ChannelTypeEnum.PlanetChat)
                    {
                        await ch.OpenWithResult("ArgonautDiceBot");
                        Console.WriteLine($"Realtime opened: {planet.Name} -> {ch.Name}");
                    }
                }

                initializedPlanets.Add(planet.Id);
            }
        }
    }
}
