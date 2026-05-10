using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using FluentAssertions;
using LexicalAnalyzer;

namespace LexicalAnalyzer.Tests
{
   
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
        private readonly ICodeReader _reader;

        public AnalysisService(ICodeReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public List<Token> AnalyzeFile(string path)
        {
            if (!_reader.FileExists(path))
                throw new FileNotFoundException($"Файл не знайдено: {path}");

            var code = _reader.ReadCode(path);
            return Lexer.Tokenize(code);
        }

        public List<List<Token>> AnalyzeDirectory(string directory)
        {
            var results = new List<List<Token>>();
            foreach (var file in _reader.GetFiles(directory, "*.py"))
            {
                var code = _reader.ReadCode(file);
                results.Add(Lexer.Tokenize(code));
            }
            return results;
        }
    }

   
    public class AnalysisServiceMockTests
    {
        private readonly ICodeReader _mockReader;
        private readonly AnalysisService _service;

        public AnalysisServiceMockTests()
        {
            _mockReader = Substitute.For<ICodeReader>();
            _service = new AnalysisService(_mockReader);
        }

       
        [Fact]
        public void AnalyzeFile_ValidPath_CallsReaderMethodsOnce()
        {
            const string path = "test.py";
            _mockReader.FileExists(path).Returns(true);
            _mockReader.ReadCode(path).Returns("x = 42");

            var tokens = _service.AnalyzeFile(path);

            tokens.Should().NotBeEmpty();
            _mockReader.Received(1).FileExists(path);
            _mockReader.Received(1).ReadCode(path);
        }

       
        [Fact]
        public void AnalyzeFile_ReaderThrowsIOException_PropagatesException()
        {
            const string path = "broken.py";
            _mockReader.FileExists(path).Returns(true);
            _mockReader.ReadCode(path).Throws(new IOException("Диск недоступний"));

            Action act = () => _service.AnalyzeFile(path);

            act.Should().Throw<IOException>()
               .WithMessage("Диск недоступний");
            _mockReader.Received(1).ReadCode(path);
        }

        
        [Fact]
        public void AnalyzeFile_FileNotExists_ThrowsFileNotFoundException()
        {
            const string path = "missing.py";
            _mockReader.FileExists(path).Returns(false);

            Action act = () => _service.AnalyzeFile(path);

            act.Should().Throw<FileNotFoundException>()
               .WithMessage("*missing.py*");
            _mockReader.DidNotReceive().ReadCode(Arg.Any<string>());
        }

       
        [Fact]
        public void AnalyzeFile_ArgumentMatching_DifferentBehaviorByExtension()
        {
            _mockReader.FileExists(Arg.Is<string>(p => p.EndsWith(".py"))).Returns(true);
            _mockReader.FileExists(Arg.Is<string>(p => p.EndsWith(".txt"))).Returns(false);
            _mockReader.ReadCode(Arg.Is<string>(p => p.EndsWith(".py"))).Returns("x = 1");

            var pyTokens = _service.AnalyzeFile("script.py");
            pyTokens.Should().NotBeEmpty();

            Action act = () => _service.AnalyzeFile("notes.txt");
            act.Should().Throw<FileNotFoundException>();

            _mockReader.DidNotReceive().ReadCode(Arg.Is<string>(p => p.EndsWith(".txt")));
        }

       
        [Fact]
        public void AnalyzeDirectory_ReturnsSequenceOfDifferentResults()
        {
            var files = new[] { "a.py", "b.py" };
            _mockReader.GetFiles("src", "*.py").Returns(files);
            _mockReader.ReadCode("a.py").Returns(
                "if x: return True",
                "# коментар");         

           
            var results1 = _service.AnalyzeDirectory("src");
            results1.Should().HaveCount(2);
            results1[0].Should().Contain(t => t.Type == TokenType.Keyword);

           
            var results2 = _service.AnalyzeDirectory("src");
            results2[0].Should().Contain(t => t.Type == TokenType.Comment);
        }

       
        [Fact]
        public void AnalyzeFile_CallOrder_FileExistsBeforeReadCode()
        {
            const string path = "order.py";
            var callOrder = new List<string>();

            _mockReader.FileExists(path).Returns(x => {
                callOrder.Add("FileExists");
                return true;
            });
            _mockReader.ReadCode(path).Returns(x => {
                callOrder.Add("ReadCode");
                return "pass";
            });

            _service.AnalyzeFile(path);

            callOrder.Should().ContainInOrder("FileExists", "ReadCode");
        }
    }
}