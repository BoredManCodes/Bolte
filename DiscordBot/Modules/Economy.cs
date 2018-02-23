﻿using Discord.Commands;
using System.Threading.Tasks;
using DiscordBot.Core.UserAccounts;
using Discord;
using DiscordBot;

namespace DiscordBot.Modules
{
    public class Economy : ModuleBase<SocketCommandContext>
    {
        [Command("Money"), Alias("$", "bal")]
        public async Task HowMuchDoIHave()
        {
            var ua = UserAccounts.GetAccount(Context.User);
            var bal = ua.Money.ToString();
            var embed = new EmbedBuilder();

            embed.WithFooter(Utilities.GetFormattedAlert("CommandFooter", Context.User.Username));
            embed.WithDescription(Utilities.GetFormattedAlert("MoneyCommandText", bal));
            embed.WithColor(Config.bot.defaultEmbedColour);
            embed.WithThumbnailUrl("http://www.stickpng.com/assets/images/580b585b2edbce24c47b2878.png");

            await Context.Channel.SendMessageAsync("", false, embed);
        }

    }
}
