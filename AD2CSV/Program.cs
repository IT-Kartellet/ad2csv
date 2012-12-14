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
            var delimiter = Char.Parse(ConfigurationManager.AppSettings["Delimiter"]);
            var quotechar = Char.Parse(ConfigurationManager.AppSettings["QuoteChar"]);
            var quotealways = Boolean.Parse(ConfigurationManager.AppSettings["QuoteAlways"]);

            var filters = new Dictionary<string, Regex>();
            foreach (var filterstr in ConfigurationManager.AppSettings["Filters"].Split(delimiter))
            {
                var name = filterstr.Substring(0, filterstr.IndexOf("="));
                var regex = new Regex(filterstr.Substring(filterstr.IndexOf("=")));
                filters.Add(name, regex);
            }
            var headers = ConfigurationManager.AppSettings["Headers"].Split(delimiter);
            var properties = ConfigurationManager.AppSettings["Properties"].Split(delimiter);
            var outfile = ConfigurationManager.AppSettings["OutFile"];

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

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outfile, true))
            {
                file.WriteLine(String.Join(delimiter.ToString(), headers));

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
                }
            }
        }
    }
}
