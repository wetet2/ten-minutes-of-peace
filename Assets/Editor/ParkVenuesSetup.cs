using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PeacePark.Editor.ParkScenery;
using Object = UnityEngine.Object;

namespace PeacePark.Editor
{
    internal static class ParkVenuesSetup
    {
        internal static void Apply(Scene scene)
        {
            var roots=scene.GetRootGameObjects();
            var source=Array.Find(roots, root=>root.name=="Peace Park");
            var countryside=Array.Find(roots, root=>root.name=="Storybook countryside");
            var controller=Array.Find(roots, root=>root.GetComponent<ParkPlayground>())?.GetComponent<ParkPlayground>();
            if(!source||!controller||!controller.OrbitCamera||controller.Venues?.Length>0)return;

            var garden=Group("Garden venue",null,Vector3.zero);
            SceneManager.MoveGameObjectToScene(garden.gameObject,scene);
            source.transform.SetParent(garden,true);
            if(countryside)countryside.transform.SetParent(garden,true);

            var orbit=controller.OrbitCamera;
            var gardenVenue=new ParkVenue
            {
                Name="공원", Root=garden.gameObject, Houses=controller.Houses, Fountain=controller.Fountain,
                Path=source.transform.Find("Winding path").GetComponent<MeshFilter>(),
                LawnHeight=source.transform.Find("Meadow island").GetComponent<Renderer>().bounds.max.y,
                WalkArea=controller.WalkArea, Focus=orbit.focus, Distance=orbit.distance, Yaw=orbit.yaw,
                Pitch=orbit.pitch, Sky=orbit.GetComponent<Camera>().backgroundColor, Preview=Preview("Garden")
            };
            var venues=new System.Collections.Generic.List<ParkVenue>{gardenVenue};
            if(ParkForestSetup.IsInstalled)
            {
                var forest=Group("Forest venue",null,Vector3.zero);
                SceneManager.MoveGameObjectToScene(forest.gameObject,scene);
                var forestVenue=ParkForestSetup.Create(forest,gardenVenue);
                forestVenue.Preview=Preview("Forest");
                forest.gameObject.SetActive(false);
                venues.Add(forestVenue);
            }
            var cafe=Group("Kids cafe venue",null,Vector3.zero);
            SceneManager.MoveGameObjectToScene(cafe.gameObject,scene);
            var cafeVenue=ParkKidsCafeSetup.Create(cafe);
            cafeVenue.Preview=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Venues/KidsCafe.png");
            cafe.gameObject.SetActive(false);
            venues.Add(cafeVenue);
            controller.Venues=venues.ToArray();
        }

        static Texture2D Preview(string venue) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/Venues/{venue}.jpg");
    }
}
