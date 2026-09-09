using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RedSea.Match3.Architecture
{
    public sealed class LocalSaveData
    {
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion = CurrentSchemaVersion;
        public string LevelId;
        public string ConfigVersion;
        public int MovesRemaining;
        public int AreaToolsRemaining;
        public int Score;
        public int GoalProgress;
    }

    public sealed class LocalSaveResult
    {
        public bool Success { get; internal set; }
        public bool UsedDefault { get; internal set; }
        public string Message { get; internal set; }
        public LocalSaveData Data { get; internal set; }
    }

    public static class LocalSaveService
    {
        public const string DirectoryName = "Saves";

        public static LocalSaveData Default(string levelId, string configVersion, int moves, int areaTools)
        {
            return new LocalSaveData { LevelId = levelId, ConfigVersion = configVersion, MovesRemaining = moves, AreaToolsRemaining = areaTools };
        }

        public static LocalSaveResult Load(string path, string levelId, string configVersion, int moves, int areaTools)
        {
            var fallback = Default(levelId, configVersion, moves, areaTools);
            if (!File.Exists(path)) return Result(true, true, "Save file not found; default progress used.", fallback);
            try
            {
                var data = Parse(File.ReadAllText(path));
                if (data.SchemaVersion != LocalSaveData.CurrentSchemaVersion) return Result(true, true, "Save schema is unsupported; default progress used.", fallback);
                if (data.LevelId != levelId || data.ConfigVersion != configVersion) return Result(true, true, "Save belongs to another level or config version; default progress used.", fallback);
                if (data.MovesRemaining < 0 || data.MovesRemaining > moves || data.AreaToolsRemaining < 0 || data.AreaToolsRemaining > areaTools || data.Score < 0 || data.GoalProgress < 0) return Result(true, true, "Save values are out of range; default progress used.", fallback);
                return Result(true, false, "Save loaded.", data);
            }
            catch (Exception exception)
            {
                return Result(true, true, "Save load failed; default progress used: " + exception.Message, fallback);
            }
        }

        public static LocalSaveResult Save(string path, LocalSaveData data)
        {
            try
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save path must include a directory.", nameof(path));
                Directory.CreateDirectory(directory);
                var temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, Serialize(data), new UTF8Encoding(false));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporaryPath, path);
                return Result(true, false, "Save written.", data);
            }
            catch (Exception exception)
            {
                return Result(false, false, "Save write failed: " + exception.Message, null);
            }
        }

        public static string Serialize(LocalSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var builder = new StringBuilder(256);
            builder.AppendLine("{");
            Append(builder, "schemaVersion", data.SchemaVersion.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "levelId", Quote(data.LevelId), false);
            Append(builder, "configVersion", Quote(data.ConfigVersion), false);
            Append(builder, "movesRemaining", data.MovesRemaining.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "areaToolsRemaining", data.AreaToolsRemaining.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "score", data.Score.ToString(CultureInfo.InvariantCulture), false);
            Append(builder, "goalProgress", data.GoalProgress.ToString(CultureInfo.InvariantCulture), true);
            builder.Append('}');
            return builder.ToString();
        }

        private static LocalSaveData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{") || !json.TrimEnd().EndsWith("}")) throw new InvalidDataException("Save is not a JSON object.");
            return new LocalSaveData
            {
                SchemaVersion = ReadInt(json, "schemaVersion"),
                LevelId = ReadString(json, "levelId"),
                ConfigVersion = ReadString(json, "configVersion"),
                MovesRemaining = ReadInt(json, "movesRemaining"),
                AreaToolsRemaining = ReadInt(json, "areaToolsRemaining"),
                Score = ReadInt(json, "score"),
                GoalProgress = ReadInt(json, "goalProgress")
            };
        }

        private static int ReadInt(string json, string key)
        {
            var match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*(-?\\d+)");
            if (!match.Success) throw new InvalidDataException("Missing integer field: " + key);
            return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static string ReadString(string json, string key)
        {
            var match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*\\\"((?:\\\\.|[^\\\"\\\\])*)\\\"");
            if (!match.Success) throw new InvalidDataException("Missing string field: " + key);
            return Unquote(match.Groups[1].Value);
        }

        private static string Unquote(string value)
        {
            var builder = new StringBuilder(value.Length);
            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] != '\\') { builder.Append(value[index]); continue; }
                if (++index >= value.Length) throw new InvalidDataException("Invalid string escape.");
                var escaped = value[index];
                if (escaped == '"' || escaped == '\\' || escaped == '/') builder.Append(escaped);
                else if (escaped == 'b') builder.Append('\b');
                else if (escaped == 'f') builder.Append('\f');
                else if (escaped == 'n') builder.Append('\n');
                else if (escaped == 'r') builder.Append('\r');
                else if (escaped == 't') builder.Append('\t');
                else if (escaped == 'u' && index + 4 < value.Length) { builder.Append((char)int.Parse(value.Substring(index + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); index += 4; }
                else throw new InvalidDataException("Invalid string escape.");
            }
            return builder.ToString();
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
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(character); break;
                }
            }
            return builder.Append('"').ToString();
        }

        private static void Append(StringBuilder builder, string key, string value, bool last)
        {
            builder.Append("  \"").Append(key).Append("\": ").Append(value);
            if (!last) builder.Append(',');
            builder.AppendLine();
        }

        private static LocalSaveResult Result(bool success, bool usedDefault, string message, LocalSaveData data)
        {
            return new LocalSaveResult { Success = success, UsedDefault = usedDefault, Message = message, Data = data };
        }
    }
}
