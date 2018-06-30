using ProtoBuf;
using System;
using System.IO;

public class NetworkMessageSerializer<T> : IDisposable {
    private MemoryStream memoryStream = new MemoryStream();

    public T Deserialize(byte[] data) {
        //memoryStream.Seek(0, SeekOrigin.Begin);
        //memoryStream.Write(data, 0, data.Length);
        //memoryStream.Position = 0;
        using (MemoryStream memoryStream = new MemoryStream(data)) {
            return Serializer.Deserialize<T>(memoryStream);
        }
    }

    public void Dispose() {
        memoryStream.Dispose();
    }

    public byte[] Serialize(T data) {
        //memoryStream.Seek(0, SeekOrigin.Begin);
        //memoryStream.Position = 0;

        using (MemoryStream memoryStream = new MemoryStream()) {
            Serializer.Serialize(memoryStream, data);
            return memoryStream.ToArray();
        }
    }

}
