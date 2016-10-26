﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactorings
{
    internal static class EventDeclarationRefactoring
    {
        public static void ComputeRefactorings(RefactoringContext context, EventDeclarationSyntax eventDeclaration)
        {
            if (context.IsRefactoringEnabled(RefactoringIdentifiers.MarkMemberAsStatic)
                && eventDeclaration.Span.Contains(context.Span)
                && MarkMemberAsStaticRefactoring.CanRefactor(eventDeclaration))
            {
                context.RegisterRefactoring(
                    "Mark event as static",
                    cancellationToken => MarkMemberAsStaticRefactoring.RefactorAsync(context.Document, eventDeclaration, cancellationToken));

                MarkAllMembersAsStaticRefactoring.RegisterRefactoring(context, (ClassDeclarationSyntax)eventDeclaration.Parent);
            }
        }
    }
}