﻿using KMPAccounting.Objects.Accounts;
using KMPAccounting.Objects.BookKeeping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static KMPAccounting.Objects.Accounts.AccountNode;

namespace KMPAccounting.Objects
{
    public static class AccountHelper
    {
        public enum SideOptionEnum
        {
            SameAsParent,
            OnlyLeafOppositeToParent,
            AllCredit,
            AllDebit
        }

        public static AccountNode? GetStateNode(string accountFullName)
        {
            var split = accountFullName.Split('.', 2);
            Debug.Assert(split.Length <= 2);
            return AccountsRoot.GetAccountsRoot(split[0]);
        }

        public static AccountNode? GetAccount(string fullName)
        {
            var split = fullName.Split('.', 2);
            Debug.Assert(split.Length <= 2);
            var state = AccountsRoot.GetAccountsRoot(split[0]);
            if (state == null) return null;
            if (split.Length == 1)
            {
                return state;
            }

            return state.GetAccount(split[1]);
        }

        public static AccountNode? GetAccount(this AccountsRoot state, string fullName)
        {
            var splitNames = fullName.Split('.');
            AccountNode p = state;
            foreach (var splitName in splitNames)
            {
                if (!p.Children.TryGetValue(splitName, out var account))
                {
                    return null;
                }

                p = account;
            }

            return p;
        }

        /// <summary>
        ///  Zero out the leaves of the specified node into its main node. The node's balance remains unchanged.
        /// </summary>
        /// <param name="node">The node to clear the leaves of</param>
        /// <param name="toDebit">Leaf accounts to debit.</param>
        /// <param name="toCredit">Leaf accounts to credit.</param>
        public static void ReckonAccountByTransactions(this AccountNode node, out List<(string, decimal)> toDebit,
            out List<(string, decimal)> toCredit)
            => ReckonAccountsIntoTarget(node.GetAllLeafNodesWithNonZeroBalance().Where(x => x != node.MainNode),
                node.MainNode!.FullName, out toDebit, out toCredit);

        public static void ReckonAccountsIntoTarget(IEnumerable<AccountNode> sources, string target,
            out List<(string, decimal)> toDebit, out List<(string, decimal)> toCredit)
        {
            toDebit = [];
            toCredit = [];
            var netDebited = 0m;
            foreach (var leaf in sources)
            {
                var leafPositiveBalance = leaf.Balance > 0;
                var leafDebitSide = leaf.Side == SideEnum.Debit;
                var amount = Math.Abs(leaf.Balance);
                if (leafPositiveBalance ^ leafDebitSide)
                {
                    // Debit leaf and credit base
                    toDebit.Add((leaf.FullName, amount));
                    netDebited += amount;
                }
                else
                {
                    // Credit leaf and debit base
                    toCredit.Add((leaf.FullName, amount));
                    netDebited -= amount;
                }
            }

            // There's a chance these accounts cancel themselves out
            if (netDebited > 0)
            {
                toCredit.Add((target, netDebited));
            }
            else if (netDebited < 0)
            {
                toDebit.Add((target, -netDebited));
            }
        }

        public static bool CancelOut(AccountNode a, AccountNode b, out string? toDebit, out string? toCredit,
            out decimal? amount)
        {
            toDebit = null;
            toCredit = null;
            amount = null;

            var balanceA = a.Balance;
            var balanceB = b.Balance;

            if (balanceA == 0 || balanceB == 0) return false; // No need to do it

            var debitA = (balanceA > 0) ^ (a.Side == SideEnum.Debit);
            var debitB = (balanceB > 0) ^ (b.Side == SideEnum.Debit);

            if (debitA == debitB) return false;

            if (debitA)
            {
                toDebit = a.FullName;
                toCredit = b.FullName;
            }
            else
            {
                toDebit = b.FullName;
                toCredit = a.FullName;
            }

            amount = Math.Min(Math.Abs(balanceA), Math.Abs(balanceB));
            return true;
        }

        public static IEnumerable<AccountNode> GetAllLeafNodesWithNonZeroBalance(this AccountNode node)
            => node.GetAllLeafNodes().Where(x => x.Balance != 0);

        public static IEnumerable<AccountNode> GetAllLeafNodes(this AccountNode root)
        {
            if (root.Children.Count > 0)
            {
                foreach (var (_, v) in root.Children)
                {
                    foreach (var ln in GetAllLeafNodes(v))
                    {
                        yield return ln;
                    }
                }
            }
            else
            {
                yield return root;
            }
        }

        public static Func<string[], int, SideEnum, SideEnum> GetChooseSideFunc(SideOptionEnum option)
        {
            return option switch
            {
                SideOptionEnum.SameAsParent => SameAsParent,
                SideOptionEnum.OnlyLeafOppositeToParent => OnlyLeafOppositeToParent,
                SideOptionEnum.AllCredit => AllCredit,
                SideOptionEnum.AllDebit => AllDebit,
                _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
            };

            SideEnum SameAsParent(string[] splitNames, int i, SideEnum parentSide)
            {
                return i == splitNames.Length - 1 ? GetOppositeSide(parentSide) : parentSide;
            }

            SideEnum OnlyLeafOppositeToParent(string[] splitNames, int i, SideEnum parentSide)
            {
                return i == splitNames.Length - 1 ? GetOppositeSide(parentSide) : parentSide;
            }

            SideEnum AllDebit(string[] splitNames, int i, SideEnum parentSide)
            {
                return SideEnum.Debit;
            }

            SideEnum AllCredit(string[] splitNames, int i, SideEnum parentSide)
            {
                return SideEnum.Credit;
            }
        }

