using UnityEngine;
using UnityEngine.UI;
using TMPro;
public sealed class SaveIndicatorView:MonoBehaviour {
 public Image Icon;
 public TMP_Text Label;
 public Color BusyColor=new Color(.5f,.85f,1),IdleColor=Color.white;
 [Min(0)] public float PulseSpeed=6;
 [Range(0,1)] public float MinimumAlpha=.3f;
 public void Present(string message,bool busy,float time){
  Label.text=message;var color=busy?BusyColor:IdleColor;if(busy)color.a*=Mathf.Lerp(MinimumAlpha,1,(1+Mathf.Sin(time*PulseSpeed))*.5f);Icon.color=color;
 }
}
