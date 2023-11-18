using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnbloatDB.Generators;

public class OutboundReferences
{
    public TypeDeclarationSyntax Group;
    public Dictionary<string, TypeDeclarationSyntax> References; // Property : Generator

    public OutboundReferences(TypeDeclarationSyntax group)
    {
        References = new Dictionary<string, TypeDeclarationSyntax>();
    }
}