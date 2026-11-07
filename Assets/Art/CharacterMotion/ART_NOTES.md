# Character motion assets

The three PNG sheets were created for this project using built-in imagegen, based on the approved character designs. They are newly generated assets, not extracted game artwork. Prompts and input provenance are recorded in generation-prompts.json. Rejected opaque-background attempts were not integrated.

- HeroWalk.png: eight walking frames in a 4 x 2 sheet.
- GuardWalk.png: eight patrol frames in a 4 x 2 sheet; lantern stays at the belt.
- GuardObserve.png: four acting frames in one row: retrieve, raise, observe, lower.

CharacterMotionSetup imports tight sprite rectangles, bottom pivots and stable sprite names/IDs. Runtime renderers normalize sprite height and advance gait by actual displacement; the guard holds the observation frame longer than transitions. Texture CPU readability is disabled after slicing.

The existing idle and climbing sprites remain in use. The partially assembled hero uses its existing separate legs with alternating joint movement. Pixel textures retain the generated alpha; no runtime white-background key is used.

Moving the scene art does not automatically change its foot-ground polygon. Update the authored profiles and front interaction points when changing layout; keep upper and lower platforms separate except for the ladder interaction.