using System.Diagnostics.CodeAnalysis;
using PackDB.Core;

namespace PackDB.FileSystem.Tests
{
    [ExcludeFromCodeCoverage]
    public class UniqueIndexableData : BasicData
    {
        [Index(IsUnique = true)] public string IndexValue1 { get; set; }
    }
}