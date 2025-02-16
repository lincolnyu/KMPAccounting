using KMPAccounting.Objects.AccountCreation;
using KMPAccounting.Objects.Accounts;
using KMPCommon;
using System;
using System.Text;

namespace KMPAccounting.Objects.BookKeeping
{
    public class OpenAccount : Entry
    {
        public OpenAccount(DateTime dateTime, (AccountNodeReference, AccountNode.SideEnum)? parentAndSide, string name)
            : base(dateTime)
        {
            ParentAndSide = parentAndSide;
            Name = name;
        }

        public override bool Equals(Entry other)
        {
            if (other is OpenAccount otherOa)
            {
                if (!CsvUtility.TimestampsAreEqual(DateTime, other.DateTime))
                {
                    return false;
                }

                if (!Name.Equals(otherOa.Name))
                {
                    return false;
                }

                if (!ParentAndSide.Equals(otherOa.ParentAndSide))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public static OpenAccount ParseLine(DateTime dateTime, string line)
        {
            int p = 0;
            int newp;
            line.GetNextWord('|', p, out newp, out string? name);
            p = newp + 1;

            (AccountNodeReference, AccountNode.SideEnum)? parentAndSide = null;
            if (line.GetNextWord('|', p, out newp, out string? parentName))
            {
                p = newp + 1;
                line.GetNextWord('|', p, out newp, out string? sideName);
                var side = sideName == "C" ? AccountNode.SideEnum.Credit : AccountNode.SideEnum.Debit;
                parentAndSide = (new AccountNodeReference(parentName!), side);
            }

            return new OpenAccount(dateTime, parentAndSide, name!);
        }

        public override string SerializeToLine()
        {
            var sb = new StringBuilder();

            sb.Append(CsvUtility.TimestampToString(DateTime));
            sb.Append("|");
            sb.Append("OpenAccount|");

            sb.Append(Name);
            sb.Append('|');
            if (ParentAndSide.HasValue)
            {
                sb.Append(ParentAndSide.Value.Item1.FullName);
                sb.Append('|');
                sb.Append(ParentAndSide.Value.Item2 == AccountNode.SideEnum.Credit ? "C" : "D");
                sb.Append('|');
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(DateTime.ToShortDateOnlyString());
            var type = ParentAndSide == null
                ? ""
                : (ParentAndSide.Value.Item2 == AccountNode.SideEnum.Credit ? "Credit " : "Debit ");
            var fullName = AccountPath.Join(ParentAndSide?.Item1.FullName ?? string.Empty, Name);
            sb.AppendLine($"Open {type}Account {fullName}");

            return sb.ToString();
        }

        public (AccountNodeReference, AccountNode.SideEnum)? ParentAndSide { get; }
        public string Name { get; }

        public override void Redo()
        {
            if (ParentAndSide != null)
            {
                var (parent, side) = ParentAndSide.Value;
                var parentNode = parent.Get()!;
                parentNode.Children.Add(Name, new AccountNode(side, Name) { Parent = parentNode });
            }
            else
            {
                // Create the state.
                var state = new AccountsState(Name);
                AccountsState.AddState(Name, state);
            }
        }

        public override void Undo()
        {
            if (ParentAndSide != null)
            {
                var (parent, _) = ParentAndSide.Value;
                var parentNode = parent.Get()!;
                if (parentNode.Children.Remove(Name, out var child))
                {
                    child.Dispose();
                }
            }
            else
            {
                // Remove the state.
                AccountsState.RemoveState(Name);
            }
        }
    }
}