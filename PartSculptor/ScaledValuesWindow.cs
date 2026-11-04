// Live, read-only inventory of properties affected by PartSculptor.
// It never modifies the scaled values or requires debugLogChanges.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClickThroughFix;
using UnityEngine;

namespace PartSculptor
{
    internal sealed class ScaledValue
    {
        internal readonly string Group;
        internal readonly string Name;
        internal readonly string Original;
        internal readonly string Current;
        // Tooltip and clipboard use round-trip precision, not the shortened table text.
        internal readonly string OriginalFull;
        internal readonly string CurrentFull;
        internal readonly string Details;
        internal readonly bool Changed;

        internal ScaledValue(string group, string name, object original, object current, string details)
        {
            Group = group;
            Name = name;
            Original = Format(original);
            Current = Format(current);
            OriginalFull = ScaleDebug.Format(original);
            CurrentFull = ScaleDebug.Format(current);
            Details = details ?? "";
            Changed = !Equivalent(original, current);
        }

        static string Format(object value)
        {
            if (value == null) return "<null>";
            if (value is float || value is double || value is decimal)
            {
                try
                {
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture)
                        .ToString("0.######", CultureInfo.InvariantCulture);
                }
                catch { }
            }
            return ScaleDebug.Format(value);
        }

        static bool Equivalent(object a, object b)
        {
            if (a == null || b == null) return a == b;
            double left, right;
            if (ExponentBinding.Number(a, out left) && ExponentBinding.Number(b, out right))
                return Close(left, right);
            if (a is Vector3 && b is Vector3)
            {
                Vector3 x = (Vector3)a, y = (Vector3)b;
                return Close(x.x, y.x) && Close(x.y, y.y) && Close(x.z, y.z);
            }
            // FloatCurves include the keys and their tangents in ScaleDebug.Format.
            return string.Equals(ScaleDebug.Format(a), ScaleDebug.Format(b),
                StringComparison.Ordinal);
        }

