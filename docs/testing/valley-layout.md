# Valley layout numbers

**Headless design calculation, not a seated-reach, FoV, Unity editor or headset pass.**
This is the S7 handoff until D3 publishes content types. Requirements and build plan are unchanged.
All numbers are starting values at default world scale; planting scales the whole layout.

## Coordinates and footprint

Metres in valley-local space: X is sideways, Y up, +Z away from the player. The planted palm is the
origin. The intended facing puts the player on the -Z side. The world origin/rotation is D2's concern.
Reserve X [-0.30, 0.30], Z [-0.24, 0.21]: **60 × 45 cm**, including decoration and the waystone.
Island tops are 2 cm above the palm, except the two raised lookouts. These are deck-top heights,
not island centre heights. Centre positions form a shallow arc, with side islands behind it.

| Island | X | top Y | Z | top radius |
|---|---:|---:|---:|---:|
| S Camp | -0.270 | 0.020 | -0.150 | 0.020 |
| A Ledge | -0.110 | 0.020 | -0.100 | 0.020 |
| C Market | 0.050 | 0.020 | -0.100 | 0.020 |
| Sanctuary | 0.210 | 0.020 | -0.150 | 0.020 |
| G Garden | -0.250 | 0.020 | 0.030 | 0.025 |
| L1 Lookout | 0.210 | 0.060 | -0.100 | 0.020 |
| L2 Lookout | 0.140 | 0.060 | 0.050 | 0.020 |
| E Rock | -0.100 | 0.020 | 0.010 | 0.020 |
| G1 Garden plot | -0.105 | 0.020 | 0.150 | 0.020 |
| G2 Garden plot | -0.070 | 0.020 | 0.070 | 0.020 |

No island extends outside the reserved footprint. Top radii describe the numerical greybox, not
final art bounds. The two garden plots are the Twins' destinations, not extra puzzles.

## Anchor construction and gaps

For each edge, take the horizontal unit vector from source centre to destination centre. Put its
source anchor at source centre + source radius × unit vector, its destination anchor at destination
centre - destination radius × unit vector. Each anchor's Y is its island's top height. Thus horizontal
gap = centre distance minus the two radii. Keep the anchors distinct for separate branches.

| Edge | Horizontal gap (cm) | Height change (cm) | Use |
|---|---:|---:|---|
| S → A | 12.763 | 0 | Board, First Bridge, finale rebuild |
| A → C | 12.000 | 0 | Width, reused by Bram |
| C → Sanctuary | 12.763 | 0 | Finale new plank |
| C → L1 | 12.000 | +4 | Fork branch |
| C → L2 | 13.493 | +4 | Fork branch |
| C → E | 14.601 | 0 | Arch; Merchant cart later |
| A → G | 14.605 | 0 | Gardener; Lantern-maker later |
| G → G1 | 14.322 | 0 | Twins branch |
| G → G2 | 13.939 | 0 | Twins branch |
| L1 → L2 | 12.553 | 0 | Stargazer, above the existing Fork |

Even including the 4 cm elevation changes, straight anchor distances stay within 12–15 cm.
Raised lookout anchors allow a Stargazer deck at 6 cm to clear a nominal 2 cm Fork deck by
4 cm, exceeding the walker's 3.5 cm headroom. This is a clearance budget only: the actual
curves, crossing location, slopes, widths and obstacle bounds still need D4's validation.

## Six puzzles and five visitors

1. Board: S → A on the live hand, no bridge required.
2. First Bridge: A → S, A's 1.5 cm usable ledge width caps the crossing below cart capacity.
3. Width: A → C, require the cart's **full** rule from the outset: width ≥2.5 cm, slope ≤25°,
   headroom ≥5 cm. A group alone cannot certify the later cart route.
4. Fork: C → L1 and C → L2. A steeper finger must be refused without refusing a good branch.
5. Arch: C → E, any valid geometry. Rock bounds are **not settled** here; H1 remains owner-run.
6. Finale: visibly widen A's S-facing ledge, rebuild S → A as plank, reuse A → C, build
   C → Sanctuary. Exactly three cart crossings; Fork and Arch stay optional for the cart.

Visitor handoff: Gardener uses new A → G walker route; Merchant upgrades C → E for the cart;
Twins add G → G1/G2; Stargazer adds L1 → L2 above the Fork; Lantern-maker upgrades the player's
A → G route for a cart. Keep old bridges until replacement validation passes (R4). Before each
visitor and the finale, validate every required edge (V3), including already-saved bridges.

## Tracking, reach and open questions

The main three crossings run mostly sideways. Some side edges are diagonal; **L1 → L2 is only
25° off the forward axis** with this candidate. It must not be called a T14 pass. The Fork triangle
and its Stargazer reuse need headset evaluation and may require adjusted anchor zones or a revised
layout. Do not enable EX4 (valley turning) without its build-plan gate and the owner's decision.

Likewise, a footprint check cannot prove E3/E6 seated comfort, V5 reach with either hand, natural
Arch slopes, or E18's active-step FoV. At the furthest corners this layout can require head turning.
Run the owner sheets in table, lap and air planting, with small/medium/large calibration, left-only
and right-only. Measure support time and directional dropouts. Final H1/H2 values override these
starting numbers. Waystone placement/target angular widths are S4's task and need their own FoV check.

S7 completion still needs D3 data types, a content test through the actual director and cart rules,
and owner-run reach verification. No runtime content model is invented in parallel with Dario's.
