using Valour.Sdk.Client;
using Valour.Sdk.Models;

namespace ArgonautDiceBot
{
    public class CommandHandler
    {
        private readonly SessionManager _session;
        private readonly DiceService _dice;

        public CommandHandler(SessionManager session, DiceService dice)
        {
            _session = session;
            _dice = dice;
        }

        public async Task HandleAsync(ValourClient client, Channel channel, Message message)
        {
            if (message.AuthorUserId == client.Me.Id)
                return;

            if (!message.Content.StartsWith("Arg/", StringComparison.OrdinalIgnoreCase))
                return;

            var parts = message.Content.Substring(4).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            var command = parts[0].ToLower();

            switch (command)
            {
                case "roll":
                    if (parts.Length < 2)
                    {
                        await channel.SendMessageAsync("Usage: Arg/roll XdY");
                        return;
                    }

                    var (total, breakdown) = _dice.Roll(parts[1]);
                    await channel.SendMessageAsync($"🎲 Rolls: {breakdown}\nTotal: {total}");
                    break;

                case "startsession":
                    if (!_session.StartSession(channel.Id, message.AuthorUserId))
                    {
                        await channel.SendMessageAsync("Session already active.");
                        return;
                    }

                    await channel.SendMessageAsync("Session started.");
                    break;

                case "endsession":
                    if (!_session.IsDM(channel.Id, message.AuthorUserId))
                    {
                        await channel.SendMessageAsync("Only the DM can end the session.");
                        return;
                    }

                    _session.EndSession(channel.Id);
                    await channel.SendMessageAsync("Session ended.");
                    break;

                case "join":
                    if (!_session.JoinSession(channel.Id, message.AuthorUserId))
                    {
                        await channel.SendMessageAsync("No active session.");
                        return;
                    }

                    await channel.SendMessageAsync("You joined the session.");
                    break;
            }
        }
    }
}
