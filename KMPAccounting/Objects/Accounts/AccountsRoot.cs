using System.Collections.Generic;
using System.Text;

namespace KMPAccounting.Objects.Accounts
{
    public class AccountsRoot(string name) : AccountNode(SideEnum.Debit, name)
    {
        public static void Clear()
        {
            AccountRoots.Clear();
        }

        public static AccountsRoot? GetAccountsRoot(string name)
        {
            return AccountRoots.GetValueOrDefault(name);
        }

        public static void AddRoot(string name, AccountsRoot root)
        {
            AccountRoots.Add(name, root);
        }

        public static bool RemoveRoot(string name)
        {
            var result = AccountRoots.Remove(name, out var removed);
            removed?.Dispose();
            return result;
        }

        private static readonly Dictionary<string, AccountsRoot> AccountRoots = [];

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