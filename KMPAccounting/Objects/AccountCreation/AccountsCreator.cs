using System;
using System.Linq;
using System.Reflection;
using KMPAccounting.Objects.BookKeeping;

namespace KMPAccounting.Objects.AccountCreation
{
    public static class AccountsCreator
    {
        public static void CreateAccounts<T>(this Ledger ledger, DateTime dateTime)
        {
            CreateAccounts(ledger, dateTime, typeof(T), null);
        }

        public static void CreateAccounts(this Ledger ledger, DateTime dateTime, object o)
        {
            CreateAccounts(ledger, dateTime, o.GetType(), o);
        }

        public static void CreateAccounts(this Ledger ledger, DateTime dateTime, Type t, object? o = null)
        {
            var allItems = t.GetProperties().Where(x =>
                {
                    if (x.GetCustomAttribute<IsNotAccountAttribute>() != null) return false;
                    if (x.PropertyType != typeof(string) && x.PropertyType != typeof(AccountPath)) return false;
                    return o != null || x.GetGetMethod().IsStatic;
                }).Select(x => (x.GetValue(o).ToString(), x.GetCustomAttribute<OppositeToParentAttribute>() != null))
                .Concat(t.GetFields().Where(x =>
                {
                    if (x.GetCustomAttribute<IsNotAccountAttribute>() != null) return false;
                    if (x.FieldType != typeof(string) && x.FieldType != typeof(AccountPath)) return false;
                    return o != null || x.IsStatic;
                }).Select(x => (x.GetValue(o).ToString(), x.GetCustomAttribute<OppositeToParentAttribute>() != null)))
                .OrderBy(pair => pair.Item1);

            foreach (var (accountPath, oppositeToParent) in allItems)
            {
                ledger.EnsureCreateAccount(dateTime, accountPath, oppositeToParent);
            }
        }
    }
}