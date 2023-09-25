﻿// Copyright (c) .NET Foundation and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.Testing.CSharp.MSTest;

/// <summary>
/// Represents a verifier for a C# diagnostic that is produced by <see cref="DiagnosticAnalyzer"/>.
/// </summary>
public abstract class MSTestDiagnosticVerifier<TAnalyzer, TFixProvider> : CSharpDiagnosticVerifier<TAnalyzer, TFixProvider>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TFixProvider : CodeFixProvider, new()
{
    /// <summary>
    /// Initializes a new instance of <see cref="MSTestDiagnosticVerifier{TAnalyzer, TFixProvider}"/>.
    /// </summary>
    protected MSTestDiagnosticVerifier() : base(MSTestAssert.Instance)
    {
    }
}
