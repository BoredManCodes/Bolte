using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gommon;
using Volte;

namespace Volte.Entities
{
    public sealed class GuildExtras
    {
        internal GuildExtras()
        {
            SelfRoleIds = new HashSet<ulong>(25);
            Suggestions = new HashSet<Suggestion>(25);
            DelayedRole = null;
            Tags = new HashSet<Tag>();
            Warns = new HashSet<Warn>();
        }
        
        [JsonPropertyName("mod_log_case_number")]
        public ulong ModActionCaseNumber { get; set; }
        
        [JsonPropertyName("auto_parse_quote_urls")]
        public bool AutoParseQuoteUrls { get; set; }
        
        [JsonPropertyName("delayed_role")]
        public DelayedRole DelayedRole { get; set; }

        [JsonPropertyName("self_roles")]
        public HashSet<ulong> SelfRoleIds { get; set; }
        
        [JsonPropertyName("suggestion_channel")]
        public ulong SuggestionNotificationChannelId { get; set; }
        
        [JsonPropertyName("suggestions")]
        public HashSet<Suggestion> Suggestions { get; set; }
        
        [JsonPropertyName("todo_channel")]
        public ulong TodoNotificationChannelId { get; set; }
        
        [JsonPropertyName("todos")]
        public HashSet<Todo> Todos { get; set; }

        [JsonPropertyName("tags")]
        public HashSet<Tag> Tags { get; set; }

        [JsonPropertyName("warns")]
        public HashSet<Warn> Warns { get; set; }

        public void AddTag(Action<Tag> initializer) => Tags.Add(new Tag().Apply(initializer));
        public void AddWarn(Action<Warn> initializer) => Warns.Add(new Warn().Apply(initializer));
        public void AddSuggestion(Action<Suggestion> initializer) 
            => Suggestions.Add(new Suggestion().Apply(initializer));
        
        public override string ToString() => this.AsJson();
    }
}