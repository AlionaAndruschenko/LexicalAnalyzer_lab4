// <copyright file="LexerTests.cs" company="KNU">
// Copyright (c) 2026 Андрущенко Альона. All rights reserved.
// </copyright>

namespace LexicalAnalyzer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using LexicalAnalyzer;
    using Xunit;

    public class LexerTests
    {
        private readonly List<Token> simpleTokens;
        private readonly List<Token> keywordTokens;

        public LexerTests()
        {
            this.simpleTokens = Lexer.Tokenize("x = 42 + y");
            this.keywordTokens = Lexer.Tokenize("if x: return True");
        }

        [Fact]
        public void TokenizeReturnsCorrectCount()
        {
            Assert.Equal(5, this.simpleTokens.Count);
        }

        [Fact]
        public void TokenizeReturnsCorrectTypes()
        {
            Assert.Equal(TokenType.Identifier, this.simpleTokens[0].Type);
            Assert.Equal(TokenType.Operator, this.simpleTokens[1].Type);
            Assert.Equal(TokenType.Number, this.simpleTokens[2].Type);
            Assert.Equal(TokenType.Operator, this.simpleTokens[3].Type);
            Assert.Equal(TokenType.Identifier, this.simpleTokens[4].Type);
        }

        [Fact]
        public void KeywordsAreRecognized()
        {
            var keywords = this.keywordTokens
                .Where(t => t.Type == TokenType.Keyword)
                .ToList();
            Assert.True(keywords.Count >= 3,
                $"Очікувалось >= 3 ключових слова, отримано: {keywords.Count}");
        }

        [Fact]
        public void ThrowsOnNullInput()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Lexer.Tokenize(null));
        }

        [Fact]
        public void EmptyInputReturnsEmptyList()
        {
            var result = Lexer.Tokenize("");
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void TokenValuesMatchInput()
        {
            var values = this.simpleTokens.Select(t => t.Value).ToList();
            values.Should().ContainInOrder("x", "=", "42", "+", "y");
            values.Should().NotContain(string.Empty);
        }

        [Fact]
        public void CommentIsTokenizedCorrectly()
        {
            var tokens = Lexer.Tokenize("x = 1 # це коментар");
            tokens.Should().ContainSingle(t => t.Type == TokenType.Comment);
            tokens.Should().NotContain(t => t.Value == "#");
        }

        [Theory]
        [InlineData("42", TokenType.Number)]
        [InlineData("3.14", TokenType.Number)]
        [InlineData("0xFF", TokenType.Number)]
        [InlineData("#comment", TokenType.Comment)]
        [InlineData("+", TokenType.Operator)]
        [InlineData("(", TokenType.Delimiter)]
        [InlineData("myVar", TokenType.Identifier)]
        [InlineData("if", TokenType.Keyword)]
        [InlineData("return", TokenType.Keyword)]
        public void SingleTokenHasCorrectType(string input, TokenType expected)
        {
            var tokens = Lexer.Tokenize(input);
            Assert.Single(tokens);
            Assert.Equal(expected, tokens[0].Type);
        }

        [Theory]
        [InlineData("if")]
        [InlineData("while")]
        [InlineData("return")]
        [InlineData("True")]
        [InlineData("None")]
        public void PythonKeywordsAreClassifiedCorrectly(string keyword)
        {
            var tokens = Lexer.Tokenize(keyword);
            Assert.Single(tokens);
            tokens[0].Type.Should().Be(TokenType.Keyword,
                because: $"{keyword} є зарезервованим словом Python");
        }

        [Theory]
        [InlineData("==")]
        [InlineData("!=")]
        [InlineData("<=")]
        [InlineData(">=")]
        [InlineData("**")]
        public void OperatorsAreClassifiedCorrectly(string op)
        {
            var tokens = Lexer.Tokenize(op);
            Assert.Single(tokens);
            Assert.Equal(TokenType.Operator, tokens[0].Type);
        }

        [Fact]
        public void FullExpressionIsTokenizedCorrectly()
        {
            var tokens = Lexer.Tokenize("def foo(x): return x * 2");
            tokens.Should().NotBeEmpty();
            tokens.Should().Contain(t => t.Type == TokenType.Keyword && t.Value == "def");
            tokens.Should().Contain(t => t.Type == TokenType.Identifier && t.Value == "foo");
            tokens.Should().Contain(t => t.Type == TokenType.Number && t.Value == "2");
        }

        [Fact]
        public void UnknownCharacterProducesError()
        {
            var tokens = Lexer.Tokenize("@");
            tokens.Should().ContainSingle();
            tokens[0].Type.Should().Be(TokenType.Error);
        }
    }
}