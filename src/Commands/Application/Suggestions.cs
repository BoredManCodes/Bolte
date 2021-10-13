using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Gommon;
using Volte.Entities;
using Volte.Helpers;
using Volte.Interactions;

namespace Volte.Commands.Application
{
    public class SuggestionCommand : ApplicationCommand
    {
        public SuggestionCommand()
            : base("suggestion", "Create suggestions, and if you're an admin, view and delete them, too.", true)
            => Signature(x =>
            {
                x.Subcommand("view", "View and accept/deny, and/or delete suggestions.");
                x.Subcommand("create",
                    "Create a suggestion in this server for the staff to view and accept or deny.", o =>
                    {
                        o.RequiredString("title", "Sum up your suggestion in a few words.");
                        o.RequiredString("full-description", "Your full suggestion.");
                    });
                x.SubcommandGroup("notification-channel",
                    "Get or set the channel that new suggestions are sent to.", o =>
                    {
                        o.Subcommand("get", "Get the current channel for suggestion notifications.");
                        o.Subcommand("set", "Set a new channel for suggestion notifications.", opts =>
                            opts.RequiredChannel("channel",
                                "The new channel for notifications. Must be a text channel.")
                        );
                    });
            });

        private readonly Func<IEnumerable<Suggestion>, SelectMenuBuilder> _getSuggestionsMenu = s
            => new SelectMenuBuilder()
                .WithPlaceholder("Choose a suggestion...")
                .WithCustomId("suggestion:adminMenu")
                .WithOptions(s.Take(25).Select(sugg => new SelectMenuOptionBuilder()
                        .WithLabel($"{sugg.ShortSummary.Truncate(100)}")
                        .WithDescription(sugg.LongDescription.Truncate(100))
                        .WithValue(sugg.Uuid))
                    .ToList());

        private readonly Func<Suggestion, MessageComponent> _getSuggestionButtons = s
            => new ComponentBuilder().AddActionRow(r =>
                    r.AddComponent(Buttons.Success($"suggestion:approve:{s.Uuid}", "Approve",
                            DiscordHelper.BallotBoxWithCheck))
                        .AddComponent(Buttons.Danger($"suggestion:deny:{s.Uuid}", "Deny", DiscordHelper.X)))
                .AddActionRow(r =>
                    r.AddComponent(Buttons.Secondary($"suggestion:pass:{s.Uuid}", "Choose another",
                        DiscordHelper.Question)))
                .Build();

        public override async Task HandleSlashCommandAsync(SlashCommandContext ctx)
        {
            var reply = ctx.CreateReplyBuilder(true);
            var subcommandGroup = ctx.Options.First().Value;
            //the power of discord engineering
            var subcommandOrArgument = subcommandGroup.Options.FirstOrDefault();
            switch (subcommandGroup.Name)
            {
                case "create":
                    if (ctx.GuildSettings.Extras.Suggestions.Count >= SelectMenuBuilder.MaxOptionCount)
                    {
                        reply.WithEmbed(x => x.WithTitle("Too many suggestions!").WithDescription("This guild has 25 currently active suggestions."));
                        break;
                    }
                        
                    var title = subcommandGroup.GetOption("title").GetAsString();
                    var fullDescription = subcommandGroup.GetOption("full-description").GetAsString();
                    var suggestion = new Suggestion
                    {
                        Uuid = Guid.NewGuid(),
                        ShortSummary = title,
                        LongDescription = fullDescription,
                        CreatorId = ctx.User.Id
                    };
                    ctx.ModifyGuildSettings(d => d.Extras.Suggestions.Add(suggestion));
                    reply.WithEmbed(x =>
                    {
                        x.WithAuthor("Suggestion created!");
                        x.WithTitle(title);
                        x.WithDescription(fullDescription);
                    });
                    await SendInitialAsync(ctx, suggestion);
                    break;
                case "view":
                    if (!ctx.IsModerator(ctx.GuildUser))
                        reply.WithEmbed(e => e.WithTitle("You can't use this command."));
                    else if (ctx.GuildSettings.Extras.Suggestions.IsEmpty())
                        reply.WithEmbed(e => e.WithTitle("There's no suggestions in this guild."));
                    else
                        reply.WithEmbed(e => e.WithTitle("Choose a suggestion below to proceed."))
                            .WithSelectMenu(_getSuggestionsMenu(ctx.GuildSettings.Extras.Suggestions));
                    break;
                case "notification-channel":
                    if (!ctx.IsAdmin(ctx.GuildUser))
                        reply.WithEmbedFrom("You can't use this command.");
                    else if (subcommandOrArgument!.Name is "get")
                    {
                        var channel =
                            ctx.Guild.GetTextChannel(ctx.GuildSettings.Extras.SuggestionNotificationChannelId);
                        reply.WithEmbedFrom(channel is null
                            ? "The suggestion notification channel is currently not set."
                            : $"The current suggestion notification channel is {channel.Mention}.");
                    }
                    else
                    {
                        var newChannel = subcommandOrArgument.Options.First().GetAsGuildChannel();
                        if (!(newChannel is SocketTextChannel textChannel))
                            reply.WithEmbedFrom("You can only use text channels.");
                        else
                        {
                            reply.WithEmbedFrom($"The new suggestion notification channel is {textChannel.Mention}.");
                            ctx.ModifyGuildSettings(data =>
                                data.Extras.SuggestionNotificationChannelId = newChannel.Id);
                        }
                    }
                    break;
            }

            await reply.RespondAsync();
        }

