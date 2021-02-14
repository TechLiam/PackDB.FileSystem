using System.Diagnostics.CodeAnalysis;

namespace PackDB.Core.Tests
{
    [ExcludeFromCodeCoverage]
    public class IndexedEntity : BasicEntity
    {
        [Index] public string IndexedValue { get; set; }
    }
}