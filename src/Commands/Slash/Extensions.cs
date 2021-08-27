using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Gommon;
using Volte.Core;
using Volte.Core.Helpers;

namespace Volte.Commands.Slash
{
    public static class Extensions
    {
        public static SelectMenuBuilder AddOptions(this SelectMenuBuilder menu,
            IEnumerable<SelectMenuOptionBuilder> options)
        {
            options.ForEach(opt => menu.AddOption(opt));
            return menu;
        }

        public static ActionRowBuilder AsActionRow(this IEnumerable<IMessageComponent> components)
            => new ActionRowBuilder().AddComponents(components);
        
        public static ActionRowBuilder AddComponents(this ActionRowBuilder builder,
            IEnumerable<IMessageComponent> components)
            => builder.Apply(x => components.ForEach(c => x.AddComponent(c)));

        public static ComponentBuilder AddActionRow(this ComponentBuilder builder,
            Action<ActionRowBuilder> initializer)
            => builder.AddActionRows(new ActionRowBuilder().Apply(initializer));
        
        public static ComponentBuilder AddActionRows(this ComponentBuilder builder,
            IEnumerable<ActionRowBuilder> actionRows) => builder.AddActionRows(actionRows.ToArray());
        
        public static ComponentBuilder AddActionRows(this ComponentBuilder builder,
            params ActionRowBuilder[] actionRows)
        {
            builder.ActionRows ??= new List<ActionRowBuilder>();
            builder.ActionRows.AddRange(actionRows);
            return builder;
        }

        public static EmbedBuilder CreateEmbedBuilder(this SocketSlashCommand command, string content = null) 
            => new EmbedBuilder()
                .WithColor(command.User.Cast<SocketGuildUser>()?.GetHighestRole()?.Color ?? Config.SuccessColor)
                .WithDescription(content ?? string.Empty);
        
        public static SocketSlashCommandDataOption GetOption(this SocketSlashCommand command, string name) =>
            command.Data.Options?.FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));

        public static T GetValueOr<T>(this SocketSlashCommandDataOption option, object @default) =>
            (option?.Value ?? @default).Cast<T>();
    }
}