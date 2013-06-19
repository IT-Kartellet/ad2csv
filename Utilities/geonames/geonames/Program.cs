using System;
using System.Collections.Generic;
using System.Linq;

namespace geonames
{
	class CountryInfo {
		public string ISO;
		public string ISO3;
		public string ISONumeric;
		public string Fips;
		public string Country;
		public string Capital;
		public string Area;
		public string Population;
		public string Continent;
		public string TLD;
		public string CurrencyCode;
		public string CurrencyName;
		public string Phone;
		public string PostalCodeFormat;
		public string PostalCodeRegex;
		public string Languages;
		public string GeonameId;
		public string Neighbours;
		public string EquivalentFipsCode;

		public CountryInfo (string ISO, string ISO3, string ISONumeric, string Fips, string Country, 
		                    string Capital, string Area, string Population, string Continent, string TLD, string CurrencyCode,
		                    string CurrencyName, string Phone, string PostalCodeFormat, string PostalCodeRegex, 
		                    string Languages, string GeonameId, string Neighbours, string EquivalentFipsCode)
		{
			this.ISO = ISO;
			this.ISO3 = ISO3;
			this.ISONumeric = ISONumeric;
			this.Fips = Fips;
			this.Country = Country;
			this.Capital = Capital;
			this.Area = Area;
			this.Population = Population;
			this.Continent = Continent;
			this.TLD = TLD;
			this.CurrencyCode = CurrencyCode;
			this.CurrencyName = CurrencyName;
			this.Phone = Phone;
			this.PostalCodeFormat = PostalCodeFormat;
			this.PostalCodeRegex = PostalCodeRegex;
			this.Languages = Languages;
			this.GeonameId = GeonameId;
			this.Neighbours = Neighbours;
			this.EquivalentFipsCode = EquivalentFipsCode;
		}
	}

	class GeoName {
		public string geonameid; //integer id of record in geonames database
		public string name; //name of geographical point (utf8) varchar(200)
		public string asciiname; //name of geographical point in plain ascii characters, varchar(200)
		public string alternatenames; //alternatenames, comma separated varchar(5000)
		public string latitude; //latitude in decimal degrees (wgs84)
		public string longitude; //longitude in decimal degrees (wgs84)
		public string featureclass; //see http://www.geonames.org/export/codes.html, char(1)
		public string featurecode; //see http://www.geonames.org/export/codes.html, varchar(10)
		public string countrycode; //ISO-3166 2-letter country code, 2 characters
		public string cc2; //alternate country codes, comma separated, ISO-3166 2-letter country code, 60 characters
		public string admin1code; //fipscode (subject to change to iso code), see exceptions below, see file admin1Codes.txt for display names of this code; varchar(20)
		public string admin2code; //code for the second administrative division, a county in the US, see file admin2Codes.txt; varchar(80) 
		public string admin3code; //code for third level administrative division, varchar(20)
		public string admin4code; //code for fourth level administrative division, varchar(20)
		public string population; //bigint (8 byte int)
		public string elevation; //in meters, integer
		public string dem; //digital elevation model, srtm3 or gtopo30, average elevation of 3''x3'' (ca 90mx90m) or 30''x30'' (ca 900mx900m) area in meters, integer. srtm processed by cgiar/ciat.
		public string timezone; //the timezone id (see file timeZone.txt) varchar(40)
		public string modificationdate; //date of last modification in yyyy-MM-dd format

		public GeoName(string geonameid, string name, string asciiname, string alternatenames,
		               string latitude, string longitude, string featureclass, string featurecode,
		               string countrycode, string cc2, string admin1code, string admin2code,
		               string admin3code, string admin4code, string population, string elevation,
		               string dem, string timezone, string modificationdate)
		{
			this.geonameid = geonameid;
			this.name = name;
			this.asciiname = asciiname;
			this.alternatenames = alternatenames;
			this.latitude = latitude;
			this.longitude = longitude;
			this.featureclass = featureclass;
			this.featurecode = featurecode;
			this.countrycode = countrycode;
			this.cc2 = cc2;
			this.admin1code = admin1code;
			this.admin2code = admin2code;
			this.admin3code = admin3code;
			this.admin4code = admin4code;
			this.population = population;
			this.elevation = elevation;
			this.dem = dem;
			this.timezone = timezone;
			this.modificationdate = modificationdate;
		}
	}

	class TimeZone {
		public string CountryCode;
		public string TimeZoneId;
		public string GMT;
		public string Offset;

		public TimeZone(string CountryCode, string TimeZoneId, string GMT, string Offset){
			this.CountryCode = CountryCode;
			this.TimeZoneId = TimeZoneId;
			this.GMT = GMT;
			this.Offset = Offset;
		}
	}

	class GeoDecoder {
		public List<CountryInfo> countries;
		public List<TimeZone> timezones;
		public List<GeoName> geonames;

