#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class ColorRGBA
    {
        public const bool HasHeader = false;
        public const bool KnownSize = true;

        public Data data;

        public ColorRGBA()
        {
        }

        public ColorRGBA(byte[] SERIALIZEDSTUFF)
        {
            data = SerializationHelper.Deserialize<Data>(SERIALIZEDSTUFF);
        }

        public byte[] Serialize()
        {
            return SerializationHelper.Serialize(data);
        }

        #region Nested type: Data

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Data
        {
            public double r;
            public double g;
            public double b;
            public double a;
        }

        #endregion
    }
}