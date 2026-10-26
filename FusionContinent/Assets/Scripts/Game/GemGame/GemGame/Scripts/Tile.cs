using System;
using System.Collections;
using Config;
using FusionContinent;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    //public TileState state { get; private set; }
    public TileCell cell { get; private set; }
    public int number { get; private set; }
    public bool locked { get;  set; }

    private Image icon;

    private TbGameConst _tbGameConst;

    public void SetState(int number)
    {
        if (icon == null)
        {
            icon =transform.GetChild(0).GetComponent<Image>();
        }

        if (_tbGameConst == null)
        {
            _tbGameConst= DataConfigManager.Tables.TbGameConst;
        }

        //this.state = state;
        this.number = number;
        for (int i = 0; i <_tbGameConst.GemFuinGameGridIconList.Count; i++)
        {
            if (number == _tbGameConst.GemFuinGameGridIconList[i].Num)
            {
                icon.sprite = Global.FusionGemIconSpriteAtlas.GetSprite(_tbGameConst.GemFuinGameGridIconList[i].Icon);
                break;
            }
        }
        
        //background.color = state.backgroundColor;
        //text.color = state.TextColor;
        //text.text = number.ToString();
    }

    public void Spawn(TileCell cell)
    {
        if (this.cell != null)
        {
            this.cell.tile = null;
        }
        this.cell = cell;
        this.cell.tile = this;
        transform.position = cell.transform.position;
    }

    public void MoveTo(TileCell cell)
    {
        if (this.cell != null)
        {
            this.cell.tile = null;
        }
        this.cell = cell;
        this.cell.tile = this;
        StartCoroutine(Animte(cell.transform.position, false));
    }

    public void Merge(TileCell cell)
    {
        if (this.cell != null)
        {
            this.cell.tile = null;
        }
        this.cell = null;
        cell.tile.locked = true;

        StartCoroutine(Animte(cell.transform.position, true));
    }

    private IEnumerator Animte(Vector3 to, bool merging)
    {
        float elapsed = 0f;
        float duration = 0.1f;
        Vector3 from = transform.position;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = to;
        if (merging)
        {
            Destroy(gameObject);
        }
    }
}
