using System;
using System.Collections.Generic;
using System.IO;

namespace KMPAccounting.Objects.BookKeeping
{
    public class Ledger : IEquatable<Ledger>
    {
        /// <summary>
        ///  All systemwide accounting entries sorted in chronicle order
        /// </summary>
        public List<Entry> Entries { get; } = new List<Entry>();

        public void SerializeToStream(StreamWriter sw)
        {
            foreach (var entry in Entries)
            {
                sw.WriteLine(entry.SerializeToLine());
            }
        }

        public void DeserializeFromStream(StreamReader sr, bool append = false)
        {
            if (!append)
            {
                Entries.Clear();
            }

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                var entry = EntryFactory.DeserializeFromLine(line);
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