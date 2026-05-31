#if UNITY_EDITOR
using UnityEditor;

public static class MainMenuSetupBatch
{
    public static void Execute()
    {
        MainMenuSetupEditor.RunSetupBatch();
    }
}
#endif
