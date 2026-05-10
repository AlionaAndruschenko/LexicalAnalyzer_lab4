// <copyright file="Program.cs" company="KNU">
// Copyright (c) 2026 Андрущенко Альона. All rights reserved.
// </copyright>

using LexicalAnalyzer;
using System;
using System.IO;

Console.Write("Enter the name of the Python file to analyze: ");
string filename = Console.ReadLine() ?? string.Empty;

if (!File.Exists(filename))
{
    Console.Error.WriteLine($"Error: Could not open file '{filename}'");
    return;
}

string code = await File.ReadAllTextAsync(filename);
var tokens = Lexer.Tokenize(code);

Console.WriteLine("\n--- Lexical Analysis Result ---");
foreach (var token in tokens)
{
    await Console.Out.WriteLineAsync(token.ToString());
}

Console.WriteLine("\nAnalysis completed.");