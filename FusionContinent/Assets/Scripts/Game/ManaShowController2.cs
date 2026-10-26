using System;
using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using QFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ManaShowController2 : MonoSingleton<ManaShowController2>
{
    public GameObject[] Fragment;
    public Text manatxt;
    public Text maxmanatxt;
    public GameObject ManaFragmenteffectf;
    public GameObject ManaFragmenteffecthf;
    public GameObject Manafulleffect;
    public GameObject Upwaterffect;
    public GameObject Lastwaterffect;
    public CardSlotLongClick ManaBtn;
    public GameObject tip;

    // Start is called before the first frame update
    void Start()
    {
        // 记录物体的原始缩放值
        originalScale = Vector3.one * 0.75f;
        Upwaterffect.GetComponent<ParticleSystemRenderer>().material
            .SetTextureOffset("_maintex", new Vector2(0, 0.42f));
        Lastwaterffect.GetComponent<ParticleSystemRenderer>().material
            .SetTextureOffset("_maintex", new Vector2(0, 0.45f));
        SetMaxMana(Global.MaxGameMana.Value);
        SetManaFragment(Global.GameManaFragment.Value);
        SetMana(Global.GameMana.Value);
        ManaBtn.onLongPress.AddListener(() =>
        {
            tip.GetComponent<TMP_Text>().text = "卡能值恢复速度：\n"+GamePlayManager.Default.GetrecoverGameManaNum()+"卡能值碎片/s";
            tip.SetActive(true);
        });
        ManaBtn.onLongPressUp.AddListener(() =>
        {
            tip.SetActive(false);
        });
    }

    public void SetMaxMana(int level)
    {
        maxmanatxt.text = level.ToString();
        // LogTool.LogError(level);
        if (level < 10 && level >= 0)
        {
            for (int i = Fragment.Length - 1; i >= 0; i--)
            {
                if (i >= level)
                {
                    Fragment[i].gameObject.SetActive(false);
                }
                else
                {
                    Fragment[i].gameObject.SetActive(true);
                }
            }
        }

        if (level >= 10)
        {
            for (int i = 0; i < Fragment.Length; i++)
            {
                Fragment[i].gameObject.SetActive(true);
            }
        }
    }

    public float pulseDuration = 0.5f; // 脉冲动画的持续时间
    public float scaleFactor = 1.2f; // 缩放的比例（例如 1.2 表示放大到 120%）

    private Vector3 originalScale; // 存储物体的原始缩放值

    private int lastLevel = 0; // 记录上一次的 level
    private Tween currentTween; // 记录当前的动画，保证动画播放完后再开始新的

    public void SetManaFragment(int level)
    {
        // 如果魔力已满，直接设置最终效果
        if (Global.GameMana.Value == Global.MaxGameMana.Value)
        {
            Upwaterffect.GetComponent<ParticleSystemRenderer>().material
                .SetTextureOffset("_maintex", new Vector2(0, 0.06f));
            Lastwaterffect.GetComponent<ParticleSystemRenderer>().material
                .SetTextureOffset("_maintex", new Vector2(0, 0.06f));
            ManaFragmenteffecthf.SetActive(true);
            lastLevel = level; // 记录当前 level
            return;
        }

        if (level == GamePlayManager.Default.GameManaFragmentToEntire)
        {
            lastLevel = 0;
            level = 0;
            Upwaterffect.GetComponent<ParticleSystemRenderer>().material
                .SetTextureOffset("_maintex", new Vector2(0, 0.42f));
            Lastwaterffect.GetComponent<ParticleSystemRenderer>().material
                .SetTextureOffset("_maintex", new Vector2(0, 0.45f));
        }

        float startOffset1 = MapNumber(lastLevel, 0, GamePlayManager.Default.GameManaFragmentToEntire, 0.42f, 0.06f);
        float endOffset1 = MapNumber(level, 0, GamePlayManager.Default.GameManaFragmentToEntire, 0.42f, 0.06f);
        float startOffset2 = MapNumber(lastLevel, 0, GamePlayManager.Default.GameManaFragmentToEntire, 0.45f, 0.06f);
        float endOffset2 = MapNumber(level, 0, GamePlayManager.Default.GameManaFragmentToEntire, 0.45f, 0.06f);

        Material mat1 = Upwaterffect.GetComponent<ParticleSystemRenderer>().material;
        Material mat2 = Lastwaterffect.GetComponent<ParticleSystemRenderer>().material;

        // 如果当前有动画在播放，则等动画结束后再执行新的动画
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.OnComplete(() =>
                StartNewAnimation(mat1, mat2, startOffset1, endOffset1, startOffset2, endOffset2, level));
        }
        else
        {
            StartNewAnimation(mat1, mat2, startOffset1, endOffset1, startOffset2, endOffset2, level);
        }
    }

    private void StartNewAnimation(Material mat1, Material mat2, float startOffset1, float endOffset1,
        float startOffset2, float endOffset2, int level)
    {
        // 创建一个新的动画序列
        Sequence sequence = DOTween.Sequence();

        sequence.Append(DOTween.To(
            () => startOffset1,
            x => mat1.SetTextureOffset("_maintex", new Vector2(0, x)),
            endOffset1,
            0.7f).SetEase(Ease.InOutSine));

        sequence.Join(DOTween.To(
            () => startOffset2,
            x => mat2.SetTextureOffset("_maintex", new Vector2(0, x)),
            endOffset2,
            0.7f).SetEase(Ease.InOutSine));

        // 动画完成后执行
        sequence.AppendCallback(() =>
        {
            if (level / (GamePlayManager.Default.GameManaFragmentToEntire == 0
                    ? 1
                    : GamePlayManager.Default.GameManaFragmentToEntire) >= 0.5f)
                ManaFragmenteffecthf.SetActive(true);
            else
                ManaFragmenteffecthf.SetActive(false);
        });

        // 记录当前动画
        currentTween = sequence;
        lastLevel = level; // 记录当前 level
    }

    // 线性映射方法
    float MapNumber(float value, float inputMin, float inputMax, float outputMin, float outputMax)
    {
        // 确保输入值在范围内
        value = Mathf.Clamp(value, inputMin, inputMax);

        // 计算映射后的值
        return (value - inputMin) * (outputMax - outputMin) / (inputMax - inputMin) + outputMin;
    }

    private int lastMana = 0;

    public void SetMana(int level)
    {
        manatxt.text = level.ToString();
        if (lastMana < level)
        {
            // AudioKit.PlaySound("resources://Audio/卡能值增加音效");
            if (level % 10 != 0 && level != Global.MaxGameMana.Value)
            {
                Fragment[level % 10 - 1].transform.GetChild(0).gameObject.SetActive(false);
                Fragment[level % 10 - 1].transform.GetChild(0).gameObject.SetActive(true);
                DOTween.To(() => 1, x => Fragment[level % 10 - 1].transform.localScale = new Vector3(x, x, 1), 1.2f,
                        0.5f)
                    .OnComplete(() =>
                    {
                        DOTween.To(() => 1.2f,
                            x => Fragment[level % 10 - 1].transform.localScale = new Vector3(x, x, 1), 1f,
                            0.5f).OnComplete(() =>
                        {
                            Fragment[level % 10 - 1].transform.GetChild(0).gameObject.SetActive(false);
                        });
                    });
            }
        }
        else
        {
            for (int i = 0; i <= (lastMana - level) % 10; i++)
            {
                int currentIndex = i; // 创建一个局部变量来捕获当前的 i 值
                int levelindex = level;
                if (level >= 10)
                {
                    levelindex = Mathf.Abs((10 * level / 10) - level);
                }

                Fragment[(levelindex + currentIndex) % 10].transform.GetChild(1).gameObject.SetActive(false);
                Fragment[(levelindex + currentIndex) % 10].transform.GetChild(1).gameObject.SetActive(true);
                DOTween.To(() => 1,
                        x => Fragment[(levelindex + currentIndex) % 10].transform.localScale = new Vector3(x, x, 1),
                        0.9f, 0.5f)
                    .OnComplete(() =>
                    {
                        DOTween.To(() => 0.9f,
                                x => Fragment[(levelindex + currentIndex) % 10].transform.localScale =
                                    new Vector3(x, x, 1), 1f, 0.5f)
                            .OnComplete(() =>
                            {
                                Fragment[(levelindex + currentIndex) % 10].transform.GetChild(1).gameObject
                                    .SetActive(false);
                            });
                    });
            }
        }

        lastMana = level;
        // 停止当前所有动画
        manatxt.transform.DOKill();
        // 立即恢复到原始大小
        manatxt.transform.localScale = originalScale;
        // 创建一个 DOTween 序列
        Sequence pulseSequence = DOTween.Sequence();
        // 第一步：放大物体
        pulseSequence.Append(manatxt.transform.DOScale(originalScale * scaleFactor, pulseDuration / 2)
            .SetEase(Ease.OutSine));
        // 第二步：恢复物体到原始大小
        pulseSequence.Append(manatxt.transform.DOScale(originalScale, pulseDuration / 2).SetEase(Ease.InSine));
        // 播放动画
        pulseSequence.Play();
        ManaFragmenteffectf.SetActive(true);
        DOTween.To(v => { }, 0, 0, 2f).SetEase(Ease.Linear).onComplete += () =>
        {
            ManaFragmenteffectf.SetActive(false);
        };
        SetManaFragment(Global.GameManaFragment.Value);
        if (level == Global.MaxGameMana.Value)
        {
            Manafulleffect.SetActive(true);
        }
        else
        {
            Manafulleffect.SetActive(false);
        }

        if (level <= 10 && level >= 0)
        {
            for (int i = 0; i < Fragment.Length; i++)
            {
                if (i <= level - 1)
                {
                    Fragment[i].GetComponent<Image>().color = new Color32(254, 240, 140, 255);
                }
                else
                {
                    Fragment[i].GetComponent<Image>().color = new Color32(0, 0, 0, 168);
                }
            }
        }

        if (level <= 20 && level >= 10)
        {
            for (int i = 0; i < Fragment.Length; i++)
            {
                if (i <= level - 11)
                {
                    Fragment[i].GetComponent<Image>().color = new Color32(254, 145, 71, 255);
                }
                else
                {
                    Fragment[i].GetComponent<Image>().color = new Color32(254, 240, 140, 255);
                }
            }
        }

        if (level <= 30 && level >= 20)
        {
            for (int i = 0; i < Fragment.Length; i++)
            {
                if (i <= level - 21)
                {
                    Fragment[i].GetComponent<Image>().color = new Color32(140, 48, 204, 255);
                }
                else
                {
                    Fragment[i].GetComponent<Image>().color = new Color32(254, 145, 71, 255);
                }
            }
        }
    }
}