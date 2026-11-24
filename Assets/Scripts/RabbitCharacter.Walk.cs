using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    public sealed partial class RabbitCharacter
    {
        const float Turn = Mathf.PI * 2;
        const float StrideScale = 1.16f;
        const float JogStrideScale = 2.20f;
        static readonly Vector3 BodyRest = new Vector3(0, 1.68f, 0);
        static readonly Vector3 LeftHipRest = new Vector3(-.32f, RabbitMesh.HipHeight, 0);
        static readonly Vector3 RightHipRest = new Vector3(.32f, RabbitMesh.HipHeight, 0);

        readonly struct WalkGait
        {
            public readonly float Stride, Stance, Lift, Rebound, ArmSwing, Lean, Sway, Twist, HeadNod, HeadLag;

            public WalkGait(float stride, float stance, float lift, float rebound, float armSwing,
                float lean, float sway, float twist, float headNod, float headLag)
            {
                Stride = stride * StrideScale;
                Stance = stance;
                Lift = lift;
                Rebound = rebound;
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
            AnimalKind.Bear => new WalkGait(1.24f, .466f, .199f, .235f, 18, 4, 2.8f, 4, 2.1f, .55f),
            AnimalKind.Tiger => new WalkGait(1.36f, .446f, .257f, .263f, 23, 5, 2, 7, 2.3f, .78f),
            AnimalKind.Frog => new WalkGait(.94f, .404f, .31f, .36f, 16, 5, 2.3f, 3, 3.4f, .50f),
            AnimalKind.Giraffe => new WalkGait(1.50f, .456f, .23f, .198f, 15, 2, 1.8f, 4, 1.7f, 1.30f),
            AnimalKind.Elephant => new WalkGait(1.53f, .477f, .171f, .173f, 13, 2, 2.6f, 3, 1.7f, 1.16f),
            AnimalKind.Triceratops => new WalkGait(1.19f, .470f, .198f, .206f, 14, 4, 2.4f, 3.2f, 2f, .27f),
            AnimalKind.Crocodile => new WalkGait(1.31f, .467f, .185f, .194f, 17, 6, 2, 7, 1.7f, 1.05f),
            AnimalKind.Hippo => new WalkGait(1.11f, .474f, .184f, .203f, 13, 3.5f, 3, 3.8f, 1.9f, .63f),
            AnimalKind.Gorilla => new WalkGait(1.43f, .453f, .231f, .254f, 25, 8, 2.8f, 6, 2f, .65f),
            _ => new WalkGait(1.10f, .423f, .282f, .318f, 21, 4, 2.2f, 4, 3f, .88f)
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

        float StrideLength(float jog) => gait.Stride * Mathf.Lerp(1, JogStrideScale, jog);

        internal float AdvanceWalkPhase(float phase, float worldDistance, float jog = 0)
        {
            float stride = StrideLength(jog) * transform.lossyScale.x;
            return Mathf.Repeat(phase + Turn * worldDistance / stride, Turn);
        }

        void WalkPose(float phase, float strength = 1, float jog = 0)
        {
            float cycle = Mathf.Repeat(phase, Turn) / Turn;
            if (correctiveReady)
            {
                skin.SetBlendShapeWeight(0, 100 * (1 - strength));
                skin.SetBlendShapeWeight(1, (1 + 99 * cycle) * strength);
            }

            // Absorb each landing, then extend over the planted foot before a brief lift-off.
            float stride = StrideLength(jog);
            float stance = gait.Stance * Mathf.Lerp(1, .79f, jog);
            float lean = gait.Lean + 7.5f * jog;
            float halfStep = Mathf.Repeat(cycle, .5f);
            float beat = Turn * (halfStep * 2 - .17f);
            float rebound = gait.Rebound * (1 + .27f * jog) * strength;
            float rise = rebound * (.08f - .5f * Mathf.Cos(beat));
            float headRise = rebound * (.07f - .39f * Mathf.Cos(beat - gait.HeadLag * .58f));
            float air = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(stance, .5f, halfStep));
            model.localPosition += Vector3.up * ((.5f - stance) * .83f * air * air * strength);

            float midStance = phase - Mathf.PI * stance;
            float weightShift = Mathf.Cos(midStance);
            float counterSwing = Mathf.Cos(phase - .3f) * strength;
            float roll = gait.Sway * weightShift * strength;
            float delayedBeat = Mathf.Sin(beat - gait.HeadLag * .58f) * strength;
            body.localPosition += new Vector3(-.009f * roll, rise, 0);
            body.localRotation = Quaternion.Euler(-lean * strength,
                gait.Twist * counterSwing, roll);
            head.localPosition += new Vector3(-.005f * roll, headRise, 0);
            head.localRotation = Quaternion.Euler(gait.HeadNod * delayedBeat - lean * .21f * strength,
                gait.Twist * .36f * Mathf.Cos(phase - gait.HeadLag) * strength, -roll * .6f);
            leftShoulder.localPosition += Vector3.up * (rise - .006f * roll);
            rightShoulder.localPosition += Vector3.up * (rise + .006f * roll);
            float armSwing = gait.ArmSwing * (1 + .40f * jog) * counterSwing;
            leftArm.localRotation = Quaternion.Euler(-armSwing, 0, 40);
            rightArm.localRotation = Quaternion.Euler(armSwing, 0, -40);

            float lift = gait.Lift * (1 + .36f * jog);
            PoseFoot(leftLeg, LeftHipRest, leftSole, cycle, strength, stride, stance, lift);
            PoseFoot(rightLeg, RightHipRest, rightSole, Mathf.Repeat(cycle + .5f, 1), strength, stride, stance, lift);
        }

        void PoseFoot(Transform leg, Vector3 hip, FootSole sole, float cycle, float strength,
            float stride, float stance, float swingHeight)
        {
            float contactSpan = stride * stance;
            float foreAft = stride * cycle - contactSpan * .5f;
            float lift = 0;
            if (cycle > stance)
            {
                float swing = (cycle - stance) / (1 - stance);
                // Match stance velocity at toe-off and landing so the transfer does not jerk.
                foreAft = contactSpan * (.5f - Mathf.SmoothStep(0, 1, swing))
                    + stride * (1 - stance) * swing * (1 - swing) * (1 - 2 * swing);
                float arch = Mathf.Sin(Mathf.PI * swing);
                lift = swingHeight * arch * arch;
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
