using Godot;

/// <summary>
/// Central scene path registry. All ChangeSceneToFile calls must go through
/// these constants so renames only require changing one file.
///
/// Game flow:  Home -> School -> FriendCafe -> GasStation -> Ending
/// </summary>
public static class SceneFlow
{
    public const string Home       = "res://Home.tscn";
    public const string School     = "res://School.tscn";
    public const string FriendCafe = "res://map.tscn";
    public const string GasStation = "res://GasStation.tscn";
    public const string CafeWork   = "res://CafeWork.tscn";
    public const string Ending     = "res://Run.tscn";
}
