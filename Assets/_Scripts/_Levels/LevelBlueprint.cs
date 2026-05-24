using UnityEngine;

[System.Serializable]
public class LevelBlueprint
{
    public string levelName;
    public int width;
    public int depth;
    public float time;

    [TextArea(10, 20)] // This makes a big box in the Inspector
    public string layout;
}