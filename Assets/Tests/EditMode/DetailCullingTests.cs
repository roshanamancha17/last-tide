using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class DetailCullingTests
    {
        private GameObject group;

        [SetUp]
        public void Create()
        {
            group = new GameObject("Scenery");
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(group);
        }

        private GameObject Child(string name, float size)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(group.transform, false);
            cube.transform.localScale = Vector3.one * size;
            return cube;
        }

        [TestCase(0.3f, 0.02f)]
        [TestCase(2.5f, 0.012f)]
        [TestCase(4f, 0.012f)]
        [TestCase(8f, 0f)]
        [TestCase(150f, 0f)]
        public void SmallThingsAreCulledAndBigThingsNever(float size, float expected)
        {
            Assert.AreEqual(expected, DetailCulling.CullScreenHeight(size), 0.0001f);
        }

        [Test]
        public void AddsDistanceCullingToSmallSceneryOnly()
        {
            GameObject stone = Child("Stone", 0.4f);
            GameObject bush = Child("Bush", 2.5f);
            GameObject palm = Child("Palm", 9f);

            Assert.AreEqual(2, DetailCulling.Apply(group.transform));
            Assert.IsNotNull(stone.GetComponent<LODGroup>());
            Assert.AreEqual(0.012f, bush.GetComponent<LODGroup>().GetLODs()[0].screenRelativeTransitionHeight, 0.0001f);
            Assert.IsNull(palm.GetComponent<LODGroup>(), "big objects stay visible at any distance");
        }

        [Test]
        public void KeepsEveryPartOfAMultiPartObjectTogether()
        {
            GameObject pickup = new GameObject("Wood Pickup");
            pickup.transform.SetParent(group.transform, false);
            GameObject bark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bark.transform.SetParent(pickup.transform, false);
            bark.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            GameObject ends = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ends.transform.SetParent(pickup.transform, false);
            ends.transform.localScale = Vector3.one * 0.2f;

            DetailCulling.Apply(group.transform);
            LOD[] lods = pickup.GetComponent<LODGroup>().GetLODs();
            Assert.AreEqual(2, lods[0].renderers.Length);
        }

        [Test]
        public void DoesNotTouchObjectsThatAlreadyHaveLevelsOfDetail()
        {
            GameObject rock = Child("Rock", 1f);
            rock.AddComponent<LODGroup>();
            Assert.AreEqual(0, DetailCulling.Apply(group.transform));
        }

        [Test]
        public void IgnoresEmptyChildren()
        {
            new GameObject("Spawn Marker").transform.SetParent(group.transform, false);
            Assert.AreEqual(0, DetailCulling.Apply(group.transform));
        }
    }
}
