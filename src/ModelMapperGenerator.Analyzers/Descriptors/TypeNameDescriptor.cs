namespace ModelMapperGenerator.Analyzers.Descriptors
{
    internal readonly struct TypeNameDescriptor(string? TypeName = null, string? TypeArguments = null)
    {
        public bool HasValue => TypeName is not null;
        public string? TypeName { get; } = TypeName;
        public string? TypeArguments { get; } = TypeArguments;

        public static bool operator ==(TypeNameDescriptor left, TypeNameDescriptor right)
        {
            return Equals(ref left, ref right);
        }

        public static bool operator !=(TypeNameDescriptor left, TypeNameDescriptor right)
        {
            return !Equals(ref left, ref right);
        }

        public override bool Equals(object obj)
        {
            if (obj is not TypeNameDescriptor desc)
            {
                return false;
            }

            TypeNameDescriptor a = this;
            return Equals(ref a, ref desc);
        }

        private static bool Equals(ref TypeNameDescriptor left, ref TypeNameDescriptor right)
        {
            if (!left.HasValue || !right.HasValue)
            {
                return left.HasValue == right.HasValue;
            }

            if (left.TypeArguments is null || right.TypeArguments is null)
            {
                return left.TypeName == right.TypeName;
            }
            else
            {
                return left.TypeName == right.TypeName && left.TypeArguments == right.TypeArguments;
            }
        }

        public override int GetHashCode() =>
            (HasValue, TypeName, TypeArguments).GetHashCode();
    }
}
