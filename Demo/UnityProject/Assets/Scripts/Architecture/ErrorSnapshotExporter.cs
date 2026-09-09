using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace RedSea.Match3.Architecture
{
    public static class ErrorSnapshotExporter
    {
        public const int SchemaVersion = 1;
        public const string DirectoryName = "ErrorSnapshots";

        public static string Export(ErrorSnapshot snapshot, string rootDirectory)
        {
            return Export(snapshot, rootDirectory, DateTime.UtcNow);
        }

        public static string Export(ErrorSnapshot snapshot, string rootDirectory, DateTime utcNow)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(rootDirectory)) throw new ArgumentException("Export root directory is required.", nameof(rootDirectory));

            var directory = Path.Combine(rootDirectory, DirectoryName);
            Directory.CreateDirectory(directory);
            var timestamp = utcNow.ToUniversalTime().ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture);
            var baseName = "error-" + timestamp + "-turn-" + snapshot.TurnId + "-" + snapshot.ErrorType.ToString().ToLowerInvariant();
            var path = UniquePath(directory, baseName);
            File.WriteAllText(path, Serialize(snapshot), new UTF8Encoding(false));
            return path;
        }

        public static bool TryExport(ErrorSnapshot snapshot, string rootDirectory, out string path, out string error)
        {
            try
            {
                path = Export(snapshot, rootDirectory);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                path = null;
                error = exception.Message;
                return false;
            }
        }

        public static string Serialize(ErrorSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var builder = new StringBuilder(512);
            builder.AppendLine("{");
            Append(builder, "schemaVersion", SchemaVersion.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "errorType", Quote(snapshot.ErrorType.ToString()), false);
            Append(builder, "message", Quote(snapshot.Message), false);
            Append(builder, "turnId", snapshot.TurnId.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "state", Quote(snapshot.State.ToString()), false);
            Append(builder, "seed", snapshot.Seed.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "initialRandomIndex", snapshot.InitialRandomIndex.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "refillRandomIndex", snapshot.RefillRandomIndex.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "shuffleRandomIndex", snapshot.ShuffleRandomIndex.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "eventQueueCount", snapshot.EventQueueCount.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "boardSnapshot", Quote(snapshot.BoardSnapshot), true);
            builder.Append('}');
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string name, string value, bool last)
        {
            builder.Append("  \"").Append(name).Append("\": ").Append(value);
            if (!last) builder.Append(',');
            builder.AppendLine();
        }

        private static string Quote(string value)
        {
            if (value == null) return "null";
            var builder = new StringBuilder(value.Length + 2).Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 32) builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(character);
                        break;
                }
            }
            return builder.Append('"').ToString();
        }

        private static string UniquePath(string directory, string baseName)
        {
            var path = Path.Combine(directory, baseName + ".json");
            var suffix = 1;
            while (File.Exists(path)) path = Path.Combine(directory, baseName + "-" + suffix++ + ".json");
            return path;
        }
    }
}
