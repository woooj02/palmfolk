# Recovery hand and solution hints

Verification label: Unity editor with synthetic inputs; no headset or readability pass.

`HandGuidanceView` consumes the valley presentation and the real event stream from `GamePresentation`.
It draws `GhostHand` at the observed valley-local joint positions only while recovering. Known joints
and bones form a solid carved hand; unusable joints are omitted. The cached geometry never animates.
If a resumed recovery has no `GhostHand`, the existing carved `GhostRoute` remains the fallback; no
missing joint positions are invented.

`HintDue` enables the solution illustration. Merely having a `Hint` never displays it. Progress events
(boarding, build start/completion, checkpoint, puzzle/visitor change or replant) dismiss it. Recovery,
planting and pause hide it. A null hint stays hidden, including a ledge that cannot fit the rules.

Core owns the passing `Hint.Tree`, its `Shape` and `Rise`. The view poses an original stylised hand along
that tree: a single extended finger with curled neighbours, four parallel fingers, a raised arch or a
spread into two finger groups. The Board example puts fingertips at Pip and the palm toward the next
island. The preferred/owning hand determines the thumb side. All route dots retain the actual supplied
tree points. The illustration is not an XR joint sample and is not claimed to be an anatomically validated
physical solution; natural reach/spread and H1 still need owner tests.

The hint has broken outlines, separate knuckle beads and translucent surfaces, plus travelling beads
along the passing tree. It bobs slightly; reduced motion stops both bobbing and bead travel. The carved
ghost remains solid and still. Their difference is geometric as well as colour-based. High contrast
brightens their materials. Neither component moves a camera or changes gameplay/settings/save state.

Editor regressions use each owning hand for an actual tracking-loss snapshot, check that its geometry
stays fixed, wait for the real `HintDue`, check progress dismissal/reduced motion and draw all five
published hint shapes from an actual chapter run. These checks do not certify headset legibility,
transparency, no-slip contact, frame rate or the final art.
