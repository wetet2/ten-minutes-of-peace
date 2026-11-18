using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed partial class RabbitCharacter
    {
        const float Turn = Mathf.PI * 2;
        const float StrideScale = 1.16f;
        static readonly Vector3 BodyRest = new Vector3(0, 1.68f, 0);
        static readonly Vector3 LeftHipRest = new Vector3(-.32f, RabbitMesh.HipHeight, 0);
        static readonly Vector3 RightHipRest = new Vector3(.32f, RabbitMesh.HipHeight, 0);

        readonly struct WalkGait
        {
            public readonly float Stride, Stance, Lift, BodyRise, ArmSwing, Lean, Sway, Twist, HeadNod, HeadLag;

            public WalkGait(float stride, float stance, float lift, float bodyRise, float armSwing,
                float lean, float sway, float twist, float headNod, float headLag)
            {
                Stride = stride * StrideScale;
                Stance = stance;
                Lift = lift;
                BodyRise = bodyRise;
                ArmSwing = armSwing;
                Lean = lean;
                Sway = sway;
                Twist = twist;
                HeadNod = headNod;
                HeadLag = headLag;
            }
        }

        static WalkGait GaitFor(AnimalKind kind) => kind switch
        {
            AnimalKind.Bear => new WalkGait(1.24f, .64f, .13f, .042f, 15, 4, 2.8f, 4, 1.1f, .55f),
            AnimalKind.Tiger => new WalkGait(1.36f, .60f, .16f, .038f, 21, 6, 2, 7, 1, .78f),
            AnimalKind.Frog => new WalkGait(.94f, .59f, .18f, .054f, 12, 6, 2.3f, 3, 1.6f, .50f),
            AnimalKind.Giraffe => new WalkGait(1.50f, .61f, .14f, .036f, 11, 2, 1.8f, 4, .8f, 1.30f),
            AnimalKind.Elephant => new WalkGait(1.53f, .65f, .11f, .040f, 10, 2, 2.6f, 3, 1, 1.16f),
            AnimalKind.Triceratops => new WalkGait(1.19f, .64f, .13f, .044f, 11, 5, 2.4f, 3.2f, 1.2f, .27f),
            AnimalKind.Crocodile => new WalkGait(1.31f, .63f, .12f, .030f, 13, 8, 2, 7, .9f, 1.05f),
            AnimalKind.Hippo => new WalkGait(1.11f, .64f, .14f, .048f, 9, 3.5f, 3, 3.8f, 1.2f, .63f),
            AnimalKind.Gorilla => new WalkGait(1.43f, .63f, .16f, .046f, 23, 10, 2.8f, 6, 1, .65f),
            _ => new WalkGait(1.10f, .59f, .16f, .046f, 18, 4, 2.2f, 4, 1.3f, .88f)
        };

        WalkGait gait;
        FootSole leftSole, rightSole;

        // Optimize the ground check by retaining the vertices that support the foot
        // at the permitted pitch angles. The shoe is weighted entirely to its leg.
        sealed class FootSole
        {
            readonly Vector2[] points;
            public readonly float RestHeight;

            public FootSole(List<Vector2> vertices)
            {
                var support = new HashSet<Vector2>();
                for (int angle = -30; angle <= 30; angle++)
                {
                    float sine = Mathf.Sin(angle * Mathf.Deg2Rad);
                    float cosine = Mathf.Cos(angle * Mathf.Deg2Rad);
                    float lowest = float.PositiveInfinity;
                    Vector2 contact = default;
                    foreach (var vertex in vertices)
                    {
                        float height = vertex.x * cosine - vertex.y * sine;
                        if (height >= lowest) continue;
                        lowest = height;
                        contact = vertex;
                    }
                    support.Add(contact);
                }
                points = new Vector2[support.Count];
                support.CopyTo(points);
                RestHeight = Height(0, 1);
            }

            public float Height(float sine, float cosine)
            {
                float lowest = float.PositiveInfinity;
                foreach (var point in points)
                    lowest = Mathf.Min(lowest, point.x * cosine - point.y * sine);
                return lowest;
            }
        }

        void PrepareSoles(Mesh surface)
        {
            var feet = new[] { new List<Vector2>(), new List<Vector2>() };
            var vertices = surface.vertices;
            var weights = surface.boneWeights;
            for (int i = 0; i < SkinVertexCount; i++)
            {
                var vertex = vertices[i];
                int side = weights[i].boneIndex0 - 4;
                if (side >= 0 && side < 2 && vertex.y <= .49f && weights[i].weight0 == 1)
                    feet[side].Add(new Vector2(vertex.y - RabbitMesh.HipHeight, vertex.z));
            }
            leftSole = new FootSole(feet[0]);
            rightSole = new FootSole(feet[1]);
        }

        internal float AdvanceWalkPhase(float phase, float worldDistance)
        {
            float stride = gait.Stride * transform.lossyScale.x;
            return Mathf.Repeat(phase + Turn * worldDistance / stride, Turn);
        }

        void WalkPose(float phase, float strength = 1)
        {
            float cycle = Mathf.Repeat(phase, Turn) / Turn;
            if (correctiveReady)
            {
                skin.SetBlendShapeWeight(0, 100 * (1 - strength));
                skin.SetBlendShapeWeight(1, (1 + 99 * cycle) * strength);
            }

            // Shift weight toward the planted foot. The body is lowest as support changes feet.
            float midStance = phase - Mathf.PI * gait.Stance;
            float weightShift = Mathf.Cos(midStance);
            float rise = gait.BodyRise * (.5f * Mathf.Cos(2 * midStance)) * strength;
            float counterSwing = Mathf.Cos(phase - .3f) * strength;
            float roll = gait.Sway * weightShift * strength;
            float delayedBeat = Mathf.Sin(phase * 2 - gait.HeadLag) * strength;
            body.localPosition += new Vector3(-.009f * roll, rise, 0);
            body.localRotation = Quaternion.Euler(-gait.Lean * strength,
                gait.Twist * counterSwing, roll);
            head.localPosition += new Vector3(-.005f * roll, rise * .65f, 0);
            head.localRotation = Quaternion.Euler(gait.HeadNod * delayedBeat - gait.Lean * .21f * strength,
                gait.Twist * .36f * Mathf.Cos(phase - gait.HeadLag) * strength, -roll * .6f);
            leftShoulder.localPosition += Vector3.up * (rise - .006f * roll);
            rightShoulder.localPosition += Vector3.up * (rise + .006f * roll);
            leftArm.localRotation = Quaternion.Euler(-gait.ArmSwing * counterSwing, 0, 40);
            rightArm.localRotation = Quaternion.Euler(gait.ArmSwing * counterSwing, 0, -40);

            PoseFoot(leftLeg, LeftHipRest, leftSole, cycle, strength);
            PoseFoot(rightLeg, RightHipRest, rightSole, Mathf.Repeat(cycle + .5f, 1), strength);
        }

        void PoseFoot(Transform leg, Vector3 hip, FootSole sole, float cycle, float strength)
        {
            float contactSpan = gait.Stride * gait.Stance;
            float foreAft = gait.Stride * cycle - contactSpan * .5f;
            float lift = 0;
            if (cycle > gait.Stance)
            {
                float swing = (cycle - gait.Stance) / (1 - gait.Stance);
                // Match stance velocity at toe-off and landing so the transfer does not jerk.
                foreAft = contactSpan * (.5f - Mathf.SmoothStep(0, 1, swing))
                    + gait.Stride * (1 - gait.Stance) * swing * (1 - swing) * (1 - 2 * swing);
                float arch = Mathf.Sin(Mathf.PI * swing);
                lift = gait.Lift * arch * arch;
            }

            const float soleCenterZ = -.14f;
            float pitch = Mathf.Clamp(-Mathf.Atan2(foreAft, -sole.RestHeight) * Mathf.Rad2Deg - lift * 18, -30, 30) * strength;
            float sine = Mathf.Sin(pitch * Mathf.Deg2Rad);
            float cosine = Mathf.Cos(pitch * Mathf.Deg2Rad);
            float rotatedCenterZ = sole.RestHeight * sine + soleCenterZ * cosine;
            leg.localRotation = Quaternion.Euler(pitch, 0, 0);
            leg.localPosition = hip + new Vector3(0, sole.RestHeight - sole.Height(sine, cosine) + lift * strength,
                soleCenterZ + foreAft * strength - rotatedCenterZ);
        }
    }
}
