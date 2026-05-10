using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LexicalAnalyzer
{
    public enum TokenType
    {
        Number,
        String,
        Comment,
        Keyword,
        Operator,
        Delimiter,
        Identifier,
        Error
    }

    public class Token
    {
        public string Value { get; }
        public TokenType Type { get; }

        public Token(string value, TokenType type)
        {
            Value = value;
            Type = type;
        }

        public override string ToString() => $"<\"{Value}\", {Type}>";
    }

    public static class Lexer
    {
        private static readonly HashSet<string> Keywords = new()
        {
            "False","None","True","and","as","assert","async","await",
            "break","class","continue","def","del","elif","else",
            "except","finally","for","from","global","if","import",
            "in","is","lambda","nonlocal","not","or","pass","raise",
            "return","try","while","with","yield"
        };

        private static readonly (Regex Pattern, TokenType Type)[] Patterns =
        {
            (new Regex(@"#[^\n]*"),                                      TokenType.Comment),
            (new Regex(@"(""(?:[^""\\]|\\.)*"")|(\'(?:[^\'\\]|\\.)*\')"), TokenType.String),
            (new Regex(@"\b0[xX][0-9a-fA-F]+"),                          TokenType.Number),
            (new Regex(@"\b[0-9]*\.?[0-9]+([eE][+-]?[0-9]+)?"),          TokenType.Number),
            (new Regex(@"\*\*|//=?|<<=?|>>=?|==|!=|<=|>=|[-+*/%&|^~<>!=]"), TokenType.Operator),
            (new Regex(@"[()\[\]{}.,:;]"),                               TokenType.Delimiter),
            (new Regex(@"\b[a-zA-Z_][a-zA-Z0-9_]*"),                     TokenType.Identifier),
            (new Regex(@"."),                                             TokenType.Error),
        };

        public static List<Token> Tokenize(string code)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            var tokens = new List<Token>();
            int pos = 0;
            int len = code.Length;

            while (pos < len)
            {
                
                if (char.IsWhiteSpace(code[pos])) { pos++; continue; }

                bool matched = false;
                foreach (var (pattern, type) in Patterns)
                {
                    var m = pattern.Match(code, pos);
                    if (m.Success && m.Index == pos)
                    {
                        var val = m.Value;
                        var actualType = (type == TokenType.Identifier
                                          && Keywords.Contains(val))
                                         ? TokenType.Keyword : type;
                        tokens.Add(new Token(val, actualType));
                        pos += m.Length;
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    tokens.Add(new Token(code[pos].ToString(), TokenType.Error));
                    pos++;
                }
            }
            return tokens;
        }

        public static string TokenTypeToString(TokenType t) => t.ToString();
    }
}