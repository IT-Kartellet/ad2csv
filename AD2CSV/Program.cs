﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.IO;
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

            //var ldapfilter = "(&(&(sAMAccountType=805306368)(ObjectClass=person))(sAMAccountName=tlb013))";
            var ldapfilter = "(&(sAMAccountType=805306368)(ObjectClass=person)))";
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

            var skipemptyproperties = new List<string>();
            if (ConfigurationManager.AppSettings["SkipEmptyProperties"] != null)
            {
                skipemptyproperties = new List<string>(ConfigurationManager.AppSettings["SkipEmptyProperties"].Split(delimiter));
            }

            int skipexpiredpassword;
            if (!int.TryParse(ConfigurationManager.AppSettings["SkipExpiredPassword"], out skipexpiredpassword))
            {
                skipexpiredpassword = 90;
            }

            bool skipdontexpirepassword;
            if (!Boolean.TryParse(ConfigurationManager.AppSettings["SkipDontExpirePassword"], out skipdontexpirepassword))
            {
                skipdontexpirepassword = true;
            }

            bool skipdisabledaccount;
            if (!Boolean.TryParse(ConfigurationManager.AppSettings["SkipDisabledAccount"], out skipdisabledaccount))
            {
                skipdisabledaccount = true;
            }

            int mincount;
            if (!int.TryParse(ConfigurationManager.AppSettings["MinimumCount"], out mincount))
            {
                mincount = 1;
            }

            string countrysrc;
            string citysrc;
            string timezonesrc;

            string tempPath = System.IO.Path.GetTempPath();

            if(File.Exists(Path.Combine(tempPath, "countryInfo.txt"))) {
                countrysrc = File.ReadAllText(Path.Combine(tempPath, "countryInfo.txt"), Encoding.UTF8);
            }
            else
            {
                countrysrc = new System.Net.WebClient().DownloadString("http://download.geonames.org/export/dump/countryInfo.txt");
                File.WriteAllText(Path.Combine(tempPath, "countryInfo.txt"), countrysrc, Encoding.UTF8);
            }

            if(File.Exists(Path.Combine(tempPath, "cities1000.txt"))) {
                citysrc = File.ReadAllText(Path.Combine(tempPath, "cities1000.txt"), Encoding.UTF8);
            }
            else
            {
                citysrc = new System.Net.WebClient().DownloadString("http://stormies.dk/cities1000.txt"); // FIXME: Add support for extracting from zip.
                File.WriteAllText(Path.Combine(tempPath, "cities1000.txt"), citysrc, Encoding.UTF8);
            }
            
            if(File.Exists(Path.Combine(tempPath, "timeZones.txt"))) {
                timezonesrc = File.ReadAllText(Path.Combine(tempPath, "timeZones.txt"), Encoding.UTF8);
            }
            else
            {
                timezonesrc = new System.Net.WebClient().DownloadString("http://download.geonames.org/export/dump/timeZones.txt");
                File.WriteAllText(Path.Combine(tempPath, "timeZones.txt"), timezonesrc, Encoding.UTF8);
            }
            
            //FIXME: Move this to config file Exception mapping
            //Note this maps a city to a different city not a timezone
            Dictionary<string, string> exceptionMap = new Dictionary<string, string>();
            exceptionMap["CN/Hong Kong"] = "HK/Hong Kong";
            exceptionMap["China/Hong Kong"] = "HK/Hong Kong";
            exceptionMap["US/Giralda Farms"] = "US/Madison";

            var decoder = new geonames.GeoDecoder(countrysrc, citysrc, timezonesrc, exceptionMap);

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
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outfile + ".tmp", false, Encoding.UTF8))
            {
                file.WriteLine(String.Join(delimiter.ToString(), headers));

                // Filter values based on regex
                foreach (SearchResult item in searcher.FindAll())
                {
                    DirectoryEntry entry = item.GetDirectoryEntry();
                    if (count % 10 == 0) Console.Write(".");

                    int UserAccountControl = Convert.ToInt32(entry.Properties["userAccountcontrol"].Value);

                    // Check if account is disabled  
                    if (skipdisabledaccount && (UserAccountControl & (int)UserAccountControlFlags.ACCOUNTDISABLE) > 0)
                    {
                        continue;
                    }

                    // Check if account has password has Don't expire password set  
                    if (skipdontexpirepassword && (UserAccountControl & (int)UserAccountControlFlags.DONT_EXPIRE_PASSWORD) > 0)
                    {
                        continue;
                    }

                    // Check if the password expired x days ago
                    if (skipexpiredpassword > 0)
                    {
                        if (entry.Properties.Contains("pwdLastSet"))
                        {
                            var pwdLastSet = DateTime.FromFileTime(ConvertADSLargeIntegerToInt64(entry.Properties["pwdLastSet"].Value));
                            var pwdLimit = DateTime.Now.AddDays(skipexpiredpassword * -1);
                            if (pwdLastSet < pwdLimit)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Username {0} has never set a password", entry.Properties["sAMAccountName"].Value.ToString());
                            continue;
                        }
                    }

                    // Skip properties that are empty
                    var skip = false;
                    foreach (var prop in skipemptyproperties)
                    {
                        if (entry.Properties.Contains(prop) && entry.Properties[prop].Count > 0)
                        {
                            foreach (var value in entry.Properties[prop])
                            {
                                if (String.IsNullOrEmpty(value.ToString())) skip = true;
                            }
                        }
                    }
                    if (skip) continue;

                    // Filter values and skip entry if they don't match
                    skip = false;
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


                    // Lookup city information base
                    string country = null;
                    string city = null;
                    string latetude = null;
                    string longitude = null;
                    string timezone = null;
                    if (entry.Properties.Contains("c") && entry.Properties["c"].Count > 0)
                    {
                        country = entry.Properties["c"][0].ToString();
                    }
                    if (entry.Properties.Contains("l") && entry.Properties["l"].Count > 0)
                    {
                        city = entry.Properties["l"][0].ToString();
                    }

                    if (country != null && city != null) {
                        var geoentry = decoder.GetEntry(country, city);
                        if (geoentry != null)
                        {
                            latetude = geoentry.latitude;
                            longitude = geoentry.longitude;
                            timezone = geoentry.timezone.TimeZoneId;
                        }
                        else
                        {
                            Console.WriteLine("Could not find {0}, {1}", country, city);
                        }
                    }

                    // Create line for output 
                    var line = new List<string>();
                    foreach (var name in properties)
                    {
                        if(name != null && name.StartsWith("=TimeZone") && timezone != null) {
                            line.Add(timezone);
                        }
                        else if (name != null && name.StartsWith("=Latetude") && latetude != null)
                        {
                            line.Add(latetude);
                        }
                        else if (name != null && name.StartsWith("=Longitude") && longitude != null)
                        {
                            line.Add(longitude);
                        }
                        else if (!String.IsNullOrEmpty(name) && entry.Properties.Contains(name) && entry.Properties[name].Count > 0)
                        {
                            var value = entry.Properties[name][0].ToString();

                            // Replace line breaks with comma
                            value = value.Replace("\r\n", "\n");
                            value = value.Replace("\r", "\n");
                            value = value.Replace("\n", ",");

                            // Check if need to quote the item in the line
                            if (quotealways || value.IndexOf(quotechar) > -1 || value.IndexOf(delimiter) > -1)
                            {
                                line.Add(quotechar + value.Replace(quotechar.ToString(), (quotechar.ToString() + quotechar.ToString())) + quotechar);
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

            if (count > mincount)
            {

                if (File.Exists(outfile))
                {
                    try
                    {
                        File.Replace(outfile + ".tmp", outfile, outfile + ".bak");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Failed to update file {0} because of: {1}", outfile, ex.Message);
                        File.Delete(outfile + ".tmp");
                    }
                }
                else
                {
                    File.Move(outfile + ".tmp", outfile);
                }
                Console.WriteLine();
                Console.WriteLine("Count: {0}", count);
            }
            else
            {
                Console.WriteLine("To few records found so not replacing {0}: {1} < {2}", outfile, count, mincount);
                //Console.ReadKey();
            }
        }

        public static Int64 ConvertADSLargeIntegerToInt64(object adsLargeInteger)
        {
            var highPart = (Int32)adsLargeInteger.GetType().InvokeMember("HighPart", System.Reflection.BindingFlags.GetProperty, null, adsLargeInteger, null);
            var lowPart = (Int32)adsLargeInteger.GetType().InvokeMember("LowPart", System.Reflection.BindingFlags.GetProperty, null, adsLargeInteger, null);
            return highPart * ((Int64)UInt32.MaxValue + 1) + lowPart;
        }
    }
}
