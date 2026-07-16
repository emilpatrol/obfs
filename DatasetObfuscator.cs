using System;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace DatasetObfuscator
{
    public class DatasetItem
    {
        public string instruction { get; set; }
        public string input { get; set; }
        public string output { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string inputFile = "Z://dataset.jsonl";
            string outputFile = "Z://dataset_obfuscated.jsonl";

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Файл {inputFile} не найден!");
                return;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            };

            var pipeline = new ObfuscationPipeline();
            int processedCount = 0;

            Console.WriteLine("Начинаем процесс обфускации...");

            using (StreamReader reader = new StreamReader(inputFile))
            using (StreamWriter writer = new StreamWriter(outputFile, false))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        var item = JsonSerializer.Deserialize<DatasetItem>(line);

                        if (item != null && !string.IsNullOrEmpty(item.input))
                        {
                           
                            string obfuscatedCode = pipeline.Process(item.input);

                           
                            item.output = obfuscatedCode;

                       
                            string newLine = JsonSerializer.Serialize(item, jsonOptions);
                            writer.WriteLine(newLine);

                            processedCount++;
                            if (processedCount % 100 == 0)
                            {
                                Console.WriteLine($"Обработано методов: {processedCount}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                      
                        Console.WriteLine($"Ошибка парсинга строки: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"\nГотово! Датасет сохранен в {outputFile}");
            Console.WriteLine($"Успешно обфусцировано: {processedCount} примеров.");
        }
    }

  
    public class ObfuscationPipeline
    {
        private readonly List<CSharpSyntaxRewriter> _techniques;

        public ObfuscationPipeline()
        {
          
            _techniques = new List<CSharpSyntaxRewriter>
            {
                new StringEncryptionRewriter(),
                new RenameVariablesRewriter(),
                new DeadCodeRewriter(),
                new OpaquePredicateRewriter()
            };
        }

        public string Process(string sourceCode)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            SyntaxNode currentRoot = root;

          
            foreach (var technique in _techniques)
            {
                currentRoot = technique.Visit(currentRoot);
            }

          
            return currentRoot.NormalizeWhitespace().ToFullString();
        }
    }

    // --- ТЕХНИКА 1: Шифрование строк (String Encryption) ---
    public class StringEncryptionRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression))
            {
                string rawValue = node.Token.ValueText;

               
                if (string.IsNullOrEmpty(rawValue))
                    return base.VisitLiteralExpression(node);

               
                string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawValue));

              
                var encryptedNode = SyntaxFactory.ParseExpression($"System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(\"{base64}\"))");

                return encryptedNode.WithTriviaFrom(node);
            }
            return base.VisitLiteralExpression(node);
        }
    }

    // --- ТЕХНИКА 2: Переименование переменных (Variable Renaming) ---
    public class RenameVariablesRewriter : CSharpSyntaxRewriter
    {
        private int _counter = 0;
     
        private Dictionary<string, string> _nameMap = new Dictionary<string, string>();

    
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
           
            _nameMap.Clear();

           
            _counter = 0;

           
            return base.VisitMethodDeclaration(node);
        }

       
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            string currentName = node.Identifier.Text;

            if (_nameMap.TryGetValue(currentName, out string newName))
            {
                return SyntaxFactory.IdentifierName(newName).WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }
    }

    public class DeadCodeRewriter : CSharpSyntaxRewriter
    {
        private int _counter = 0;
        private Random _rnd = new Random();

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            
            if (node.Statements.Count == 0) return base.VisitBlock(node);

            
            int randomNum1 = _rnd.Next(10, 999);
            int randomNum2 = _rnd.Next(10, 999);
            string junkCode = $"int _dead_{_counter++} = {randomNum1} + {randomNum2};";

            var junkStatement = SyntaxFactory.ParseStatement(junkCode);

           
            var newStatements = node.Statements.Insert(0, junkStatement);
            var newBlock = node.WithStatements(newStatements);

            return base.VisitBlock(newBlock);
        }
    }

    public class OpaquePredicateRewriter : CSharpSyntaxRewriter
{
    private Random _rnd = new Random();


    private string GetAlwaysTruePredicate()
    {
        string[] predicates = new string[]
        {
            "System.DateTime.Now.Year > 1900",
            "System.Environment.TickCount != 2147483647", // Не равно MaxValue
            "System.Guid.NewGuid().ToString().Length > 5",
            "new System.Random().Next() >= 0",
            "System.Diagnostics.Process.GetCurrentProcess().Id > 0",
            "System.TimeSpan.TicksPerSecond > 0"
        };
        
        return predicates[_rnd.Next(predicates.Length)];
    }

    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var visitedNode = (BlockSyntax)base.VisitBlock(node);
        var newStatements = new List<StatementSyntax>();

        foreach (var statement in visitedNode.Statements)
        {
            if (statement.ToString().Contains("_dead_"))
            {
                newStatements.Add(statement);
                continue;
            }

            if (statement is LocalDeclarationStatementSyntax)
            {
                newStatements.Add(statement);
                continue; 
            }

            if (_rnd.Next(0, 2) == 0)
            {
              
                string conditionString = GetAlwaysTruePredicate();
                var condition = SyntaxFactory.ParseExpression(conditionString);

            
                var elseBlock = SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement("int _junk_ = " + _rnd.Next(1000, 9999) + ";")
                );

                var ifStatement = SyntaxFactory.IfStatement(
                    condition, 
                    SyntaxFactory.Block(statement)
                ).WithElse(SyntaxFactory.ElseClause(elseBlock));

                newStatements.Add(ifStatement);
            }
            else
            {
                newStatements.Add(statement);
            }
        }

        return visitedNode.WithStatements(SyntaxFactory.List(newStatements));
    }
}
}
