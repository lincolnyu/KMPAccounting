using KMPAccounting.Objects.Accounts;
using KMPCommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace KMPAccounting.Objects.BookKeeping
{
    public class CompositeTransaction : Entry
    {
        public CompositeTransaction(DateTime dateTime)
            : base(dateTime)
        {
        }

        public override bool Equals(Entry other)
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
                pt.Remarks = remarks;
            }

            return pt;
        }

        public override string SerializeToLine()
        {
            var sb = new StringBuilder();

            sb.Append(CsvUtility.TimestampToString(DateTime));
            sb.Append("|");
            sb.Append("CompositeTransaction|");

            sb.Append($"{Debited.Count}|");
            foreach (var (acc, amount) in Debited)
            {
                sb.Append(acc.FullName);
                sb.Append("|");
                sb.Append(amount);
                sb.Append("|");
            }

            sb.Append($"{Credited.Count}|");
            foreach (var (acc, amount) in Credited)
            {
                sb.Append(acc.FullName);
                sb.Append("|");
                sb.Append(amount);
                sb.Append("|");
            }

            if (Remarks != null)
            {
                sb.Append($"{SerializationHelper.SerializeRemarks(Remarks)}");
                sb.Append("|");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(DateTime.ToShortDateOnlyString());
            sb.AppendLine("Debit");

            foreach (var (acc, amount) in Debited)
            {
                sb.AppendLine($"  {amount} to {acc.FullName}");
            }

            sb.AppendLine("Credit");

            foreach (var (acc, amount) in Credited)
            {
                sb.AppendLine($"  {amount} to {acc.FullName}");
            }

            if (Remarks != null)
            {
                sb.AppendLine($"Remarks: {Remarks}");
            }

            return sb.ToString();
        }

        // The accounts being debited
        public List<(AccountNodeReference, decimal)> Debited { get; } = new List<(AccountNodeReference, decimal)>();

        // The accounts being credited
        public List<(AccountNodeReference, decimal)> Credited { get; } = new List<(AccountNodeReference, decimal)>();

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
    }
}