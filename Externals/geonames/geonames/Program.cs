using System;

namespace geonames
{
	class MainClass
	{
		public static void test_Timezone(GeoDecoder decoder, string country, string city){
			string result=decoder.GetTimezone (country, city);
			//Console.WriteLine ("Result: "+result);
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

			//data parsing
			Console.WriteLine ("Starting parsing.");
			t1 = DateTime.Now;
			GeoDecoder decoder = new GeoDecoder (countrysrc, citysrc, timezonesrc);
			t2 = DateTime.Now;
			Console.WriteLine ("Parsing took " + (t2-t1).Milliseconds + " ms.\n");

			//test
			Console.WriteLine ("Starting 10.000 test.");
			t1 = DateTime.Now;

			for (int i = 0; i<1000; i++) {
				test_Timezone (decoder, "DK", "Copenhagen");
				test_Timezone (decoder, "DNK", "Copenhagen");
				test_Timezone (decoder, "208", "Copenhagen");
				test_Timezone (decoder, "DK", "Roskilde");
				test_Timezone (decoder, "DNK", "Roskilde");
				test_Timezone (decoder, "208", "Roskilde");
				test_Timezone (decoder, "RO", "Craiova");
				test_Timezone (decoder, "ROU", "Oradea");
				test_Timezone (decoder, "642", "Arad");
				test_Timezone (decoder, "Denmark", "Copenhagen");
			}

			t2 = DateTime.Now;
			Console.WriteLine ("10.000 test took " + (t2-t1).Milliseconds + " ms.\n");
		}
	}
}
