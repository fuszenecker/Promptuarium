namespace Promptuarium
{
    public partial class Element
    {
        #region Constants
        // Number of size bits in the control byte
        private const byte NibbleSizeInBits = 4;

        // Max. value represented by NibbleSizeInBits
        private const byte NibbleMaxValue = (1 << NibbleSizeInBits) - 1;

        // Log. value of ChunkMaxValue
        private const byte ChunkSizeInBits = NibbleSizeInBits + NibbleMaxValue;

        // Max. size of a chunk of data
        private const long ChunkMaxValue = 1 << ChunkSizeInBits;
        #endregion

        #region Enumerations
        private enum Directions : byte
        {
            Append = 0x00,
            Up = 0x80,
            Down = 0x40,
            New = 0xC0,
            Mask = 0xC0
        }

        /// <summary>
        /// Type of the node
        /// </summary>
        private enum DataType : byte
        {
            Data = 0x00,
            MetaData = 0x20,
            Mask = 0x20
        }

        private enum SizeType : byte
        {
            Linear = 0x00,
            Logarithmic = 0x10,
            Mask = 0x10
        }
        #endregion
    }
}
