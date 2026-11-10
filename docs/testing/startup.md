# Startup visuals

`Palmfolk.UI.StartupView` is an original, presentation-only component driven by `WorldPresentation` and
the accumulated `GameEvent`s. It shows a seed-lantern, waving Pip, one descending flat-palm demonstration
and controller put-down silhouettes. It never bypasses Core's planting/startup safeguards.

Place its world-space transform at the platform's comfortable invitation pose (E10's 30–45 cm, 10–30°
down is a starting range, not a measured comfort pass). Do not attach a camera-moving effect. The platform
passes frames and events, and handles visibility/legibility at actual viewing distance.

Three input states stay distinct: controllers held shows the put-down demonstration; controllers just
set down with a usable hand removes it and awaits the resting second; no usable hand waits. Short text
appears only when Core's `ShowStartupText` is true. The T18 line is exact and driven by `QualityPromptDue`,
cleared by recovery completion/planting. No other screen-space UI is created. Reduced motion keeps the
demonstration at its lowered pose and suppresses repeated lowering motion.

`PalmfolkView.Consume` uses the actual `Planted` event for a short valley growth; reduced motion shows
the full scale immediately. It changes only presentation scale, never stored valley-local bridges.

This implements startup visual states, not final art or scene wiring. In-headset
placement/readability, permitted prompts and time-to-plant/cold-start remain owner checks. No startup
timing target is claimed from an editor state test.
