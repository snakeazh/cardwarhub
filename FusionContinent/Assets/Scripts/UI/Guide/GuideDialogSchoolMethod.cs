using UnityEngine;

namespace FusionContinent.Guide
{
    public class GuideDialogSchoolMethod : GuideDialogClickMethod
    {
        protected override GameObject GetInterObject(string path)
        {
            
            GameObject interRoot = base.GetInterObject(path);

            return FirstActiveGameObject(interRoot);
        }

        GameObject FirstActiveGameObject(GameObject obj)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                GameObject node = obj.transform.GetChild(i).gameObject;
                if (node.activeSelf)
                {
                    return node;
                }
            }

            return null;
        }
    }
}