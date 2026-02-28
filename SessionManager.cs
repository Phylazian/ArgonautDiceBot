namespace ArgonautDiceBot
{
    public class SessionManager
    {
        private Dictionary<long, ChannelMode> ChannelModes = new();
        private Dictionary<long, long> ChannelDMs = new();
        private Dictionary<long, HashSet<long>> ActivePlayers = new();

        public bool StartSession(long channelId, long userId)
        {
            if (ChannelModes.ContainsKey(channelId))
                return false;

            ChannelModes[channelId] = ChannelMode.Session;
            ChannelDMs[channelId] = userId;
            ActivePlayers[channelId] = new HashSet<long>();

            return true;
        }

        public bool EndSession(long channelId)
        {
            if (!ChannelModes.ContainsKey(channelId))
                return false;

            ChannelModes.Remove(channelId);
            ChannelDMs.Remove(channelId);
            ActivePlayers.Remove(channelId);

            return true;
        }

        public bool JoinSession(long channelId, long userId)
        {
            if (!ActivePlayers.ContainsKey(channelId))
                return false;

            ActivePlayers[channelId].Add(userId);
            return true;
        }

        public bool IsDM(long channelId, long userId)
        {
            return ChannelDMs.TryGetValue(channelId, out var dmId) && dmId == userId;
        }

        public bool SessionExists(long channelId)
        {
            return ChannelModes.ContainsKey(channelId);
        }
    }
}
