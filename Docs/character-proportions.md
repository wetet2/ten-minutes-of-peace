# Upright animal character direction

All five animals share a chubby, childlike biped body. Species identity is carried primarily by the head, ears, facial features and small tail/markings.

- No visible neck: a large round animal head rests directly on sloping shoulders.
- Rounded chest, belly and pelvis; no separate oval belly patch.
- Short rounded arms formed as one flowing shape, with no visible elbow/wrist pinch.
- A small underarm separation; no long disconnected-looking limbs.
- A broad continuous torso flowing directly into two thick rounded legs, with no waist pinch, ankle constriction or protruding feet.
- Front and back silhouettes align exactly; facial features only on the front.

The coloring templates are not yet UV-mapped production models. Future 3D assets need pelvis/spine/neck/head, paired upper/lower arm/hand and thigh/shin/foot bones, weights with elbow and knee deformation, and a walk cycle with foot contact. Keep the shoulder/hip proportions consistent across species. The drawing pose is a readable relaxed A-pose; final rig bind pose can be adjusted independently while preserving texture correspondence.

Implementation: `AnimalCanvas.BodyDistance` defines the common silhouette in normalized template coordinates. The animal-specific head is positioned above it with independent facial coordinates.

The latest body uses a hand-shaped cubic contour matching the user reference. The shared silhouette is cached as a signed-distance raster. Internal bones for walking should not introduce visible anatomical pinches into this soft mascot shape.

## Big-head animation proportion

The final template enlarges each species head to 116% while narrowing the shared body to 84% and shortening it to 90%. The feet stay on the same baseline, so the change reads as a larger animated-character head rather than a floating scaled character. A high head-scaling pivot makes tall rabbit ears and giraffe horns grow into the canvas instead of being clipped. The head overlaps the shoulders slightly, matching the neckless mascot construction in the reference.

## Species head revision

`AnimalHead.cs` now owns five distinct head silhouettes and facial overlays. Bear: round ears and oval muzzle. Tiger: cheek lobes, rounded ears and forehead/cheek stripes. Rabbit: upright long ears, inner-ear line, pink nose and whiskers. Frog: broad low head, raised eye lobes and wide smile, without mammalian ears or muzzle. Giraffe: elongated forehead, leaf-shaped ears, capped horns, nostrils and broad muzzle; spots are separated from eyes. Back views preserve silhouette and species markings without showing facial features. The accepted body contour remains unchanged.

The giraffe front muzzle was refined after review: it is now a compact lower-face oval with closer nostrils and a shorter smile. It no longer spans from cheek to cheek or create a second oversized facial outline.
