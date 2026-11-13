using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeacePark.Editor
{
    internal static class ParkGardenLayout
    {
        const float Expansion = 1.20f;
        static readonly Vector3 HorizontalScale = new Vector3(Expansion, 1, Expansion);

        internal static Vector3 Expand(Vector3 position) => Vector3.Scale(position, HorizontalScale);

        internal static void Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var park = Array.Find(roots, root => root.name == "Peace Park");
            var countryside = Array.Find(roots, root => root.name == "Storybook countryside");
            var playground = Array.Find(roots, root => root.GetComponent<ParkPlayground>());
            if (!park || !playground || park.transform.Find("Duck fountain")) return;

            foreach (Transform item in park.transform)
            {
                item.localPosition = Expand(item.localPosition);
                switch (item.name)
                {
                    case "Meadow island":
                    case "Earth edge":
                    case "Winding path":
                    case "Entry path":
                        item.localScale = Expand(item.localScale);
                        break;
                    case "Fence upper rail":
                    case "Fence lower rail":
                        // The cylinders run along their local Y axis, which is horizontal here.
                        item.localScale = Vector3.Scale(item.localScale, new Vector3(1, Expansion, 1));
                        break;
                }
                if (item.name == "Grass tuft" && item.localPosition.sqrMagnitude < 3.05f * 3.05f)
                    item.gameObject.SetActive(false);
            }

            if (countryside)
            {
                foreach (Transform item in countryside.transform)
                {
                    if (item.name == "Welcome trail") item.localScale = Expand(item.localScale);
                    if (item.name == "Meadow tree") item.localPosition = Expand(item.localPosition);
                }
            }

            var controller = playground.GetComponent<ParkPlayground>();
            var area = controller.WalkArea;
            controller.WalkArea = new Rect(area.position * Expansion, area.size * Expansion);
            if (controller.OrbitCamera) controller.OrbitCamera.distance *= Expansion;
            controller.Fountain = ParkFountainSetup.Create(park.transform);
        }
    }
}
