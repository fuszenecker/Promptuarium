using System.IO;
using System.Threading.Tasks;

namespace Promptuarium
{
    public partial class Element
    {
        #region Load functions

        /// <summary>
        /// Loads a tree from a stream (synchronously).
        /// </summary>
        /// <param name="stream">The source stream</param>
        /// <returns>The tree, if no error occured</returns>
        public static Task<Element> LoadAsync(Stream stream)
        {
            return DeserializeAsync(stream);
        }

        /// <summary>
        /// Loads a tree from a file (synchronously).
        /// </summary>
        /// <param name="fileName">The source file name</param>
        /// <returns>The tree, if no error occured</returns>
        public static async Task<Element> LoadAsync(string fileName)
        {
            Element result;

            using (Stream fileStream = new FileStream(fileName, FileMode.Open))
            {
                result = await LoadAsync(fileStream);
            }

            return result;
        }

        #endregion

        #region Save functions

        /// <summary>
        /// Saves the tree into a stream (synchronously).
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <returns>True, if no error occured</returns>
        public async Task SaveAsync(Stream stream)
        {
            await SerializeAsync(stream, new SerializationArguments());
            stream.WriteByte(ControlByte(Directions.Append, DataType.Data, SizeType.Linear, 0));
        }

        /// <summary>
        /// Saves the tree into a file (synchronously).
        /// </summary>
        /// <param name="fileName">The target file name</param>
        /// <returns>True, if no error occured</returns>
        public async Task SaveAsync(string fileName)
        {
            using (Stream stream = new FileStream(fileName, FileMode.Create))
            {
                await SaveAsync(stream);
            }
        }

        #endregion
    }
}
