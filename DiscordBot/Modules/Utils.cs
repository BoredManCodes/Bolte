﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.Core.UserAccounts;
using System.Threading.Tasks;
using System;

namespace DiscordBot.Modules
{
    public class Utils : ModuleBase<SocketCommandContext>
    {
        DiscordSocketClient _client;

        [Command("UserInfo"), Alias("uinfo", "useri", "ui"), Priority(0)]
        public async Task UserInformationCommand()
        {
            var embed = new EmbedBuilder();
            embed.AddField("Username", Context.User.Username + "#" + Context.User.Discriminator);
            embed.AddField("User ID", Context.User.Id);
            embed.AddField("Game", Context.User.Game);
            embed.AddField("Status", Context.User.Status);
            embed.AddField("Account Created", Context.User.CreatedAt.UtcDateTime);
            embed.WithThumbnailUrl(Context.User.GetAvatarUrl());
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithColor(Config.bot.defaultEmbedColour);
            embed.WithTitle("User Information");
            embed.AddField("Is Bot", Context.User.IsBot);

            await Context.Channel.SendMessageAsync("", false, embed);
        }

        [Command("Feedback"), Alias("Fb")]
        public async Task SendFeedbackToDev([Remainder]string feedback)
        {
            var embed = new EmbedBuilder();
            embed.WithDescription(Utilities.GetFormattedAlert("FeedbackCommandText", feedback));
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithColor(Config.bot.defaultEmbedColour);
            embed.WithTitle("Feedback to Greem");

            var feedbackGuild = _client.GetGuild(405806471578648588);
            var feedbackChannel = feedbackGuild.GetTextChannel(Config.bot.feedbackChannelId);
            await feedbackChannel.SendMessageAsync("", false, embed);
        }

        [Command("Calculator"), Alias("Calc")]
        public async Task Calculate(string oper, int val1, int val2 = 0)
        {
            var embed = new EmbedBuilder();
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithColor(Config.bot.defaultEmbedColour);
            embed.WithTitle("Calculator");

            if (oper != "")
            {
                if (oper == "add")
                {
                    var result = val1 + val2;
                    embed.WithDescription($"The answer is `{result.ToString()}`");
                }
                if (oper == "sub")
                {
                    var result = val1 - val2;
                    embed.WithDescription($"The answer is `{result.ToString()}`");
                }
                if (oper == "mult")
                {
                    var result = Math.BigMul(val1, val2);
                    embed.WithDescription($"The answer is `{result.ToString()}`");
                }
                if (oper == "div")
                {
                    var result = Math.DivRem(val1, val2, out int b);
                    embed.WithDescription($"The answer is `{b.ToString()}`");
                }
                if (oper == "sqrt")
                {
                    var result = Math.Sqrt(val1);
                    embed.WithDescription($"The answer is `{result.ToString()}`");
                }
                if (oper == "power")
                {
                    var result = Math.Pow(val1, val2);
                    embed.WithDescription($"The answer is `{result.ToString()}`");
                }

                await Context.Channel.SendMessageAsync("", false, embed);
            }
            else
            {
                await Context.Channel.SendMessageAsync("You forgot to specify an operation. Valid operations are `add`, `sub`, `mult`, `div`, `power`, and `sqrt`.");
            }
        }

        [Command("YouTube"), Alias("Yt")]
        public async Task SearchYouTube([Remainder]string query)
        {
            var embed = new EmbedBuilder();
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithColor(Config.bot.defaultEmbedColour);
            embed.WithThumbnailUrl("https://www.freepnglogos.com/uploads/youtube-logo-hd-8.png");

            var url = "https://youtube.com/results?search_query=";
            var newQuery = query.Replace(' ', '+');
            embed.WithDescription(url + newQuery);

            await Context.Channel.SendMessageAsync("", false, embed);
        }

