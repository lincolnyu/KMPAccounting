using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KMPAccounting.Objects.Serialization;

namespace KMPAccounting.Objects.BookKeeping
{
    public class Ledger : IEquatable<Ledger>
    {
        /// <summary>
        ///  All systemwide accounting entries sorted in chronicle order
        /// </summary>
        public List<Entry> Entries { get; } = new List<Entry>();

        public void SerializeToStream(StreamWriter sw, bool indentedRemarks)
        {
            var sb = new StringBuilder();
            foreach (var entry in Entries)
            {
                entry.Serialize(sb, indentedRemarks);
            }
            sw.Write(sb);
        }

        public void DeserializeFromStream(StreamReader sr, bool indentedRemarks, bool append = false)
        {
            if (!append)
            {
                Entries.Clear();
            }

            using var lineLoader = new LineLoader(sr);

            while (lineLoader.PeekLine() != null)
            {
                var entry = EntryDeserializationFactory.DeserializeFromLine(lineLoader, indentedRemarks);
                Entries.Add(entry);
            }
        }

        public bool Equals(Ledger other)
        {
            if (Entries.Count != other.Entries.Count)
            {
                return false;
            }

            for (var i = 0; i < Entries.Count; ++i)
            {
                if (!Entries[i].Equals(other.Entries[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}