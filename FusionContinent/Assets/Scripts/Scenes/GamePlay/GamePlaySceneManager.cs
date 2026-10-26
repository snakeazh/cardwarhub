using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlaySceneManager : SceneLoadingManager
{
    public List<SceneMapBase> SceneMapInits = new List<SceneMapBase>();
    private void Awake()
    {
        foreach (var sceneMap in SceneMapInits)
        {
            this.RegisterModule(sceneMap);
        }
   
    }
}
