using System;
using System.Collections.Generic;
using UnityEngine;

public enum DetailSection { Name, Artwork, Description, Availability, Mastery, Attacks, Price, Stats, Materials, Upgrade, Notice, Custom }
[CreateAssetMenu(menuName="Game/UI/Details Panel Layout")]
public sealed class DetailsPanelLayout : ScriptableObject
{
    [Serializable] public sealed class Section
    {
        public DetailSection Binding;
        public bool Visible=true;
        public DetailsSectionView Prefab;
        [TextArea] public string CustomText;
    }
    [Min(0)] public float Spacing=10;
    public RectOffset Padding;
    [Tooltip("Drag rows to reorder. Missing data is hidden. Use each binding once; Custom can repeat.")]
    public List<Section> Sections=new();
    void OnEnable(){Sections??=new();Padding??=new RectOffset(12,12,12,12);}
}
