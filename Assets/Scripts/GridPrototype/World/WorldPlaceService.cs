using UnityEngine;
using Game.GridPrototype;
namespace Game.World
{
    // No timers: the world driver calls pending-return checks only outside combat.
    public sealed class WorldPlaceService : MonoBehaviour
    {
        public WorldPlaceObject ActiveObject { get; private set; }
        public bool PendingReturn { get; private set; }
        [Min(.1f)] public float DragDistance = 3;
        [Min(.1f)] public float DragReach = 4;
        public string Message { get; private set; } = "Choose an object.";
        bool Fail(string reason) { Message = reason; return false; }
        static bool Person(Collider c) => c is CharacterController || c.GetComponentInParent<BattleCharacterInfo>();
        public static bool Occupied(WorldPlaceObject obj)
        {
            var b = obj.Volume(obj.transform.position);
            // Include feet on the top and actors currently crossing the volume.
            b.Expand(new Vector3(.16f, .8f, .16f));
            foreach(var c in Physics.OverlapBox(b.center, b.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (!c.transform.IsChildOf(obj.transform) && Person(c)) return true;
            return false;
        }
        public static bool ClearVolume(WorldPlaceObject obj, Vector3 position, out string reason)
        {
            var b = obj.Volume(position); var extents = Vector3.Max(Vector3.one*.01f, b.extents-Vector3.one*.025f);
            foreach(var c in Physics.OverlapBox(b.center, extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (!c.transform.IsChildOf(obj.transform)) { reason = "That space is occupied."; return false; }
            reason = ""; return true;
        }
        public static bool Supported(WorldPlaceObject obj, Vector3 position)
        {
            var b = obj.Volume(position);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                var foot = new Vector3(b.center.x+x*b.extents.x*.9f,b.min.y,b.center.z+z*b.extents.z*.9f);
                bool support = false;
                foreach(var hit in Physics.RaycastAll(foot+Vector3.up*.15f,Vector3.down,.35f,~0,QueryTriggerInteraction.Ignore))
                    if (!hit.collider.transform.IsChildOf(obj.transform) && !Person(hit.collider)
                        && hit.collider.gameObject.layer != LayerMask.NameToLayer("Water") && Vector3.Angle(hit.normal,Vector3.up)<35) { support=true; break; }
                if (!support) return false;
            }
            return true;
        }
        bool CanReturn(out string reason)
        {
            reason=""; if (!ActiveObject) return true;
            if (Occupied(ActiveObject)) { reason="Someone is on or crossing the structure. Move them clear first."; return false; }
            return !ActiveObject.Displaced || ClearVolume(ActiveObject, ActiveObject.ReturnPosition, out reason);
        }
        bool CanReplace(WorldPlaceObject obj)
        {
            if (ActiveObject == obj) return true;
            return CanReturn(out var reason) || Fail(reason);
        }
        void Claim(WorldPlaceObject obj)
        {
            if (ActiveObject && ActiveObject != obj) ActiveObject.Revert();
            obj.RememberOrigin(); ActiveObject=obj; PendingReturn=false;
        }
        bool InRange(WorldPlaceObject obj, Transform caster, float range)
            => obj && caster && Vector3.Distance(caster.position, obj.Volume(obj.transform.position).ClosestPoint(caster.position)) <= range;
        public bool TryRestore(WorldPlaceObject obj, Transform caster, float range)
        {
            if (!obj || !obj.PastForm) return Fail("This object has no authored past form.");
            if (!InRange(obj,caster,range)) return Fail("Outside restoration range.");
            if (obj.Restored) return TryDismiss();
            if (Occupied(obj)) return Fail("Move everyone clear before restoring this structure.");
            if (!ClearVolume(obj,obj.transform.position,out var reason)) return Fail(reason);
            if (!CanReplace(obj)) return false;
            Claim(obj); obj.SetRestored(true); Message="Past form restored. No time limit."; return true;
        }
        public bool PreviewMove(WorldPlaceObject obj, Vector3 destination, Transform caster, int strength, float range, bool magical, out string reason)
        {
            reason="";
            if (!obj || !caster) { reason="Choose an object."; return false; }
            if (magical ? !obj.CanRelocate : !obj.CanDrag) { reason="This structure cannot be moved that way."; return false; }
            if (Occupied(obj)) { reason="Someone is standing on or crossing it."; return false; }
            if (!magical && obj.HasEffect) { reason="Dismiss Place's effect before ordinary dragging."; return false; }
            if (!InRange(obj,caster,magical ? range : DragReach)) { reason="Move closer to the object."; return false; }
            if (magical && Vector3.Distance(caster.position,destination)>range) { reason="Destination is outside Place's range."; return false; }
            if (!magical && strength<obj.RequiredStrength) { reason=$"Requires {obj.RequiredStrength} Strength (you have {strength})."; return false; }
            float distance=Vector3.Distance(obj.transform.position,destination);
            if (!magical && distance>DragDistance+.001f) { reason=$"Maximum {DragDistance:0.#} m per drag."; return false; }
            if (!ClearVolume(obj,destination,out reason)) return false;
            if (!Supported(obj,destination)) { reason="Needs a supported, reasonably level surface."; return false; }
            if (!magical)
            {
                int steps=Mathf.Max(1,Mathf.CeilToInt(distance/.1f));
                for(int i=1;i<=steps;i++)
                {
                    var p=Vector3.Lerp(obj.transform.position,destination,i/(float)steps);
                    if (!ClearVolume(obj,p,out reason)) return false;
                    if (!Supported(obj,p)) { reason="Ordinary dragging cannot cross a gap or step."; return false; }
                }
            }
            return true;
        }
        public bool TryMove(WorldPlaceObject obj, Vector3 destination, Transform caster, int strength, float range, bool magical, WorldPlaceSocket socket=null)
        {
            if (socket && (!socket.Accepts(obj) || Vector3.Distance(destination,socket.Position)>.05f)) return Fail("The object does not fit that socket.");
            if (!PreviewMove(obj,destination,caster,strength,range,magical,out var reason)) return Fail(reason);
            if (magical && !CanReplace(obj)) return false;
            if (magical) { Claim(obj); obj.Relocate(destination); }
            else { obj.transform.position=destination; Physics.SyncTransforms(); }
            if(socket)socket.Activate(obj);
            Message=magical ? "Relocated until dismissed. Original position reserved for return." : "Object moved.";
            return true;
        }
        public bool TryDismiss()
        {
            if (!ActiveObject) return Fail("No active Place effect.");
            if (!CanReturn(out var reason)) { PendingReturn=true; return Fail(reason+" Return pending until clear."); }
            ActiveObject.Revert(); ActiveObject=null; PendingReturn=false; Message="Present-day state restored."; return true;
        }
        public void TickPendingReturn() { if(!ActiveObject) PendingReturn=false; else if(PendingReturn) TryDismiss(); }
    }
}
