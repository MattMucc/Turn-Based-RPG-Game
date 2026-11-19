using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Player Menu Shell Layout")]
public sealed class PlayerMenuShellLayout:ScriptableObject {
 public PlayerMenuShellView Prefab;
 public Vector2 ReferenceResolution=new Vector2(1280,720);
 public Color SelectedPageColor=new Color(.16f,.36f,.44f);
}