        public override async Task HandleComponentAsync(MessageComponentContext ctx)
        {
            switch (ctx.Id.Action)
            {
                case "approve":
                    var target = ctx.GuildSettings.Extras.Suggestions.First(x => 
                        x.Uuid == Guid.Parse(ctx.Id.Value));
                    
                    ctx.ModifyGuildSettings(d => d.Extras.Suggestions.RemoveWhere(x => x.Uuid == target.Uuid));

                    var ss = ctx.GuildSettings.Extras.Suggestions;
                    
                    await ctx.UpdateAsync(x =>
                    {
                        x.Components = ss.IsEmpty()
                            ? new ComponentBuilder().Build()
                            : new ComponentBuilder()
                                .WithSelectMenu(_getSuggestionsMenu(ctx.GuildSettings.Extras.Suggestions))
                                .Build();

                        x.Embed = ctx.CreateEmbedBuilder(ss.IsEmpty()
                                ? "You've handled all of the suggestions."
                                : "Deleted that suggestion. Please select another suggestion from the list.")
                            .Build();
                    });
                    await SendFinalStatusAsync(ctx, target, true);
                    break;
                case "pass":
                    if (ctx.GuildSettings.Extras.Suggestions.Where(x => x.Uuid != Guid.Parse(ctx.Id.Value)).IsEmpty())
                        await ctx.CreateReplyBuilder(true)
                            .WithEmbed(e => e.WithTitle("There's no other suggestions in this guild.")).RespondAsync();
                    else
                    {
                        await ctx.UpdateAsync(m =>
                        {
                            m.Components = new ComponentBuilder()
                                    .WithSelectMenu(_getSuggestionsMenu(ctx.GuildSettings.Extras.Suggestions))
                                    .Build();

                            m.Embed = ctx.CreateEmbedBuilder("We'll let that suggestion sit for a while. Please select another below.").Build();
                        });
                    }
                    break;
                
                case "deny":
                    var targetSuggestion = ctx.GuildSettings.Extras.Suggestions.First(x => 
                        x.Uuid == Guid.Parse(ctx.Id.Value));
                    
                    ctx.ModifyGuildSettings(d => d.Extras.Suggestions.RemoveWhere(x => x.Uuid == targetSuggestion.Uuid));

                    var suggestions = ctx.GuildSettings.Extras.Suggestions;

                    await ctx.UpdateAsync(x =>
                    {
                        x.Components = suggestions.IsEmpty()
                            ? new ComponentBuilder().Build()
                            : new ComponentBuilder()
                                .WithSelectMenu(_getSuggestionsMenu(ctx.GuildSettings.Extras.Suggestions))
                                .Build();

                        x.Embed = ctx.CreateEmbedBuilder(suggestions.IsEmpty()
                                ? "You've handled all of the suggestions."
                                : "Deleted that suggestion. Please select another suggestion from the list.")
                            .Build();
                    });
                    await SendFinalStatusAsync(ctx, targetSuggestion, false);
                    break;
                case "adminMenu":
                    var suggestion =
                        ctx.GuildSettings.Extras.Suggestions.First(x =>
                            x.Uuid == Guid.Parse(ctx.SelectedMenuOptions.First()));

                    await ctx.Interaction.UpdateAsync(x =>
                    {
                        x.Embed = ctx.CreateEmbedBuilder()
                            .WithAuthor(ctx.Guild.GetUser(suggestion.CreatorId))
                            .WithTitle(suggestion.ShortSummary)
                            .WithDescription(suggestion.LongDescription)
                            .WithFooter(suggestion.Uuid.ToString())
                            .Build();
                        x.Components = _getSuggestionButtons(suggestion);
                    });
                    break;
            }
        }

        private async Task SendInitialAsync(SlashCommandContext ctx, Suggestion suggestion)
        {
            var e = ctx.CreateEmbedBuilder()
                .WithAuthor("New Suggestion")
                .WithTitle(suggestion.ShortSummary.Truncate(EmbedBuilder.MaxTitleLength))
                .WithDescription(suggestion.LongDescription.Truncate(EmbedBuilder.MaxDescriptionLength))
                .WithFooter($"Created by {ctx.User}.", ctx.User.GetEffectiveAvatarUrl());
            
            var c = ctx.Guild.GetTextChannel(ctx.GuildSettings.Extras.SuggestionNotificationChannelId);
            if (c != null)
                await e.SendToAsync(c);
        }

        private async Task SendFinalStatusAsync(MessageComponentContext ctx, Suggestion suggestion, bool approved)
        {
            var e = ctx.CreateEmbedBuilder()
                .WithAuthor(approved ? "Suggestion approved!" : "Suggestion denied!")
                .WithFooter($"{(approved ? "Approved" : "Denied")} by {ctx.User}.", ctx.User.GetEffectiveAvatarUrl())
                .WithTitle(suggestion.ShortSummary.Truncate(EmbedBuilder.MaxTitleLength))
                .WithDescription(suggestion.LongDescription.Truncate(EmbedFieldBuilder.MaxFieldValueLength))
                .WithColor(approved ? Color.Green : Color.Red);

            var u = ctx.Guild.GetUser(suggestion.CreatorId);
            if (u != null)
                await e.SendToAsync(u);

            var c = ctx.Guild.GetTextChannel(ctx.GuildSettings.Extras.SuggestionNotificationChannelId);
            if (c != null)
                await e.SendToAsync(c);
        }
    }
}