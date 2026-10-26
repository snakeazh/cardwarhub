using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class MonsterTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<SkeletonGraphic>().AnimationState.Event += (
            (TrackEntry , e) =>
            {
                if (e.Data.Name == "atk")
                {
                    LogTool.LogError("我触发了攻击");
                }
            });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "atk", false);
            this.GetComponent<SkeletonGraphic>().AnimationState.AddAnimation(0, "walk", true,0);
        }
    }
}
