using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Geocoding.BAN
{
    /// <remarks>
    /// http://adresse.data.gouv.fr/api/
    /// </remarks>
    public class BANGeocoder : IGeocoder, IAsyncGeocoder
    {

        const string BAN_BASE_URL = "http://api-adresse.data.gouv.fr/search/";

        public BANGeocoder() { }

        public BANGeocoder(int limit)
            : this()
        {
            Limit = limit;
        }


        public IWebProxy Proxy { get; set; }

        public int? Limit { get; set; }

        private Uri AddQuery(Uri uri, string name, string value)
        {
            var ub = new UriBuilder(uri);
            var httpValueCollection = HttpUtility.ParseQueryString(uri.Query);
            httpValueCollection.Add(name, value);
            ub.Query = httpValueCollection.ToString();
            return ub.Uri;
        }

        public Uri ServiceUri
        {
            get
            {
                Uri url = new Uri(BAN_BASE_URL);
                if (Limit.HasValue)
                    url = AddQuery(url, "limit", Limit.Value.ToString());
                return url;
            }
        }

        public IEnumerable<BANAddress> Geocode(string address)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException("address");

            HttpWebRequest request = BuildWebRequest("q", HttpUtility.UrlEncode(address));
            return ProcessRequest(request);
        }

        public IEnumerable<BANAddress> ReverseGeocode(Location location)
        {
            if (location == null)
                throw new ArgumentNullException("location");

            return ReverseGeocode(location.Latitude, location.Longitude);
        }

        public IEnumerable<BANAddress> ReverseGeocode(double latitude, double longitude)
        {
            throw new NotImplementedException("BAN reverse geocoding not available yet !");
        }

        public Task<IEnumerable<BANAddress>> GeocodeAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException("address");

            HttpWebRequest request = BuildWebRequest("address", HttpUtility.UrlEncode(address));
            return ProcessRequestAsync(request);
        }

        public Task<IEnumerable<BANAddress>> GeocodeAsync(string address, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException("address");

            HttpWebRequest request = BuildWebRequest("address", HttpUtility.UrlEncode(address));
            return ProcessRequestAsync(request, cancellationToken);
        }

        public Task<IEnumerable<BANAddress>> ReverseGeocodeAsync(double latitude, double longitude)
        {
            throw new NotImplementedException("BAN reverse geocoding not available yet !");
        }

        public Task<IEnumerable<BANAddress>> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("BAN reverse geocoding not available yet !");
        }

        private string BuildAddress(string street, string city, string state, string postalCode, string country)
        {
            return string.Format("{0} {1}, {2} {3}, {4}", street, city, state, postalCode, country);
        }

        private string BuildGeolocation(double latitude, double longitude)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0},{1}", latitude, longitude);
        }

        private IEnumerable<BANAddress> ProcessRequest(HttpWebRequest request)
        {
            using (WebResponse response = request.GetResponse())
            {
                return ProcessWebResponse(response);
            }
        }

        private Task<IEnumerable<BANAddress>> ProcessRequestAsync(HttpWebRequest request, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken != null)
            {
                cancellationToken.Value.ThrowIfCancellationRequested();
                cancellationToken.Value.Register(() => request.Abort());
            }

            var requestState = new RequestState(request, cancellationToken);
            return Task.Factory.FromAsync(
                (callback, asyncState) => SendRequestAsync((RequestState)asyncState, callback),
                result => ProcessResponseAsync((RequestState)result.AsyncState, result),
                requestState
            );
        }

        private IAsyncResult SendRequestAsync(RequestState requestState, AsyncCallback callback)
        {
            return requestState.request.BeginGetResponse(callback, requestState);
        }

        private IEnumerable<BANAddress> ProcessResponseAsync(RequestState requestState, IAsyncResult result)
        {
            if (requestState.cancellationToken != null)
                requestState.cancellationToken.Value.ThrowIfCancellationRequested();

            using (var response = (HttpWebResponse)requestState.request.EndGetResponse(result))
            {
                return ProcessWebResponse(response);
            }
        }

        IEnumerable<Address> IGeocoder.Geocode(string address)
        {
            return Geocode(address).Cast<Address>();
        }

        IEnumerable<Address> IGeocoder.Geocode(string street, string city, string state, string postalCode, string country)
        {
            return Geocode(BuildAddress(street, city, state, postalCode, country)).Cast<Address>();
        }

        IEnumerable<Address> IGeocoder.ReverseGeocode(Location location)
        {
            return ReverseGeocode(location).Cast<Address>();
        }

        IEnumerable<Address> IGeocoder.ReverseGeocode(double latitude, double longitude)
        {
            return ReverseGeocode(latitude, longitude).Cast<Address>();
        }

        Task<IEnumerable<Address>> IAsyncGeocoder.GeocodeAsync(string address)
        {
            return GeocodeAsync(address)
                .ContinueWith(task => task.Result.Cast<Address>());
        }

        Task<IEnumerable<Address>> IAsyncGeocoder.GeocodeAsync(string address, CancellationToken cancellationToken)
        {
            return GeocodeAsync(address, cancellationToken)
                .ContinueWith(task => task.Result.Cast<Address>(), cancellationToken);
        }

        Task<IEnumerable<Address>> IAsyncGeocoder.GeocodeAsync(string street, string city, string state, string postalCode, string country)
        {
            return GeocodeAsync(BuildAddress(street, city, state, postalCode, country))
                .ContinueWith(task => task.Result.Cast<Address>());
        }

        Task<IEnumerable<Address>> IAsyncGeocoder.GeocodeAsync(string street, string city, string state, string postalCode, string country, CancellationToken cancellationToken)
        {
            return GeocodeAsync(BuildAddress(street, city, state, postalCode, country), cancellationToken)
                .ContinueWith(task => task.Result.Cast<Address>(), cancellationToken);
        }

        Task<IEnumerable<Address>> IAsyncGeocoder.ReverseGeocodeAsync(double latitude, double longitude)
        {
            return ReverseGeocodeAsync(latitude, longitude)
                .ContinueWith(task => task.Result.Cast<Address>());
        }

        Task<IEnumerable<Address>> IAsyncGeocoder.ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken)
        {
            return ReverseGeocodeAsync(latitude, longitude, cancellationToken)
                .ContinueWith(task => task.Result.Cast<Address>(), cancellationToken);
        }

        private HttpWebRequest BuildWebRequest(string type, string value)
        {
            Uri uri = AddQuery(ServiceUri, type, value);


            var req = WebRequest.Create(uri) as HttpWebRequest;
            if (this.Proxy != null)
            {
                req.Proxy = Proxy;
            }
            req.Method = "GET";
            return req;
        }

        private IEnumerable<BANAddress> ProcessWebResponse(WebResponse response)
        {

            HttpWebResponse httpResponse = response as HttpWebResponse;

            if ((int)httpResponse.StatusCode >= 300) //error
                throw new BANGeocodingException(httpResponse.StatusCode, httpResponse.StatusDescription);

            string jsonTextResponse = LoadStringResponse(httpResponse);

            if (!string.IsNullOrWhiteSpace(jsonTextResponse))
                return ParseAddresses(jsonTextResponse).ToArray();

            return new BANAddress[0];
        }

        private string LoadStringResponse(WebResponse response)
        {
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private IEnumerable<BANAddress> ParseAddresses(string jsonTextResponse)
        {

            GeoJSON.Net.Feature.FeatureCollection featureCollection = JsonConvert.DeserializeObject<GeoJSON.Net.Feature.FeatureCollection>(jsonTextResponse);

            //new BANAddress("", null, 0);

            List<BANAddress> list = new List<BANAddress>();

            foreach (GeoJSON.Net.Feature.Feature feature in featureCollection.Features.Where(f => f.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.Point).OrderByDescending(f => f.Properties["score"]))
            {
                GeoJSON.Net.Geometry.Point point = feature.Geometry as GeoJSON.Net.Geometry.Point;
                GeoJSON.Net.Geometry.GeographicPosition position = point.Coordinates as GeoJSON.Net.Geometry.GeographicPosition;

                double latitude = position.Latitude;
                double longitude = position.Longitude;

                double score = (double)feature.Properties["score"];
                string label = (string)feature.Properties["label"];

                Location coordinates = new Location(latitude, longitude);
                list.Add(new BANAddress(label, coordinates, score));
            }

            return list;

        }

        protected class RequestState
        {
            public readonly HttpWebRequest request;
            public readonly CancellationToken? cancellationToken;

            public RequestState(HttpWebRequest request, CancellationToken? cancellationToken)
            {
                if (request == null) throw new ArgumentNullException("request");

                this.request = request;
                this.cancellationToken = cancellationToken;
            }
        }
    }
}
