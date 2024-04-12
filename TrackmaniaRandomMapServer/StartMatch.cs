using GbxRemoteNet.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer
{
    internal class StartMatch
    {
        [JsonProperty("count")]
        public int Count;

        [JsonProperty("valid")]
        public int Valid;

        [JsonProperty("restarted")]
        public bool Restarted;

        [JsonProperty("time")]
        public int Time;

        [JsonProperty("map")]
        public TmSMapInfo? Map;
    }
}
