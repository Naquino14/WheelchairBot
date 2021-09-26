using System.Collections.Generic;
using System.Diagnostics;

namespace WheelchairBot.Modules
{
    public class ServerQueue
    {
        public ServerQueue(List<string> serverQueues, ulong serverId, int trackNumber, int dledTrackIndex, Process ffmpegProcess)
        { this.serverQueues = serverQueues; this.serverId = serverId; this.trackNumber = trackNumber; this.dledTrackIndex = dledTrackIndex; this.ffmpegProcess = ffmpegProcess; }
        public List<string> serverQueues;
        public ulong serverId;
        public int trackNumber;
        public int dledTrackIndex;
        public Process ffmpegProcess;
    }
}
