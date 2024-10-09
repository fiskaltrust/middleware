using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// This dummy class is required to compile records when targeting .NET Standard
/// https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IsExternalInit
{
}


public class RequiredMemberAttribute : Attribute { }
public class CompilerFeatureRequiredAttribute : Attribute
{
#pragma warning disable 
    public CompilerFeatureRequiredAttribute(string name) { }
#pragma warning restore 
}
