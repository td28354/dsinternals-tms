using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DSInternals.Common;

namespace DSInternals.DataStore
{
    public class AttributeMetadataCollection // : IReadOnlyCollection<AttributeMetadata>
    {
        private const ulong defaultVersion = 1;
        private const int guidSize = 16;
        private const int entrySize = 3 * sizeof(long) + 2 * sizeof(int) + guidSize;

        public ulong Version
        {
            get;
            private set;
        }

        public int Count
        {
            get
            {
                return (InnerList != null) ? InnerList.Count : 0;
            }
        }

        protected IList<AttributeMetadata> InnerList
        {
            get;
            private set;
        }

        public AttributeMetadataCollection() : this(null) { }

        public AttributeMetadataCollection(byte[] buffer)
        {
            if(buffer == null)
            {
                this.Version = defaultVersion;
                this.InnerList = new List<AttributeMetadata>();
                return;
            }

            if(buffer.Length < 2 * sizeof(ulong))
            {
                throw new ArgumentOutOfRangeException("buffer");
            }

            using(Stream stream = new MemoryStream(buffer))
            {
                using(BinaryReader reader = new BinaryReader(stream))
                {
                    this.Version = reader.ReadUInt64();
                    ulong numEntries = reader.ReadUInt64();
                    ulong expectedBufferSize = CalculateBinarySize(numEntries);
                    Validator.AssertLength(buffer, expectedBufferSize, "buffer");
                    this.InnerList = new List<AttributeMetadata>((int) numEntries);
                    for(int i = 1; i <= numEntries; i++)
                    {
                        uint attrTyp = reader.ReadUInt32();
                        uint version = reader.ReadUInt32();
                        long timestamp = reader.ReadInt64();
                        Guid originatingDSA = new Guid(reader.ReadBytes(16));
                        long originatingUSN = reader.ReadInt64();
                        long localUSN = reader.ReadInt64();
                        var entry = new AttributeMetadata(attrTyp, version, timestamp, originatingDSA, originatingUSN, localUSN);
                        this.InnerList.Add(entry);
                    }
                }
            }
        }

        public void Update(uint attrTyp, Guid invocationId, DateTime time, long usn)
        {
            var existingEntry = this.InnerList.FirstOrDefault(item => item.AttrTyp == attrTyp);
            if(existingEntry != null)
            {
                // This attribute is already contained in the list, so we just update it
                existingEntry.Update(invocationId, time, usn);
            }
            else
            {
                // This is a newly added attribute
                var newEntry = new AttributeMetadata(attrTyp, invocationId, time, usn);
                this.InnerList.Add(newEntry);
            }
        }

        public byte[] ToByteArray()
        {
            if(this.InnerList.Count == 0)
            {
                return null;
            }
            byte[] buffer = new byte[CalculateBinarySize(this.Count)];
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(this.Version);
                    // Important: Write Count as 64-bit and not 32-bit:
                    writer.Write((long) this.Count);
                    foreach(var entry in this.InnerList)
                    {
                        writer.Write(entry.AttrTyp);
                        writer.Write(entry.Version);
                        writer.Write(entry.LastOriginatingChangeTimestamp);
                        writer.Write(entry.LastOriginatingInvocationId.ToByteArray());
                        writer.Write(entry.OriginatingChangeUsn);
                        writer.Write(entry.LocalChangeUsn);
                    }
                }
            }
            return buffer;
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            foreach (var entry in InnerList)
            {
                text.AppendLine(entry.ToString());
            }
            return text.ToString();
        }

        public IEnumerator<AttributeMetadata> GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        private static ulong CalculateBinarySize(ulong numEntries)
        {
            // Version + Entry Count + Entries
            return 2 * sizeof(ulong) + numEntries * entrySize;
        }
    }
}
