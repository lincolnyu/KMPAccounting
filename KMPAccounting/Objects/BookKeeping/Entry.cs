using System;
using System.Collections.Generic;
using System.Text;

namespace KMPAccounting.Objects.BookKeeping
{
    public abstract class Entry : IEquatable<Entry>
    {
        protected Entry(DateTime dateTime)
        {
            DateTime = dateTime;
        }

        // When the transaction occurs
        public DateTime DateTime { get; }

        public string? Remarks { get; set; }

        public abstract bool Equals(Entry other);

        public abstract void Redo();
        public abstract void Undo();

        public abstract void Serialize(StringBuilder sb, bool indentedRemarks);
    }
}