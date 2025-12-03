#if UNITY_EDITOR
using UnityEditor;
using static Twinny.Editor.TESceneTemplateService;
namespace Twinny.Editor
{
    public static class OpenXRMenuItems
    {
        [MenuItem("Twinny/Create Scene/OpenXR/Singleplayer Platform Scene")]
        public static void CreatePlatformScene() => CreateTemplateScene("317d9c92fa08a7845a55357510324d34",false);

        [MenuItem("Twinny/Create Scene/OpenXR/Multiplayer Platform Scene")]
        public static void CreateMultiplayerScene() => CreateTemplateScene("6c3205d36907e764fb4f8ffd88c47b00",false);

        [MenuItem("Twinny/Create Scene/OpenXR/Start Scene")]
        public static void CreateStartScene() => CreateTemplateScene("6258111892133dd4387edda2217d3f76",false);

        [MenuItem("Twinny/Create Scene/OpenXR/Mockup Scene")]
        public static void CreateMockupScene() => CreateTemplateScene("2c8bc8e35bfb84043b57203c53801f40",false);

        [MenuItem("Twinny/Create Scene/OpenXR/Tour Scene")]
        public static void CreateTourScene() => CreateTemplateScene("056bfcc19be77904897eb96d98297a98",false);

        [MenuItem("Twinny/Create Scene/OpenXR/Experience Scene")]
        public static void CreateExperienceScene() => CreateTemplateScene("da733818688948b4ab2ba37afea83d12",false);

       
    }
}
#endif