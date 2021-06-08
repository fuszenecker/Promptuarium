using System.IO;
using System.Threading;
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
        public static Task<Element> LoadAsync(Stream stream, CancellationToken cancellationToken)
        {
            return DeserializeAsync(stream, cancellationToken);
        }

        public static Task<Element> LoadAsync(Stream stream)
        {
            return LoadAsync(stream, CancellationToken.None);
        }

        /// <summary>
        /// Loads a tree from a file (synchronously).
        /// </summary>
        /// <param name="fileName">The source file name</param>
        /// <returns>The tree, if no error occured</returns>
        public static async Task<Element> LoadAsync(string fileName, CancellationToken cancellationToken)
        {
            Element result;

            using (Stream fileStream = new FileStream(fileName, FileMode.Open))
            {
                result = await LoadAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        public static Task<Element> LoadAsync(string fileName)
        {
            return LoadAsync(fileName, CancellationToken.None);
        }

        #endregion

        #region Save functions

        /// <summary>
        /// Saves the tree into a stream (synchronously).
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <returns>True, if no error occured</returns>
        public async Task SaveAsync(Stream stream, CancellationToken cancellationToken)
        {
            await SerializeAsync(stream, new SerializationArguments(), cancellationToken).ConfigureAwait(false);
            stream.WriteByte(ControlByte(Directions.Append, DataType.Data, SizeType.Linear, 0));
        }

        public Task SaveAsync(Stream stream)
        {
            return SaveAsync(stream, CancellationToken.None);
        }

        /// <summary>
        /// Saves the tree into a file (synchronously).
        /// </summary>
        /// <param name="fileName">The target file name</param>
        /// <returns>True, if no error occured</returns>
        public async Task SaveAsync(string fileName, CancellationToken cancellationToken)
        {
            using (Stream stream = new FileStream(fileName, FileMode.Create))
            {
                await SaveAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task SaveAsync(string fileName)
        {
            return SaveAsync(fileName, CancellationToken.None);
        }
        #endregion
    }
}
