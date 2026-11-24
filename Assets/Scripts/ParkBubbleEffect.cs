using UnityEngine;
using UnityEngine.Rendering;

namespace PeacePark
{
    internal sealed class ParkBubbleEffect : MonoBehaviour
    {
        ParticleSystem bubbles;
        Material material;

        internal static ParkBubbleEffect Create(Transform parent)
        {
            var root = new GameObject("Floating soap bubbles");
            root.transform.SetParent(parent, false);
            var effect = root.AddComponent<ParkBubbleEffect>();
            effect.Configure();
            return effect;
        }

        void Configure()
        {
            bubbles = gameObject.AddComponent<ParticleSystem>();
            bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = bubbles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 192;
            var emission = bubbles.emission;
            emission.enabled = false;
            var shape = bubbles.shape;
            shape.enabled = false;

            var velocity = bubbles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            var still = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Constant(0, 1, 0));
            velocity.x = velocity.z = still;
            // Bubbles drift at mouth height first, then rise as the film fades.
            velocity.y = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
                new Keyframe(0, .04f), new Keyframe(.36f, .10f), new Keyframe(.75f, .85f), new Keyframe(1, 1.25f)));

            var noise = bubbles.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = noise.strengthZ = .16f;
            noise.strengthY = .05f;
            noise.frequency = .8f;
            noise.scrollSpeed = .24f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.rotationAmount = 0;
            noise.sizeAmount = 0;

            var opacity = new Gradient();
            opacity.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .09f),
                    new GradientAlphaKey(1, .58f), new GradientAlphaKey(.48f, .82f), new GradientAlphaKey(0, 1) });
            var color = bubbles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(opacity);

            material = new Material(Resources.Load<Shader>("SoapBubble")) { name = "Soap Bubble Film" };
            var renderer = bubbles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.maxParticleSize = .075f;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        internal void Emit(Vector3 mouth, Vector3 direction)
        {
            if (!bubbles.isPlaying) bubbles.Play();
            var forward = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            var sideways = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < 4; i++)
            {
                var particle = new ParticleSystem.EmitParams
                {
                    position = mouth + direction * Random.Range(.04f, .14f),
                    velocity = forward * Random.Range(.30f, .85f) + sideways * Random.Range(-.40f, .40f) + Vector3.up * .14f,
                    startLifetime = Random.Range(3.1f, 4.4f),
                    startSize = Random.Range(.13f, .34f),
                    startColor = Color.white
                };
                bubbles.Emit(particle, 1);
            }
        }

        internal void Clear() => bubbles.Clear();

        void OnDestroy() => Destroy(material);
    }
}
