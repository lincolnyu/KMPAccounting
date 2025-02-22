using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KMPAccounting.Objects.Fundamental;

namespace KMPAccounting.Objects.Accounts
{
    public class AccountNode(AccountNode.SideEnum side, string name) : WeakPointed<AccountNode>
    {
        public enum SideEnum
        {
            Debit,
            Credit
        }

        public static SideEnum GetOppositeSide(SideEnum side)
        {
            return side == SideEnum.Credit ? SideEnum.Debit : SideEnum.Credit;
        }

        public string ToString(SideEnum sheetRootSide, int indentDepth, int tabSize, bool showZeroBranches = false)
        {
            if (!showZeroBranches && IsZeroBranch())
            {
                return "";
            }

            var sb = new StringBuilder();
            sb.Append(' ', indentDepth * tabSize);
            var displayBalance = Side != sheetRootSide ? -Balance : Balance;
            var sideStr = Side == SideEnum.Debit ? "D" : "C";
            sb.Append($"{Name}({sideStr}) = {displayBalance}\n");
            if (Children.Count > 0)
            {
                foreach (var (_, child) in Children.OrderBy(x => x.Key))
                {
                    sb.Append(child.ToString(sheetRootSide, indentDepth + 1, tabSize));
                }
            }

            return sb.ToString();
        }

        private bool IsZeroBranch()
        {
            if (Balance != 0) return false;
            foreach (var c in Children.Values)
            {
                if (!c.IsZeroBranch()) return false;
            }

            return true;
        }

        public override void Dispose()
        {
            foreach (var c in Children.Values)
            {
                c.Dispose();
            }

            base.Dispose();
        }

        public SideEnum Side { get; } = side;

        public string Name { get; set; } = name;

        // Note: This uniquely identifies the account node systemwide.
        public string FullName => Parent != null ? Parent.FullName + "." + Name : Name;

        public AccountNode? Parent { get; set; }
        public Dictionary<string, AccountNode> Children { get; } = new Dictionary<string, AccountNode>();

        public decimal Balance
        {
            get
            {
                if (Children.Count > 0)
                {
                    if (_balanceInvalidated)
                    {
                        _balance = Children.Values.Sum(x => SameAccountSide(x) ? x.Balance : -x.Balance);
                        _balanceInvalidated = false;
                    }
                }

                return _balance;
            }
            set
            {
                if (Children.Count > 0)
                {
                    throw new InvalidOperationException("Setting balance to an aggregate account is not allowed");
                }

                if (_balance != value)
                {
                    _balance = value;
                    _balanceInvalidated = false;

                    InvalidateParentsBalances();
                }
            }
        }

        public AccountNode? MainNode
        {
            get
            {
                if (Children.TryGetValue(Constants.MainNodeName, out var mainNode))
                {
                    if (mainNode.Children.Count > 0)
                    {
                        throw new InvalidOperationException("Base node does not allow to have chldren.");
                    }

                    return mainNode;
                }

                return null;
            }
        }

        public bool SameAccountSide(AccountNode that) => Side == that.Side;

        public bool IsSameAccountAs(AccountNode that) => FullName == that.FullName && Side == that.Side;

        /// <summary>
        ///  Duplicate the tree from the current node to 'that'
        /// </summary>
        /// <param name="that">The target root to copy to</param>
        /// <param name="exactCopy">Remove the branches in the target if the source doesn't have them instead of just zeroing them.</param>
        /// <remarks>
        ///  Assuming that node has already the same side and name
        /// </remarks>
        public void CopyTo(AccountNode that, bool exactCopy)
        {
            CopyBasicFieldsTo(that);
            foreach (var (childName, child) in Children)
            {
                if (!that.Children.TryGetValue(childName, out var thatChild))
                {
                    thatChild = new AccountNode(child.Side, childName)
                    {
                        Parent = that,
                    };
                    that.Children.Add(childName, thatChild);
                }

                child.CopyTo(thatChild, exactCopy);
            }

            foreach (var (thatChildName, thatChild) in that.Children)
            {
                if (!Children.ContainsKey(thatChildName))
                {
                    if (exactCopy)
                    {
                        that.Children.Remove(thatChildName);
                        thatChild.Dispose();
                    }
                    else
                    {
                        thatChild.ZeroOutBalanceOfTree();
                    }
                }
            }
        }

        /// <summary>
        ///  Force the balance of every node on the tree starting from this account to zero.
        /// </summary>
        /// <remarks>
        ///  This is not a balanced accounting operation.
        /// </remarks>
        public void ZeroOutBalanceOfTree()
        {
            var needToInvalidateParentsBalances = _balance != 0;

            ZeroOutBalanceOfTreeWithoutInvalidatingParents();

            if (needToInvalidateParentsBalances)
            {
                InvalidateParentsBalances();
            }
        }

        private void InvalidateParentsBalances()
        {
            for (var p = Parent; p != null; p = p.Parent)
            {
                p._balanceInvalidated = true;
            }
        }

        private void ZeroOutBalanceOfTreeWithoutInvalidatingParents()
        {
            _balance = 0;
            _balanceInvalidated = false;
            foreach (var child in Children.Values)
            {
                child.ZeroOutBalanceOfTreeWithoutInvalidatingParents();
            }
        }

        private void CopyBasicFieldsTo(AccountNode that)
        {
            that._balance = _balance;
            that._balanceInvalidated = _balanceInvalidated;
        }

        public void ReckonInstantly()
        {
            MainNode!._balance = Balance;

            foreach (var child in Children.Values.Where(x => x != MainNode))
            {
                child.ZeroOutBalanceOfTreeWithoutInvalidatingParents();
            }
        }

        private decimal _balance = 0;
        private bool _balanceInvalidated = false;
    }
}