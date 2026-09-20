# Terminal concept — imagegen

Mode: built-in `image_gen.imagegen`, with local image references. No CLI/API fallback.

Selected final image: [terminal-compact-concept-final.png](terminal-compact-concept-final.png).
This is a generated design reference, not a screenshot of the implementation.
The interactive implementation reuses native Valheim widgets, fonts and items;
the generated pixels are not shipped as UI assets.

Initial design brief: use the supplied BottomlessChest and SCS screenshots as
references for a compact native Valheim wood panel, eight columns/four visible
rows, title and capacity, search beside display ordering, a dedicated scrollbar,
and one row with Take all/Stack all/Organize. Keep native item gestures.
The first concept's header weight and bottom help line were superseded by the
user's subsequent corrections. Reference inputs are retained as
`bottomless-layout-reference.png` and `terminal-0.7.5-layout-reference.png`.

## Weight correction prompt

Target: `terminal-compact-concept.png`; supporting reference:
`bottomless-layout-reference.png`. Output: `terminal-compact-concept-native-weight.png`.

```text
Edit the first image, the new compact Valheim terminal concept. Preserve its native wood frame, all 8 columns and 4 rows, item icons, title GENERAL, capacity line, Red button, search, sort control, dedicated scrollbar, three footer buttons and help text, with the same styling and layout. Change ONLY the weight presentation: remove 'Peso: 618' from the header. Instead reuse Valheim's native external chest weight pouch/tab, as shown on the right side of the second reference image (which displays 96). Add that same native weight tab attached just outside the terminal's right edge, near the lower-right grid area, with a small dark weight-bag icon and the readable gold value '618'. This is the total network weight, with no slash or weight limit. Extend the framing slightly to the right if necessary so the native weight tab is fully visible. Keep the scrollbar inside the main pane and free of overlap with footer or weight tab. No new UI elements beyond the native weight indicator, no modern styling, no explanations or callouts.
```

## Final prompt

Target: `terminal-compact-concept-native-weight.png`.
Output: `terminal-compact-concept-final.png`.

```text
Make one precise final edit to this Valheim terminal UI concept. Remove the bottom instruction/help line 'Arrastrar · Shift: dividir · Ctrl: mover pila' completely. Tighten the bottom wooden frame upward so only a small normal margin remains beneath the three action buttons; do not leave a blank strip where the help was. Preserve absolutely everything else: eight columns/four rows, native wood styling, title and capacity, search and sort, Red, all three footer action buttons, scrollbar bounded beside the grid, and the native external weight pouch showing 618. No extra instructions, labels, decoration or features. This is the final compact design.
```

Visual inspection: final image retains eight columns/four rows, full-width grid,
separate scrollbar, three actions, external weight pouch and no help strip.
