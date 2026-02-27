using Valour.Sdk.Client;
using Valour.Sdk.Models;
using Valour.Shared.Models;

using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace ArgonautDiceBot
{
    enum ChannelMode { Open, Locked }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            // =====================================================
            // Application Startup
            // =====================================================

            Console.WriteLine("Starting ArgonautDiceBot...");

            var client = new ValourClient("https://api.valour.gg/");
            client.SetupHttpClient();

            var token = Environment.GetEnvironmentVariable("VALOUR_BOT_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("VALOUR_BOT_TOKEN not set.");
                return;
            }

            var login = await client.InitializeUser(token);
            if (!login.Success)
            {
                Console.WriteLine($"Login failed: {login.Message}");
                return;
            }

            Console.WriteLine($"Logged in as {client.Me.Name}");


            // =====================================================
            // Bot State Dictionaries (Per Channel Session State)
            // =====================================================

            Dictionary<long, ChannelMode> ChannelModes = new();
            Dictionary<long, long> ChannelDMs = new();
            Dictionary<long, Channel> MyChannelCache = new();
            Dictionary<long, string> ChannelRulesets = new();
            Dictionary<long, HashSet<long>> ActivePlayers = new();
            Dictionary<long, Dictionary<string, double>> InitiativeTracker = new();
            Dictionary<long, Dictionary<long, string>> SessionCharacters = new();
            HashSet<long> InitializedPlanets = new();

            
            // =====================================================
            // Dynamic Planet Monitor (Auto-detect Invites)
            // =====================================================

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    foreach (var joinedPlanet in client.PlanetService.JoinedPlanets)
                    {
                        if (InitializedPlanets.Contains(joinedPlanet.Id))
                            continue;

                        Console.WriteLine($"Initializing planet: {joinedPlanet.Name}");

                        await joinedPlanet.EnsureReadyAsync();
                        await joinedPlanet.FetchInitialDataAsync();

                        foreach (var ch in joinedPlanet.Channels)
                        {
                            MyChannelCache[ch.Id] = ch;

                            if (ch.ChannelType == ChannelTypeEnum.PlanetChat)
                            {
                                await ch.OpenWithResult("ArgonautDiceBot");
                                Console.WriteLine($"Realtime opened for: {joinedPlanet.Name} -> {ch.Name}");
                            }
                        }

                        InitializedPlanets.Add(joinedPlanet.Id);
                    }

                    await Task.Delay(5000);
                }
            });


            // =====================================================
            // Helper Methods
            // =====================================================

            bool IsDM(long channelId, long userId) =>
                ChannelDMs.ContainsKey(channelId) && ChannelDMs[channelId] == userId;

            int SecureRoll(int minInclusive, int maxInclusive)
            {
                return RandomNumberGenerator.GetInt32(minInclusive, maxInclusive + 1);
            }

            int[] ParseAndRollDetailed(string input, out int total, out string breakdown)
            {
                var match = Regex.Match(input, @"(\d+)d(\d+)([+-]\d+)?");

                if (!match.Success)
                {
                    total = 0;
                    breakdown = "Invalid dice notation.";
                    return Array.Empty<int>();
                }

                int numDice = int.Parse(match.Groups[1].Value);
                int dieSides = int.Parse(match.Groups[2].Value);
                int modifier = match.Groups[3].Success
                    ? int.Parse(match.Groups[3].Value)
                    : 0;

                int[] rolls = new int[numDice];
                int sum = 0;

                for (int i = 0; i < numDice; i++)
                {
                    rolls[i] = SecureRoll(1, dieSides);
                    sum += rolls[i];
                }

                total = sum + modifier;

                breakdown =
                    $"{total} ({string.Join("+", rolls)}" +
                    (modifier != 0
                        ? $"{(modifier > 0 ? "+" : "")}{modifier}"
                        : "") +
                    ")";

                return rolls;
            }


            // =====================================================
            // Wait for MessageService
            // =====================================================

            while (client.MessageService == null)
            {
                Console.WriteLine("Waiting for MessageService to initialize...");
                await Task.Delay(500);
            }


            // =====================================================
            // Message Handler
            // =====================================================

            client.MessageService.MessageReceived += async (message) =>
            {
                try
                {
                    if (message.AuthorUserId == client.Me.Id) return;
                    if (!MyChannelCache.TryGetValue(message.ChannelId, out var channel)) return;

                    string? content = message.Content?.Trim();
                    if (string.IsNullOrWhiteSpace(content)) return;
                    if (!content.StartsWith("!")) return;

                    // Session Lock Check
                    if (ChannelModes.ContainsKey(channel.Id) &&
                        ChannelModes[channel.Id] == ChannelMode.Locked &&
                        !IsDM(channel.Id, message.AuthorUserId))
                    {
                        await channel.SendMessageAsync("Session is locked.");
                        return;
                    }

                    string[] parts = content.Substring(1).Split(' ', 2);
                    string cmd = parts[0].ToLower();
                    string args = parts.Length > 1 ? parts[1] : "";

                    switch (cmd)
                    {
                        case "startsession":
                            if (ChannelDMs.ContainsKey(channel.Id))
                            {
                                await channel.SendMessageAsync("Session already started.");
                                return;
                            }

                            ActivePlayers[channel.Id] = new HashSet<long>();
                            InitiativeTracker[channel.Id] = new Dictionary<string, double>();
                            ChannelModes[channel.Id] = ChannelMode.Open;
                            ChannelDMs[channel.Id] = message.AuthorUserId;
                            ChannelRulesets[channel.Id] = "gurps";

                            InitiativeTracker.Remove(channel.Id);
                            SessionCharacters.Remove(channel.Id);

                            await channel.SendMessageAsync("Session started. Ruleset: GURPS (default).");
                            break;

                        case "join":
                            if (!ActivePlayers.ContainsKey(channel.Id))
                            {
                                await channel.SendMessageAsync("No active session.");
                                return;
                            }

                            ActivePlayers[channel.Id].Add(message.AuthorUserId);
                            await channel.SendMessageAsync("Joined session.");
                            break;

                        case "roll":
                            if (!ActivePlayers.ContainsKey(channel.Id) ||
                                !ActivePlayers[channel.Id].Contains(message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("Join session first.");
                                return;
                            }

                            int total;
                            string breakdown;
                            ParseAndRollDetailed(args, out total, out breakdown);
                            await channel.SendMessageAsync($"{args} → {breakdown}");
                            break;

                        case "lock":
                            if (!IsDM(channel.Id, message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("Only the DM can lock the session.");
                                return;
                            }

                            ChannelModes[channel.Id] = ChannelMode.Locked;
                            await channel.SendMessageAsync("Session locked.");
                            break;

                        case "unlock":
                            if (!IsDM(channel.Id, message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("Only the DM can unlock the session.");
                                return;
                            }

                            ChannelModes[channel.Id] = ChannelMode.Open;
                            await channel.SendMessageAsync("Session unlocked.");
                            break;

                        case "rules":
                            if (!IsDM(channel.Id, message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("Only the DM can change rulesets.");
                                return;
                            }

                            if (string.IsNullOrWhiteSpace(args))
                            {
                                await channel.SendMessageAsync("Usage: !rules gurps | dnd");
                                return;
                            }

                            string selected = args.ToLower();

                            if (selected != "gurps" && selected != "dnd")
                            {
                                await channel.SendMessageAsync("Supported rulesets: gurps, dnd");
                                return;
                            }

                            ChannelRulesets[channel.Id] = selected;
                            await channel.SendMessageAsync($"Ruleset changed to {selected.ToUpper()}.");
                            break;

                        case "reg":
                            if (!ActivePlayers.ContainsKey(channel.Id) ||
                                !ActivePlayers[channel.Id].Contains(message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("Join session first using !join.");
                                return;
                            }

                            if (string.IsNullOrWhiteSpace(args))
                            {
                                await channel.SendMessageAsync("Usage: !reg <character name>");
                                return;
                            }

                            if (!SessionCharacters.ContainsKey(channel.Id))
                                SessionCharacters[channel.Id] = new Dictionary<long, string>();

                            SessionCharacters[channel.Id][message.AuthorUserId] = args.Trim();
                            await channel.SendMessageAsync($"Character registered: {args.Trim()}");
                            break;

                        case "char":
                            if (!SessionCharacters.ContainsKey(channel.Id) ||
                                !SessionCharacters[channel.Id].ContainsKey(message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("You have not registered a character. Use !reg <name>.");
                                return;
                            }

                            await channel.SendMessageAsync(
                                $"Your character: {SessionCharacters[channel.Id][message.AuthorUserId]}"
                            );
                            break;

                        case "init":
                            if (!ActivePlayers.ContainsKey(channel.Id) ||
                                !ActivePlayers[channel.Id].Contains(message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("Join session first using !join.");
                                return;
                            }

                            if (string.IsNullOrWhiteSpace(args) ||
                                !double.TryParse(args, out double initiative))
                            {
                                await channel.SendMessageAsync("Usage: !init <number>");
                                return;
                            }

                            if (!InitiativeTracker.ContainsKey(channel.Id))
                                InitiativeTracker[channel.Id] = new Dictionary<string, double>();

                            string playerName =
                                SessionCharacters.ContainsKey(channel.Id) &&
                                SessionCharacters[channel.Id].ContainsKey(message.AuthorUserId)
                                    ? SessionCharacters[channel.Id][message.AuthorUserId]
                                    : $"Player-{message.AuthorUserId}";

                            InitiativeTracker[channel.Id][playerName] = initiative;
                            await channel.SendMessageAsync($"{playerName} initiative set to {initiative}");
                            break;

                        case "initorder":
                            if (!InitiativeTracker.ContainsKey(channel.Id) ||
                                InitiativeTracker[channel.Id].Count == 0)
                            {
                                await channel.SendMessageAsync("No initiative rolled yet.");
                                return;
                            }

                            var order = InitiativeTracker[channel.Id]
                                .OrderByDescending(kvp => kvp.Value)
                                .Select((kvp, i) => $"{i + 1}. {kvp.Key}: {kvp.Value}");

                            await channel.SendMessageAsync(
                                "Initiative Order:\n" + string.Join("\n", order)
                            );
                            break;

                        case "endsession":
                            if (!ChannelDMs.ContainsKey(channel.Id))
                            {
                                await channel.SendMessageAsync("No active session to end.");
                                return;
                            }

                            if (!IsDM(channel.Id, message.AuthorUserId))
                            {
                                await channel.SendMessageAsync("Only the DM can end the session.");
                                return;
                            }

                            ActivePlayers.Remove(channel.Id);
                            InitiativeTracker.Remove(channel.Id);
                            SessionCharacters.Remove(channel.Id);
                            ChannelModes.Remove(channel.Id);
                            ChannelDMs.Remove(channel.Id);
                            ChannelRulesets.Remove(channel.Id);

                            await channel.SendMessageAsync("Session ended. All session data cleared.");
                            break;

                        default:
                            await channel.SendMessageAsync($"Unknown command: {cmd}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Handler error: {ex.Message}");
                }
            };


            // =====================================================
            // Keep Bot Alive
            // =====================================================

            Console.WriteLine("ADB is listening...");
            await Task.Delay(Timeout.Infinite);
        }
    }
}
            await Task.Delay(Timeout.Infinite);
        }
    }

}
