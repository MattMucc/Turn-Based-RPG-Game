using UnityEngine;
public static class PlayerMenuShellPresentation {
 public static void Apply(PlayerMenuShellView view,PlayerMenuShellView template,bool service,bool character){
  view.Brand.gameObject.SetActive(!service);view.CommandRail.gameObject.SetActive(!service);view.Exit.gameObject.SetActive(!service);
  view.Character.gameObject.SetActive(character);view.Character.Name.gameObject.SetActive(character);
  Copy(service?template.ServiceBody:template.Body,view.Body);Copy(service?template.ServiceTitle:template.Title.rectTransform,view.Title.rectTransform);
 }
 static void Copy(RectTransform source,RectTransform target){target.anchorMin=source.anchorMin;target.anchorMax=source.anchorMax;target.pivot=source.pivot;target.anchoredPosition=source.anchoredPosition;target.sizeDelta=source.sizeDelta;}
}
