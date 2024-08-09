using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Models;

namespace TrackmaniaRandomMapServer
{
    public class RandomMapService
    {
        public Queue<TmxMap> DownloadedMaps = new();

        public void DownloadMaps(int count)
        {

        }
    }
}
