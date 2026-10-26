using System;

namespace Game.RedDot
{
    public readonly struct LocalRedDotKey : IEquatable<LocalRedDotKey>
    {
        public string Scope { get; }
        public string Path { get; }
        public RedNodeType NodeType { get; }
        public bool HasNodeType { get; }

        LocalRedDotKey(string scope, string path, RedNodeType nodeType, bool hasNodeType)
        {
            Scope = scope;
            Path = path;
            NodeType = nodeType;
            HasNodeType = hasNodeType;
        }

        public static LocalRedDotKey Of(string scope, string path)
        {
            return new LocalRedDotKey(scope, path, default, false);
        }

        public static LocalRedDotKey OfType(string scope, RedNodeType type, string path)
        {
            return new LocalRedDotKey(scope, path, type, true);
        }

        public bool Equals(LocalRedDotKey other)
        {
            return Scope == other.Scope && Path == other.Path;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalRedDotKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Scope, Path);
        }

        public override string ToString()
        {
            return HasNodeType ? $"{Scope}:{NodeType}({Path})" : $"{Scope}:{Path}";
        }
    }
}
