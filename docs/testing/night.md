# Night and finale presentation port

`NightView` owns a rough VR valley base, five sanctuary windows and finale lines along supplied saved
curve trees. Configure the actual sanctuary position, then pass authoritative solved-puzzle count (0–6),
visitor/window count (0–5), finale state and bridges to `SetState`. No puzzle progression is inferred
from M1 phase or a wall clock. `Present` applies the valley pose. The director/platform must bind this
after D3/D8; this component does not invent a second content model.

Each puzzle changes the target night level; `Advance`/render updates only blend presentation. Reduced
motion removes the pulse and leaves a monotonic fade; finale lighting is static in that mode. Nothing
moves a camera. In Table mode the VR base is hidden. Passthrough manipulation remains Dario's platform
ownership: subscribe to `TableDimmingRequested` only after the owner verifies a safe table-edge limit.
The default verified limit is **zero**, so no unmeasured Table dimming is requested.

This is source preparation, not finished art, a full-valley binding, a Table edge-visibility pass or H5.
Night music remains unbound; final palette, dimming levels, all-bridge finale and persistent window state
need the director/save integration and owner editor/headset review. Do not ship this greybox as final P1/P11 art.
