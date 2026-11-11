# Night and finale presentation port

`NightView` owns a rough VR valley base, five sanctuary windows and finale lines along supplied saved
curve trees. Configure the actual sanctuary position, then pass authoritative solved-puzzle count (0–6),
visitor/window count (0–5), finale state and bridges to `SetState`. No puzzle progression is inferred
from M1 phase or a wall clock. The live `Present(ValleyPresentation)` port applies the valley pose and
reads D8's actual window bitmask and visitor count. `GamePresentation` binds it to the runner.

Each puzzle changes the target night level; `Advance`/render updates only blend presentation. Reduced
motion removes the pulse and leaves a monotonic fade; finale lighting is static in that mode. Nothing
moves a camera. In Table mode the VR base is hidden. `TableDimmingBinding` subscribes to
`TableDimmingRequested` and calls the SDK brightness API with the owner-verified cap.
The default verified limit is **zero**, so no unmeasured Table dimming is requested.

The full valley, finale, saved windows and original music are now bound. This remains greybox presentation,
not finished art, a Table edge-visibility pass or H5. Final palette, dimming levels, audio and persistent
window readability need owner editor/headset review. Do not claim final P1/P11 art from source tests.
