using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace AD2CSV
{
    class Program
    {
        static void Main(string[] args)
        {
            var filters = new Dictionary<string, Regex>() {
                { "mail", new Regex(@"@damco\.com$") }
            };

            var headers = new List<string>()
            {
                "Person_UniqueID", "Person_FirstName","Person_LastName","Person_NotificationMailAddress","OfficePhoneNumber","MobilePhoneNumber","OfficeFaxNumber","TypeOfStaff_Desc","Person_Function","Person_JobTitle","Person_Section","OBU_Name","Department_Name","Manager_UniqueID","Location_Code","Location_AddressLine1","Location_AddressLine2","Country_Name","City_Name","Location_Latitude","Location_Longitude"
            };

            var properties = new List<string>()
            {
                "sAMAccountName", "givenName", "sn", "mail", "telephoneNumber", "", "facsimileTelephoneNumber", "employeeType", "", "title", "", "", "", "", "", "", "", "", "", 
            };

            var delimiter = ";";
            var quotechar = "\"";
            var quotealways = true;

            DirectorySearcher ds = new DirectorySearcher();
            SearchResult sr = ds.FindOne();
            var adroot = sr.GetDirectoryEntry().Path;

            DirectoryEntry root = new DirectoryEntry(
                adroot,
                null,
                null,
                AuthenticationTypes.Secure
            );

            DirectorySearcher searcher = new DirectorySearcher(root);
            searcher.ReferralChasing = ReferralChasingOption.All;
            searcher.SearchScope = SearchScope.Subtree;
            searcher.Filter = "ObjectClass=person";

            Console.WriteLine(String.Join(";", headers));
            foreach (SearchResult item in searcher.FindAll())
            {
                DirectoryEntry entry = item.GetDirectoryEntry();

                // Filter values and skip entry if they match
                var skip = false;
                foreach (var filter in filters)
                {
                    if (entry.Properties[filter.Key].Count > 0 && !filter.Value.IsMatch(entry.Properties[filter.Key].ToString(), 0))
                    {
                        // DO nothing 
                    }
                    else
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip) continue;

                // Create line for output 
                var line = new List<string>();
                foreach (var name in properties)
                {
                    if (!String.IsNullOrEmpty(name) && entry.Properties[name].Count > 0)
                    {
                        var value = entry.Properties[name][0].ToString();
                        if (quotealways || value.IndexOf(quotechar) > -1 || value.IndexOf(delimiter) > -1)
                        {
                            line.Add(value.Replace(quotechar, quotechar + quotechar));
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
                Console.WriteLine(String.Join(";", line));
            }
            Console.ReadKey();
        }
    }
}
