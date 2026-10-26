using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Utils
{
    public class StringTool
    {
        public static IEnumerator Typing(TMPro.TMP_Text t, string strText, float speed = 0.2f)
        {
            t.text = ""; 
            foreach (var str in strText)
            {
                t.text += str; 
                yield return new WaitForSeconds(speed);  // 每输出一个字，等待0.1秒
            }
        }
    }
}