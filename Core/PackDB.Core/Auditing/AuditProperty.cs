using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace PackDB.Core.Auditing
{
    [MessagePackObject]
    public class AuditProperty
    {
        [ExcludeFromCodeCoverage]
        public AuditProperty()
        {
        }

        public AuditProperty(string propertyName, object oldValue, object newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        [Key(1)] public string PropertyName { get; set; }

        [Key(2)] public object OldValue { get; set; }

        [Key(3)] public object NewValue { get; set; }
    }
}