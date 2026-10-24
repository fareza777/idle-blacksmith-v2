using System;
using System.IO;
using UnityEngine;

namespace ForgeEmpire
{
    public static class WorkshopSave
    {
        public static WorkshopState Load(string path, out bool recovered)
        {
            recovered=false;
            if(TryLoad(path,out WorkshopState state))return state;
            if(TryLoad(path+".bak",out state)){recovered=true;return state;}
            return new WorkshopState();
        }

        static bool TryLoad(string path,out WorkshopState state)
        {
            state=null;
            if(!File.Exists(path))return false;
            try {
                var info=new FileInfo(path);
                if(info.Length>1024*1024||info.Length<4)return false;
                string json=File.ReadAllText(path);
                if(!json.Contains("\"Levels\"")||!json.Contains("\"Gold\""))return false;
                state=JsonUtility.FromJson<WorkshopState>(json);
                return state!=null;
            } catch(IOException){return false;} catch(ArgumentException){return false;}
        }

        public static void Write(string path, WorkshopState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary=path+".tmp";
            File.WriteAllText(temporary,JsonUtility.ToJson(state));
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");
            else File.Move(temporary,path);
        }
    }
}
