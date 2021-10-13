using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Gommon;
using Humanizer;
using Volte.Entities;
using Volte.Helpers;

namespace Volte.Services
{
    public class DelayedRoleService : IVolteService
    {
        private static readonly TimeSpan pollRate = 30.Seconds();
        private static Timer _checker;
        private readonly DatabaseService _db;
        private readonly DiscordShardedClient _client;

        public DelayedRoleService(DatabaseService databaseService,
            DiscordShardedClient client)
        {
            _client = client;
            _db = databaseService;
        }

        /// <summary>
        ///     Sets the value of private static field <see cref="_checker"/>.
        ///     If its value is already set; this method returns immediately.
        /// </summary>
        public void Initialize()
        {
            _checker ??= new Timer(_ => Executor.Execute(async () =>
                {
                    Logger.Debug(LogSource.Service, "Checking all delayed roles.");
                    foreach (var g in _client.Guilds)
                    {
                        var drole = _db.GetData(g).Extras.DelayedRole;
                        if (drole != null)
                            foreach (var u in g.Users.Where(u => DateTimeOffset.Now - u.JoinedAt >= drole.DelayAfterJoining))
                                if (!u.HasRole(drole.RoleIdToAdd))
                                    await ApplyAsync(u, drole);
                            
                    }
                }),
                null,
                5.Seconds(),
                pollRate
            );
        }

        private async Task ApplyAsync(SocketGuildUser user, DelayedRole role)
        {
            await user.ModifyAsync(p =>
                p.Roles = user.Roles.ToList().Apply(x =>
                {
                    x.Remove(user.Guild.GetRole(role.RoleIdToRemove));
                    x.Remove(user.Guild.EveryoneRole);
                    x.Add(user.Guild.GetRole(role.RoleIdToAdd));
                })
            );
        }
    }
}