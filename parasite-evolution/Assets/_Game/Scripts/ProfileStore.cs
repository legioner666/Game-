using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ParasiteEvolution
{
    public sealed class ProfileStore
    {
        readonly string path;
        public ProfileData Current { get; private set; }

        public ProfileStore(string file)
        {
            path = file;
            var any = false;
            ProfileData best = null;
            foreach (var candidate in new[] { path, path + ".tmp", path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                any = true;
                try
                {
                    var parsed = JsonUtility.FromJson<ProfileData>(File.ReadAllText(candidate));
                    if (parsed == null) continue;
                    if (parsed.schemaVersion > 1) throw new NotSupportedException("Save needs a newer game version.");
                    parsed.Validate();
                    if (best == null || parsed.revision > best.revision) best = parsed;
                }
                catch (NotSupportedException) { throw; }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is InvalidOperationException)
                { Debug.LogWarning("Unreadable save candidate: " + Path.GetFileName(candidate)); }
            }
            if (any && best == null) throw new InvalidDataException("No valid save. Existing files were preserved.");
            Current = best ?? ProfileData.Fresh();
        }

        public bool Apply(Func<ProfileData, bool> transaction)
        {
            var next = JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(Current));
            if (!transaction(next)) return false;
            next.Validate();
            next.revision = checked(Current.revision + 1);
            Write(next);
            Current = next;
            return true;
        }

        void Write(ProfileData next)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(next, true));
            using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (!File.Exists(path)) File.Move(path + ".tmp", path);
            else
            {
                try { File.Replace(path + ".tmp", path, path + ".bak"); }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                    File.Move(path + ".tmp", path);
                }
            }
        }
    }
}
