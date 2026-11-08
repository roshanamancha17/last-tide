using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace LastTide.Tests
{
    public class SaveTests
    {
        private string folder;

        [SetUp]
        public void Create()
        {
            folder = Path.Combine(Path.GetTempPath(), "LastTideSaveTests_" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void Remove()
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        private static SaveData Sample()
        {
            return new SaveData
            {
                itemNames = new[] { "Wood", "Knife" },
                itemCounts = new[] { 4, 1 },
                playerPosition = new Vector3(6.2f, 1.9f, -58.8f),
                playerYaw = 135f,
                hunger = 42.5f,
                energy = 77f,
                day = 3,
                hour = 21.5f,
                marlowAwake = true,
                collected = new[] { "Stone@12,4,-300" },
                treeKeys = new[] { "Palm.004@50,20,10" },
                treeChopsLeft = new[] { 1 },
                campfireBuilt = true,
                campfirePosition = new Vector3(1f, 2f, 3f),
                events = new[] { "Made a Knife" },
            };
        }

        [Test]
        public void ASaveComesBackExactlyAsItWasWritten()
        {
            SaveData loaded = SaveCodec.FromJson(SaveCodec.ToJson(Sample()));
            Assert.IsNotNull(loaded);
            CollectionAssert.AreEqual(new[] { "Wood", "Knife" }, loaded.itemNames);
            CollectionAssert.AreEqual(new[] { 4, 1 }, loaded.itemCounts);
            Assert.AreEqual(new Vector3(6.2f, 1.9f, -58.8f), loaded.playerPosition);
            Assert.AreEqual(42.5f, loaded.hunger);
            Assert.AreEqual(3, loaded.day);
            Assert.AreEqual(21.5f, loaded.hour);
            Assert.IsTrue(loaded.marlowAwake && loaded.campfireBuilt);
            CollectionAssert.AreEqual(new[] { "Stone@12,4,-300" }, loaded.collected);
            CollectionAssert.AreEqual(new[] { 1 }, loaded.treeChopsLeft);
            CollectionAssert.AreEqual(new[] { "Made a Knife" }, loaded.events);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not json at all")]
        [TestCase("{\"version\":99}")]
        [TestCase("{\"version\":0}")]
        public void DamagedOrUnknownSavesAreRefused(string json)
        {
            Assert.IsNull(SaveCodec.FromJson(json));
        }

        [Test]
        public void OutOfRangeNumbersAreRepairedInsteadOfLosingTheGame()
        {
            SaveData data = Sample();
            data.hunger = 500f;
            data.energy = -20f;
            data.day = -4;
            data.hour = 30f;
            data.itemCounts = new[] { -3, 1 };
            SaveData loaded = SaveCodec.FromJson(SaveCodec.ToJson(data));
            Assert.AreEqual(100f, loaded.hunger);
            Assert.AreEqual(0f, loaded.energy);
            Assert.AreEqual(1, loaded.day);
            Assert.AreEqual(6f, loaded.hour, 0.001f);
            Assert.AreEqual(0, loaded.itemCounts[0]);
        }

        [Test]
        public void MismatchedListsAreTrimmedToMatch()
        {
            SaveData data = Sample();
            data.itemCounts = new[] { 4 };
            data.treeChopsLeft = new int[0];
            SaveData loaded = SaveCodec.FromJson(SaveCodec.ToJson(data));
            Assert.AreEqual(1, loaded.itemNames.Length);
            Assert.AreEqual(0, loaded.treeKeys.Length);
        }

        [Test]
        public void WorldKeysIgnoreTinyPositionDifferences()
        {
            Assert.AreEqual(WorldKey.Of("Stone", new Vector3(1.2f, 0.4f, -30f)),
                            WorldKey.Of("Stone", new Vector3(1.2001f, 0.3999f, -30.002f)));
            Assert.AreNotEqual(WorldKey.Of("Stone", new Vector3(1.2f, 0.4f, -30f)),
                               WorldKey.Of("Stone", new Vector3(1.5f, 0.4f, -30f)));
            Assert.AreNotEqual(WorldKey.Of("Stone", Vector3.zero), WorldKey.Of("Wood", Vector3.zero));
        }

        [Test]
        public void SavingTwiceReplacesTheOldSaveAndLeavesNoTemporaryFile()
        {
            string path = Path.Combine(folder, "save.json");
            SaveStore.Write(path, "first");
            SaveStore.Write(path, "second");
            Assert.AreEqual("second", SaveStore.Read(path));
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void AFailedWriteKeepsTheOldSave()
        {
            string path = Path.Combine(folder, "save.json");
            SaveStore.Write(path, "good save");
            // Simulate a crash partway through the next save: the temporary file exists but was never swapped in.
            File.WriteAllText(path + ".tmp", "half writ");
            Assert.AreEqual("good save", SaveStore.Read(path));
        }

        [Test]
        public void NoSaveAndDeletingWork()
        {
            string path = Path.Combine(folder, "save.json");
            Assert.IsNull(SaveStore.Read(path));
            SaveStore.Write(path, "x");
            SaveStore.Delete(path);
            Assert.IsNull(SaveStore.Read(path));
        }
    }
}