        /// <summary>
        ///  Ensure the specified account is created in the specified state by executing the OpenAccount entries it creates as required.
        /// </summary>
        /// <param name="ledger">The ledger to use for the account opening entry.</param>
        /// <param name="dateTime">The time to open the account for the path if it hasn't.</param>
        /// <param name="state">The accounts root the account is in.</param>
        /// <param name="fullName">The full name that identify the account in the state.</param>
        /// <param name="chooseSide">Func to choose the side for the current node</param>
        public static void EnsureCreateAccount(this Ledger? ledger, DateTime dateTime, AccountsRoot state,
            string fullName, Func<string[], int, SideEnum, SideEnum> chooseSide)
        {
            var splitNames = fullName.Split('.');
            AccountNode p = state;
            string? parentFullName = null;
            var i = 0;
            foreach (var seg in splitNames)
            {
                if (parentFullName != null || !p.Children.TryGetValue(seg, out var child))
                {
                    parentFullName ??= p.FullName;
                    var side = chooseSide(splitNames, i, p.Side);
                    var openAccount = new OpenAccount(dateTime, (new AccountNodeReference(parentFullName), side), seg);
                    ledger.AddAndExecute(openAccount);
                    parentFullName += "." + seg;
                }
                else
                {
                    p = child;
                }

                ++i;
            }
        }

        /// <summary>
        ///  Ensure the specified account is created in an existing state as specified in its full name.
        /// </summary>
        /// <param name="ledger">The ledger to use for the account opening entry.</param>
        /// <param name="dateTime">The time to open the account for the path if it hasn't.</param>
        /// <param name="fullName">The full name that identify the account globally.</param>
        /// <param name="chooseSide">Func to choose the side for the current node</param>
        public static void EnsureCreateAccount(this Ledger? ledger, DateTime dateTime, string fullName, Func<string[], int, SideEnum, SideEnum> chooseSide)
        {
            // This makes sure the state is already created.
            // The function is meant to create accounts for a state. And that's why it expects the fullName to have at least 2 segments.
            var split = fullName.Split('.', 2);

            var stateName = split[0];
            var state = AccountsRoot.GetAccountsRoot(stateName);
            if (state == null)
            {
                var openAccount = new OpenAccount(dateTime, null, stateName);
                ledger.AddAndExecute(openAccount);

                state = AccountsRoot.GetAccountsRoot(stateName)!;
            }

            if (split.Length == 2)
            {
                ledger.EnsureCreateAccount(dateTime, state, split[1], chooseSide);
            }
        }

        public static void AddAndExecute(this Ledger? ledger, Entry entry)
        {
            ledger?.Entries.Add(entry);
            entry.Redo();
        }

        public static SimpleTransaction? SmartCreateTransaction(DateTime dateTime, string debitedAccountFullName,
            string creditedAccountFullName, decimal amount, string? remarks = null)
        {
            if (amount > 0)
            {
                return CreateTransaction(dateTime, debitedAccountFullName, creditedAccountFullName, amount, remarks);
            }

            if (amount < 0)
            {
                return CreateTransaction(dateTime, creditedAccountFullName, debitedAccountFullName, -amount, remarks);
            }

            return null;
        }

        public static SimpleTransaction CreateTransaction(DateTime dateTime, string debitedAccountFullName,
            string creditedAccountFullName, decimal amount, string? remarks = null)
            => new(dateTime, new AccountNodeReference(debitedAccountFullName),
                new AccountNodeReference(creditedAccountFullName), amount) { Remarks = remarks };

        public static void AddAndExecuteTransaction(this Ledger? ledger, DateTime dateTime,
            string debitedAccountFullName, string creditedAccountFullName, decimal amount, string? remarks = null)
        {
            var transaction =
                CreateTransaction(dateTime, debitedAccountFullName, creditedAccountFullName, amount, remarks);
            ledger.AddAndExecute(transaction);
        }

        public static CompositeTransaction CreateTransaction(DateTime dateTime, IEnumerable<(string, decimal)> debited,
            IEnumerable<(string, decimal)> credited, string? remarks = null)
        {
            var transaction = new CompositeTransaction(dateTime)
            {
                Remarks = remarks
            };
            foreach (var (accountFullName, amount) in debited)
            {
                transaction.Debited.Add((new AccountNodeReference(accountFullName), amount));
            }

            foreach (var (accountFullName, amount) in credited)
            {
                transaction.Credited.Add((new AccountNodeReference(accountFullName), amount));
            }

            return transaction;
        }

        public static void AddAndExecuteTransaction(this Ledger? ledger, DateTime dateTime,
            IEnumerable<(string, decimal)> debited, IEnumerable<(string, decimal)> credited, string? remarks = null)
        {
            var transaction = CreateTransaction(dateTime, debited, credited, remarks);
            // TODO Add balance checking assert.
            ledger.AddAndExecute(transaction);
        }
    }
}