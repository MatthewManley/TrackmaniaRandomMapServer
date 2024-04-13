using GbxRemoteNet;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Events;

namespace TrackmaniaRandomMapServer
{
    public class TrackmaniaRemoteClient : GbxRemoteNet.GbxRemoteClient
    {
        public TrackmaniaRemoteClient(string host, int port, ILogger logger = null) : base(host, port, logger)
        {
            this.OnModeScriptCallback += TrackmaniaRemoteClient_OnModeScriptCallback;
        }

        public TrackmaniaRemoteClient(string host, int port, GbxRemoteClientOptions options, ILogger logger = null) : base(host, port, options, logger)
        {
            this.OnModeScriptCallback += TrackmaniaRemoteClient_OnModeScriptCallback;
        }

        public delegate Task ManiaplanetEndMapHandler(object sender, ManiaplanetEndMap e);
        public delegate Task ManiaplanetStartMapHandler(object sender, ManiaplanetStartMap e);
        public delegate Task ManiaplanetTimeHandler(object sender, ManiaplanetTime e);
        public delegate Task TrackmaniaStartlineHandler(object sender, TrackmaniaStartline e);
        public delegate Task TrackmaniaEventWaypointHandler(object sender, TrackmaniaWaypoint e);


        public event ManiaplanetEndMapHandler OnEndMapStart;
        public event ManiaplanetEndMapHandler OnEndMapEnd;
        public event ManiaplanetStartMapHandler OnStartMapStart;
        public event ManiaplanetStartMapHandler OnStartMapEnd;
        public event ManiaplanetTimeHandler OnPodiumStart;
        public event ManiaplanetTimeHandler OnPodiumEnd;
        public event TrackmaniaStartlineHandler OnStartline;
        public event TrackmaniaEventWaypointHandler OnWaypoint;

        private async Task TrackmaniaRemoteClient_OnModeScriptCallback(string method, JObject data)
        {
            switch (method)
            {
                case "Maniaplanet.EndMap_Start":
                    {
                        var deserializedData = data.ToObject<ManiaplanetEndMap>();
                        if (OnEndMapStart != null)
                            await OnEndMapStart(this, deserializedData);
                        break;
                    }
                case "Maniaplanet.EndMap_End":
                    {
                        var deserializedData = data.ToObject<ManiaplanetEndMap>();
                        if (OnEndMapEnd != null)
                            await OnEndMapEnd(this, deserializedData);
                        break;
                    }
                case "Maniaplanet.Podium_Start":
                    {
                        var deserializedData = data.ToObject<ManiaplanetTime>();
                        if (OnPodiumStart != null)
                            await OnPodiumStart(this, deserializedData);
                        break;
                    }
                case "Maniaplanet.Podium_End":
                    {
                        var deserializedDataq = data.ToObject<ManiaplanetTime>();
                        if (OnPodiumEnd != null)
                            await OnPodiumEnd(this, deserializedDataq);
                        break;
                    }
                case "Trackmania.Event.StartLine":
                    {
                        var deserializedData = data.ToObject<TrackmaniaStartline>();
                        if (OnStartline != null)
                            await OnStartline(this, deserializedData);
                        break;
                    }
                case "Trackmania.Event.WayPoint":
                    {
                        var deserializedData = data.ToObject<TrackmaniaWaypoint>();
                        if (OnWaypoint != null)
                            await OnWaypoint(this, deserializedData);
                        break;
                    }
                case "Maniaplanet.StartMap_Start":
                    {
                        var deserializedData = data.ToObject<ManiaplanetStartMap>();
                        if (OnStartMapStart != null)
                            await OnStartMapStart(this, deserializedData);
                        break;
                    }
                case "Maniaplanet.StartMap_End":
                    {
                        var deserializedData = data.ToObject<ManiaplanetStartMap>();
                        if (OnStartMapEnd != null)
                            await OnStartMapEnd(this, deserializedData);
                        break;
                    }
            }
        }
    }
}