        [Command("UserInfo"), Alias("uinfo", "useri", "ui"), Priority(1)]
        public async Task UserInformationCommand(SocketGuildUser user)
        {
            var embed = new EmbedBuilder();
            embed.AddField("Username", user.Username + "#" + user.Discriminator);
            embed.AddField("User ID", user.Id);
            embed.AddField("Game", user.Game);
            embed.AddField("Status", user.Status);
            embed.AddField("Account Created", user.CreatedAt.UtcDateTime);
            embed.WithThumbnailUrl(user.GetAvatarUrl());
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", user.Username));
            embed.WithColor(Config.bot.defaultEmbedColour);
            embed.WithTitle("User Information");
            embed.AddField("Is Bot", Context.User.IsBot);

            await Context.Channel.SendMessageAsync("", false, embed);
        }

        [Command("ServerInfo"), Alias("sinfo", "serveri", "si")]
        public async Task ServerInformationCommand()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("Server Information");
            embed.AddField("Name", Context.Guild.Name);
            embed.AddField("Created", Context.Guild.CreatedAt.UtcDateTime);
            embed.AddField("Users", Context.Guild.Users.Count);
            embed.AddField("Text Channels", Context.Guild.TextChannels.Count);
            embed.AddField("Voice Channels", Context.Guild.VoiceChannels.Count);
            embed.AddField("Region", Context.Guild.VoiceRegionId);
            embed.WithThumbnailUrl(Context.Guild.IconUrl);
            embed.AddField("Roles", Context.Guild.Roles.Count);
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithColor(Config.bot.defaultEmbedColour);

            await Context.Channel.SendMessageAsync("", false, embed);
        }
        
        [Command("LeaveServer"), Alias("ls")]
        public async Task LeaveTheServerOnDemand()
        {
            if (Context.User.Id != Config.bot.botOwner) {
                //we don't want random users making the bot leave now do we?
                var embed = new EmbedBuilder();
                embed.WithDescription(Utilities.GetFormattedAlert("NotEnoughPermission", Context.User.Mention));
                embed.WithColor(new Color(Config.bot.defaultEmbedColour));
                embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
                Console.WriteLine($"User {Context.User.Username}#{Context.User.Discriminator} tried to get the bot to leave the server {Context.Guild.Name}, but failed.");
                await Context.Channel.SendMessageAsync("", false, embed);

            }
            else
            {
                var embed = new EmbedBuilder();
                embed.WithDescription(Utilities.GetAlert("BotLeftServer"));
                embed.WithColor(Config.bot.defaultEmbedColour);
                embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
                await Context.Channel.SendMessageAsync("", false, embed);
                await Context.Guild.LeaveAsync();

            }
        }

        [Command("Ping")]
        public async Task PingTheFuckingBot()
        {
            var embed = new EmbedBuilder();
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithDescription(Utilities.GetFormattedAlert("PingCommandText", (Context.Client as DiscordSocketClient).Latency));
            embed.WithColor(new Color(Config.bot.defaultEmbedColour));

            await Context.Channel.SendMessageAsync("", false, embed);
        }

        [Command("Google")]
        public async Task Google([Remainder]string Search)
        {
            Search = Search.Replace(' ', '+');
            string SearchUrl = $"https://google.com/search?q={Search}";
            var embed = new EmbedBuilder();
            embed.WithDescription(SearchUrl);
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithColor(new Color(Config.bot.defaultEmbedColour));

            await Context.Channel.SendMessageAsync("", false, embed);
        }

        [Command("AddXp")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddXP(uint xp)
        {
            var account = UserAccounts.GetAccount(Context.User);
            account.XP += xp;
            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync($"You gained {xp} XP.");
        }

        [Command("Invite")]
        public async Task InviteUserToUseBot()
        {
            var embed = new EmbedBuilder() {
                Description = "Invite: https://discordapp.com/oauth2/authorize?client_id=320942091049893888&scope=bot&permissions=8",
            };
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithColor(Config.bot.defaultEmbedColour);

            await Context.Channel.SendMessageAsync("", false, embed);
        }

        /*[Command("Uptime")]
        public async Task UptimeCommand()
        {
            var now = DateTime.UtcNow;
            var delta = now - 
            var embed = new EmbedBuilder();
            embed.WithDescription($"Bot Uptime: {uptime}");
            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
        }*/

    }
}
