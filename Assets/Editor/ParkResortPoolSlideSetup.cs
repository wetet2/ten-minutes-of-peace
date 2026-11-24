using UnityEngine;
using UnityEngine.Rendering;
using static PeacePark.Editor.ParkScenery;
using static PeacePark.ParkResortRiver;
using static PeacePark.ParkResortSlide;

namespace PeacePark.Editor
{
    internal sealed partial class ParkResortPoolSetup
    {
        const float TrackWidthScale = 1.28f;
        ParkResortSlide MakeWaterSlide(Transform root)
        {
            var slide = Group("West terrace water slide", root, Vector3.zero).gameObject.AddComponent<ParkResortSlide>();
            var tower = Group("Limestone slide tower", slide.transform, At(Tower, 0));
            MakeSlideTower(tower);
            var course = Group("Spiral chute into the main pool", slide.transform, Vector3.zero);
            var shell = Paint("Sea-glass slide shell", "#77B8B3");
            var enamel = Paint("Aquamarine slide lining", "#9BDDD6");
            var water = new Material(Shader.Find("PeacePark/Resort Water")) { name = "Flowing water on the spiral chute" };
            water.SetColor("_Tint", new Color(.075f, .59f, .62f, .45f));
            water.SetFloat("_Current", 1);
            float edge = TrackHalfWidth;
            MakeMesh("Continuous aqua chute", SlideRibbon(new[] {
                new Vector2(-edge, .48f), new Vector2(-.94f, .25f), new Vector2(-.78f, .07f), new Vector2(-.59f, 0),
                new Vector2(.59f, 0), new Vector2(.78f, .07f), new Vector2(.94f, .25f), new Vector2(edge, .48f)
            }), enamel, course);
            MakeMesh("Solid underside of the chute", SlideRibbon(new[] {
                new Vector2(-edge - .055f, .49f), new Vector2(-1.01f, .22f), new Vector2(-.85f, .01f), new Vector2(-.63f, -.16f),
                new Vector2(.63f, -.16f), new Vector2(.85f, .01f), new Vector2(1.01f, .22f), new Vector2(edge + .055f, .49f)
            }, true), shell, course);
            MakeMesh("Water flowing to the pool", SlideRibbon(new[] { new Vector2(-.68f, .026f), new Vector2(.68f, .026f) }), water, course, false);
            MakeMesh("Water streaming into the landing", SlideWaterfall(), water, course, false);
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * edge;
                MakeMesh("Cream slide coping", SlideRibbon(new[] {
                    new Vector2(x - .070f, .485f), new Vector2(x, .532f), new Vector2(x + .070f, .485f)
                }), ivory, course);
            }
            var supports = new Pieces();
            foreach (float t in new[] { .463f, .501f, .569f, .636f, .708f, .779f, .871f, .937f })
            {
                var point = TrackPoint(t);
                if (point.y < .54f) continue;
                supports.Add(softBox, ivory, new Vector3(point.x, .18f, point.z), new Vector3(.62f, .25f, .64f));
                supports.Add(cube, ivory, new Vector3(point.x, (point.y + DeckHeight) * .5f - .05f, point.z),
                    new Vector3(.36f, point.y - DeckHeight - .10f, .36f));
                Block(point.x, point.z, .40f, .40f);
            }
            supports.Build(Group("Stone chute supports", course, Vector3.zero));
            BlockLowChute();
            MakeSlidePoolExit(slide.transform);
            slide.Configure(MakeSlideSplash(slide.transform), MakeSlideRipples(slide.transform));
            return slide;
        }

        void BlockLowChute()
        {
            const int sections = 15;
            float edge = PoolCenter.x - PoolHalf.x;
            var previous = TrackPoint(.79f);
            for (int i = 1; i <= sections && previous.x < edge; i++)
            {
                var next = TrackPoint(Mathf.Lerp(.79f, 1, i / (float)sections));
                var axis = next - previous;
                float side = (TrackHalfWidth * TrackWidthScale + .25f) * Mathf.Abs(axis.normalized.x);
                obstacles.Add(Rect.MinMaxRect(Mathf.Min(previous.x, next.x) - .26f, Mathf.Min(previous.z, next.z) - side,
                    Mathf.Min(edge, Mathf.Max(previous.x, next.x) + .26f), Mathf.Max(previous.z, next.z) + side));
                previous = next;
            }
        }

