using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using System.IO;
using Newtonsoft.Json;
using WheelchairBot;
using DSharpPlus.VoiceNext;

namespace WheelchairBot
{
    public class Bot
    {
        public DiscordClient client { get; private set; }
        public CommandsNextExtension commands { get; private set; }
        public VoiceNextExtension voice { get; set; }
        public static bool isDev = false;
        public static string APIKey;
        public async Task RunAsync(Base.RunMode mode)
        {
            var json = string.Empty;

            using (FileStream fs = File.OpenRead("config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<Configuration>(json);

            Bot.APIKey = configJson.APIKey;

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
            };

            client = new DiscordClient(config);
            client.Ready += OnClientReady;

            var commandsConfig = new CommandsNextConfiguration 
            { 
                StringPrefixes = new string[] { configJson.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false
            };
            commands = client.UseCommandsNext(commandsConfig);

            commands.RegisterCommands<Modules.VoiceModule>();

            await client.ConnectAsync();

            //switch (mode)
            //{
            //    case Base.RunMode.normal:
            //        await clien.SetGameAsync("Good Software", null, ActivityType.Playing);
            //        await client.SetStatusAsync(UserStatus.Online);
            //        break;
            //    case Base.RunMode.debug:
            //        await client.SetGameAsync("in debug mode", null, ActivityType.Playing);
            //        await client.SetStatusAsync(UserStatus.DoNotDisturb);
            //        break;
            //    case Base.RunMode.dev:
            //        isDev = true;
            //        await client.SetGameAsync("in development", null, ActivityType.Playing);
            //        await client.SetStatusAsync(UserStatus.DoNotDisturb);
            //        break;
            //}

            voice = client.UseVoiceNext();

            await Task.Delay(-1);


        }

        private Task OnClientReady(DiscordClient c, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}
