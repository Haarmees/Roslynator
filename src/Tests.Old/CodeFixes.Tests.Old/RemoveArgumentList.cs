﻿// Copyright (c) .NET Foundation and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Roslynator.CSharp.CodeFixes.Tests
{
    internal static class RemoveArgumentList
    {
        private static void Foo()
        {
            string s = null;

            int i = s.Length();
            int? ni = s?.Length();
        }
    }
}
