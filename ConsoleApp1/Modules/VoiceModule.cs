using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System.Text.RegularExpressions;
using SeasideResearch.LibCurlNet;

namespace WheelchairBot.Modules
{
    public class VoiceModule : BaseCommandModule
    {
        private readonly Curler curler = new Curler();

        private readonly string fart = @"sfx\fart.mp3";

        int currentTrack = 1;

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
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            // check whether VNext is enabled
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
            vnc.Disconnect();
            await ctx.RespondAsync("later losers");
        }

        [Command("fuckoff"), Description("Leaves a voice channel.")]
        public async Task FuckOff(CommandContext ctx)
        {
            // check whether VNext is enabled
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
            vnc.Disconnect();
            await ctx.RespondAsync("alright chill ill leave");
        }

        [Command("play"), Description("Plays an audio file.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Full path to the file to play.")] string input = null)
        {
            string fileName = "";

            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                await ctx.RespondAsync("epic audio join fail");
                return;
            }

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                await ctx.RespondAsync("bro i aint even in a vc");
                return;
            }

            // get regex of links, if its not a link, get a file

            LinkType type = CheckURL(input);
            await ctx.Channel.SendMessageAsync(type.ToString());
            fileName = input.Split('/')[input.Split('/').Length - 1];
            bool vidFlag = false;
            switch (type)
            {
                case LinkType.path:
                    ;
                    break;
                case LinkType.curl:
                    // get file name from link
                    // split string at / and get the last part
                    // add queue\ to path
                    var curlPsi = new ProcessStartInfo
                    {
                        FileName = "curl.exe",
                        Arguments = $"\"{input}\" --output \"curl\\{fileName}\"",
                        UseShellExecute = false
                    };

                    var curlProcess = Process.Start(curlPsi);

                    vidFlag = true;
                    await ctx.Channel.SendMessageAsync($"downloading file: {input}. please wait.");
                    while (vidFlag)
                    {
                        vidFlag = false;
                        if (!curlProcess.HasExited)
                            vidFlag = true;
                    }
                    break;
                case LinkType.youtube:
                    var ytpsi = new ProcessStartInfo
                    {
                        FileName = "youtube-dl.exe",
                        Arguments = $"--output \"queue\\{currentTrack}.mp3\" --audio-format mp3 -f bestaudio \"{input}\"",
                        UseShellExecute = false
                    };

                    var ytProcess = Process.Start(ytpsi);

                    vidFlag = true;
                    while (vidFlag)
                    {
                        vidFlag = false;
                        if (!ytProcess.HasExited)
                            vidFlag = true;
                    }
                    break;
            }

            while (vnc.IsPlaying)
                await vnc.WaitForPlaybackFinishAsync();


            Exception exc = null;

            // check if file exists

            // find what type of audio the file is

            string formatArgs = input;
            if (type == LinkType.path)
                formatArgs = input;
            else if (type == LinkType.curl)
                formatArgs = $@"curl\{fileName}";
            else if (type == LinkType.youtube)
                formatArgs = $@"queue\{currentTrack}.mp3";


            if (!File.Exists(formatArgs))
            { await ctx.Channel.SendMessageAsync($"unable to download or locate file {formatArgs}"); return; }

            await ctx.Message.RespondAsync($"now playing `{formatArgs}`");

            try
            {
                await vnc.SendSpeakingAsync(true);
                
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $@"-i ""{formatArgs}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
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
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vnc.SendSpeakingAsync(false);
                if (type == LinkType.curl)
                    File.Delete(@"curl\" + fileName);
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

        #endregion
    }
}
