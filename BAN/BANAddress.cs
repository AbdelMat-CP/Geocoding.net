using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Geocoding.BAN
{
    public class BANAddress : Address
    {

        readonly double score;

        public double Score
        {
            get { return score; }
        }

        public BANAddress(string formattedAddress, Location coordinates, double score)
	        : base(formattedAddress, coordinates, "BAN")
	    {
            this.score = score;

	    }


    }
}
