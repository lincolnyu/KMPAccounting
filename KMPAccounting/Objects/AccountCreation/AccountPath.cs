namespace KMPAccounting.Objects.AccountCreation
{
    public class AccountPath(string path)
    {
        public static implicit operator AccountPath(string path)
        {
            return new AccountPath(path);
        }

        public static implicit operator string(AccountPath accountGroup)
        {
            return accountGroup.Path;
        }

        public string this[int index] => Path.Split('.')[index];

        public static AccountPath operator +(AccountPath group, string suffix)
        {
            var trimmedGroupPath = group.Path.TrimEnd('.');
            if (trimmedGroupPath == "") return suffix.TrimStart('.');
            suffix = suffix.Trim();
            return suffix == "" ? trimmedGroupPath : $"{trimmedGroupPath}.{suffix.TrimStart('.')}";
        }

        public static AccountPath Join(string a, string b)
        {
            return (AccountPath)a + b;
        }

        public bool StrictStartsWith(AccountPath accountPath)
        {
            return Path.StartsWith(accountPath.Path + ".");
        }

        public string GetRelative(string prefix)
        {
            var res = Path[prefix.Length..];
            return res.TrimStart('.');
        }

        public override string ToString()
        {
            return Path;
        }

        public string Path { get; } = path;
    }
}