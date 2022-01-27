using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct MemoData
{
    public int memoId;
    public string message;
    public Vector2 position;
    public Vector2 size;
    public List<int> connectedIds;
    public bool highlighted;
}

public class Memo : MonoBehaviour
{
    [SerializeField]
    private Pin pin;

    [SerializeField]
    private Text note;

    [SerializeField]
    private GameObject highlight;

    RectTransform _rectTransform;
    private RectTransform rectTransform
    {
        get
        {
            if (!_rectTransform)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            return _rectTransform;
        }
    }

    public MemoData Data
    {
        get
        {
            return new MemoData
            {
                memoId = pin ? pin.PinId : -1,
                connectedIds = pin ? pin.ConnectedIds : new List<int>(),
                message = note ? note.text : "",
                position = rectTransform ? rectTransform.anchoredPosition : Vector2.zero,
                size = rectTransform ? rectTransform.sizeDelta : new Vector2(300, 256),
                highlighted = highlight ? highlight.activeSelf : false,
            };
        }

        set
        {
            if (pin)
            {
                pin.PinId = value.memoId;
                pin.ConnectedIds = value.connectedIds;
            }

            if (note) { note.text = value.message; }

            if (rectTransform)
            {
                rectTransform.anchoredPosition = value.position;
                rectTransform.sizeDelta = value.size;
            }

            if (highlight)
            {
                highlight.SetActive(value.highlighted);
            }
        }

    }

    public bool Highlighted
    {
        get { return highlight ? highlight.activeSelf : false; }
        set { if (highlight) { highlight.SetActive(value); } }
    }

    public void Destroy()
    {
        if (pin)
        {
            pin.DestroyStrings();
        }

        Destroy(gameObject);
    }    
}
