using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Growth Page Layout")]
public sealed class GrowthPageLayout:ScriptableObject {
    public GrowthWorkspaceView Workspace;
    public GrowthNodeView Node;
    public UnityEngine.UI.Image Connection;
    public GrowthTreeArrangement Arrangement;
    public Color LearnedFill=new Color(.25f,.2f,.08f),LockedFill=new Color(.07f,.12f,.18f);
    public Color LearnedBorder=new Color(.95f,.74f,.3f),AvailableBorder=new Color(.5f,.65f,.7f),LockedBorder=new Color(.3f,.36f,.42f);
    public Color LearnedConnection=new Color(.65f,.51f,.25f);
    [Min(0)] public float HorizontalPadding=80,TopPadding=54,BottomPadding=96,ConnectionInset=28;
}
