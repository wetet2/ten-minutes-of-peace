using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PeacePark
{
    public sealed class ParkVillageDeer : MonoBehaviour
    {
        const int SamplesPerLeg = 24;
        [SerializeField] Animator animator;
        [SerializeField] AnimationClip idleClip, walkClip;
        [SerializeField] Vector2[] route;
        [SerializeField] float strideSpeed, startingPhase;

        Vector3[] points;
        float[] distances;
        float distance, elapsed, walkBlend;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable walk;

        public void Configure(Animator rig, AnimationClip idle, AnimationClip walking, Vector2[] waypoints,
            float speed, float phase)
        {
            animator = rig;
            idleClip = idle;
            walkClip = walking;
            route = waypoints;
            strideSpeed = speed;
            startingPhase = phase;
            PrepareRoute();
            UpdatePosition();
        }

        void OnEnable()
        {
            if (route == null || route.Length < 3 || !animator || !idleClip || !walkClip) return;
            PrepareRoute();
            elapsed = startingPhase * 24;
            walkBlend = 1;
            graph = PlayableGraph.Create("Village deer animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var idle = AnimationClipPlayable.Create(graph, idleClip);
            walk = AnimationClipPlayable.Create(graph, walkClip);
            graph.Connect(idle, 0, mixer, 0);
            graph.Connect(walk, 0, mixer, 1);
            mixer.SetInputWeight(1, 1);
            AnimationPlayableOutput.Create(graph, "Deer", animator).SetSourcePlayable(mixer);
            graph.Play();
            UpdatePosition();
        }

        void OnDisable()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        void Update()
        {
            if (!graph.IsValid()) return;
            elapsed += Time.deltaTime;
            bool moving = elapsed % 18f < 15.4f;
            walkBlend = Mathf.MoveTowards(walkBlend, moving ? 1 : 0, Time.deltaTime * 3);
            mixer.SetInputWeight(0, 1 - walkBlend);
            mixer.SetInputWeight(1, walkBlend);
            var position = AtDistance(distance);
            var before = AtDistance(distance - .42f);
            var after = AtDistance(distance + .42f);
            float bend = Vector3.Angle(position - before, after - position);
            float pace = Mathf.Lerp(1, .58f, Mathf.InverseLerp(10, 48, bend));
            walk.SetSpeed(walkBlend > 0 ? pace : 0);
            distance = Mathf.Repeat(distance + strideSpeed * pace * walkBlend * Time.deltaTime, distances[distances.Length - 1]);
            UpdatePosition();
        }

        void PrepareRoute()
        {
            int samples = route.Length * SamplesPerLeg;
            points = new Vector3[samples + 1];
            distances = new float[samples + 1];
            for (int i = 0; i < samples; i++)
            {
                int leg = i / SamplesPerLeg;
                float t = i % SamplesPerLeg / (float)SamplesPerLeg;
                var a = route[(leg + route.Length - 1) % route.Length];
                var b = route[leg];
                var c = route[(leg + 1) % route.Length];
                var d = route[(leg + 2) % route.Length];
                var p = .5f * ((2 * b) + (c-a)*t + (2*a-5*b+4*c-d)*t*t + (-a+3*b-3*c+d)*t*t*t);
                points[i] = new Vector3(p.x, .025f, p.y);
                if (i > 0) distances[i] = distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            }
            points[samples] = points[0];
            distances[samples] = distances[samples - 1] + Vector3.Distance(points[samples - 1], points[0]);
            distance = distances[samples] * startingPhase;
        }

        void UpdatePosition()
        {
            const float headingSample = .09f;
            var tangent = AtDistance(distance + headingSample) - AtDistance(distance - headingSample);
            transform.SetLocalPositionAndRotation(AtDistance(distance), Quaternion.LookRotation(tangent));
        }

        Vector3 AtDistance(float traveled)
        {
            traveled = Mathf.Repeat(traveled, distances[distances.Length - 1]);
            int i = System.Array.BinarySearch(distances, traveled);
            if (i >= 0) return points[i];
            i = ~i;
            return Vector3.Lerp(points[i - 1], points[i], Mathf.InverseLerp(distances[i - 1], distances[i], traveled));
        }
    }
}
