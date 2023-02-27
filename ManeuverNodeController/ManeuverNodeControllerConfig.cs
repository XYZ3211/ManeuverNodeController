using SpaceWarp.API.Configuration;
using Newtonsoft.Json;

namespace ManeuverNodeController
{
    [JsonObject(MemberSerialization.OptOut)]
    [ModConfig]
    public class ManeuverNodeControllerConfig
    {
    }
}