        void MakeSlideTower(Transform tower)
        {
            const float halfWidth = 2.79f, halfDepth = 1.89f, roofHeight = LaunchHeight + 3.10f;
            var pieces = new Pieces();
            foreach (int side in new[] { -1, 1 })
            foreach (int end in new[] { -1, 1 })
            {
                var foot = new Vector3(side * halfWidth, DeckHeight, end * halfDepth);
                pieces.Add(softBox, honey, foot + Vector3.up * .19f, new Vector3(.92f, .39f, .92f));
                pieces.Add(cube, ivory, foot + Vector3.up * ((roofHeight - DeckHeight) * .5f), new Vector3(.50f, roofHeight - DeckHeight, .50f));
                for (int level = 1; level <= Flights; level++)
                {
                    float y = Mathf.Lerp(DeckHeight, LaunchHeight, level / (float)Flights);
                    pieces.Add(cube, honey, new Vector3(foot.x, y - .12f, foot.z), new Vector3(.67f, .20f, .67f));
                }
            }
            for (int flight = 0; flight < Flights; flight++)
            {
                var from = StairStart(flight) - At(Tower, 0);
                var to = StairEnd(flight) - At(Tower, 0);
                var axis = to - from;
                float treadWidth = Mathf.Abs(axis.x) / 12;
                for (int step = 0; step < 12; step++)
                {
                    var foot = Vector3.Lerp(from, to, (step + .5f) / 12);
                    foot.y = Mathf.Lerp(from.y, to.y, step / 12f);
                    pieces.Add(cube, wood, foot - Vector3.up * .15f, new Vector3(treadWidth + .015f, .30f, 1.44f));
                    if (step % 2 == 0)
                        foreach (int side in new[] { -1, 1 })
                            pieces.Add(cube, darkWood, foot + new Vector3(0, .407f, side * .69f), new Vector3(.055f, .852f, .059f));
                }
                foreach (int side in new[] { -1, 1 })
                {
                    var hand = new Vector3(0, .86f, side * .69f);
                    AddHandrail(pieces, from + hand, to + hand);
                }
                if (flight < Flights - 1)
                {
                    pieces.Add(cube, ivory, new Vector3(to.x, to.y - .134f, 0), new Vector3(1.67f, .269f, 3.50f));
                    AddGuard(pieces, new Vector3(Mathf.Sign(to.x) * 2.96f, to.y, -1.70f), new Vector3(Mathf.Sign(to.x) * 2.96f, to.y, 1.70f));
                }
            }
            pieces.Add(cube, ivory, new Vector3(0, roofHeight - .02f, 0), new Vector3(6.65f, .26f, 4.71f));
            pieces.Build(tower);
            MakeUpperSlideLanding(tower);
            BuildHotelRoof(tower, new Vector3(0, roofHeight + .12f, 0), 7.34f, 5.32f, 1.37f);
            Block(Tower.x, Tower.y, 3.29f, 2.45f);
        }

