using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace fiskaltrust.Interface.Tagging.Generator
{
    public interface IToGenerate
    {
        public string GetSourceFileName();
    }

    public interface IToGenerateFactory<T> where T : IToGenerate
    {
        public T Create(string nameSpace, string name, List<string> members);
        public void AddProperties(System.Collections.Immutable.ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments);
    }
}