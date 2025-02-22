using System;
using System.Text;

namespace KMPAccounting.Objects.BookKeeping
{
    public abstract class Entry(DateTime dateTime) : IEquatable<Entry>
    {
        // When the transaction occurs
        public DateTime DateTime { get; } = dateTime;

        public string? Remarks { get; set; }

        public abstract bool Equals(Entry? other);

        public abstract void Redo();
        public abstract void Undo();

        public abstract void Serialize(StringBuilder sb, bool indentedRemarks);
    }
}