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
    public class TodoCommand : ApplicationCommand
    {
        public TodoCommand()
            : base("todo", "Create to-dos, and if you're an admin, view and delete them, too.", true)
            => Signature(x =>
            {
                x.Subcommand("view", "View and complete/remove, and/or delete to-dos.");
                x.Subcommand("create",
                    "Create a to-do in this server for the staff to view and complete or remove.", o =>
                    {
                        o.RequiredString("title", "Sum up your to-do in a few words.");
                        o.RequiredString("full-description", "Your full to-do.");
                    });
                x.SubcommandGroup("notification-channel",
                    "Get or set the channel that new to-dos are sent to.", o =>
                    {
                        o.Subcommand("get", "Get the current channel for to-do notifications.");
                        o.Subcommand("set", "Set a new channel for to-do notifications.", opts =>
                            opts.RequiredChannel("channel",
                                "The new channel for notifications. Must be a text channel.")
                        );
                    });
            });

        private readonly Func<IEnumerable<Todo>, SelectMenuBuilder> _getTodosMenu = s
            => new SelectMenuBuilder()
                .WithPlaceholder("Choose a to-do...")
                .WithCustomId("todo:adminMenu")
                .WithOptions(s.Take(25).Select(todo => new SelectMenuOptionBuilder()
                        .WithLabel($"{todo.ShortSummary.Truncate(100)}")
                        .WithDescription(todo.LongDescription.Truncate(100))
                        .WithValue(todo.Uuid))
                    .ToList());

        private readonly Func<Todo, MessageComponent> _getTodoButtons = s
            => new ComponentBuilder().AddActionRow(r =>
                    r.AddComponent(Buttons.Success($"todo:complete:{s.Uuid}", "Complete",
                            DiscordHelper.BallotBoxWithCheck))
                        .AddComponent(Buttons.Danger($"todo:remove:{s.Uuid}", "Remove", DiscordHelper.X)))
                .AddActionRow(r =>
                    r.AddComponent(Buttons.Secondary($"todo:pass:{s.Uuid}", "Choose another",
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
                    if (ctx.GuildSettings.Extras.Todos.Count >= SelectMenuBuilder.MaxOptionCount)
                    {
                        reply.WithEmbed(x => x.WithTitle("Too many to-dos!").WithDescription("This guild has 25 currently active to-dos."));
                        break;
                    }
                        
                    var title = subcommandGroup.GetOption("title").GetAsString();
                    var fullDescription = subcommandGroup.GetOption("full-description").GetAsString();
                    var todo = new Todo
                    {
                        Uuid = Guid.NewGuid(),
                        ShortSummary = title,
                        LongDescription = fullDescription,
                        CreatorId = ctx.User.Id
                    };
                    ctx.ModifyGuildSettings(d => d.Extras.Todos.Add(todo));
                    reply.WithEmbed(x =>
                    {
                        x.WithAuthor("To-do created!");
                        x.WithTitle(title);
                        x.WithDescription(fullDescription);
                    });
                    await SendInitialAsync(ctx, todo);
                    break;
                case "view":
                    if (!ctx.IsModerator(ctx.GuildUser))
                        reply.WithEmbed(e => e.WithTitle("You can't use this command."));
                    else if (ctx.GuildSettings.Extras.Todos.IsEmpty())
                        reply.WithEmbed(e => e.WithTitle("There's no to-dos in this guild."));
                    else
                        reply.WithEmbed(e => e.WithTitle("Choose a to-do below to proceed."))
                            .WithSelectMenu(_getTodosMenu(ctx.GuildSettings.Extras.Todos));
                    break;
                case "notification-channel":
                    if (!ctx.IsAdmin(ctx.GuildUser))
                        reply.WithEmbedFrom("You can't use this command.");
                    else if (subcommandOrArgument!.Name is "get")
                    {
                        var channel =
                            ctx.Guild.GetTextChannel(ctx.GuildSettings.Extras.TodoNotificationChannelId);
                        reply.WithEmbedFrom(channel is null
                            ? "The to-do notification channel is currently not set."
                            : $"The current to-do notification channel is {channel.Mention}.");
                    }
                    else
                    {
                        var newChannel = subcommandOrArgument.Options.First().GetAsGuildChannel();
                        if (!(newChannel is SocketTextChannel textChannel))
                            reply.WithEmbedFrom("You can only use text channels.");
                        else
                        {
                            reply.WithEmbedFrom($"The new to-do notification channel is {textChannel.Mention}.");
                            ctx.ModifyGuildSettings(data =>
                                data.Extras.TodoNotificationChannelId = newChannel.Id);
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
                case "complete":
                    var target = ctx.GuildSettings.Extras.Todos.First(x => 
                        x.Uuid == Guid.Parse(ctx.Id.Value));
                    
                    ctx.ModifyGuildSettings(d => d.Extras.Todos.RemoveWhere(x => x.Uuid == target.Uuid));

                    var ss = ctx.GuildSettings.Extras.Todos;
                    
                    await ctx.UpdateAsync(x =>
                    {
                        x.Components = ss.IsEmpty()
                            ? new ComponentBuilder().Build()
                            : new ComponentBuilder()
                                .WithSelectMenu(_getTodosMenu(ctx.GuildSettings.Extras.Todos))
                                .Build();

                        x.Embed = ctx.CreateEmbedBuilder(ss.IsEmpty()
                                ? "You've handled all of the to-dos."
                                : "Deleted that to-do. Please select another to-do from the list.")
                            .Build();
                    });
                    await SendFinalStatusAsync(ctx, target, true);
                    break;
                case "pass":
                    if (ctx.GuildSettings.Extras.Todos.Where(x => x.Uuid != Guid.Parse(ctx.Id.Value)).IsEmpty())
                        await ctx.CreateReplyBuilder(true)
                            .WithEmbed(e => e.WithTitle("There's no other to-dos in this guild.")).RespondAsync();
                    else
                    {
                        await ctx.UpdateAsync(m =>
                        {
                            m.Components = new ComponentBuilder()
                                    .WithSelectMenu(_getTodosMenu(ctx.GuildSettings.Extras.Todos))
                                    .Build();

                            m.Embed = ctx.CreateEmbedBuilder("We'll let that to-do sit for a while. Please select another below.").Build();
                        });
                    }
                    break;
                
                case "remove":
                    var targetTodo = ctx.GuildSettings.Extras.Todos.First(x => 
                        x.Uuid == Guid.Parse(ctx.Id.Value));
                    
                    ctx.ModifyGuildSettings(d => d.Extras.Todos.RemoveWhere(x => x.Uuid == targetTodo.Uuid));

                    var todos = ctx.GuildSettings.Extras.Todos;

                    await ctx.UpdateAsync(x =>
                    {
                        x.Components = todos.IsEmpty()
                            ? new ComponentBuilder().Build()
                            : new ComponentBuilder()
                                .WithSelectMenu(_getTodosMenu(ctx.GuildSettings.Extras.Todos))
                                .Build();

                        x.Embed = ctx.CreateEmbedBuilder(todos.IsEmpty()
                                ? "You've handled all of the to-dos."
                                : "Deleted that to-do. Please select another to-do from the list.")
                            .Build();
                    });
                    await SendFinalStatusAsync(ctx, targetTodo, false);
                    break;
                case "adminMenu":
                    var todo =
                        ctx.GuildSettings.Extras.Todos.First(x =>
                            x.Uuid == Guid.Parse(ctx.SelectedMenuOptions.First()));

                    await ctx.Interaction.UpdateAsync(x =>
                    {
                        x.Embed = ctx.CreateEmbedBuilder()
                            .WithAuthor(ctx.Guild.GetUser(todo.CreatorId))
                            .WithTitle(todo.ShortSummary)
                            .WithDescription(todo.LongDescription)
                            .WithFooter(todo.Uuid.ToString())
                            .Build();
                        x.Components = _getTodoButtons(todo);
                    });
                    break;
            }
        }

        private async Task SendInitialAsync(SlashCommandContext ctx, Todo todo)
        {
            var e = ctx.CreateEmbedBuilder()
                .WithAuthor("New To-do")
                .WithTitle(todo.ShortSummary.Truncate(EmbedBuilder.MaxTitleLength))
                .WithDescription(todo.LongDescription.Truncate(EmbedBuilder.MaxDescriptionLength))
                .WithFooter($"Created by {ctx.User}.", ctx.User.GetEffectiveAvatarUrl());
            
            var c = ctx.Guild.GetTextChannel(ctx.GuildSettings.Extras.TodoNotificationChannelId);
            if (c != null)
                await e.SendToAsync(c);
        }

        private async Task SendFinalStatusAsync(MessageComponentContext ctx, Todo todo, bool completed)
        {
            var e = ctx.CreateEmbedBuilder()
                .WithAuthor(completed ? "To-Do completed!" : "To-do Removed!")
                .WithFooter($"{(completed ? "Completed" : "Removed")} by {ctx.User}.", ctx.User.GetEffectiveAvatarUrl())
                .WithTitle(todo.ShortSummary.Truncate(EmbedBuilder.MaxTitleLength))
                .WithDescription(todo.LongDescription.Truncate(EmbedFieldBuilder.MaxFieldValueLength))
                .WithColor(completed ? Color.Green : Color.Red);

            var u = ctx.Guild.GetUser(todo.CreatorId);
            if (u != null)
                await e.SendToAsync(u);

            var c = ctx.Guild.GetTextChannel(ctx.GuildSettings.Extras.TodoNotificationChannelId);
            if (c != null)
                await e.SendToAsync(c);
        }
    }
}