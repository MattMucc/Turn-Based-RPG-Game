using UnityEngine;
using TMPro;
public sealed class CheckpointMenuView:MonoBehaviour {
 public TMP_Text Title,Warning;
 public GameObject Options,Confirmation;
 public InventoryButtonView Rest,Save,Leave,Confirm,Cancel;
 public void Present(string title,bool confirmation,bool available){
  Title.text=title;Options.SetActive(!confirmation||!available);Confirmation.SetActive(confirmation&&available);
  Rest.Select.interactable=available;Save.Select.interactable=available;
 }
}
