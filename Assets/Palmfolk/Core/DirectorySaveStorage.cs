using System;
using System.IO;

namespace Palmfolk.Core
{
    public sealed class DirectorySaveStorage : ISaveStorage
    {
        private readonly string directory;
        public DirectorySaveStorage(string directory) { this.directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory))); }
        private string PathFor(string slot)
        {
            if (slot != "save-a.bin" && slot != "save-b.bin") throw new ArgumentException("Unknown save slot.");
            return Path.Combine(directory, slot);
        }
        public byte[] Read(string slot) { string path = PathFor(slot); return File.Exists(path) ? File.ReadAllBytes(path) : null; }
        public void WriteAtomic(string slot, byte[] data)
        {
            Directory.CreateDirectory(directory); string path = PathFor(slot), temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(data, 0, data.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }
    }
}
