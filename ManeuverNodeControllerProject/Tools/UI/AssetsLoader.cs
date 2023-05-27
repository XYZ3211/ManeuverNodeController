
using UnityEngine;
using SpaceWarp.API.Assets;
using static KSP.Map.impl.Targeting.Sample;
using ManeuverNodeController;

namespace ManeuverNodeController.UI
{
    public class AssetsLoader
    {
// BEPEXVersion
        public static Texture2D loadIcon(string path)
        {
            string full_path = $"{ManeuverNodeControllerMod.Instance.Info.Metadata.GUID}/images/{path}.png";
            var imageTexture = AssetManager.GetAsset<Texture2D>(full_path);

            //   Check if the texture is null
            if (imageTexture == null)
            {
                // Print an error message to the Console
                Debug.LogError("Failed to load image texture from path: " + path);

                // Print the full path of the resource
                Debug.Log("Full resource path: " + Application.dataPath + "/" + path);

                // Print the type of resource that was expected
                Debug.Log("Expected resource type: Texture2D");
            }

            return imageTexture;
        }
    }
}