using System;
using System.IO;

namespace SmartCraftStorage.Storage.Core
{
    // Persist what was requested, not the transient inventories used to plan it.
    public sealed class StorageRequestIntent
    {
        public readonly string Kind, Id, WorldId, AnchorId;
        public readonly StorageScope Scope;
        public readonly float X, Y, Z, Radius;
        public readonly byte[] Body;

        public StorageRequestIntent(string kind, string id, string worldId, StorageScope scope,
            float x, float y, float z, float radius, string anchorId, byte[] body)
        {
            Kind = kind; Id = id; WorldId = worldId; Scope = scope;
            X = x; Y = y; Z = z; Radius = radius; AnchorId = anchorId;
            Body = (byte[])body.Clone();
        }

        public byte[] Encode()
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(1); writer.Write(Kind); writer.Write(Id); writer.Write(WorldId); writer.Write((int)Scope);
                writer.Write(X); writer.Write(Y); writer.Write(Z); writer.Write(Radius); writer.Write(AnchorId);
                writer.Write(Body.Length); writer.Write(Body); return stream.ToArray();
            }
        }

        public bool MatchesTarget(string world, string anchor, string kind) => WorldId == world && AnchorId == anchor &&
            (string.IsNullOrEmpty(kind) || Kind == kind);

        public static StorageRequestIntent Decode(byte[] encoded)
        {
            using (var reader = new BinaryReader(new MemoryStream(encoded)))
            {
                if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported storage intent");
                string kind = reader.ReadString(), id = reader.ReadString(), world = reader.ReadString();
                var scope = (StorageScope)reader.ReadInt32();
                float x = reader.ReadSingle(), y = reader.ReadSingle(), z = reader.ReadSingle(), radius = reader.ReadSingle();
                string anchor = reader.ReadString(); int length = reader.ReadInt32();
                if (length < 0 || length > 8 * 1024 * 1024 || length != reader.BaseStream.Length - reader.BaseStream.Position)
                    throw new InvalidDataException("Invalid storage intent payload");
                return new StorageRequestIntent(kind, id, world, scope, x, y, z, radius, anchor, reader.ReadBytes(length));
            }
        }

        public byte[] Rebuild(Func<byte[]> currentHeader)
        {
            var header = currentHeader(); var request = new byte[checked(header.Length + Body.Length)];
            Buffer.BlockCopy(header, 0, request, 0, header.Length);
            Buffer.BlockCopy(Body, 0, request, header.Length, Body.Length);
            return request;
        }
    }
}
