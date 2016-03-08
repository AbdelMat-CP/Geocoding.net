using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GeocodingConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            if (!CheckArgs(args))
                return;

            Console.WriteLine("Start");

            if (args.Length > 1 && args[0].ToUpper() == "-F")
            {
                // Batch file mode
                GeocodeMany(args[1]);
            }
            else
            {
                // Unitary mode
                GeocodeOne(args[0]);
            }

            



            Console.WriteLine("End");

        }

        private static void GeocodeMany(string inputFile)
        {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Join(";", new string[] { "ADDRESS", "GOOGLAT", "GOOGLONG", "BANLAT", "BANLONG", "DISTANCE", "URL" }));

            foreach (string address in File.ReadLines(inputFile))
            {
                Geocoding.Address googAddress = TestGeocoder("GOOGLE", address, true);

                Geocoding.Address banAddress = TestGeocoder("BAN", address, true);

                System.Threading.Thread.Sleep(150);

                Geocoding.Distance? distance = null;
                string googleMap2PointsUrl = "-";

                if (googAddress != null && banAddress != null)
                {
                    distance = banAddress.DistanceBetween(googAddress, Geocoding.DistanceUnits.Kilometers);
                    googleMap2PointsUrl = GetGoogleMap2PointsUrl(banAddress.Coordinates, googAddress.Coordinates);
                }

                List<string> values = new List<string>();
                values.Add(address);
                if (googAddress != null)
                {
                    values.Add(googAddress.Coordinates.Latitude.To6DecimalsPrecisionString());
                    values.Add(googAddress.Coordinates.Longitude.To6DecimalsPrecisionString());
                }
                else
                {
                    values.Add("-");
                    values.Add("-");
                }
                if (banAddress != null)
                {
                    values.Add(banAddress.Coordinates.Latitude.To6DecimalsPrecisionString());
                    values.Add(banAddress.Coordinates.Longitude.To6DecimalsPrecisionString());
                }
                else
                {
                    values.Add("-");
                    values.Add("-");
                }
                if (distance.HasValue)
                {
                    values.Add(distance.Value.Value.To6DecimalsPrecisionString());
                }
                else
                {
                    values.Add("-");
                }
                values.Add(googleMap2PointsUrl);

                sb.AppendLine(string.Join(";", values.ToArray()));

            }

            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile) + ".geocoded" + Path.GetExtension(inputFile));
            File.AppendAllText(outputFile, sb.ToString());

        }

        private static void GeocodeOne(string address)
        {
            Geocoding.Address googAddress = TestGeocoder("GOOGLE", address, true);
            Display(true, "");
            Geocoding.Address banAddress = TestGeocoder("BAN", address, true);
            Display(true, "");

            if (googAddress != null && banAddress != null)
            {
                Geocoding.Distance distance = banAddress.DistanceBetween(googAddress, Geocoding.DistanceUnits.Kilometers);
                Display(true, "Distance in meters between BAN and GOOGLE points is '{0}'...", distance.Value * 1000);
                Display(true, "");
                Display(true, "");

                string googleMap2PointsUrl = GetGoogleMap2PointsUrl(banAddress.Coordinates, googAddress.Coordinates);

                Display(true, "Showing the google map with the two points... '{0}'", googleMap2PointsUrl);

                System.Diagnostics.Process.Start(googleMap2PointsUrl);

                Display(true, "");
                Display(true, "");
                Display(true, "");
            }
        }

            private static bool CheckArgs(string[] args)
        {
            if (args.Length == 0)
            {
                Display(true, "You must call this tool with at least one argument for the target address to geocode or -f <Text file with adresses>...");
            }

            return args.Length > 0;
        }

        private static string GetGoogleMap2PointsUrl(Geocoding.Location banLocation, Geocoding.Location googLocation)
        {
            const string googleMap2PointsUrl_Format = "https://www.google.fr/maps/dir/'{banLat},{banLng}'/{googLat},{googLng}/@{banLat},{banLng}";

            string url = googleMap2PointsUrl_Format;

            url = url.Replace("{banLat}", banLocation.Latitude.To6DecimalsPrecisionString());
            url = url.Replace("{banLng}", banLocation.Longitude.To6DecimalsPrecisionString());
            url = url.Replace("{googLat}", googLocation.Latitude.To6DecimalsPrecisionString());
            url = url.Replace("{googLng}", googLocation.Longitude.To6DecimalsPrecisionString());
            return url;
        }

        private static Geocoding.Address TestGeocoder(string geocoderType, string address, bool disp)
        {

            Display(disp, "Starting some '{0}' geocoding for '{1}'...", geocoderType, address);
            Display(disp, "");

            Geocoding.IGeocoder geocoder = null;
            if (geocoderType == "GOOGLE")
            {
                geocoder = new Geocoding.Google.GoogleGeocoder();
            }
            if (geocoderType == "BAN")
            {
                geocoder = new Geocoding.BAN.BANGeocoder(2);
            }

            geocoder.Proxy = System.Net.WebRequest.GetSystemWebProxy();
            geocoder.Proxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

            IEnumerable<Geocoding.Address> addresses = null;

            try
            {
                addresses = geocoder.Geocode(address);
            }
            catch (Geocoding.Google.GoogleGeocodingException ex)
            {
                Display(true, "Address : '{0}', Geocoder '{1}' : Error occured : '{2}' --> Google Status : '{3}'", address, geocoderType, ex.Message, ex.Status);
                if (ex.Status == Geocoding.Google.GoogleStatus.OverQueryLimit)
                    Display(true, "Okay we are over for the google quota limit...");
            }
            catch (Geocoding.BAN.BANGeocodingException ex)
            {
                Display(true, "Address : '{0}', Geocoder '{1}' : Error occured : '{2}' --> Http Status Code : '{3}'", address, geocoderType, ex.Message, ex.Status);
            }
            catch (Exception ex)
            {
                Display(true, "Address : '{0}', Geocoder '{1}' : General error occured : '{2}'", address, geocoderType, ex.Message);
            }

            if (addresses == null)
            {
                Display(disp, "Address : '{0}', Geocoder '{1}' : Nothing !!!", address, geocoderType);
            }
            else
            {
                Display(disp, "Address : '{0}', Geocoder '{1}' : raw output : '{2}'", address, geocoderType, addresses.ToJsonString());
            }

            return addresses.FirstOrDefault();

        }

        private static void Display(bool display, string message , params object[] args)
        {
            if (display)
                Console.WriteLine(message, args);
        }

    }
}
