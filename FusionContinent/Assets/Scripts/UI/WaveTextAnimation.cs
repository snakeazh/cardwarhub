using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
 
public class WaveTextAnimation : MonoBehaviour

{
    //tmp���
    TextMeshProUGUI m_txtSelf = null;
    //�ƶ�����
    float m_moveTotalTime = 1f;
    //���¸�������
    float m_amplitude = 100;
    Vector3[] m_rawVertex;

    public void Awake()
    {
        m_txtSelf = transform.GetComponent<TextMeshProUGUI>();
    }

    private void GetRawVertex()
    {
        m_txtSelf.ForceMeshUpdate();
        if (m_txtSelf.textInfo.characterCount > 0)
        {
            TMP_CharacterInfo charInfo = m_txtSelf.textInfo.characterInfo[0];
            TMP_MeshInfo meshInfo = m_txtSelf.textInfo.meshInfo[charInfo.materialReferenceIndex];
            //���������������ʼֵ
            m_rawVertex = new Vector3[meshInfo.vertices.Length];
            for (int i = 0; i < meshInfo.vertices.Length; i++)
            {
                m_rawVertex[i] = new Vector3(meshInfo.vertices[i].x, meshInfo.vertices[i].y, meshInfo.vertices[i].z);
            }
        }
    }

    public void Start()
    {
        GetRawVertex();
        StartCoroutine(Shake());
    }
    public IEnumerator Shake()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            Tweener tweener = DOTween.To(() => 0f, y =>
            {
                for (int i = 0; i < m_txtSelf.textInfo.characterCount; i++)
                {
                    // ��ȡ�ַ���Ϣ��MeshInfo
                    TMP_CharacterInfo currentCharInfo = m_txtSelf.textInfo.characterInfo[i];
                    TMP_MeshInfo meshInfo = m_txtSelf.textInfo.meshInfo[currentCharInfo.materialReferenceIndex];

                    int nextVertexIndex = meshInfo.vertices.Length;
                    if (i < m_txtSelf.textInfo.characterCount - 1)
                    {
                        TMP_CharacterInfo nextCharInfo = m_txtSelf.textInfo.characterInfo[i + 1];
                        nextVertexIndex = nextCharInfo.vertexIndex;
                    }

                    // ��ȡ��ʼ��������
                    int vertexIndex = currentCharInfo.vertexIndex;

                    // ����ƫ��
                    for (int j = vertexIndex; j < nextVertexIndex; j++)
                    {
                        float yOffset = y - (float)i / m_txtSelf.textInfo.characterCount;
                        if (yOffset >= 0 && yOffset <= 1)
                        {
                            meshInfo.vertices[j] = m_rawVertex[j] + Mathf.Sin(yOffset * Mathf.PI) * Vector3.up * m_amplitude;
                        }
                    }
                }
                m_txtSelf.UpdateVertexData();
            }, 2, m_moveTotalTime);

            yield return new WaitForSeconds(m_moveTotalTime);
        }
    }
}