        static bool Close(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b)) return false;
            return Math.Abs(a - b) <= Math.Max(1e-6d,
                1e-6d * Math.Max(Math.Abs(a), Math.Abs(b)));
        }
    }

    // Allow windows to be moved partly outside the screen while keeping a
    // draggable portion accessible. Never force an entire window onscreen.
    internal static class InspectorWindowPosition
    {
        internal static void KeepPartiallyVisible(ref Rect rect)
        {
            float visibleX = Mathf.Min(100f, Screen.width * 0.4f);
            float visibleY = Mathf.Min(70f, Screen.height * 0.4f);
            rect.x = Mathf.Clamp(rect.x,
                visibleX - rect.width, Screen.width - visibleX);
            rect.y = Mathf.Clamp(rect.y,
                visibleY - rect.height, Screen.height - visibleY);
        }
    }

    // Owned by one ModulePartSculptot; other parts can have separate windows.
    internal sealed class ScaledValuesWindow
    {
        readonly List<ScaledValue> rows = new List<ScaledValue>();
        // Keep the same rows and control tree throughout IMGUI's Layout/Repaint
        // cycle. Rebuilding for Repaint can change the number of GUILayout
        // controls if part values change during the same editor frame.
        string readError;
        Rect windowRect;
        Vector2 scroll;
        bool visible;
        bool showUnchanged;
        bool pendingShowUnchanged;
        bool hasPendingShowUnchanged;

        // The Vessel Inspector creates a fresh report for each selected part.
        // Carry the filter choice across reports, including a toggle that is
        // queued for the next IMGUI Layout pass but has not yet been applied.
        internal bool ShowUnchangedValues
        {
            get { return hasPendingShowUnchanged ? pendingShowUnchanged : showUnchanged; }
        }

        internal ScaledValuesWindow(bool initiallyShowUnchanged = false)
        {
            showUnchanged = initiallyShowUnchanged;
        }

        // Keep the controls clear of the built-in window title.
        // Dragging is registered for the whole window after all controls.
        const float TitleSpacingHeight = 24f;
        const string CellTooltipPrefix = "PartSculptorValueCell:";
        ModulePartSculptor owner;
        // Track the tooltip across IMGUI passes. Large curves may need scrolling.
        string activeTooltip = string.Empty;
        Rect tooltipRect;
        Vector2 tooltipScroll;

        internal void Toggle(Part part)
        {
            if (visible) { Close(); return; }
            if (part == null) return;
            float width = Mathf.Min(900f, Screen.width - 24f);
            float height = Mathf.Min(610f, Screen.height - 40f);
            if (windowRect.width < 10f || windowRect.height < 10f)
                windowRect = new Rect((Screen.width - width) * 0.5f,
                    (Screen.height - height) * 0.5f, width, height);
            else
            {
                windowRect.width = width;
                windowRect.height = height;
            }
            InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);
            visible = true;
            scroll = Vector2.zero;
        }

        internal void Close()
        {
            // ClickThroughBlocker manages its input lock as the window opens,
            // closes and moves; no per-part InputLockManager code is needed.
            visible = false;
            activeTooltip = string.Empty;
            tooltipRect = Rect.zero;
            tooltipScroll = Vector2.zero;
        }

        internal void Draw(ModulePartSculptor module)
        {
            if (!visible || module == null || module.part == null) return;
            owner = module;
            windowRect.width = Mathf.Min(windowRect.width, Screen.width - 12f);
            windowRect.height = Mathf.Min(windowRect.height, Screen.height - 12f);
            InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);

            GUISkin previous = GUI.skin;
            try
            {
                GUI.skin = ScaleUserSettings.WindowSkin(previous);
                windowRect = ClickThruBlocker.GUIWindow(module.GetInstanceID() ^ 0x51234,
                    windowRect, DrawContents, "PartSculptor - Scaled Values");
                InspectorWindowPosition.KeepPartiallyVisible(ref windowRect);
            }
            finally { GUI.skin = previous; }

        }

        // The vessel-wide inspector uses the SAME report renderer as this
        // PAW window. Each host holds its own ScaledValuesWindow instance,
        // keeping scroll, checkbox and Layout/Repaint caches independent.
        internal void DrawEmbedded(ModulePartSculptor module, float availableWidth,
            float hostWidth, float hostHeight)
        {
            if (module == null || module.part == null) return;
            owner = module;
            DrawReport(availableWidth, false);
        }

        void DrawContents(int id)
        {
            // Reserve room for the standard window title, then draw the same
            // report that is embedded in the right pane of VesselInspector.
            GUILayout.Space(TitleSpacingHeight);
            DrawReport(windowRect.width, true);
            // Draw over the report and outside its scroll view. The tooltip can
            // extend across the whole window rather than being clipped to a cell.
            DrawTooltipOverlay(windowRect.width, windowRect.height);
            // Register dragging last, after clickable controls.
            GUI.DragWindow();
        }

        void DrawReport(float availableWidth, bool standalone)
        {
            // Never change the number of controls between Layout and Repaint.
            // This is particularly important if the selected part is changed
            // while the current vessel is being edited.
            if (Event.current.type == EventType.Layout)
            {
                if (hasPendingShowUnchanged)
                {
                    showUnchanged = pendingShowUnchanged;
                    hasPendingShowUnchanged = false;
                }
                rows.Clear();
                readError = null;
                try { owner.CollectScaledValues(rows); }
                catch (Exception e)
                {
                    rows.Clear();
                    readError = e.Message;
                }
            }

            if (!string.IsNullOrEmpty(readError))
            {
                GUILayout.Label("Unable to read scaling values: " + readError);
                if (standalone && GUILayout.Button("Close")) Close();
                return;
            }

            int modified = 0;
            foreach (ScaledValue row in rows) if (row.Changed) ++modified;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Part: " + (owner.part.partInfo != null ?
                owner.part.partInfo.title : owner.part.name), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Copy report", GUILayout.Width(105f))) CopyReport();
                if (standalone && GUILayout.Button("Close", GUILayout.Width(65f))) Close();
            }
            GUILayout.Label(modified + " changed / " + rows.Count +
                " tracked values   |   Original = baseline before PartSculptor");
            bool requestedShowUnchanged = GUILayout.Toggle(showUnchanged,
                "Show unchanged values");
            if (requestedShowUnchanged != showUnchanged)
            {
                pendingShowUnchanged = requestedShowUnchanged;
                hasPendingShowUnchanged = true;
            }

            // Use the width of the *containing pane*, not the independent
            // PAW window. Both contexts should get the same columns/tooltips.
            float width = Mathf.Max(180f, availableWidth - (standalone ? 52f : 30f));
            float nameWidth = width * 0.36f;
            float originalWidth = width * 0.23f;
            float currentWidth = width * 0.23f;
            GUIStyle header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };
            GUIStyle cell = new GUIStyle(GUI.skin.label)
                { wordWrap = false, clipping = TextClipping.Clip };

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Property", header, GUILayout.Width(nameWidth));
                GUILayout.Label("Original", header, GUILayout.Width(originalWidth));
                GUILayout.Label("Current", header, GUILayout.Width(currentWidth));
                GUILayout.Label("Notes", header, GUILayout.ExpandWidth(true));
            }

            scroll = GUILayout.BeginScrollView(scroll, false, true,
                GUILayout.ExpandHeight(true));
            string lastGroup = null;
            int shown = 0;
            foreach (ScaledValue row in rows)
            {
                if (!showUnchanged && !row.Changed) continue;
                ++shown;
                if (!string.Equals(lastGroup, row.Group, StringComparison.Ordinal))
                {
                    lastGroup = row.Group;
                    GUILayout.Space(5f);
                    GUILayout.Label(row.Group, header);
                }
                // Every cell in a row shows the complete row details on hover.
                // A prefix prevents other GUIs' tooltips from appearing here.
                using (new GUILayout.HorizontalScope())
                {
                    string tooltip = row.Name + "\nOriginal: " + row.Original +
                        "\nCurrent: " + row.Current + "\n" + row.Details;
                    // Keep the round-trip precision from 0.0.4.25 even when the
                    // displayed values are shortened in the report table.
                    if (!string.Equals(row.Original, row.OriginalFull, StringComparison.Ordinal) ||
                        !string.Equals(row.Current, row.CurrentFull, StringComparison.Ordinal))
                    {
                        tooltip = row.Name + "\nOriginal: " + row.OriginalFull +
                            "\nCurrent: " + row.CurrentFull + "\n" + row.Details;
                    }
                    GUILayout.Label(new GUIContent(row.Name,
                        CellTooltipPrefix + tooltip), cell, GUILayout.Width(nameWidth));
                    GUILayout.Label(new GUIContent(row.Original,
                        CellTooltipPrefix + tooltip), cell, GUILayout.Width(originalWidth));
                    GUILayout.Label(new GUIContent(row.Current,
                        CellTooltipPrefix + tooltip), cell, GUILayout.Width(currentWidth));
                    GUILayout.Label(new GUIContent(row.Details,
                        CellTooltipPrefix + tooltip), cell, GUILayout.ExpandWidth(true));
                }
            }
            if (shown == 0)
                GUILayout.Label("No changed values at the current scale. " +
                    "Enable 'Show unchanged values' to inspect all bindings.");
            GUILayout.EndScrollView();

            // The tooltip is drawn by the containing window AFTER all panes,
            // not here inside the embedded report's clipped layout group.
        }

        // Called by both the individual PAW window and the vessel inspector,
        // after their normal contents have been drawn. All coordinates are
        // relative to the containing GUI.Window, not the right-hand pane.
        internal void DrawTooltipOverlay(float hostWidth, float hostHeight)
        {
            if (Event.current.type == EventType.Repaint)
            {
                string raw = GUI.tooltip;
                string hovered = !string.IsNullOrEmpty(raw) &&
                    raw.StartsWith(CellTooltipPrefix, StringComparison.Ordinal)
                    ? raw.Substring(CellTooltipPrefix.Length) : null;

                if (hovered != null)
                {
                    if (!string.Equals(activeTooltip, hovered, StringComparison.Ordinal))
                    {
                        activeTooltip = hovered;
                        tooltipScroll = Vector2.zero;
                        tooltipRect = Rect.zero; // Reposition for the newly hovered cell.
                    }
                }
                else if (!tooltipRect.Contains(Event.current.mousePosition))
                {
                    activeTooltip = string.Empty;
                    tooltipRect = Rect.zero;
                }
            }

            bool visibleTooltip = !string.IsNullOrEmpty(activeTooltip);

            // Allow tooltips to show the FULL precision of floats/doubles and
            // the full key list of FloatCurves. If that needs more vertical
            // room than the window has, use a scrollable tooltip.
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(4, 4, 3, 3)
            };
            GUIContent content = new GUIContent(visibleTooltip ? activeTooltip : string.Empty);
            float maxWidth = Mathf.Max(120f, Mathf.Min(640f, hostWidth - 20f));
            float desiredWidth = textStyle.CalcSize(content).x + 30f;
            float width = Mathf.Clamp(desiredWidth, 120f, maxWidth);
            float textWidth = Mathf.Max(65f, width - 30f);
            float textHeight = Mathf.Max(22f, textStyle.CalcHeight(content, textWidth) + 8f);
            float maxHeight = Mathf.Max(40f, hostHeight - 20f);
            float height = Mathf.Min(textHeight + 10f, maxHeight);

            // Recompute from the current mouse position unless the pointer is
            // already inside the tooltip (to allow scrolling long contents).
            Vector2 mouse = Event.current.mousePosition;
            if (visibleTooltip && (!tooltipRect.Contains(mouse) ||
                tooltipRect.width != width || tooltipRect.height != height))
            {
                float x = mouse.x + 16f;
                float y = mouse.y + 18f;
                if (x + width > hostWidth - 6f) x = mouse.x - width - 12f;
                if (y + height > hostHeight - 6f) y = mouse.y - height - 12f;
                x = Mathf.Clamp(x, 4f, Mathf.Max(4f, hostWidth - width - 4f));
                y = Mathf.Clamp(y, 4f, Mathf.Max(4f, hostHeight - height - 4f));
                tooltipRect = new Rect(x, y, width, height);
            }

            // Always issue the same absolute-mode controls, including when
            // there is no tooltip. This keeps the GUI control IDs stable across
            // Layout, MouseMove and Repaint before the parent's DragWindow().
            // Hidden controls live offscreen and never block mouse input.
            Rect drawRect = visibleTooltip ? tooltipRect :
                new Rect(-10000f, -10000f, 1f, 1f);
            GUI.Box(drawRect, GUIContent.none, GUI.skin.box);
            Rect viewport = visibleTooltip ?
                new Rect(drawRect.x + 4f, drawRect.y + 4f,
                    drawRect.width - 8f, drawRect.height - 8f) :
                new Rect(-10000f, -10000f, 1f, 1f);
            Rect view = new Rect(0f, 0f, textWidth + 8f, textHeight);
            tooltipScroll = GUI.BeginScrollView(viewport, tooltipScroll, view,
                false, visibleTooltip && textHeight > viewport.height);
            GUI.Label(new Rect(0f, 0f, textWidth, textHeight), content, textStyle);
            GUI.EndScrollView();
        }

        void CopyReport()
        {
            var report = new StringBuilder("PartSculptor scaled values for ")
                .Append(owner.part.name).AppendLine();
            report.AppendLine("Group\tProperty\tOriginal\tCurrent\tNotes");
            foreach (ScaledValue row in rows)
            {
                if (!showUnchanged && !row.Changed) continue;
                report.Append(row.Group).Append('\t').Append(row.Name).Append('\t')
                    .Append(row.OriginalFull).Append('\t').Append(row.CurrentFull)
                    .Append('\t').Append(row.Details).AppendLine();
            }
            GUIUtility.systemCopyBuffer = report.ToString();
        }
    }
}
