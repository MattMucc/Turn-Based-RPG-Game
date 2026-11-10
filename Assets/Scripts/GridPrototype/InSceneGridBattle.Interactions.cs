using UnityEngine;
namespace Game.GridPrototype
{
    public sealed partial class InSceneGridBattle
    {
        public bool CanUseObject(BattleTarget target)
        {
            if(!PlayerReady || ChosenAction!=null || target==null || !target.Alive || !battleTargets.Contains(target))return false;
            var usable=target.Transform.GetComponent<BattleUsable>();
            if(!usable || !usable.Available || (usable.CostsAction && !Active.ActionAvailable))return false;
            foreach(var cell in TargetCells(target))
                if(Mathf.Max(Mathf.Abs(cell.x-Active.Position.x),Mathf.Abs(cell.y-Active.Position.y))<=Mathf.Max(1,usable.Reach) && ObjectInSight(target,cell))return true;
            return false;
        }
        bool ObjectInSight(BattleTarget target, Vector2Int cell)
        {
            Vector3 start=Grid.Point(Active.Position)+Vector3.up, end=Grid.Point(cell)+Vector3.up;
            Vector3 delta=end-start;
            foreach(var hit in Physics.RaycastAll(start,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(hit.transform!=target.Transform && !hit.transform.IsChildOf(target.Transform))return false;
            return true;
        }
        public bool UseObject(BattleTarget target)
        {
            if(!CanUseObject(target))return false;
            var usable=target.Transform.GetComponent<BattleUsable>();
            if(usable.CostsAction && !Active.SpendAction())return false;
            usable.TryUse();
            if(Grid!=null && !closing){Physics.SyncTransforms();Grid.RefreshSurfaces();RebuildOverlay();Refresh();}
            ActionResolved?.Invoke();return true;
        }
    }
}
