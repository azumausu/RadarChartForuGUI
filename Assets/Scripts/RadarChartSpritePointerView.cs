using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Graph
{
    [RequireComponent(typeof(Image))]
    public class RadarChartSpritePointerView : RadarChartPointerView
    {
        [SerializeField]
        private Entry[] _rankEntries;
        
        private Image _image;
        private RectTransform _rectTransform;
        
        // Editorでも動くようようにAwakeでのキャッシュではなく動的なキャッシュにする
        private Image Image
        {
            get
            {
                if (_image == null) _image = GetComponent<Image>();
                return _image;
            }
        }

        private RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null) _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }

        public void UpdateSprite(float value)
        {
            foreach (var entry in _rankEntries.OrderByDescending(x => x.Threshold))
            {
                if (value >= entry.Threshold)
                {
                    Image.sprite = entry.PointerSprite;
                    break;
                }
            }  
        }

        public override void UpdatePointerPosition(Vector2 anchoredPosition)
        {
            RectTransform.anchoredPosition = anchoredPosition;
        }

        [Serializable]
        private class Entry
        {
            public Sprite PointerSprite;
            public float Threshold;
        }
    }
}