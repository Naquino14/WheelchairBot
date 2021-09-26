using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System.Collections.Generic;
using Google.Apis.YouTube.v3;

namespace WheelchairBot.Modules
{
    public class VoiceModule : BaseCommandModule
    {
        private readonly string fart = @"sfx\fart.mp3";

        //private readonly Regex curlReg = new Regex(@"")
        //private readonly Regex youtubeReg
        //private readonly Regex spotifyReg
        //private readonly Regex soundCloudReg

        private enum LinkType
        {
            curl,
            youtube,
            spotify,
            soundcloud,
            path
        }

        /// <summary>
        /// arraylist of type Queues
        /// </summary>
        List<ServerQueue> globalQueue = new List<ServerQueue>();
        
        #region commands

        [Command("ping")]
        public async Task Ping(CommandContext ctx) => await ctx.Channel.SendMessageAsync("pong!");

        [Command("join"), Description("Joins a voice channel.")]
        public async Task Join(CommandContext ctx, DiscordChannel chn = null)
        {
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                await ctx.RespondAsync("epic voice join fail!");
                return;
            }

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                // already connected
                await ctx.RespondAsync("bro are you blind or something???? im already in a voice channel");
                return;
            }

            // get member's voice state
            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && chn == null)
            {
                // they did not specify a channel and are not in one
                await ctx.RespondAsync("hey peter, get a load of this dumbass tryna get me to join a channel without being in one lmao");
                return;
            }

            // channel not specified, use user's
            if (chn == null)
                chn = vstat.Channel;

            // connect
            vnc = await vnext.ConnectAsync(chn);
            await ctx.RespondAsync($"whats up fuckers, i connected to {chn.Name}");

            globalQueue.Add(new ServerQueue(new List<string>(), ctx.Guild.Id, 0, 0, null));

            foreach (ServerQueue serverQueue in globalQueue)
                if (Directory.Exists($@"queue\{serverQueue.serverId}"))
                    Directory.CreateDirectory($@"queue\{serverQueue.serverId}");
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task Leave(CommandContext ctx, bool fuckoff = false)
        {
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("epic leave channel fail");
                return;
            }

            // check whether we are connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // not connected
                await ctx.RespondAsync("idiot! im not connected to anything");
                return;
            }
            // disconnect
            vnc.Disconnect(); // anything past this wont fire
            // get sqi
            int sqi = 0;
            foreach (ServerQueue serverQueue in globalQueue)
            {
                if (serverQueue.serverId == ctx.Channel.Id)
                    break;
                sqi++;
            }
            // close ffmpeg
            globalQueue[sqi].ffmpegProcess.Close();
            // delete queue
            if (Directory.Exists($@"queue\{globalQueue[sqi].serverId}"))
                Directory.Delete($@"queue\{globalQueue[sqi].serverId}");
            globalQueue.Remove(globalQueue[sqi]);

            switch (fuckoff)
            {
                case true:
                    await ctx.RespondAsync("alright chill ill leave");
                    await ctx.Channel.SendMessageAsync("https://tenor.com/view/joeswanson-stand-walk-family-guy-gif-17739079");
                    break;
                case false:
                    await ctx.RespondAsync("later losers");
                    break;
            }
        }

        [Command("fuckoff"), Description("Leaves a voice channel.")]
        public async Task FuckOff(CommandContext ctx) => await Leave(ctx, true);

        [Command("play"), Description("Plays an audio file.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Full path to the file to play.")] string input = "")
        {
            if (input == "")
            { await ctx.RespondAsync("i need something to play dumbass!"); return; }
            int sqi = 0; // server queue index
            foreach (ServerQueue serverQueue in globalQueue)
            {
                if (serverQueue.serverId == ctx.Guild.Id)
                    break;
                sqi++;
            }
            globalQueue[sqi].serverQueues.Add(input);

            string fileName = "";
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            { await ctx.RespondAsync("epic audio join fail"); return; }
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            { await Join(ctx, ctx.Channel); }

            LinkType type = CheckURL(input);
            fileName = input.Split('/')[input.Split('/').Length - 1];
            bool vidFlag = false;
            switch (type)
            {
                case LinkType.path:
                    ;
                    break;
                case LinkType.curl: // TODO: integrate with queueing
                    var curlPsi = GenCURLPSI(input, fileName);

                    var curlProcess = Process.Start(curlPsi);

                    vidFlag = true;
                    await ctx.Channel.SendMessageAsync($"downloading file: {input}. please wait.");
                    while (vidFlag)
                    { vidFlag = false;
                        if (!curlProcess.HasExited)
                            vidFlag = true; }

                    break;
                case LinkType.youtube:
                    await ctx.Channel.SendMessageAsync("downloading song... please wait.");
                    var ytpsi = GenYTPSI(globalQueue[sqi].trackNumber, input, ctx.Guild.Id);
                    var ytProcess = Process.Start(ytpsi);

                    vidFlag = true;
                    while (vidFlag)
                    { vidFlag = false;
                        if (!ytProcess.HasExited)
                            vidFlag = true; }

                    break;
            }

            while (vnc.IsPlaying)
                await vnc.WaitForPlaybackFinishAsync();

            // check track number

            Exception exc = null;
            string formatArgs = input;
            if (type == LinkType.path)
                formatArgs = input;
            else if (type == LinkType.curl)
                formatArgs = $@"curl\{fileName}";
            else if (type == LinkType.youtube)
                formatArgs = $@"queue\{globalQueue[sqi].serverId}\{globalQueue[sqi].trackNumber}.mp3";


            if (!File.Exists(formatArgs))
            { await ctx.Channel.SendMessageAsync($"unable to download or locate file {formatArgs}"); return; }

            await ctx.Message.RespondAsync($"now playing `{formatArgs}`");

            try
            {
                await vnc.SendSpeakingAsync(true);

                var psi = GenFfmpegPSI(formatArgs);
                globalQueue[sqi].ffmpegProcess = Process.Start(psi);
                var ffout = globalQueue[sqi].ffmpegProcess.StandardOutput.BaseStream;

                var txStream = vnc.GetTransmitSink();
                await ffout.CopyToAsync(txStream);
                await txStream.FlushAsync();
                await vnc.WaitForPlaybackFinishAsync();
            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vnc.SendSpeakingAsync(false);
                if (type == LinkType.curl)
                    File.Delete(@"curl\" + fileName);
                if (type == LinkType.youtube)
                    File.Delete(@$"queue\{globalQueue[sqi].serverId}\{globalQueue[sqi].trackNumber}.mp3");

                globalQueue[sqi].trackNumber++;
            }

            if (exc != null)
                await ctx.RespondAsync(exc.ToString());
        }

        [Command("fart")]
        public async Task Fart(CommandContext ctx) => await PlaySound(
            ctx, 
            fart,
            "epic fart fail!",
            "bro you cant hear my fart if u arent in vc dumbass",
            "i fart it"
            );

        #endregion

        #region funcs

        private LinkType CheckURL(string input)
        {
            bool containsYt = (input.Contains("yt.be", StringComparison.OrdinalIgnoreCase) || input.Contains("youtube", StringComparison.OrdinalIgnoreCase) || input.Contains("youtu.be", StringComparison.OrdinalIgnoreCase));
            bool containsProtocol = (input.Contains(@"http://", StringComparison.OrdinalIgnoreCase) || input.Contains(@"https://", StringComparison.OrdinalIgnoreCase));
            // check for not url
            if (!containsYt && !containsProtocol)
                return LinkType.path;

            // check for yt
            if (containsYt && containsProtocol)
                return LinkType.youtube;

            // ill add others later on

            // in case its not any of these but is still a link, its a curl

            return LinkType.curl;
        }

        private async Task PlaySound(CommandContext ctx, string path, string failMsg = "Voice next is not configured.", string ncMsg = "You are not in a voice channel.", string finalMsg = "Finished playing file")
        {
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            { await ctx.RespondAsync(failMsg); return; }

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            { await ctx.RespondAsync(ncMsg); }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $@"-i ""{path}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var ffmpeg = Process.Start(psi);
                var ffout = ffmpeg.StandardOutput.BaseStream;

                var txStream = vnc.GetTransmitSink();
                await ffout.CopyToAsync(txStream);
                await txStream.FlushAsync();
                await vnc.WaitForPlaybackFinishAsync();
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync(ex.ToString());
            }
            finally
            {
                await vnc.SendSpeakingAsync(false);
                await ctx.Channel.SendMessageAsync(finalMsg);
            }
        }

        #region PSI Generators

        private ProcessStartInfo GenYTPSI(int trNum, string inpt, ulong guId, bool playlist = false)
        {
            switch (playlist)
            {
                case false:
                    return new ProcessStartInfo
                    {
                        FileName = "youtube-dl.exe",
                        Arguments = $"--output \"queue\\{guId}\\{trNum}.mp3\" --audio-format mp3 -f bestaudio \"{inpt}\"",
                        UseShellExecute = false
                    };
                case true:
                    throw new NotImplementedException();
            }
        }

        private ProcessStartInfo GenCURLPSI(string input, string fileName)
        {
            return new ProcessStartInfo
            {
                FileName = "curl.exe",
                Arguments = $"\"{input}\" --output \"queue\\{fileName}\"",
                UseShellExecute = false
            };
        }

        private ProcessStartInfo GenFfmpegPSI(string path)
        {
            return new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $@"-i ""{path}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
        }

        #endregion

        #endregion
    }
}
