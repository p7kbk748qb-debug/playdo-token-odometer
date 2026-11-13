# Mini and Compact settings

## Tiny

Tiny is a 300 × 68 DIP panel that shows the exact selected token count. Open **Tiny** from the dashboard, tray or window picker, or press **Ctrl+Shift+T**. Drag anywhere on the number or background. The two small controls in the top right hide it to the tray or expand to the dashboard. Double-click the background to expand.

Right-click for Today / 7 days / Recorded, copy, CSV export and Appearance. Existing source selection, transparency, theme and corner preferences apply. Tiny deliberately omits widget rows and branding so the count stays readable. Long totals scale down to fit without rounding.

Enable **Animate token increases** in Appearance for a brief tick-up when new usage arrives. It is off by default, never animates first load or scope/provider changes, and respects **Reduce motion**. Hidden or minimized windows stop animation work.

Open Mini or Compact and select the **Widget settings** sliders. You can also open **Appearance & settings → Mini & Compact settings** from the dashboard.

Select a **Screen corner** to place the widget inside your current monitor's work area. Drag the widget by its name to move it freely; double-click the name or select **Open dashboard** to expand it. **Transparent background** makes the panel translucent while keeping the text and controls opaque. Use **Hide to tray** to clear your desktop without stopping collection.

Choose **Codex**, **Claude Code**, or **Codex + Claude Code** under **Usage sources**. The choice applies to the dashboard and both small windows. Claude-only mode hides Codex allowance bars; switching back restores your widget choices.

Choose which window to configure. Each view saves its own choices immediately:

- **5-hour usage bar** and **Weekly usage bar** show the percentage of account allowance used, when Codex supplies those windows.
- **Reset times below bars** adds the reported reset time for each visible bar.
- **Input and output totals** follows Today, 7 days or Recorded, matching the main counter.
- **Most-used model** shows the leading model for that token scope. Model attribution depends on local metadata.
- **Activity status** shows import, pause and latest-usage status.

Select **Open this view** to preview the selected layout. Changes to the current view appear behind the settings window. **Done** or Escape closes settings. **Appearance** opens the existing theme and font controls.

Mini starts with both bars and activity status. Compact also includes reset times, input/output totals and the leading model. Disable widgets to reduce the window height. Window size adjusts to keep the remaining controls visible.

## Reading the bars

The filled portion means **used**. Token totals and account allowances measure different things; changing the token scope does not change allowance bars.

These are the latest reports found in local Codex records, not a direct account API feed. Reports older than 15 minutes say **older**. Hover over a bar for the report timestamp and reset details. A report past its reset says **awaiting update** and shows an empty, dimmed bar. **Not reported** means no matching window was found; it does not mean zero usage. Updates arrive when Codex writes another report.

The collector and its background schedule are unchanged. The widgets reuse its snapshots, and the visible-window age timer stops while hidden or minimized. No API key, network polling or AI calls are needed.

## Updating

Exit the app from its tray menu, extract the new release and replace the executable and assets in the existing app folder. Keep the `data` folder. Reopen the same executable path to preserve your taskbar pin. Existing appearance and usage settings are accepted; new widget settings receive the defaults above.
