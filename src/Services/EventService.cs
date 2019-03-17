using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Qmmands;
using Volte.Commands;
using Volte.Data;
using Volte.Data.Objects;
using Volte.Extensions;

namespace Volte.Services
{
    [Service("Event", "The main Service for handling some Discord gateway events.")]
    public sealed class EventService
    {
        private readonly LoggingService _logger;

        public EventService(LoggingService loggingService)
        {
            _logger = loggingService;
        }

        public async Task OnReady(ReadyEventArgs ev)
        {
            foreach (var guild in ev.Client.Guilds.Values)
            {
                if (!Config.BlacklistedOwners.Contains(guild.Id)) continue;
                await _logger.Log(LogLevel.Warning, LogSource.Volte,
                    $"Left guild \"{guild.Name}\" owned by blacklisted owner {guild.Owner.ToHumanReadable()}.");
                await guild.LeaveAsync();
            }
        }

        public async Task OnCommandAsync(Command c, IResult res, ICommandContext context, Stopwatch sw)
        {
            var ctx = (VolteContext) context;
            var commandName = ctx.Message.Content.Split(" ")[0];
            var args = ctx.Message.Content.Replace($"{commandName}", "");
            if (string.IsNullOrEmpty(args)) args = "None";
            if (res is FailedResult failedRes)
            {
                await OnCommandFailureAsync(c, failedRes, ctx, args, sw);
                return;
            }

            if (Config.LogAllCommands)
            {
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|  -Command from user: {ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|     -Command Issued: {c.Name}");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|        -Args Passed: {args.Trim()}");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|           -In Guild: {ctx.Guild.Name} ({ctx.Guild.Id})");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|         -In Channel: #{ctx.Channel.Name} ({ctx.Channel.Id})");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|        -Time Issued: {DateTime.Now}");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|           -Executed: {res.IsSuccessful} ");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    $"|              -After: {sw.Elapsed.Humanize()}");
                await _logger.Log(LogLevel.Info, LogSource.Module,
                    "-------------------------------------------------");
            }
        }

        private async Task OnCommandFailureAsync(Command c, FailedResult res, VolteContext ctx, string args,
            Stopwatch sw)
        {
            var embed = new DiscordEmbedBuilder();
            string reason;
            switch (res)
            {
                case CommandNotFoundResult _:
                    reason = "Unknown command.";
                    break;
                case ExecutionFailedResult efr:
                    reason = $"Execution of this command failed.\nFull error message: {efr.Exception.Message}";
                    await _logger.Log(LogLevel.Error, LogSource.Module, string.Empty, efr.Exception);
                    break;
                case ChecksFailedResult _:
                    reason = "Insufficient permission.";
                    break;
                case ParameterChecksFailedResult pcfr:
                    reason = $"Checks failed on parameter *{pcfr.Parameter.Name}**.";
                    break;
                case ArgumentParseFailedResult apfr:
                    reason = $"Parsing for arguments failed on argument **{apfr.Parameter?.Name}**.";
                    break;
                case TypeParseFailedResult tpfr:
                    reason =
                        $"Failed to parse type **{tpfr.Parameter.Type}** from parameter **{tpfr.Parameter.Name}**.";
                    break;
                default:
                    reason = "Unknown error.";
                    break;
            }

            if (reason != "Insufficient permission." && reason != "Unknown command.")
            {
                await embed.AddField("Error in Command:", c.Name)
                    .AddField("Error Reason:", reason)
                    .AddField("Correct Usage", c.SanitizeRemarks(ctx))
                    .WithAuthor(ctx.User.ToHumanReadable(), null, ctx.User.AvatarUrl)
                    .WithErrorColor()
                    .SendToAsync(ctx.Channel);

                if (!Config.LogAllCommands) return;

                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|  -Command from user: {ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|     -Command Issued: {c.Name}");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|        -Args Passed: {args.Trim()}");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|           -In Guild: {ctx.Guild.Name} ({ctx.Guild.Id})");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|         -In Channel: #{ctx.Channel.Name} ({ctx.Channel.Id})");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|        -Time Issued: {DateTime.Now}");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|           -Executed: {res.IsSuccessful} | Reason: {reason}");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    $"|              -After: {sw.Elapsed.Humanize()}");
                await _logger.Log(LogLevel.Error, LogSource.Module,
                    "-------------------------------------------------");
            }
        }
    }
}