using System;

namespace CodeCaster.GoodWe
{
    public class AccountConfiguration
    {
        public AccountConfiguration(string? account, string? key)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public string? Account { get; set; }
        public string? Key { get; set; }
    }
}
