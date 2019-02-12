using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Volte.Core.Commands;
using Volte.Core.Discord;
using Volte.Core.Data;
using Volte.Core.Data.Objects;
using Volte.Core.Helpers;

#pragma warning disable 1998
namespace Volte.Core.Services {
    internal class EventService {
        private readonly LoggingService _logger = VolteBot.ServiceProvider.GetRequiredService<LoggingService>();

        public async Task OnReady() {
            var dbl = VolteBot.Client.GetGuild(264445053596991498);
            if (dbl is null || Config.GetOwner() == 168548441939509248) return;
            await dbl.GetTextChannel(265156286406983680).SendMessageAsync(
                $"<@168548441939509248>: I am a Volte not owned by you. Please do not post Volte to a bot list again, <@{Config.GetOwner()}>.");
            await dbl.LeaveAsync();
        }

        public async Task OnCommand(Optional<CommandInfo> cinfo, ICommandContext context, IResult res) {
            var ctx = (VolteContext)context;
            if (!cinfo.IsSpecified) return;
            var config = VolteBot.ServiceProvider.GetRequiredService<DatabaseService>().GetConfig(ctx.Guild);
            var commandName = ctx.Message.Content.Split(" ")[0];
            var args = ctx.Message.Content.Replace($"{commandName}", "");
            if (string.IsNullOrEmpty(args)) {
                args = "None";
            }
            
            var argPos = 0;
            var embed = new EmbedBuilder();
            if (!res.IsSuccess && res.ErrorReason != "Unknown command." && res.ErrorReason != "Insufficient permission.") {
                string reason;
                switch (res.ErrorReason) {
                    case "The server responded with error 403: Forbidden":
                        reason =
                            "I'm not allowed to do that. " +
                            "Either I don't have permission, " +
                            "or the requested user is higher " +
                            "than me in the role hierarchy.";
                        break;
                    case "Failed to parse Boolean.":
                        reason = "You can only input `true` or `false` for this command.";
                        break;
                    default:
                        reason = res.ErrorReason;
                        break;
                }
                
                var aliases = cinfo.Value.Aliases.Aggregate("(", (current, alias) => current + alias + "|");

                aliases += ")";
                aliases = aliases.Replace("|)", ")");
                
                if (ctx.Message.HasMentionPrefix(VolteBot.Client.CurrentUser, ref argPos)) {
                    embed.AddField("Error in Command:", cinfo.Value.Name);
                    embed.AddField("Error Reason:", reason);
                    embed.AddField("Correct Usage", cinfo.Value.Remarks
                        .Replace("Usage: ", string.Empty)
                        .Replace("|prefix|", config.CommandPrefix)
                        .Replace($"{cinfo.Value.Name.ToLower()}", aliases));
                    embed.WithAuthor(ctx.User);
                    embed.WithColor(Config.GetErrorColor());
                    await Utils.Send(ctx.Channel, embed.Build());
                }
                else {
                    embed.AddField("Error in Command:", cinfo.Value.Name);
                    embed.AddField("Error Reason:", reason);
                    embed.AddField("Correct Usage", cinfo.Value.Remarks
                        .Replace("Usage: ", string.Empty)
                        .Replace("|prefix|", config.CommandPrefix)
                        .Replace($"{cinfo.Value.Name.ToLower()}", aliases));
                    embed.WithAuthor(ctx.User);
                    embed.WithColor(Config.GetErrorColor());
                    await Utils.Send(ctx.Channel, embed.Build());
                }
            }
            

            if (Config.GetLogAllCommands()) {
                if (res.IsSuccess) {
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        $"|  -Command from user: {ctx.User.Username}#{ctx.User.Discriminator}");
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        $"|     -Command Issued: {cinfo.Value.Name}");
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        $"|        -Args Passed: {args.Trim()}");
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        $"|           -In Guild: {ctx.Guild.Name}");
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        $"|         -In Channel: #{ctx.Channel.Name}");
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        $"|        -Time Issued: {DateTime.Now}");
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        $"|           -Executed: {res.IsSuccess} ");
                    await _logger.Log(LogSeverity.Info, LogSource.Module,
                        "-------------------------------------------------");
                    



                }
                else {
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        $"|  -Command from user: {ctx.User.Username}#{ctx.User.Discriminator}");
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        $"|     -Command Issued: {cinfo.Value.Name}");
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        $"|        -Args Passed: {args.Trim()}");
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        $"|           -In Guild: {ctx.Guild.Name}");
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        $"|         -In Channel: #{ctx.Channel.Name}");
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        $"|        -Time Issued: {DateTime.Now}");
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        $"|           -Executed: {res.IsSuccess} | Reason: {res.ErrorReason}");
                    await _logger.Log(LogSeverity.Error, LogSource.Module,
                        "-------------------------------------------------");
                }
            }
        }
    }
}