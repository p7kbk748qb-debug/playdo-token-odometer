# Mini and Compact settings

Open Mini or Compact and select **Settings**. You can also open **Appearance & settings → Mini & Compact settings** from the dashboard.

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
