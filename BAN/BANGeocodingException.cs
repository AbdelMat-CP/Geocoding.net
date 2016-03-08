using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Geocoding.BAN
{
    public class BANGeocodingException : Exception
    {
        public HttpStatusCode Status { get; set; }

        public BANGeocodingException(HttpStatusCode status, string message)
			: base(message)
		{
            this.Status = status;
		}

    }
}
