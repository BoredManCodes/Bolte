using System;

namespace Volte.Entities
{
    public sealed class Suggestion
    {
        public Guid Uuid { get; set; }
        public string ShortSummary { get; set; }
        public string LongDescription { get; set; }
        public ulong CreatorId { get; set; }
    }
}