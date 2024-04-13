using Newtonsoft.Json;
using System.Collections.Generic;

namespace TrackmaniaRandomMapServer.Models
{
    public class SetUiModules
    {
        [JsonProperty("uimodules")]
        public List<UiModule> UiModules { get; set; }

    }
}
