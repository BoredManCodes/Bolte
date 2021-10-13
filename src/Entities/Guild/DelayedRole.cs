using System;
using System.Text.Json.Serialization;
using Gommon;
using LiteDB;
using Volte.Commands;
using Volte.Interactions;

namespace Volte.Entities
{
    public sealed class DelayedRole
    {
        public static DelayedRole CreateFrom(TimeSpan delayAfterJoining, ulong roleToAdd, ulong roleToRemove) => new DelayedRole
        {
            DelayAfterJoining = delayAfterJoining,
            RoleIdToAdd = roleToAdd,
            RoleIdToRemove = roleToRemove
        };
        
        [BsonId, JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("delay_after_joining")]
        public TimeSpan DelayAfterJoining { get; set; }
        [JsonPropertyName("role_to_add")]
        public ulong RoleIdToAdd { get; set; }
        [JsonPropertyName("role_to_remove")]
        public ulong RoleIdToRemove { get; set; }

        public override string ToString() => this.AsJson();
    }
}