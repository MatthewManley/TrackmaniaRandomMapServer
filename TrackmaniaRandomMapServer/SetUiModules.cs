using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer
{
    public class SetUiModules
    {
        [JsonProperty("uimodules")]
        public List<UiModule> UiModules { get; set; }

    }
}
