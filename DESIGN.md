---
name: PlayDo’s Token Odometer
description: A customizable native desktop instrument for observed AI token usage.
colors:
  canvas: "#171C23"
  surface: "#222A34"
  text: "#F0F3F8"
  muted: "#A9B6C6"
  accent: "#FFBD69"
  signal: "#76D8E5"
  stroke: "#475363"
typography:
  counter:
    fontFamily: "Inter"
    fontSize: "68px"
    fontWeight: 500
  heading:
    fontFamily: "Inter"
    fontSize: "26px"
    fontWeight: 600
  metric:
    fontFamily: "Inter"
    fontSize: "24px"
  body:
    fontFamily: "Inter"
    fontSize: "13px"
rounded:
  control: "8px"
  panel: "12px"
  widget: "14px"
spacing:
  compact: "8px"
  group: "12px"
  section: "18px"
  panel: "22px"
  page-group: "24px"
components:
  button:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: "9px 14px"
  panel:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.panel}"
    padding: "{spacing.panel}"
---

# Design System: PlayDo’s Token Odometer

## Overview

**Creative North Star: "Clean instrument panel"**

The owner selected charcoal surfaces, crisp typography, cyan signals and amber numerals. This is a native operating tool for Windows and macOS: exact data, clear states and direct controls lead the presentation. Preserve the transparent gauge identity while allowing users to choose their own palette, typography and desktop layout.

**Key Characteristics:**
- Exact token totals with supporting detail kept subordinate.
- Flat, compact navigation and native controls.
- Ten coordinated presets with independent interface and number fonts.
- Optional, bounded motion that respects reduced motion and hidden windows.

## Colors

The frontmatter records the default Graphite palette. Semantic roles, rather than fixed Graphite colors, govern controls and dynamically built data views. Each preset in `portable/Desktop/Themes.cs` supplies its own Canvas, Surface, Text, Muted, Accent, Signal and Stroke values; Arctic and Paper also use the native light theme variant.

- **Accent:** the exact counter and selected preset outline.
- **Signal:** activity, usage bars and chart observations; the current-day bar uses Accent.
- **Canvas and Surface:** the main working area and tonal containers/navigation rail.
- **Text and Muted:** primary information and supporting labels.
- **Stroke:** dividers, control outlines and subdued chart tracks.

## Typography

Inter is bundled and addressed explicitly as `fonts:Inter#Inter`; a plain installed-family name must not stand in for the embedded collection. Interface and number fonts are independent user choices. The frontmatter describes their default roles, not a restriction on customization.

Page headings use the heading role; section headings use medium or semibold text at 16–17 DIP. Supporting metrics use the metric role, provider totals use 18 DIP, and secondary labels use 10–12 DIP. The exact dashboard count scales down inside a viewbox rather than rounding or clipping.

List available installed families with broad serif, sans, monospace and display choices. Font names preview their own family; preset names and number samples use their preset pair. Missing families fall back without background downloads. Font templates must accept the empty item passed during native popup recycling. Test real opening, end-of-list selection and reopening, not just isolated labels.

## Layout

Measurements in this native system are device-independent pixels (DIP); `px` in the portable token frontmatter denotes that native scale.

Keep Overview, Providers, Appearance and Settings as separate destinations in a compact 184 DIP rail. The main page uses 30 DIP horizontal and 28 DIP vertical padding, with 24 DIP between page groups. Page containers are bounded to the available content width instead of overflowing a narrow window. Settings never require navigating through the theme gallery to reach provider controls.

Overview progresses from the exact selected total and input/output/cache detail to provider contributions, seven-day observed usage, separate account allowance/reset details, and model attribution. Its content can scroll; the total is the primary reading, with coverage and report freshness nearby. Provider controls remain reachable from their summary buttons.

When Codex is selected, chart and allowance sit beside each other at window widths of at least 1000 DIP, with a 20 DIP gutter; below that width, allowance stacks after the chart. Without Codex selected, the chart uses the full content width. Provider summaries use four columns at 940 DIP and above, otherwise two. Preset previews use three columns at 900 DIP and above, otherwise two.

## Elevation & Depth

Use flat tonal layering and thin dividers rather than decorative shadows. The rail is a Surface against the Canvas; the selected destination returns to Canvas. Native Fluent controls provide hover, pressed, disabled and keyboard-focus feedback. Keep these states intact when applying semantic palette resources.

## Shapes

Controls use the control radius, panels the panel radius, and desktop widgets the widget radius. Borders are thin; the rail has a single trailing divider. Navigation uses authored vector paths at 16 DIP with consistent strokes, alongside text labels. The gauge remains the application’s identity asset.

## Components

**Desktop views.** Preserve Dashboard, Compact, Mini and Tiny. Tiny is a 300 × 68 DIP number-first, borderless, draggable view with small hide/expand actions. Mini and Compact retain independent content choices. Translucency applies to the requested widget background; text stays opaque.

**Observed usage.** The daily chart uses actual selected-provider aggregates. A missing report is distinct from a verified observation; tooltip and coverage text retain that boundary. Model rows pair exact counts with relative share and expand beyond the initial five. Cached input remains a subset of input.

**Account allowance.** Show the locally reported windows separately, with percentage used, reset information and snapshot freshness. Expired or missing reports must remain explicitly labeled. These snapshots are separate from token totals and do not imply billing or subscription-credit access.

**Motion.** Interface transitions and chart effects are optional. Their native 220 ms cubic ease-out sequence uses an already-visible page fade and chart scale, without a continuous decorative loop. The optional token count-up remains a separate 300 ms effect. Reduce motion overrides both. Hiding/minimizing the window stops refresh, count-up and interface-effect timers; leaving Dashboard settles its interface effects.

## Do's and Don'ts

### Do:
- **Do** preserve all providers, themes, font choices, window modes, transparency, exports and widget controls during visual refinements.
- **Do** use semantic palette resources in both XAML and dynamically built controls.
- **Do** verify native font popups, light/dark presets, narrow layouts and hidden/reduced-motion states.
- **Do** distinguish not detected, unselected, importing, tracking, expired and unavailable states.

### Don't:
- **Don't** add background font downloads or continuous decorative animation.
- **Don't** round the primary exact token count to make it fit.
- **Don't** present account allowance as token totals, subscription credits or billing data.
- **Don't** imply support for web conversations or account integrations that the local reports do not provide.
