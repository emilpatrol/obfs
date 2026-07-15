using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DatasetGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
           
            string sourceDirectory = @"Z://csharp_raw_sources";
            string outputFile = "Z://dataset.jsonl";

            var files = Directory.GetFiles(sourceDirectory, "*.cs");
            Console.WriteLine($"Найдено файлов для анализа: {files.Length}");

            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            };

            int methodsExtracted = 0;

            using (StreamWriter writer = new StreamWriter(outputFile, false))
            {
                foreach (var file in files)
                {
                    string code = File.ReadAllText(file);

                    
                    var syntaxTree = CSharpSyntaxTree.ParseText(code);
                    var root = syntaxTree.GetRoot();

                 
                    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var method in methods)
                    {
                       
                        int lineCount = method.ToString().Split('\n').Length;
                        if (method.Body != null && lineCount > 3 && lineCount < 50)
                        {
                            string cleanCode = method.ToString();

                           
                            string obfuscatedCode = FakeObfuscate(cleanCode);

                           
                            var datasetItem = new
                            {
                                instruction = "Obfuscate the following C# method by encrypting strings and flattening the control flow.",
                                input = cleanCode,
                                output = obfuscatedCode
                            };

                           
                            string jsonLine = JsonSerializer.Serialize(datasetItem, jsonOptions);
                            writer.WriteLine(jsonLine);

                            methodsExtracted++;
                        }
                    }

                    if (methodsExtracted >= 10000) 
                    {
                        break;
                    }
                }
            }

            Console.WriteLine($"Успешно! Извлечено и сохранено методов: {methodsExtracted}");
            Console.WriteLine($"Файл сохранен: {outputFile}");
        }

        
        static string FakeObfuscate(string sourceCode)
        {
            return "/* OBFUSCATED BY AI DATASET GENERATOR */\n" + sourceCode;
        }
    }
}
