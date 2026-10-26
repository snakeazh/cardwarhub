using System.Collections;
using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineAttachmentReplacement : MonoBehaviour
{
    private SkeletonGraphic skeletonGraphic;

    // 定义表情组
    private string[] faceGroup = { "face", "closeeye01", "closeeye02" };

    private string normalFace = "face"; // 睁眼

    private WaitForSeconds _waitForSeconds_2sec = new WaitForSeconds(2f);
    
    private WaitForSeconds _waitForSeconds_2f = new WaitForSeconds(0.2f);
    
    private WaitForSeconds _waitForSeconds_1 = new WaitForSeconds(0.1f);
    
    private WaitForSeconds _waitForSeconds_08 = new WaitForSeconds(0.08f);
    
    private void Start()
    {
        skeletonGraphic = GetComponent<SkeletonGraphic>();

        // 初始：清空后显示睁眼
        SwitchFace(normalFace);

        // 开始眨眼循环
        // StartCoroutine(BlinkLoop());
    }

    /// <summary>
    /// 眨眼循环：每隔约 2s 随机眨 1–3 次
    /// </summary>
    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return _waitForSeconds_2sec; // 间隔 2 秒

            int blinkCount = Random.Range(1, 4); // 随机眨 1~3 次
            for (int i = 0; i < blinkCount; i++)
            {
                yield return BlinkOnce();
                yield return _waitForSeconds_2f; // 多次眨眼间隔
            }
        }
    }

    /// <summary>
    /// 单次眨眼动作：睁 -> 半睁 -> 全闭 -> 半睁 -> 睁
    /// </summary>
    private IEnumerator BlinkOnce()
    {
        // 半睁
        SwitchFace("closeeye01");
        yield return _waitForSeconds_08;

        // 全闭
        SwitchFace("closeeye02");
        yield return _waitForSeconds_1;

        // 回到半睁
        SwitchFace("closeeye01");
        yield return _waitForSeconds_08;

        // 最后睁开
        SwitchFace(normalFace);
    }

    /// <summary>
    /// 切换表情：先关掉组里所有，再打开目标
    /// </summary>
    private void SwitchFace(string target)
    {
        foreach (var faceName in faceGroup)
        {
            HideSlot(faceName);
        }
        ShowSlot(target, target);
    }

    // ===== 工具函数 =====
    private void HideSlot(string slotName)
    {
        var slot = skeletonGraphic.Skeleton.FindSlot(slotName);
        if (slot != null) slot.Attachment = null;
    }

    private void ShowSlot(string slotName, string attachmentName)
    {
        var slot = skeletonGraphic.Skeleton.FindSlot(slotName);
        if (slot != null)
        {
            var attachment = skeletonGraphic.Skeleton.GetAttachment(slot.Data.Index, attachmentName);
            slot.Attachment = attachment;
        }
    }
}
