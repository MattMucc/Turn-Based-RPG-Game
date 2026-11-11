using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
[Serializable] public sealed class RoamingFormation
{
    public string Name="Formation";
    [Min(0)] public int Weight=1;
    public EnemySpawnEntry[] Enemies=Array.Empty<EnemySpawnEntry>();
}
public partial class BattleGroup
{
    [Header("Encounter persistence")]
    public EncounterKind Kind=EncounterKind.OneOff;
    [Tooltip("Roaming groups roll one complete formation on first spawn and each checkpoint Rest. Empty pool uses the fixed enemy list.")]
    public List<RoamingFormation> RoamingPool=new();
    [Min(.5f)] public float SpawnSeparation=2;
    readonly List<string> spawnedKeys=new();
    public bool Defeated=>experienceClaimed;
    public bool ResetsOnRest=>Kind==EncounterKind.Roaming && isActiveAndEnabled;
    public sealed class SpawnPlan {public List<EnemySpawnEntry> Entries=new();public List<Vector3> Positions=new();public List<Quaternion> Rotations=new();}
    void OnValidate()
    {
        var used=new HashSet<string>();AssignKeys(enemySpawnEntries,used);
        if(RoamingPool!=null)foreach(var formation in RoamingPool)if(formation!=null)AssignKeys(formation.Enemies,used);
    }
    static void AssignKeys(EnemySpawnEntry[] entries,HashSet<string> used)
    {if(entries==null)return;for(int i=0;i<entries.Length;i++){if(string.IsNullOrEmpty(entries[i].SaveKey)||!used.Add(entries[i].SaveKey)){entries[i].SaveKey=Guid.NewGuid().ToString("N");used.Add(entries[i].SaveKey);}}}
    List<EnemySpawnEntry> Expand(IEnumerable<EnemySpawnEntry> entries)
    {
        var result=new List<EnemySpawnEntry>();if(entries==null)return result;
        foreach(var entry in entries)if(entry.enemyPrefab&&entry.spawnAmount>0)for(int i=0;i<entry.spawnAmount;i++)result.Add(entry);
        return result;
    }
    List<EnemySpawnEntry> RollEntries()
    {
        if(Kind!=EncounterKind.Roaming||RoamingPool==null||RoamingPool.Count==0)return Expand(enemySpawnEntries);
        var valid=RoamingPool.Where(f=>f!=null&&f.Weight>0&&f.Enemies!=null&&f.Enemies.Length>0&&f.Enemies.All(e=>e.enemyPrefab&&e.spawnAmount>0)).ToList();
        int index=EncounterWeights.Pick(valid.Select(f=>f.Weight).ToList(),UnityEngine.Random.value);
        return index<0?new List<EnemySpawnEntry>():Expand(valid[index].Enemies);
    }
    void Start(){if(!Defeated&&spawnedEnemies.Count==0){if(TryPrepareSpawn(out var plan,out var error))ApplySpawn(plan,false);else Debug.LogWarning(name+": "+error);}}
    public bool TryPrepareSpawn(out SpawnPlan plan,out string error,List<Vector3> reserved=null)
    {
        plan=new SpawnPlan();error="";plan.Entries=RollEntries();
        if(plan.Entries.Count==0){error="No valid encounter formation is configured.";return false;}
        foreach(var entry in plan.Entries){
            if(!TryGroundPosition(reserved==null?plan.Positions:reserved.Concat(plan.Positions).ToList(),true,out var position)){error="No safe spawn space for "+name+". Adjust its spawn bounds or formation sizes.";return false;}
            plan.Positions.Add(position);plan.Rotations.Add(Quaternion.identity);
        }return true;
    }
    void OnDestroy(){ClearSpawns();}
    void ClearSpawns()
    {
        foreach(var enemy in spawnedEnemies)if(enemy){enemy.SetActive(false);if(Application.isPlaying)Destroy(enemy);else DestroyImmediate(enemy);}
        spawnedEnemies.Clear();enemyMovements.Clear();spawnedKeys.Clear();
    }
    public void ApplySpawn(SpawnPlan plan,bool resetReward)
    {
        ClearSpawns();if(resetReward)experienceClaimed=false;battleTriggered=false;retryAfter=Time.time+5;
        for(int i=0;i<plan.Entries.Count;i++){
            var entry=plan.Entries[i];var enemy=Instantiate(entry.enemyPrefab,plan.Positions[i],plan.Rotations[i]);
            spawnedEnemies.Add(enemy);spawnedKeys.Add(entry.SaveKey);
            var trigger=enemy.GetComponent<EnemyTrigger>();if(trigger)trigger.Initialize(this);
            var movement=enemy.GetComponent<EnemyMovement>();if(movement){movement.Initialize(this);enemyMovements.Add(movement);}
        }
    }
    bool TryGroundPosition(List<Vector3> planned,bool clearance,out Vector3 position)
    {
        position=transform.position;if(!spawnArea)spawnArea=GetComponent<BoxCollider>();if(!spawnArea)return false;
        var bounds=spawnArea.bounds;
        for(int attempt=0;attempt<96;attempt++){
            var origin=new Vector3(UnityEngine.Random.Range(bounds.min.x,bounds.max.x),bounds.max.y+1,UnityEngine.Random.Range(bounds.min.z,bounds.max.z));
            var hits=Physics.RaycastAll(origin,Vector3.down,bounds.size.y+8,groundLayer,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            foreach(var hit in hits){
                if(hit.collider.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<PlayerMovement>()||hit.collider.GetComponentInParent<EnemyMovement>())continue;
                if(Vector3.Angle(hit.normal,Vector3.up)>35)break;
                var candidate=hit.point+Vector3.up;
                if(SaveCheckpoint.InSafeArea(candidate)||planned.Any(p=>Vector3.Distance(p,candidate)<Mathf.Max(.5f,SpawnSeparation)))break;
                bool blocked=false;
                if(clearance)foreach(var collider in Physics.OverlapCapsule(hit.point+Vector3.up*.4f,hit.point+Vector3.up*1.7f,.35f,~0,QueryTriggerInteraction.Ignore)){
                    if(collider==hit.collider||collider.transform.IsChildOf(transform)||spawnedEnemies.Any(e=>e&&collider.transform.IsChildOf(e.transform)))continue;blocked=true;break;
                }
                if(!blocked){position=candidate;return true;}break;
            }
        }return false;
    }
    public Vector3 GetRoamingPosition(Vector3 fallback)=>TryGroundPosition(new List<Vector3>(),false,out var position)?position:fallback;
    public Vector3 GetRandomPositionInBounds()=>GetRoamingPosition(transform.position);
    IEnumerable<EnemySpawnEntry> AllEntries()=> (enemySpawnEntries??Array.Empty<EnemySpawnEntry>()).Concat((RoamingPool??new List<RoamingFormation>()).Where(f=>f!=null).SelectMany(f=>f.Enemies??Array.Empty<EnemySpawnEntry>()));
    public SavedGroup CaptureSave(string id)
    {
        var saved=new SavedGroup{Id=id,Defeated=Defeated,HasComposition=true};
        for(int i=0;i<spawnedEnemies.Count;i++)if(spawnedEnemies[i]){saved.Positions.Add(spawnedEnemies[i].transform.position);saved.Rotations.Add(spawnedEnemies[i].transform.rotation);saved.SpawnKeys.Add(spawnedKeys[i]);}return saved;
    }
    public void ValidateSavedComposition(SavedGroup saved)
    {
        if(saved.Defeated)return;
        if(!saved.HasComposition){if(Expand(enemySpawnEntries).Count!=saved.Positions.Count)throw new System.IO.InvalidDataException("The saved fixed encounter no longer matches its definitions.");return;}
        if(saved.SpawnKeys==null||saved.SpawnKeys.Count!=saved.Positions.Count)throw new System.IO.InvalidDataException("Saved encounter composition is incomplete.");
        var entries=AllEntries().ToList();foreach(var key in saved.SpawnKeys)if(string.IsNullOrEmpty(key)||entries.Count(e=>e.SaveKey==key&&e.enemyPrefab)!=1)throw new System.IO.InvalidDataException("A saved encounter prefab is missing or has duplicate IDs.");
    }
    public void RestoreSave(SavedGroup saved)
    {
        ValidateSavedComposition(saved);
        if(saved.Defeated){ClearSpawns();experienceClaimed=true;battleTriggered=true;retryAfter=Time.time+5;return;}
        var entries=saved.HasComposition?saved.SpawnKeys.Select(key=>AllEntries().First(e=>e.SaveKey==key)).ToList():Expand(enemySpawnEntries);
        var plan=new SpawnPlan{Entries=entries};
        if(entries.Count!=saved.Positions.Count)throw new System.IO.InvalidDataException("The saved encounter no longer matches its spawn definitions.");
        plan.Positions.AddRange(saved.Positions);plan.Rotations.AddRange(saved.Rotations);experienceClaimed=false;ApplySpawn(plan,false);
    }
}
