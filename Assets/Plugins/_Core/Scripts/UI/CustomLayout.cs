using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace HHDCore
{
    [RequireComponent(typeof(RectTransform))]
    public class CustomLayout : LayoutGroup
    {
        public enum LayoutFitType {None,HeightThenWidth,WidthThenHeight};
        public enum LayoutAlignType {Horizontal,Vertical};
        [SerializeField] private LayoutAlignType _layoutAlignType = LayoutAlignType.Horizontal;
        [SerializeField] private LayoutFitType _fitType = LayoutFitType.None;
        private List<RectTransform> _childRects = new();
        private List<AspectRatioFitter> _childAspects = new();
        [SerializeField] private float _spacing;
        [SerializeField] private bool _controlChildWidth = false;
        [SerializeField] private bool _controlChildHeight = false;
        private RectTransform _rect;
        protected override void OnValidate()
        {
            _rect = GetComponent<RectTransform>();
            UpdateLayoutFitType();
            SetDirty();
        }
        [ContextMenu("Update layout")]
        public void UpdateLayoutFitType()
        {
            _childRects.Clear();
            _childAspects.Clear();
            if (_rect == null) _rect = GetComponent<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf)
                {
                    _childRects.Add(transform.GetChild(i).GetComponent<RectTransform>());
                    if (_fitType != LayoutFitType.None)
                        _childAspects.Add(transform.GetChild(i).GetComponent<AspectRatioFitter>());
                }
            }
            if (_childRects.Count == 0) return;
            Vector2 containerSize = _rect.rect.size - new Vector2(m_Padding.left + m_Padding.right, m_Padding.top + m_Padding.bottom);
            if (containerSize.x <= 0 || containerSize.y <= 0)
            {
                foreach (var rect in _childRects)
                {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
                }
                return;
            }
            if (_layoutAlignType == LayoutAlignType.Horizontal)
            {
                if (_fitType == LayoutFitType.HeightThenWidth)
                {
                    float totalWidth = 0;
                    int i = 0;
                    int childCount = _childRects.Count;
                    foreach (var rect in _childRects)
                    {
                        _childAspects[i].aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rect.rect.height);
                        totalWidth += rect.rect.width;
                        i++;
                    }
                    totalWidth += (childCount > 0) ? (childCount - 1) * _spacing : 0;
                    bool isOverflow = totalWidth > _rect.rect.width;
                    if (!isOverflow)
                    {
                        float displayRange = totalWidth - _childRects[0].rect.width;
                        float distance = (childCount > 1) ? displayRange / (childCount - 1) : 0;
                        Vector2 startPos = new Vector2(-displayRange / 2f, 0);
                        i = 0;
                        foreach (var rect in _childRects)
                        {
                            rect.SetLocalPositionAndRotation(startPos + new Vector2(distance * i, 0), Quaternion.identity);
                            i++;
                        }
                    }
                    else
                    {
                        float newTotalWidth = containerSize.x - ((childCount > 0) ? (childCount - 1) * _spacing : 0);
                        float newWidth = newTotalWidth / childCount;
                        float displayRange = containerSize.x - newWidth;
                        float distance = (childCount > 1) ? displayRange / (childCount - 1) : 0;
                        Vector2 startPos = new Vector2(-displayRange / 2f, 0);
                        i = 0;
                        foreach (var rect in _childRects)
                        {
                            _childAspects[i].aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                            rect.SetLocalPositionAndRotation(startPos + new Vector2(distance * i, 0), Quaternion.identity);
                            i++;
                        }
                    }
                }
            }
        
        }
        void FitHeight()
        {
        
        }

        public override void CalculateLayoutInputVertical()
        {
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }
    }
}
