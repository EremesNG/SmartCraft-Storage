using System;
using UnityEngine;

namespace SmartCraftStorage.Storage.UI
{
    internal static class NativeTerminalLayout
    {
        internal const int MaxVisibleRows = 4;
        internal const float Padding = 16f, Gap = 8f, GridTop = 92f, ButtonHeight = 30f;
        private const float ScrollbarWidth = 12f;

        internal static int ViewRows(int rows) => Math.Max(1, Math.Min(MaxVisibleRows, rows));
        internal static float FooterTop(float cellSpacing, int rows = MaxVisibleRows) => GridTop + cellSpacing * ViewRows(rows) + Gap;
        internal static float ActionWidth(float panelWidth) => (panelWidth - Padding * 2 - Gap * 2) / 3;

        internal static void InitializeNativeLayout(InventoryGrid grid)
        {
            // Layout mods can lazily cache the first container's coordinates.
            // Let them observe the native baseline before moving any terminal UI.
            var previous = grid.m_inventory;
            try
            {
                if (previous == null) grid.m_inventory = new Inventory("SCS layout baseline", null, 1, 1);
                grid.UpdateGui(null, null);
            }
            finally { grid.m_inventory = previous; }
        }

        internal static float Configure(RectTransform panel, RectTransform viewport, int columns, float cellSpacing,
            RectTransform scrollbar = null, int rows = MaxVisibleRows)
        {
            float width = Math.Max(panel.rect.width, columns * cellSpacing + Padding * 2 + Gap + ScrollbarWidth);
            float gridHeight = cellSpacing * ViewRows(rows);
            var top = panel.TransformPoint(new Vector3(0f, panel.rect.yMax));
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, FooterTop(cellSpacing, rows) + ButtonHeight + 12f);
            panel.position += top - panel.TransformPoint(new Vector3(0f, panel.rect.yMax));
            // Native inventory layout mods express horizontal padding as offsets.
            // Keep horizontal stretch so a negative right inset cannot collapse the grid.
            viewport.anchorMin = Vector2.up;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(.5f, 1f);
            viewport.offsetMin = new Vector2(Padding, -GridTop - gridHeight);
            viewport.offsetMax = new Vector2(-Padding - Gap - ScrollbarWidth, -GridTop);
            if (scrollbar != null)
            {
                if (scrollbar.parent != panel) scrollbar.SetParent(panel, false);
                scrollbar.anchorMin = scrollbar.anchorMax = scrollbar.pivot = Vector2.one;
                scrollbar.anchoredPosition = new Vector2(-Padding, -GridTop);
                scrollbar.sizeDelta = new Vector2(ScrollbarWidth, gridHeight);
            }
            return width;
        }

        internal static void ArrangeGrid(RectTransform panel, InventoryGrid grid, int columns)
        {
            var viewport = (RectTransform)grid.transform;
            Configure(panel, viewport, columns, grid.m_elementSpace, grid.m_scrollbar?.transform as RectTransform,
                grid.GetInventory().GetHeight());
            // Inventory mods can change the inset during UpdateInventory. Use absolute
            // positions so restoring it neither drifts cells nor resets the scroll offset.
            float inset = (viewport.rect.width - columns * grid.m_elementSpace) / 2f;
            for (int index = 0; index < grid.m_elements.Count; index++)
            {
                var rect = (RectTransform)grid.m_elements[index].transform;
                rect.anchoredPosition = new Vector2(inset + index % columns * grid.m_elementSpace,
                    rect.anchoredPosition.y);
                // Padding is still a native drop target, but must not suggest free
                // physical capacity. These groups belong only to projection cells.
                var visibility = rect.GetComponent<CanvasGroup>() ?? rect.gameObject.AddComponent<CanvasGroup>();
                visibility.alpha = grid.m_elements[index].m_used ? 1f : 0f;
            }
        }

        internal sealed class RectState
        {
            private readonly RectTransform _rect;
            private readonly Transform _parent;
            private readonly int _sibling;
            private readonly Vector2 _min, _max, _pivot, _size, _position;
            internal RectState(RectTransform rect)
            { _rect = rect; _parent = rect.parent; _sibling = rect.GetSiblingIndex(); _min = rect.anchorMin; _max = rect.anchorMax; _pivot = rect.pivot; _size = rect.sizeDelta; _position = rect.anchoredPosition; }
            internal void Restore()
            {
                if (_rect == null) return;
                if (_rect.parent != _parent) { _rect.SetParent(_parent, false); _rect.SetSiblingIndex(_sibling); }
                _rect.anchorMin = _min; _rect.anchorMax = _max; _rect.pivot = _pivot;
                _rect.sizeDelta = _size; _rect.anchoredPosition = _position;
            }
        }
    }
}