        void MakeUpperSlideLanding(Transform tower)
        {
            var landing = Group("Top stair landing and launch deck", tower, Vector3.zero);
            var lastStep = StairEnd(Flights - 1) - At(Tower, 0);
            const float left = -3.51f, right = 3.27f, front = -2.22f, back = 2.23f;
            const float wellEnd = 2.54f, thresholdWidth = .15f;
            float wellFront = lastStep.z - .77f, wellBack = lastStep.z + .77f;
            var pieces = new Pieces();
            // The opening exposes the complete last flight. The stone threshold forms its final riser.
            Slab(left, lastStep.x - thresholdWidth, front, back, ivory);
            Slab(lastStep.x - thresholdWidth, lastStep.x, front, wellFront, ivory);
            Slab(lastStep.x - thresholdWidth, lastStep.x, wellBack, back, ivory);
            Slab(lastStep.x - thresholdWidth, lastStep.x, wellFront, wellBack, wood);
            Slab(lastStep.x, wellEnd, front, wellFront, ivory);
            Slab(lastStep.x, wellEnd, wellBack, back, ivory);
            Slab(wellEnd, right, front, back, ivory);

            Guard(left + .13f, front + .12f, left + .13f, back - .12f);
            Guard(right - .14f, front + .12f, right - .14f, back - .12f);
            Guard(left + .13f, back - .12f, right - .14f, back - .12f);
            float launchHalfWidth = (TrackHalfWidth + .04f) * TrackWidthScale;
            Guard(left + .13f, front + .12f, -launchHalfWidth, front + .12f);
            Guard(launchHalfWidth, front + .12f, right - .14f, front + .12f);
            Guard(lastStep.x+.03f, wellFront-.045f, wellEnd+.04f, wellFront-.045f);
            Guard(lastStep.x+.03f, wellBack+.045f, wellEnd+.04f, wellBack+.045f);
            Guard(wellEnd+.04f, wellFront-.045f, wellEnd+.04f, wellBack+.045f);
            pieces.Build(landing);

            void Slab(float xMin, float xMax, float zMin, float zMax, Material material) =>
                pieces.Add(cube, material, new Vector3((xMin + xMax) * .5f, LaunchHeight - .145f, (zMin + zMax) * .5f),
                    new Vector3(xMax - xMin, .29f, zMax - zMin));

            void Guard(float x1, float z1, float x2, float z2) =>
                AddGuard(pieces, new Vector3(x1, LaunchHeight, z1), new Vector3(x2, LaunchHeight, z2));
        }

        void AddHandrail(Pieces pieces, Vector3 from, Vector3 to)
        {
            var axis = to - from;
            pieces.Add(cube, wood, (from + to) * .5f, new Vector3(.10f, .11f, axis.magnitude + .07f), Quaternion.LookRotation(axis));
        }

        void AddGuard(Pieces pieces, Vector3 from, Vector3 to)
        {
            int posts = Mathf.CeilToInt(Vector3.Distance(from, to) / .24f);
            for (int i = 0; i <= posts; i++)
                pieces.Add(cube, darkWood, Vector3.Lerp(from, to, i / (float)posts) + Vector3.up * .41f,
                    new Vector3(.048f, .82f, .052f));
            AddHandrail(pieces, from + Vector3.up * .83f, to + Vector3.up * .83f);
        }

        void MakeSlidePoolExit(Transform root)
        {
            float edge = PoolCenter.x - PoolHalf.x;
            var bronze = Paint("Brushed pool ladder metal", "#A7B6B0");
            var parts = new Pieces();
            foreach (int side in new[] { -1, 1 })
            {
                float z = Exit.z + side * .43f;
                parts.Beam(cylinder, bronze, new Vector3(edge + .18f, -.84f, z), new Vector3(edge + .18f, .47f, z), .078f);
                parts.Beam(cylinder, bronze, new Vector3(edge + .18f, .47f, z), new Vector3(edge - .10f, .67f, z), .078f);
                parts.Beam(cylinder, bronze, new Vector3(edge - .10f, .67f, z), new Vector3(edge - .38f, .50f, z), .078f);
                parts.Beam(cylinder, bronze, new Vector3(edge - .38f, .50f, z), new Vector3(edge - .38f, DeckHeight, z), .078f);
            }
            for (int i = 0; i < 4; i++)
                parts.Add(softBox, bronze, new Vector3(edge + .19f, -.80f + i * .226f, Exit.z), new Vector3(.23f, .055f, .88f));
            parts.Build(Group("Pool exit ladder at the far end", root, Vector3.zero));
        }

