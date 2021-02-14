using System.Diagnostics.CodeAnalysis;
using PackDB.Core;

namespace PackDB.FileSystem.Tests
{
    [ExcludeFromCodeCoverage]
    public class IndexableData : BasicData
    {
        [Index] public string IndexValue1 { get; set; }
    }
}