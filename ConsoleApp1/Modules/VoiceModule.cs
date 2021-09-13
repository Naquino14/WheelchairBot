//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Threading.Tasks;
//using DSharpPlus;
//using DSharpPlus.CommandsNext;
//using DSharpPlus.CommandsNext.Attributes;
//using DSharpPlus.Entities;
//using DSharpPlus.VoiceNext;

//namespace WheelchairBot.Modules
//{
//    public class VoiceModule : BaseCommandModule
//    {
//        private bool playingMusic = false;

//        private static readonly string fartReverb = @"sfx\fart.mp3";

//        [Command("ping")]
//        public async Task Ping(CommandContext context)
//        {
//            await context.Channel.SendMessageAsync("pong");
//        }

//        [Command("join")]
//        public async Task Join(CommandContext context, DiscordChannel channel = null)
//        {
//            Console.WriteLine("fired");
//            var voiceNext = context.Client.GetVoiceNext();
//            if (voiceNext == null)
//            { await context.RespondAsync("epic join fail!"); return; }

//            var voiceNextConnection = voiceNext.GetConnection(context.Guild);
//            if (voiceNextConnection != null)
//            { await context.RespondAsync("bruh! i am already in a vc dumbass!!!"); return; }

//            var voiceNextStatus = context.Member?.VoiceState;
//            if (voiceNextStatus.Channel == null && channel == null)
//            { await context.RespondAsync("hey quagmire! get a load of this dumbass tryna get me in a vc without being in one lmaooo."); return; }

//            if (channel == null)
//                channel = voiceNextStatus.Channel;

//            voiceNextConnection = await voiceNext.ConnectAsync(channel);
//            await context.Channel.SendMessageAsync($"whats up fuckers, i connected to {channel.Name}");
//        }

//        //[Command("fuckoff")]
//        //public async Task Leave(CommandContext context, DiscordChannel channel)
//        //{
//        //    var voiceNext = context.Client.GetVoiceNext();
//        //    if (voiceNext == null)
//        //    { await context.RespondAsync("epic leave fail!"); return; }

//        //    var voiceNextConnection = voiceNext.GetConnection(context.Guild);
//        //    if (voiceNextConnection == null)
//        //    { await context.RespondAsync("bro i aint even connected wtf???1/1//?"); return; }

//        //    var voiceNextStatus = context.Member?.VoiceState;

//        //    voiceNextConnection.Disconnect();
//        //    await context.Channel.SendMessageAsync("later losers.");
//        //}

//        [Command("test")]
//        public async Task Fart(CommandContext context, DiscordChannel channel)
//        {
//            Console.WriteLine("fired");
//            await context.Channel.SendMessageAsync("etst");
//            var voiceNext = context.Client.GetVoiceNext();
//            if (voiceNext == null)
//            {
//                // not enabled
//                await context.RespondAsync("VNext is not enabled or configured.");
//                return;
//            }

//            // check whether we aren't already connected
//            var voiceNextConnection = voiceNext.GetConnection(context.Guild);
//            if (voiceNextConnection == null)
//            {
//                // already connected
//                await context.RespondAsync("Not connected in this guild.");
//                return;
//            }


//            if (playingMusic)
//            { await context.Channel.SendMessageAsync("bro that would disturb the music..."); return; }

//            try
//            {
//                await voiceNextConnection.SendSpeakingAsync(true);

//                var processStartInfo = new ProcessStartInfo
//                {
//                    FileName = "ffmpeg.exe",
//                    Arguments = $@"-i ""{fartReverb}"" -ac 2 -f s16le - ar 48000 pipe:1 -loglevel quiet",
//                    RedirectStandardOutput = true,
//                    UseShellExecute = false
//                };
//                var ffmpeg = Process.Start(processStartInfo);
//                var ffout = ffmpeg.StandardOutput.BaseStream;

//                var transmitStream = voiceNextConnection.GetTransmitSink();
//                await ffout.CopyToAsync(transmitStream);
//                await transmitStream.FlushAsync();
//                await voiceNextConnection.WaitForPlaybackFinishAsync();

//            }
//            catch (Exception ex)
//            {
//                await context.Channel.SendMessageAsync(ex.ToString());
//            }
//            finally
//            { await voiceNextConnection.SendSpeakingAsync(false); }

//        }
//    }
//}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

namespace WheelchairBot.Modules
{
    public class VoiceModule : BaseCommandModule
    {
        private readonly string fart = @"sfx\fart.mp3";

        [Command("join"), Description("Joins a voice channel.")]
        public async Task Join(CommandContext ctx, DiscordChannel chn = null)
        {
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                // already connected
                await ctx.RespondAsync("Already connected in this guild.");
                return;
            }

            // get member's voice state
            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && chn == null)
            {
                // they did not specify a channel and are not in one
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            // channel not specified, use user's
            if (chn == null)
                chn = vstat.Channel;

            // connect
            vnc = await vnext.ConnectAsync(chn);
            await ctx.RespondAsync($"Connected to `{chn.Name}`");
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we are connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // not connected
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            // disconnect
            vnc.Disconnect();
            await ctx.RespondAsync("Disconnected");
        }

        [Command("play"), Description("Plays an audio file.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Full path to the file to play.")] string filename)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // already connected
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            // check if file exists
            if (!File.Exists(filename))
            {
                // file does not exist
                await ctx.RespondAsync($"File `{filename}` does not exist.");
                return;
            }

            // wait for current playback to finish
            while (vnc.IsPlaying)
                await vnc.WaitForPlaybackFinishAsync();

            // play
            Exception exc = null;
            await ctx.Message.RespondAsync($"Playing `{filename}`");

            try
            {
                await vnc.SendSpeakingAsync(true);

                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $@"-i ""{filename}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
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
                await ctx.Message.RespondAsync($"Finished playing `{filename}`");
            }

            if (exc != null)
                await ctx.RespondAsync($"An exception occured during playback: `{exc.GetType()}: {exc.Message}`");
        }

        [Command("fart")]
        public async Task Fart(CommandContext ctx)
        {
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            { await ctx.RespondAsync("epic fart fail!"); return; }

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null) 
            { await ctx.RespondAsync("bro you cant hear my fart if u arent in vc dumbass"); }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $@"-i ""{fart}"" -ac 2 -f s16le -ar 48000 pipe:1 -loglevel quiet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var ffmpeg = Process.Start(psi);
                var ffout = ffmpeg.StandardOutput.BaseStream;

                var txStream = vnc.GetTransmitSink();
                await ffout.CopyToAsync(txStream);
                await txStream.FlushAsync();
                await vnc.WaitForPlaybackFinishAsync();
            } catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync(ex.ToString());
            } finally
            {
                await vnc.SendSpeakingAsync(false);
                await ctx.Channel.SendMessageAsync("i fart it");
            }
        }
    }
}
