namespace PetOffline.Core
{
    public static class SceneNames
    {
        public const string Bootstrap = "00_Bootstrap";
        public const string Day1 = "10_Day1_Meeting";
        public const string Day2 = "20_Day2_Sunbath";
        public const string UIRootTest = "90_UIRoot_Test";

        public const string BootstrapPath = "Assets/PetOffline/Scenes/00_Bootstrap.unity";
        public const string Day1Path = "Assets/PetOffline/Scenes/10_Day1_Meeting.unity";
        public const string Day2Path = "Assets/PetOffline/Scenes/20_Day2_Sunbath.unity";
        public const string UIRootTestPath = "Assets/PetOffline/Scenes/90_UIRoot_Test.unity";

        public static string GetWorldScene(LevelId level) => level switch
        {
            LevelId.Day1 => Day1,
            LevelId.Day2 => Day2,
            _ => string.Empty
        };
    }
}