		public GeoDecoder(string countrysrc,string  geosrc,string  timezonesrc){
			//init
			this.countries = new List<CountryInfo> ();
			this.timezones = new List<TimeZone> ();
			this.geonames = new List<GeoName> ();
			string[] lines;
			//decode countries
			lines = countrysrc.Split ('\n');
			for(int i=0; i<lines.Length; i++){
				string line = lines [i];
				if (line.Length==0 || line [0] == '#')
					continue;
				string[] tabs = line.Split ('\t');
				CountryInfo country = new CountryInfo (
					tabs[0], tabs[1], tabs[2], tabs[3],
					tabs[4], tabs[5], tabs[6], tabs[7],
					tabs[8], tabs[9], tabs[10], tabs[11],
					tabs[12], tabs[13], tabs[14], tabs[15],
					tabs[16], tabs[17], tabs[18]
				);
				this.countries.Add (country);
			}
			//decode geonames
			lines = geosrc.Split ('\n');
			for(int i=0; i<lines.Length; i++){
				string line = lines [i];
				if (line.Length==0 || line [0] == '#')
					continue;
				string[] tabs = line.Split ('\t');
				GeoName geoname = new GeoName (
					tabs [0], tabs [1], tabs [2], tabs [3],
					tabs [4], tabs [5], tabs [6], tabs [7],
					tabs [8], tabs [9], tabs [10], tabs [11],
					tabs [12], tabs [13], tabs [14], tabs [15],
					tabs [16], tabs [17], tabs [18]
				);
				this.geonames.Add (geoname);
			}
			//decode timezones
			lines = timezonesrc.Split ('\n');
			for(int i=0; i<lines.Length; i++){
				string line = lines [i];
				if (line.Length==0 || line [0] == '#')
					continue;
				string[] tabs = line.Split ('\t');
				TimeZone timezone = new TimeZone (
					tabs [0], tabs [1], tabs [2], tabs [3]
					);
				this.timezones.Add (timezone);
			}
		}

		public string GetGMT(string strCountry, string strCity){
			//get country
			CountryInfo country = null;
			switch (strCountry.Length) {
				case 2:
						//ISO
					country = countries.Where (e=>e.ISO==strCountry).FirstOrDefault ();
					break;
			case 3:
					int code;
					if (int.TryParse (strCountry, out code)) {
						//ISO-Numeric
						country = countries.Where (e=>e.ISONumeric==strCountry).FirstOrDefault ();
						break;
					} else {
						//ISO3
						country = countries.Where (e=>e.ISO3==strCountry).FirstOrDefault ();
						break;
					}
				default :
					//Country Name
					country = countries.Where (e=> e.Country==strCountry).OrderByDescending (e => e.Population).FirstOrDefault ();
					break;
			}
			if (country == null)
				return "Cannot find country.";

			//get city
			GeoName city = null;
			city = geonames.Where (e=>e.name==strCity).OrderByDescending (e=>e.population).FirstOrDefault ();
			if (city == null)
				return "Could not find city.";
			else
				return city.timezone;

		}
	}

	class MainClass
	{
		public static void log(string msg){
			Console.WriteLine(msg);
		}
		public static void test(GeoDecoder decoder, string country, string city){
			Console.WriteLine ();
			log ("Starting GetGMT("+country+","+city+").");
			DateTime t1 = DateTime.Now;
			string result=decoder.GetGMT (country, city);
			DateTime t2 = DateTime.Now;
			log ("Parsing took " + (t2-t1).Milliseconds + " ms.");
			Console.WriteLine ("Result: "+result);
		}

		public static void Main (string[] args)
		{
			//download mock data
			log ("Starting download.");
			DateTime t1 = DateTime.Now;
			string countryurl = "http://download.geonames.org/export/dump/countryInfo.txt";
			string countrysrc = new System.Net.WebClient ().DownloadString (countryurl);
			string cityurl = "http://stormies.dk/cities1000.txt";
			string citysrc = new System.Net.WebClient ().DownloadString (cityurl);
			string timezoneurl = "http://download.geonames.org/export/dump/timeZones.txt";
			string timezonesrc = new System.Net.WebClient ().DownloadString (timezoneurl);
			DateTime t2 = DateTime.Now;
			log ("Downloading took " + (t2-t1).Milliseconds + " ms.\n");

			//actual code
			log ("Starting parsing.");
			t1 = DateTime.Now;
			GeoDecoder decoder = new GeoDecoder (countrysrc, citysrc, timezonesrc);
			t2 = DateTime.Now;
			log ("Parsing took " + (t2-t1).Milliseconds + " ms.\n");

			//tests
			test (decoder, "DK", "Copenhagen");
			test (decoder, "DNK", "Copenhagen");
			test (decoder, "208", "Copenhagen");

			test (decoder, "DK", "Roskilde");
			test (decoder, "DNK", "Roskilde");
			test (decoder, "208", "Roskilde");

			test (decoder, "RO", "Craiova");
			test (decoder, "ROU", "Oradea");
			test (decoder, "642", "Arad");

		}
	}
}
