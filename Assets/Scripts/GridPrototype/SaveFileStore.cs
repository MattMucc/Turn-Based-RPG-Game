using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Game.GridPrototype
{
    // File I/O is independent of scenes, so round trips/failures can be tested without Play Mode.
    public sealed class SaveFileStore
    {
        [Serializable] public sealed class Envelope {public int Version=1;public string Payload,Checksum;}
        readonly string directory;
        public SaveFileStore(string directory){this.directory=directory;}
        public string PathFor(int slot){if(slot<0||slot>3)throw new ArgumentOutOfRangeException(nameof(slot));return Path.Combine(directory,slot==3?"checkpoint.json":"manual-"+(slot+1)+".json");}
        public bool Exists(int slot)=>File.Exists(PathFor(slot))||File.Exists(PathFor(slot)+".bak");
        public static string Encode(GameSaveData data)
        {var payload=JsonUtility.ToJson(data);return JsonUtility.ToJson(new Envelope{Payload=payload,Checksum=Hash(payload)});}
        public static GameSaveData Decode(string json)
        {
            if(string.IsNullOrEmpty(json)||json.Length>16000000)throw new InvalidDataException("The save file is empty or too large.");
            var envelope=JsonUtility.FromJson<Envelope>(json);
            if(envelope==null||envelope.Version!=1||string.IsNullOrEmpty(envelope.Payload)||envelope.Checksum!=Hash(envelope.Payload))throw new InvalidDataException("The save is damaged or uses an unsupported version.");
            var data=JsonUtility.FromJson<GameSaveData>(envelope.Payload);
            if(data==null||data.Version!=1||string.IsNullOrWhiteSpace(data.Scene)||data.Characters==null||data.Characters.Count==0||data.Inventory==null)throw new InvalidDataException("The save does not contain a supported game state.");
            return data;
        }
        static string Hash(string text){using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text)));}
        public void Write(int slot,string encoded,bool overwrite)
        {
            string path=PathFor(slot);Directory.CreateDirectory(directory);
            if(!overwrite&&Exists(slot))throw new IOException("This slot is occupied. Confirm overwrite first.");
            string temp=path+".tmp";
            try{
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){
                    var bytes=Encoding.UTF8.GetBytes(encoded);stream.Write(bytes,0,bytes.Length);stream.Flush(true);
                }
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public GameSaveData Read(int slot,out bool recovered)
        {
            string path=PathFor(slot);recovered=false;
            try{return Decode(File.ReadAllText(path));}
            catch(Exception e) when(e is IOException||e is ArgumentException||e is InvalidDataException){
                if(!File.Exists(path+".bak"))throw;
                var data=Decode(File.ReadAllText(path+".bak"));recovered=true;return data;
            }
        }
    }
}
