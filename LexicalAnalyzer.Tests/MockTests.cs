// <copyright file="MockTests.cs" company="KNU">
// Copyright (c) 2026 Андрущенко Альона. All rights reserved.
// </copyright>

namespace LexicalAnalyzer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using FluentAssertions;
    using LexicalAnalyzer;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using Xunit;

    public interface ICodeReader
    {
        string ReadCode(string path);

        bool FileExists(string path);

        IEnumerable<string> GetFiles(string directory, string pattern);
    }

    public class FileCodeReader : ICodeReader
    {
        public string ReadCode(string path) => File.ReadAllText(path);

        public bool FileExists(string path) => File.Exists(path);

        public IEnumerable<string> GetFiles(string directory, string pattern)
            => Directory.GetFiles(directory, pattern);
    }

    public class AnalysisService
    {
        private readonly ICodeReader reader;

        public AnalysisService(ICodeReader reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public List<Token> AnalyzeFile(string path)
        {
            if (!this.reader.FileExists(path))
            {
                throw new FileNotFoundException($"Файл не знайдено: {path}");
            }

            var code = this.reader.ReadCode(path);
            return Lexer.Tokenize(code);
        }

        public List<List<Token>> AnalyzeDirectory(string directory)
        {
            var results = new List<List<Token>>();
            foreach (var file in this.reader.GetFiles(directory, "*.py"))
            {
                var code = this.reader.ReadCode(file);
                results.Add(Lexer.Tokenize(code));
            }

            return results;
        }
    }

    public class AnalysisServiceMockTests
    {
        private readonly ICodeReader mockReader;
        private readonly AnalysisService service;

        public AnalysisServiceMockTests()
        {
            this.mockReader = Substitute.For<ICodeReader>();
            this.service = new AnalysisService(this.mockReader);
        }

        [Fact]
        public void AnalyzeFile_ValidPath_CallsReaderMethodsOnce()
        {
            const string path = "test.py";
            this.mockReader.FileExists(path).Returns(true);
            this.mockReader.ReadCode(path).Returns("x = 42");

            var tokens = this.service.AnalyzeFile(path);

            tokens.Should().NotBeEmpty();
            this.mockReader.Received(1).FileExists(path);
            this.mockReader.Received(1).ReadCode(path);
        }

        [Fact]
        public void AnalyzeFile_ReaderThrowsIOException_PropagatesException()
        {
            const string path = "broken.py";
            this.mockReader.FileExists(path).Returns(true);
            this.mockReader.ReadCode(path).Throws(new IOException("Диск недоступний"));

            Action act = () => this.service.AnalyzeFile(path);

            act.Should().Throw<IOException>()
               .WithMessage("Диск недоступний");
            this.mockReader.Received(1).ReadCode(path);
        }

        [Fact]
        public void AnalyzeFile_FileNotExists_ThrowsFileNotFoundException()
        {
            const string path = "missing.py";
            this.mockReader.FileExists(path).Returns(false);

            Action act = () => this.service.AnalyzeFile(path);

            act.Should().Throw<FileNotFoundException>()
               .WithMessage("*missing.py*");
            this.mockReader.DidNotReceive().ReadCode(Arg.Any<string>());
        }

        [Fact]
        public void AnalyzeFile_ArgumentMatching_DifferentBehaviorByExtension()
        {
            this.mockReader.FileExists(Arg.Is<string>(p => p.EndsWith(".py"))).Returns(true);
            this.mockReader.FileExists(Arg.Is<string>(p => p.EndsWith(".txt"))).Returns(false);
            this.mockReader.ReadCode(Arg.Is<string>(p => p.EndsWith(".py"))).Returns("x = 1");

            var pyTokens = this.service.AnalyzeFile("script.py");
            pyTokens.Should().NotBeEmpty();

            Action act = () => this.service.AnalyzeFile("notes.txt");
            act.Should().Throw<FileNotFoundException>();

            this.mockReader.DidNotReceive().ReadCode(Arg.Is<string>(p => p.EndsWith(".txt")));
        }

        [Fact]
        public void AnalyzeDirectory_ReturnsSequenceOfDifferentResults()
        {
            var files = new[] { "a.py", "b.py" };
            this.mockReader.GetFiles("src", "*.py").Returns(files);
            this.mockReader.ReadCode("a.py").Returns(
                "if x: return True",
                "# коментар");

            var results1 = this.service.AnalyzeDirectory("src");
            results1.Should().HaveCount(2);
            results1[0].Should().Contain(t => t.Type == TokenType.Keyword);

            var results2 = this.service.AnalyzeDirectory("src");
            results2[0].Should().Contain(t => t.Type == TokenType.Comment);
        }

        [Fact]
        public void AnalyzeFile_CallOrder_FileExistsBeforeReadCode()
        {
            const string path = "order.py";
            var callOrder = new List<string>();

            this.mockReader.FileExists(path).Returns(x =>
            {
                callOrder.Add("FileExists");
                return true;
            });
            this.mockReader.ReadCode(path).Returns(x =>
            {
                callOrder.Add("ReadCode");
                return "pass";
            });

            this.service.AnalyzeFile(path);

            callOrder.Should().ContainInOrder("FileExists", "ReadCode");
        }
    }
}