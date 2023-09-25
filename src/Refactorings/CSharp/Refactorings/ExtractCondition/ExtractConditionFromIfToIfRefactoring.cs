﻿// Copyright (c) .NET Foundation and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings.ExtractCondition;

internal sealed class ExtractConditionFromIfToIfRefactoring : ExtractConditionFromIfRefactoring
{
    private ExtractConditionFromIfToIfRefactoring()
    {
    }

    public static ExtractConditionFromIfToIfRefactoring Instance { get; } = new();

    public override string Title
    {
        get { return "Extract condition to if"; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    public Task<Document> RefactorAsync(
        Document document,
        in StatementListInfo statementsInfo,
        BinaryExpressionSyntax condition,
        ExpressionSyntax expression,
        CancellationToken cancellationToken = default)
    {
        var ifStatement = (IfStatementSyntax)condition.Parent;

        IfStatementSyntax newIfStatement = RemoveExpressionFromCondition(ifStatement, condition, expression)
            .WithFormatterAnnotation();

        SyntaxNode newNode = AddNextIf(statementsInfo, ifStatement, newIfStatement, expression);

        return document.ReplaceNodeAsync(statementsInfo.Parent, newNode, cancellationToken);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
    public Task<Document> RefactorAsync(
        Document document,
        in StatementListInfo statementsInfo,
        BinaryExpressionSyntax condition,
        in ExpressionChain expressionChain,
        CancellationToken cancellationToken)
    {
        var ifStatement = (IfStatementSyntax)condition.Parent;

        IfStatementSyntax newIfStatement = RemoveExpressionsFromCondition(ifStatement, condition, expressionChain)
            .WithFormatterAnnotation();

        ExpressionSyntax expression = SyntaxFactory.ParseExpression(expressionChain.ToString());

        SyntaxNode newNode = AddNextIf(statementsInfo, ifStatement, newIfStatement, expression);

        return document.ReplaceNodeAsync(statementsInfo.Parent, newNode, cancellationToken);
    }

    private static SyntaxNode AddNextIf(
        in StatementListInfo statementsInfo,
        IfStatementSyntax ifStatement,
        IfStatementSyntax newIfStatement,
        ExpressionSyntax expression)
    {
        IfStatementSyntax nextIfStatement = ifStatement
            .WithCondition(expression)
            .WithFormatterAnnotation();

        SyntaxList<StatementSyntax> statements = statementsInfo.Statements;

        int index = statements.IndexOf(ifStatement);

        SyntaxList<StatementSyntax> newStatements = statements
            .Replace(ifStatement, newIfStatement)
            .Insert(index + 1, nextIfStatement);

        return statementsInfo.WithStatements(newStatements).Parent;
    }
}
