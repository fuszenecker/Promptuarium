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
        private readonly Random random = new Random((int)DateTime.Now.Ticks);

        [TestMethod]
        public async Task Stability()
        {
            const int maxTests = 10000;
            const int minChildren = 0;
            const int maxChildren = 10;
            const double probability = 0.75;

            await CreateAndVerifyTrees(maxTests, minChildren, maxChildren, probability, @".\stability.p").ConfigureAwait(false);
        }

        [TestMethod]
        public async Task BigTrees()
        {
            const int maxTests = 10;
            const int minChildren = 0;
            const int maxChildren = 20;
            const double probability = 0.95;

            await CreateAndVerifyTrees(maxTests, minChildren, maxChildren, probability, @".\big_trees.p").ConfigureAwait(false);
        }

        private Element GenerateTree(int minimumChildren, int maximumChildren, double probability)
        {
            Element root = new Element();

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
                    Guid nodeGuid = Guid.NewGuid();

                    Element child = new Element()
                    {
                        MetaData = Data.FromAsciiString(nodeGuid.ToString("N")),
                        Data = Data.FromUtf8String(string.Format("LEVEL: {0}, NODE: {1}", level, nodeGuid))
                    };

                    GenerateTree(child, minimumChildren, maximumChildren, probability * probability, level++);

                    node.Add(child);
                }
            }
        }

        private async Task CreateAndVerifyTrees(int maxTests, int minChildren, int maxChildren, double probability, string fileName)
        {
            for (int x = 0; x < maxTests; x++)
            {
                Element originalTree = GenerateTree(minChildren, maxChildren, probability);
                string originalTreeString = originalTree.TreeToString();
                await originalTree.SaveAsync(fileName).ConfigureAwait(false);

                // Statistics stats = originalTree.Statistics;

                Assert.IsTrue(new FileInfo(fileName).Length > 0);

                Element currentTree = await Element.LoadAsync(fileName).ConfigureAwait(false);
                string currentTreeString = currentTree.TreeToString();

                Assert.AreEqual(originalTreeString, currentTreeString);
            }
        }
    }
}
