using System.Collections.Generic;
using System.Text;

namespace KMPAccounting.Objects.Accounts
{
    public class AccountsState : AccountNode
    {
        public AccountsState(string name)
            : base(SideEnum.Debit, name)
        {
        }

        public static void Clear()
        {
            accountStates_.Clear();
        }

        public static AccountsState? GetAccountsState(string name)
        {
            return accountStates_.GetValueOrDefault(name);
        }

        public static void AddState(string name, AccountsState state)
        {
            accountStates_.Add(name, state);
        }

        public static bool RemoveState(string name)
        {
            var result = accountStates_.Remove(name, out var removed);
            removed.Dispose();
            return result;
        }

        private static readonly Dictionary<string, AccountsState> accountStates_ =
            new Dictionary<string, AccountsState>();

        public string ToString(int tabSize)
        {
            var sb = new StringBuilder();
            var debitNodes = new List<AccountNode>();
            var creditNodes = new List<AccountNode>();

            var debitBalance = 0m;
            var creditBalance = 0m;
            foreach (var (_, child) in Children)
            {
                if (child.Side == SideEnum.Debit)
                {
                    debitNodes.Add(child);
                    debitBalance += child.Balance;
                }
                else
                {
                    creditNodes.Add(child);
                    creditBalance += child.Balance;
                }
            }

            sb.Append($"Debit = {debitBalance}\n");
            foreach (var node in debitNodes)
            {
                sb.Append(node.ToString(node.Side, 1, tabSize));
            }

            sb.Append($"Credit = {creditBalance}\n");
            foreach (var node in creditNodes)
            {
                sb.Append(node.ToString(node.Side, 1, tabSize));
            }

            return sb.ToString();
        }
    }
}