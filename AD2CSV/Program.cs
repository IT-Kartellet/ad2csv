using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AD2CSV
{
    enum UserAccountControlFlags
    {
        SCRIPT = 0x0001,
        ACCOUNTDISABLE = 0x0002,
        HOMEDIR_REQUIRED = 0x0008,
        LOCKOUT = 0x0010,
        PASSWD_NOTREQD = 0x0020,
        PASSWD_CANT_CHANGE = 0x0040,
        ENCRYPTED_TEXT_PWD_ALLOWED = 0x0080,
        TEMP_DUPLICATE_ACCOUNT = 0x0100,
        NORMAL_ACCOUNT = 0x0200,
        INTERDOMAIN_TRUST_ACCOUNT = 0x0800,
        WORKSTATION_TRUST_ACCOUNT = 0x1000,
        SERVER_TRUST_ACCOUNT = 0x2000,
        DONT_EXPIRE_PASSWORD = 0x10000,
        MNS_LOGON_ACCOUNT = 0x20000,
        SMARTCARD_REQUIRED = 0x40000,
        TRUSTED_FOR_DELEGATION = 0x80000,
        NOT_DELEGATED = 0x100000,
        USE_DES_KEY_ONLY = 0x200000,
        DONT_REQ_PREAUTH = 0x400000,
        PASSWORD_EXPIRED = 0x800000,
        TRUSTED_TO_AUTH_FOR_DELEGATION = 0x1000000
    }

    class Program
    {
        static void Main(string[] args)
        {
            char delimiter;
            if (!Char.TryParse(ConfigurationManager.AppSettings["Delimiter"], out delimiter))
            {
                delimiter = ';';
            }

            char quotechar;
            if (!Char.TryParse(ConfigurationManager.AppSettings["QuoteChar"], out quotechar))
            {
                quotechar = '"';
            }

            bool quotealways;
            if (!Boolean.TryParse(ConfigurationManager.AppSettings["QuoteAlways"], out quotealways))
            {
                quotealways = false;
            }

            var headers = new string[] { };
            if (ConfigurationManager.AppSettings["Headers"] != null)
            {
                headers = ConfigurationManager.AppSettings["Headers"].Split(delimiter);
            }
            var properties = new string[] { };
            if (ConfigurationManager.AppSettings["Properties"] != null)
            {
                properties = ConfigurationManager.AppSettings["Properties"].Split(delimiter);
            }
            var outfile = ConfigurationManager.AppSettings["OutFile"];
            if (outfile == null)
            {
                throw new Exception("OutFile has to be set");
            }

            var propload = new List<string>() { "userAccountcontrol", "pwdLastSet" };
            foreach (var prop in properties)
            {
                if (!String.IsNullOrEmpty(prop))
                {
                    propload.Add(prop);
                }
            }

            var ldapfilter = "(&(&(&(sAMAccountType=805306368)(ObjectClass=person))(sAMAccountName=tlb013))(!(userAccountControl:1.2.840.113556.1.4.803:=2)))";
            if (ConfigurationManager.AppSettings["LDAPFilters"] != null)
            {
                ldapfilter = ConfigurationManager.AppSettings["LDAPFilters"];
            }

            var filters = new Dictionary<string, Regex>();
            if (ConfigurationManager.AppSettings["PropertyFilters"] != null)
            {
                foreach (var filterstr in ConfigurationManager.AppSettings["PropertyFilters"].Split(delimiter))
                {
                    var name = filterstr.Substring(0, filterstr.IndexOf("="));
                    var regex = new Regex(filterstr.Substring(filterstr.IndexOf("=") + 1));
                    filters.Add(name, regex);
                    propload.Add(name);
                }
            }

            DirectorySearcher ds = new DirectorySearcher();
            SearchResult sr = ds.FindOne();
            var adroot = sr.GetDirectoryEntry().Path;

            DirectoryEntry root = new DirectoryEntry(
                adroot,
                null,
                null,
                AuthenticationTypes.Secure
            );

            Console.WriteLine("Search AD for users in root {0} with ldap filter of {1}", root.Properties["distinguishedName"][0], ldapfilter);
            Console.WriteLine("Filters: ");
            foreach (var filter in filters)
            {
                Console.WriteLine("  {0} = {1}", filter.Key, filter.Value);
            }

            DirectorySearcher searcher = new DirectorySearcher(root, ldapfilter, propload.ToArray());
            searcher.ReferralChasing = ReferralChasingOption.None;
            searcher.SearchScope = SearchScope.Subtree;
            searcher.PageSize = 1000;

            int count = 0;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outfile, false))
            {
                file.WriteLine(String.Join(delimiter.ToString(), headers));

                // Filter values based on regex
                foreach (SearchResult item in searcher.FindAll())
                {
                    DirectoryEntry entry = item.GetDirectoryEntry();
                    if (count % 10 == 0) Console.Write(".");

                    // Check if account has password has Don't expire password set  
                    int UserAccountControl = Convert.ToInt32(entry.Properties["userAccountcontrol"].Value);
                    if ((UserAccountControl & (int)UserAccountControlFlags.DONT_EXPIRE_PASSWORD) > 0)
                    {
                        continue;
                    }

                    var pwdLastSet = DateTime.FromFileTime(ConvertADSLargeIntegerToInt64(entry.Properties["pwdLastSet"].Value));
                    var pwdLimit = DateTime.Now.AddMonths(-3);
                    if (pwdLastSet < pwdLimit)
                    {
                        continue;
                    }

                    // Filter values and skip entry if they don't match
                    var skip = false;
                    foreach (var filter in filters)
                    {
                        // All filters has to match for a record not to be skip'ed
                        skip = true;
                        if (entry.Properties.Contains(filter.Key) && entry.Properties[filter.Key].Count > 0)
                        {
                            foreach (var value in entry.Properties[filter.Key])
                            {
                                if (filter.Value.IsMatch(value.ToString(), 0))
                                {
                                    skip = false;
                                }
                            }
                        }
                    }
                    if (skip) continue;

                    // Create line for output 
                    var line = new List<string>();
                    foreach (var name in properties)
                    {
                        if (!String.IsNullOrEmpty(name) && entry.Properties.Contains(name) && entry.Properties[name].Count > 0)
                        {
                            var value = entry.Properties[name][0].ToString();

                            // Replace line breaks with comma
                            value = value.Replace("\r\n", "\n");
                            value = value.Replace("\r", "\n");
                            value = value.Replace("\n", ",");

                            // Check if need to quote the item in the line
                            if (quotealways || value.IndexOf(quotechar) > -1 || value.IndexOf(delimiter) > -1)
                            {
                                line.Add(value.Replace(quotechar.ToString(), (quotechar + quotechar).ToString()));
                            }
                            else
                            {
                                line.Add(value);
                            }
                        }
                        else
                        {
                            line.Add("");
                        }
                    }
                    file.WriteLine(String.Join(";", line.ToArray()));
                    count++;
                }
            }
            Console.WriteLine();
            Console.WriteLine("Count: {0}", count);
        }

        public static Int64 ConvertADSLargeIntegerToInt64(object adsLargeInteger)
        {
            var highPart = (Int32)adsLargeInteger.GetType().InvokeMember("HighPart", System.Reflection.BindingFlags.GetProperty, null, adsLargeInteger, null);
            var lowPart = (Int32)adsLargeInteger.GetType().InvokeMember("LowPart", System.Reflection.BindingFlags.GetProperty, null, adsLargeInteger, null);
            return highPart * ((Int64)UInt32.MaxValue + 1) + lowPart;
        }

    }
}
