using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer
{
    public enum RmtPosition
    {
        /// <summary>
        /// Players are in the hub, start rmt button available
        /// </summary>
        NotStartedHub,

        /// <summary>
        /// Players are in the hub, have clicked start rmt button, waiting for new round
        /// </summary>
        StartedHub,

        /// <summary>
        /// New map has loaded, but not started yet
        /// </summary>
        Preround,

        /// <summary>
        /// Players are in a round
        /// </summary>
        InRound,

        /// <summary>
        /// Players are viewing the scoreboard after beating or skipping a map, RMT is not over
        /// </summary>
        PostRound,

        // TODO: Does this really need to exist? Can we just use PostRound?
        /// <summary>
        /// RMT is over, players are viewing the scoreboard, waiting to return to the hub
        /// </summary>
        EndedScoreboard,
    }
}
