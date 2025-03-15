using KMPAccounting.Objects.Accounts;
using KMPAccounting.Objects.Serialization;
using KMPCommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace KMPAccounting.Objects.BookKeeping
{
    public class CompositeTransaction(DateTime dateTime) : Entry(dateTime)
    {
        public override bool Equals(Entry? other)
        {
            if (other is CompositeTransaction otherPt)
            {
                if (!CsvUtility.TimestampsAreEqual(DateTime, other.DateTime))
                {
                    return false;
                }

                if (!Equals(Remarks, otherPt.Remarks))
                {
                    return false;
                }

                if (Debited.Count != otherPt.Debited.Count)
                {
                    return false;
                }

                if (Credited.Count != otherPt.Credited.Count)
                {
                    return false;
                }

                for (var i = 0; i < Debited.Count; ++i)
                {
                    if (!Debited[i].Equals(otherPt.Debited[i]))
                    {
                        return false;
                    }
                }

                for (var i = 0; i < Credited.Count; ++i)
                {
                    if (!Credited[i].Equals(otherPt.Credited[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        // The accounts being debited
        public List<(AccountNodeReference, decimal)> Debited { get; } = [];

        // The accounts being credited
        public List<(AccountNodeReference, decimal)> Credited { get; } = [];

        public override void Redo()
        {
            foreach (var (debited, amount) in Debited)
            {
                var node = debited.Get()!;
                if (node.Side == AccountNode.SideEnum.Debit)
                {
                    node.Balance += amount;
                }
                else
                {
                    node.Balance -= amount;
                }
            }

            foreach (var (credited, amount) in Credited)
            {
                var node = credited.Get()!;
                if (node.Side == AccountNode.SideEnum.Credit)
                {
                    node.Balance += amount;
                }
                else
                {
                    node.Balance -= amount;
                }
            }
        }

        public override void Undo()
        {
            foreach (var (debited, amount) in Debited)
            {
                var node = debited.Get()!;
                if (node.Side == AccountNode.SideEnum.Debit)
                {
                    node.Balance -= amount;
                }
                else
                {
                    node.Balance += amount;
                }
            }

            foreach (var (credited, amount) in Credited)
            {
                var node = credited.Get()!;
                if (node.Side == AccountNode.SideEnum.Credit)
                {
                    node.Balance -= amount;
                }
                else
                {
                    node.Balance += amount;
                }
            }
        }

        public static CompositeTransaction ParseLine(DateTime dateTime, string line)
        {
            var pt = new CompositeTransaction(dateTime);

            int p = 0;
            int newp;
            {
                line.GetNextWord('|', p, out newp, out string? debitedCountStr);
                p = newp + 1;
                if (debitedCountStr != null)
                {
                    var debitedCount = int.Parse(debitedCountStr);
                    for (var i = 0; i < debitedCount; ++i)
                    {
                        line.GetNextWord('|', p, out newp, out var debited);
                        p = newp + 1;
                        line.GetNextWord('|', p, out newp, out var amountStr);
                        p = newp + 1;
                        var amount = amountStr != null ? decimal.Parse(amountStr) : 0;
                        pt.Debited.Add((new AccountNodeReference(debited!), amount));
                    }
                }
            }

            {
                line.GetNextWord('|', p, out newp, out string? creditedCountStr);
                p = newp + 1;
                if (creditedCountStr != null)
                {
                    var creditedCount = int.Parse(creditedCountStr);
                    for (var i = 0; i < creditedCount; ++i)
                    {
                        line.GetNextWord('|', p, out newp, out var credited);
                        p = newp + 1;
                        line.GetNextWord('|', p, out newp, out var amountStr);
                        p = newp + 1;
                        var amount = amountStr != null ? decimal.Parse(amountStr) : 0;
                        pt.Credited.Add((new AccountNodeReference(credited!), amount));
                    }
                }
            }

            if (line.GetNextWord('|', p, out _, out string? remarks))
            {
                pt.Remarks = SerializationHelper.DeserializeRemarks(remarks!);
            }

            return pt;
        }

        public override void Serialize(StringBuilder sb, bool indentedRemarks)
        {
            sb.Append(CsvUtility.TimestampToString(DateTime));
            sb.Append('|');
            sb.Append("CompositeTransaction|");

            sb.Append($"{Debited.Count}|");
            foreach (var (acc, amount) in Debited)
            {
                sb.Append(acc.FullName);
                sb.Append('|');
                sb.Append(amount);
                sb.Append('|');
            }

            sb.Append($"{Credited.Count}|");
            foreach (var (acc, amount) in Credited)
            {
                sb.Append(acc.FullName);
                sb.Append('|');
                sb.Append(amount);
                sb.Append('|');
            }

            if (Remarks is not null)
            {
                if (indentedRemarks)
                {
                    sb.Append('\n');
                    SerializationHelper.SerializeIndentedRemarks(sb, Remarks, 1);
                    sb.Append('\n');
                }
                else
                {
                    sb.Append($"{SerializationHelper.SerializeRemarks(Remarks)}");
                    sb.Append('|');
                }
            }
            else
            {
                sb.Append('\n');
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(DateTime.ToShortDateOnlyString());
            sb.Append('\n');
            sb.Append("Debit\n");

            foreach (var (acc, amount) in Debited)
            {
                sb.Append($"  {amount} to {acc.FullName}\n");
            }

            sb.Append("Credit\n");

            foreach (var (acc, amount) in Credited)
            {
                sb.Append($"  {amount} to {acc.FullName}\n");
            }

            if (Remarks != null)
            {
                sb.Append($"Remarks: {Remarks}\n");
            }

            return sb.ToString();
        }
    }
}