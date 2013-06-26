using System;
using System.Collections.Generic;

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
		public int GeonameId;
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
			if(!int.TryParse(GeonameId, out this.GeonameId)) this.GeonameId=0;
			this.Neighbours = Neighbours;
			this.EquivalentFipsCode = EquivalentFipsCode;
		}
	}

	class GeoName {
		public int geonameid; //integer id of record in geonames database
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
		public long population; //bigint (8 byte int)
		public int elevation; //in meters, integer
		public string dem; //digital elevation model, srtm3 or gtopo30, average elevation of 3''x3'' (ca 90mx90m) or 30''x30'' (ca 900mx900m) area in meters, integer. srtm processed by cgiar/ciat.
		public TimeZone timezone; //TimeZone Object
		public string modificationdate; //date of last modification in yyyy-MM-dd format

		public GeoName(string geonameid, string name, string asciiname, string alternatenames,
		               string latitude, string longitude, string featureclass, string featurecode,
		               string countrycode, string cc2, string admin1code, string admin2code,
		               string admin3code, string admin4code, string population, string elevation,
		               string dem, TimeZone timezone, string modificationdate)
		{
			this.geonameid = int.Parse(geonameid);
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
			if(!long.TryParse(population, out this.population)) this.population=0;
			if(!int.TryParse(elevation, out this.elevation)) this.elevation=0;
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
		public Dictionary<string, GeoName> finalDict=new Dictionary<string, GeoName>();
		public Dictionary<string, CountryInfo> countryDict=new Dictionary<string, CountryInfo>();
		public Dictionary<string, TimeZone> timezoneDict = new Dictionary<string, TimeZone> ();

		public GeoDecoder(string countrysrc,string  geosrc,string  timezonesrc){
			//init
			string[] lines;

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
				timezoneDict [timezone.TimeZoneId] = timezone;
			}

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
				countryDict [country.ISO] = country;
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
					tabs [16], timezoneDict[tabs [17]], tabs [18]
					);
				CountryInfo country = countryDict [geoname.countrycode];
				GeoName old=null;
				if(finalDict.TryGetValue (String.Format("{0}/{1}", country.ISO, geoname.asciiname), out old)){
					if (old.population > geoname.population)
						continue;
				}
				finalDict [String.Format("{0}/{1}", country.ISO, geoname.asciiname)] = geoname;
				finalDict [String.Format("{0}/{1}", country.ISO3, geoname.asciiname)] = geoname;
				finalDict [String.Format("{0}/{1}", country.ISONumeric, geoname.asciiname)] = geoname;
				finalDict [String.Format("{0}/{1}", country.Country, geoname.asciiname)] = geoname;
			}
		}

        public GeoName GetEntry(string strCountry, string strCity)
        {
            try
            {
                return finalDict[String.Format("{0}/{1}", strCountry, strCity)];
            }
            catch (Exception ex)
            {
                return null;
            }
        }

		public string GetTimezone(string strCountry, string strCity){
			try {
				return finalDict[String.Format("{0}/{1}", strCountry, strCity)].timezone.TimeZoneId;
			} catch(Exception ex){
                return null;
			}
		}
		public string GetGMT(string strCountry, string strCity){
			try {
				return finalDict[String.Format("{0}/{1}", strCountry, strCity)].timezone.GMT;
			} catch(Exception ex){
                return null;
			}
		}

		public string ReadFile(string filename) {
			return System.IO.File.ReadAllText(filename);
		}
	}
}

