using System.Diagnostics.CodeAnalysis;
using PackDB.Core.Data;

namespace PackDB.Core.Tests
{
    [ExcludeFromCodeCoverage]
    public class BasicEntity : DataEntity
    {
        public string Value1 { get; set; }
    }
}