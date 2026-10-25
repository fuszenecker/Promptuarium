using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Promptuarium;

namespace PromptuariumTests
{
    [TestClass]
    public class StabilityTests
    {
        // Fixed seed, so that a failure can be reproduced
        private readonly Random random = new (20231010);

        [TestMethod]
        [TestCategory("Slow")]
        public async Task Stability()
        {
            const int maxTests = 10000;
            const int minChildren = 0;
            const int maxChildren = 10;
            const double probability = 0.75;

            await CreateAndVerifyTrees(maxTests, minChildren, maxChildren, probability, Path.Combine(Path.GetTempPath(), "promptuarium-stability.prm")).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Slow")]
        public async Task BigTrees()
        {
            const int maxTests = 10;
            const int minChildren = 0;
            const int maxChildren = 20;
            const double probability = 0.95;

            await CreateAndVerifyTrees(maxTests, minChildren, maxChildren, probability, Path.Combine(Path.GetTempPath(), "promptuarium-big-trees.prm")).ConfigureAwait(false);
        }

        private Element GenerateTree(int minimumChildren, int maximumChildren, double probability)
        {
            var root = new Element();

            GenerateTree(root, minimumChildren, maximumChildren, probability);

            return root;
        }

        private void GenerateTree(Element node, int minimumChildren, int maximumChildren, double probability, int level = 0)
        {
            if (random.NextDouble() < probability)
            {
                int numChildren = random.Next(maximumChildren - minimumChildren) + minimumChildren;

                for (int x = 0; x < numChildren; x++)
                {
                    var nodeGuid = Guid.NewGuid();

                    var child = new Element()
                    {
                        MetaData = Data.FromAsciiString(nodeGuid.ToString("N")),
                        Data = Data.FromUtf8String(string.Format("LEVEL: {0}, NODE: {1}", level, nodeGuid))
                    };

                    GenerateTree(child, minimumChildren, maximumChildren, probability * probability, level + 1);

                    node.Add(child);
                }
            }
        }

        private async Task CreateAndVerifyTrees(int maxTests, int minChildren, int maxChildren, double probability, string fileName)
        {
            for (int x = 0; x < maxTests; x++)
            {
                Element originalTree = GenerateTree(minChildren, maxChildren, probability);
                await originalTree.SaveAsync(fileName).ConfigureAwait(false);

                Statistics stats = originalTree.GetStatistics();

                Assert.IsTrue(new FileInfo(fileName).Length > 0);

                Element currentTree = await Element.LoadAsync(fileName).ConfigureAwait(false);
                Assert.IsTrue(originalTree.StructurallyEquals(currentTree));
            }
        }
    }
}
