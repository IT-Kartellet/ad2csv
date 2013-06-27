using System;
using System.Collections.Generic;

namespace geonames
{
	class MainClass
	{
		public static void test_Timezone(GeoDecoder decoder, string country, string city){
			string result=decoder.GetTimezone (country, city);
			Console.WriteLine ("Result: "+result);
		}
		public static void test_GMT(GeoDecoder decoder, string country, string city){
			string result=decoder.GetGMT (country, city);
			//Console.WriteLine ("Result: "+result);
		}

		public static void Main (string[] args)
		{
			//download mock data
			Console.WriteLine ("Starting download.");
			DateTime t1 = DateTime.Now;
			string countryurl = "http://download.geonames.org/export/dump/countryInfo.txt";
			string countrysrc = new System.Net.WebClient ().DownloadString (countryurl);
			string cityurl = "http://stormies.dk/cities1000.txt";
			string citysrc = new System.Net.WebClient ().DownloadString (cityurl);
			string timezoneurl = "http://download.geonames.org/export/dump/timeZones.txt";
			string timezonesrc = new System.Net.WebClient ().DownloadString (timezoneurl);
			DateTime t2 = DateTime.Now;
			Console.WriteLine ("Downloading took " + (t2-t1).Milliseconds + " ms.\n");

            //Exception mapping
            //Note this maps a city to a different city not a timezone
            Dictionary<string, string> exceptionMap = new Dictionary<string, string>();
            exceptionMap["CN/Hong Kong"] = "CN/Beijing";
            exceptionMap["China/Hong Kong"] = "China/Beijing";


			//data parsing
			Console.WriteLine ("Starting parsing.");
			t1 = DateTime.Now;
            GeoDecoder decoder = new GeoDecoder(countrysrc, citysrc, timezonesrc, exceptionMap);
			t2 = DateTime.Now;
			Console.WriteLine ("Parsing took " + (t2-t1).Milliseconds + " ms.\n");

			//test
			Console.WriteLine ("Starting 10.000 test.");
			t1 = DateTime.Now;

			for (int i = 0; i<1000; i++) {
				test_Timezone (decoder, "CN", "Hong Kong");
				test_Timezone (decoder, "China", "Hong Kong");
			}

			t2 = DateTime.Now;
			Console.WriteLine ("10.000 test took " + (t2-t1).Milliseconds + " ms.\n");
		}
	}
}
