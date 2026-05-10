using System;
using System.IO;
using LexicalAnalyzer;

Console.Write("Enter the name of the Python file to analyze: ");
string filename = Console.ReadLine();

if (!File.Exists(filename))
{
    Console.Error.WriteLine($"Error: Could not open file '{filename}'");
    return;
}

string code = File.ReadAllText(filename);
var tokens = Lexer.Tokenize(code);

Console.WriteLine("\n--- Lexical Analysis Result ---");
foreach (var token in tokens)
    Console.WriteLine(token);

Console.WriteLine("\nAnalysis completed.");