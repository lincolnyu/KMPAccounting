using KMPAccounting.Objects.Fundamental;
using System;

namespace KMPAccounting.Objects.Accounts
{
    public class AccountNodeReference(string fullName) : IEquatable<AccountNodeReference>
    {
        public bool Equals(AccountNodeReference? other)
        {
            return FullName.Equals(other.FullName);
        }

        public override string ToString()
        {
            return FullName;
        }

        // Including the states name as the root.
        public string FullName { get; } = fullName;

        public AccountNode? Get()
        {
            if (_nodeCache != null && _nodeCache.TryGetTarget(out var node))
            {
                return node;
            }

            var account = AccountHelper.GetAccount(FullName);
            if (account != null)
            {
                _nodeCache = new WeakPointer<AccountNode>(account);
            }

            return account;
        }

        private WeakPointer<AccountNode>? _nodeCache = null;
    }
}