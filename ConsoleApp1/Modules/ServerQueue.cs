using System.Collections.Generic;

namespace WheelchairBot.Modules
{
    public class ServerQueue
    {
        public ServerQueue(List<string> serverQueues, ulong serverId, int trackNumber)
        { this.serverQueues = serverQueues; this.serverId = serverId; this.trackNumber = trackNumber; }
        public List<string> serverQueues;
        public ulong serverId;
        public int trackNumber;
    }
}
