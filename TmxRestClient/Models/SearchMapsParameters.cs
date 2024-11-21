using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackmaniaExchangeAPI.Models
{
    /// <summary>
    /// https://api2.mania.exchange/Method/Index/4
    /// </summary>
    public class SearchMapsParameters
    {
        // TODO: implement missing fields

        /// <summary>
        /// if set to 1 then return random value
        /// </summary>
        public int? Random { get; set; }

        public int[]? ExcludedTags { get; set; }

        /// <summary>
        /// Multiplied by 15 seconds (i think)
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// Operator for the length filter
        /// </summary>
        public LengthOp? LengthOp { get; set; }
    }
}
