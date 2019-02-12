﻿using System;
using System.Threading.Tasks;
using Discord.Commands;
using Volte.Core.Extensions;

namespace Volte.Core.Commands.Modules.General {
    public partial class GeneralModule : VolteModule {
        [Command("Choose")]
        [Summary("Choose an item from a | delimited list.")]
        [Remarks("Usage: |prefix|choose {option1|option2|option3|...}")]
        public async Task Choose([Remainder] string message) {
            var opt = message.Split('|', StringSplitOptions.RemoveEmptyEntries);

            await Context.CreateEmbed($"I choose `{opt[new Random().Next(0, opt.Length)]}`.").SendTo(Context.Channel);
        }
    }
}