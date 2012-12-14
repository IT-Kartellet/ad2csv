using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.Text;
using System.Text.RegularExpressions;


namespace AD2CSV
{
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
            if(!Char.TryParse(ConfigurationManager.AppSettings["QuoteChar"], out quotechar)) {
                quotechar = '"';
            }
            
            bool quotealways;
            if(!Boolean.TryParse(ConfigurationManager.AppSettings["QuoteAlways"], out quotealways)) {
                quotealways = false;
            }

            var headers = new string[]{};
            if(ConfigurationManager.AppSettings["Headers"] != null) {
                headers = ConfigurationManager.AppSettings["Headers"].Split(delimiter);
            }
            var properties = new string[] { };
            if(ConfigurationManager.AppSettings["Properties"] != null) {
                properties = ConfigurationManager.AppSettings["Properties"].Split(delimiter);            
            }
            var outfile = ConfigurationManager.AppSettings["OutFile"];
            if (outfile == null)
            {
                throw new Exception("OutFile has to be set");
            }

            var propload = new List<string>();
            foreach(var prop in properties) {
                if(!String.IsNullOrEmpty(prop)) {
                    propload.Add(prop);
                }
            }

            var filters = new Dictionary<string, Regex>();
            if(ConfigurationManager.AppSettings["Filters"] != null) {
                foreach (var filterstr in ConfigurationManager.AppSettings["Filters"].Split(delimiter))
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

            DirectorySearcher searcher = new DirectorySearcher(root, "(&(sAMAccountType=805306368)(ObjectClass=person))", propload.ToArray());
            searcher.ReferralChasing = ReferralChasingOption.All;
            searcher.SearchScope = SearchScope.Subtree;
            searcher.PageSize = 1000;
			
            int count = 0;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outfile, false))
            {
                file.WriteLine(String.Join(delimiter.ToString(), headers));

                Console.Write("Search AD for users");
                foreach (SearchResult item in searcher.FindAll())
                {
                    DirectoryEntry entry = item.GetDirectoryEntry();
					Console.Write(".");
					
                    // Filter values and skip entry if they don't match
                    var skip = false;
                    foreach (var filter in filters)
                    {
                        // All filters has to match for a record not to be skip'ed
                        skip = true;
                        if (entry.Properties.Contains(filter.Key) && entry.Properties[filter.Key].Count > 0)
                        {
                            foreach (var value in entry.Properties[filter.Key]) {
                                if (filter.Value.IsMatch(value.ToString(), 0))
                                {
                                    skip = false;
                                }
                            }
                        }
                        if (skip) continue;
                    }
                   	
                    // Create line for output 
                    var line = new List<string>();
                    foreach (var name in properties)
                    {
                        if (!String.IsNullOrEmpty(name) && entry.Properties.Contains(name) && entry.Properties[name].Count > 0)
                        {
                            var value = entry.Properties[name][0].ToString();
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
    }
}