        ParticleSystem MakeSlideSplash(Transform root)
        {
            var landing = Group("Slide landing spray", root, ImpactPoint + Vector3.up * .064f);
            var splash = landing.gameObject.AddComponent<ParticleSystem>();
            var main = splash.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.2f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.65f, .98f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.58f, 4.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(.087f, .188f);
            main.gravityModifier = 1.04f;
            var emission = splash.emission;
            emission.enabled = false;
            var shape = splash.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 58;
            shape.radius = .26f;
            shape.rotation = new Vector3(-90, 0, 0);
            var size = splash.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, .78f), new Keyframe(.18f, 1), new Keyframe(.73f, .69f), new Keyframe(1, 0)));
            var renderer = splash.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = sphere;
            renderer.sharedMaterial = new Material(Shader.Find("Unlit/Color")) {
                name = "Bright pale aqua water droplets", color = new Color(.80f, .965f, 1)
            };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return splash;
        }

        MeshRenderer MakeSlideRipples(Transform root)
        {
            var material = new Material(Shader.Find("PeacePark/Cathedral Glow")) {
                name = "White turquoise splash ripples", color = new Color(.66f, .96f, 1), renderQueue = 3020
            };
            const int segments = 56, bands = 3;
            var vertices = new Vector3[2 * (segments + 1) * bands];
            var uv = new Vector2[vertices.Length];
            var faces = new int[2 * segments * (bands - 1) * 6];
            int face = 0;
            for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
                float radius = ring == 0 ? .66f : 1f;
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                for (int band = 0; band < bands; band++)
                {
                    int index = (ring * (segments + 1) + i) * bands + band;
                    float wave = 1 + .044f * Mathf.Sin(5 * angle + .9f * ring) + .024f * Mathf.Sin(9 * angle);
                    vertices[index] = direction * (radius * wave + (band - 1) * .041f);
                    uv[index] = new Vector2(band * .5f, .5f);
                    if (i == segments || band == bands - 1) continue;
                    faces[face++] = index; faces[face++] = index + bands; faces[face++] = index + 1;
                    faces[face++] = index + 1; faces[face++] = index + bands; faces[face++] = index + bands + 1;
                }
            }
            var part = MakeMesh("Expanding slide impact ripples", MeshOf("Two soft irregular ripple rings", vertices, faces, uv), material, root, false);
            part.localPosition = ImpactPoint + Vector3.up * .026f;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.enabled = false;
            return renderer;
        }

        static Mesh SlideRibbon(Vector2[] profile, bool underside = false)
        {
            const int spans = 320;
            int width = profile.Length;
            var vertices = new Vector3[(spans + 1) * width];
            var uv = new Vector2[vertices.Length];
            var faces = new int[spans * (width - 1) * 6];
            int face = 0;
            for (int i = 0; i <= spans; i++)
            {
                float t = i / (float)spans;
                var point = TrackPoint(t);
                var side = Vector3.Cross(Vector3.up, TrackTangent(t)).normalized;
                for (int col = 0; col < width; col++)
                {
                    int index = i * width + col;
                    vertices[index] = point + side * (profile[col].x * TrackWidthScale) + Vector3.up * profile[col].y;
                    uv[index] = new Vector2(t, col / (float)(width - 1));
                    if (i == spans || col == width - 1) continue;
                    int a = index, b = index + width, c = index + 1, d = b + 1;
                    faces[face++] = a; faces[face++] = underside ? c : b; faces[face++] = underside ? b : c;
                    faces[face++] = c; faces[face++] = underside ? d : b; faces[face++] = underside ? b : d;
                }
            }
            return MeshOf("Continuous spiral slide profile", vertices, faces, uv);
        }

        static Mesh SlideWaterfall()
        {
            const int sections = 12;
            var vertices = new Vector3[(sections + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var faces = new int[sections * 6];
            for (int i = 0; i <= sections; i++)
            {
                float t = i / (float)sections, halfWidth = Mathf.Lerp(.68f, .50f, t) * TrackWidthScale;
                var center = Outlet + new Vector3(.59f * t, .026f - .49f * t * t, 0);
                int left = i * 2;
                vertices[left] = center + Vector3.forward * halfWidth;
                vertices[left + 1] = center - Vector3.forward * halfWidth;
                uv[left] = new Vector2(t * .055f, 0);
                uv[left + 1] = new Vector2(t * .055f, 1);
                if (i == sections) continue;
                int face = i * 6;
                faces[face] = left; faces[face + 1] = left + 2; faces[face + 2] = left + 1;
                faces[face + 3] = left + 1; faces[face + 4] = left + 2; faces[face + 5] = left + 3;
            }
            return MeshOf("Curved sheet at slide exit", vertices, faces, uv);
        }
    }
}
