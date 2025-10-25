using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Auto Resizing Grid Layout Group", 153)]
public class AutoResizingGridLayoutGroup : GridLayoutGroup
{
    public enum ExtendedConstraint
    {
        Flexible,
        FixedColumnCount,
        FixedRowCount,
        AspectRatio
    }

    [SerializeField] private ExtendedConstraint m_ExtendedConstraint = ExtendedConstraint.Flexible;
    [SerializeField] private float m_AspectRatio = 1f; // width / height

    public ExtendedConstraint extendedConstraint
    {
        get => m_ExtendedConstraint;
        set => SetProperty(ref m_ExtendedConstraint, value);
    }

    public float aspectRatio
    {
        get => m_AspectRatio;
        set => SetProperty(ref m_AspectRatio, Mathf.Max(0.01f, value));
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        UpdateCellSize();
    }

    public override void CalculateLayoutInputVertical()
    {
        UpdateCellSize();
    }

    private void UpdateCellSize()
    {
        int childCount = rectChildren.Count;
        Rect rect = rectTransform.rect;
        float width = rect.width - padding.horizontal;
        float height = rect.height - padding.vertical;

        int columns = 1;
        int rows = 1;

        switch (m_ExtendedConstraint)
        {
            case ExtendedConstraint.FixedColumnCount:
                columns = constraintCount;
                rows = Mathf.CeilToInt((float)childCount / columns);
                break;

            case ExtendedConstraint.FixedRowCount:
                rows = constraintCount;
                columns = Mathf.CeilToInt((float)childCount / rows);
                break;

            case ExtendedConstraint.AspectRatio:
                float totalArea = width * height;
                float cellArea = totalArea / Mathf.Max(1, childCount);
                float cellWidth = Mathf.Sqrt(cellArea * aspectRatio);
                float cellHeight = cellWidth / aspectRatio;
                m_CellSize = new Vector2(cellWidth, cellHeight);
                return;

            case ExtendedConstraint.Flexible:
            default:
                columns = Mathf.CeilToInt(Mathf.Sqrt(childCount));
                rows = Mathf.CeilToInt((float)childCount / columns);
                break;
        }

        float cellWidthAuto = (width - (columns - 1) * spacing.x) / columns;
        float cellHeightAuto = (height - (rows - 1) * spacing.y) / rows;
        m_CellSize = new Vector2(cellWidthAuto, cellHeightAuto);
    }
}