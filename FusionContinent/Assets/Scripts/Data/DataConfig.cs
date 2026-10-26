using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Config;
using SimpleJSON;

public class DataConfig : MonoBehaviour
{
    public Tables tables;
    
    void Start()
    {
        tables = new Tables(LoadByteBuf);
    }

    private static JSONNode LoadByteBuf(string file)
    {
        return JSON.Parse(File.ReadAllText(Application.dataPath + "/../../GenerateDatas/json/" + file + ".json", System.Text.Encoding.UTF8));
    }
}
