﻿using System.Threading.Tasks;
using Volte.Commands;
using Volte.Data.Objects.EventArgs;
using Volte.Extensions;

namespace Volte.Services
{
    [Service("Blacklist", "The main Service for checking messages for blacklisted words/phrases in user's messages.")]
    public sealed class BlacklistService
    {
        internal async Task CheckMessageAsync(MessageReceivedEventArgs args)
        {
            foreach (var word in args.Config.ModerationOptions.Blacklist)
                if (args.Message.Content.ContainsIgnoreCase(word))
                {
                    await args.Message.DeleteAsync();
                    return;
                }
        }
    }
}