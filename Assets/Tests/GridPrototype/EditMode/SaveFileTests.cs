using System;
using System.IO;
using Game.GridPrototype;
using NUnit.Framework;
public class SaveFileTests
{
    string directory;SaveFileStore store;
    [SetUp]public void Setup(){directory=Path.Combine(Path.GetTempPath(),"rpg-save-tests-"+Guid.NewGuid().ToString("N"));store=new SaveFileStore(directory);}
    [TearDown]public void Cleanup(){if(Directory.Exists(directory)){Assert.IsTrue(Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase));Directory.Delete(directory,true);}}
    GameSaveData Data(string name)=>new GameSaveData{Scene="Assets/Scenes/SampleScene.unity",CaptainName=name,Inventory=new PartyInventory(),Characters=new System.Collections.Generic.List<SavedCharacter>{new SavedCharacter{Id="test"}}};
    [Test]public void WeightedFormationsIgnoreDisabledEntriesAndKeepBoundarySelectionStable()
    {
        Assert.AreEqual(1,EncounterWeights.Pick(new[]{0,1,3},0));Assert.AreEqual(2,EncounterWeights.Pick(new[]{0,1,3},.25));Assert.AreEqual(2,EncounterWeights.Pick(new[]{0,1,3},1));Assert.AreEqual(-1,EncounterWeights.Pick(new[]{0,-1},.5));
        var data=Data("composition");data.Groups.Add(new SavedGroup{Id="group",HasComposition=true,SpawnKeys=new System.Collections.Generic.List<string>{"wolf","wolf","support"}});
        CollectionAssert.AreEqual(data.Groups[0].SpawnKeys,SaveFileStore.Decode(SaveFileStore.Encode(data)).Groups[0].SpawnKeys);
    }
    [Test]public void FourSlotsAreIndependentAndManualOverwriteRequiresConfirmation()
    {
        for(int i=0;i<4;i++)store.Write(i,SaveFileStore.Encode(Data("slot"+i)),false);
        for(int i=0;i<4;i++){Assert.AreEqual("slot"+i,store.Read(i,out bool backup).CaptainName);Assert.IsFalse(backup);}
        Assert.Throws<IOException>(()=>store.Write(0,SaveFileStore.Encode(Data("changed")),false));Assert.AreEqual("slot0",store.Read(0,out _).CaptainName);
        Assert.Throws<ArgumentOutOfRangeException>(()=>store.PathFor(4));
    }
    [Test]public void CorruptPrimaryRecoversBackupAndInvalidDataNeverReplacesGameplay()
    {
        store.Write(0,SaveFileStore.Encode(Data("first")),false);store.Write(0,SaveFileStore.Encode(Data("second")),true);
        File.WriteAllText(store.PathFor(0),"broken");Assert.AreEqual("first",store.Read(0,out bool backup).CaptainName);Assert.IsTrue(backup);
        File.WriteAllText(store.PathFor(0)+".bak","broken");Assert.Throws<ArgumentException>(()=>store.Read(0,out _));
    }
    [Test]public void ChecksumRejectsChangedPayloadAndVersionIsExplicit()
    {
        var encoded=SaveFileStore.Encode(Data("original"));Assert.Throws<InvalidDataException>(()=>SaveFileStore.Decode(encoded.Replace("original","tampered")));
        var newer=Data("future");newer.Version=2;Assert.Throws<InvalidDataException>(()=>SaveFileStore.Decode(SaveFileStore.Encode(newer)));
    }
}
