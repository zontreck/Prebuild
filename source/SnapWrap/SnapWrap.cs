using System;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;

namespace SnapWrap
{
    public class SnapWrap
    {
        public static void Main(string[] args)
        {
            string input = args[0];
            string output = args[1];
            var customImports = args[2].Split("..");

            if (customImports.Length == 0) customImports = new string[3] { "System", "System.Diagnostics.Process", "System.IO" };

            try
            {
                var inputCode = File.ReadAllText(input);

                var options = ScriptOptions.Default
                    .WithReferences(AppDomain.CurrentDomain.GetAssemblies())  // Add necessary assemblies
                    .WithReferences(typeof(Process).Assembly,
                                    typeof(Console).Assembly
                        )
                    .WithOptimizationLevel(Microsoft.CodeAnalysis.OptimizationLevel.Release)
                    .WithImports(customImports)
                    .WithAllowUnsafe(true);


                Console.WriteLine("Run Script: " + input);
                Console.WriteLine("Script Length: " + inputCode.Length);


                // Capture console output using custom class
                using (var consoleOutput = new ConsoleOutput())
                {
                    var scriptState = CSharpScript.RunAsync(inputCode, options).Result;

                    // Get captured output from the ConsoleOutput class
                    string capturedOutput = consoleOutput.GetOutput();

                    // Write captured output to output file
                    File.WriteAllText(output, capturedOutput);

                }
                Console.WriteLine("Output file generated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }

    // Custom class to capture console output
    public class ConsoleOutput : IDisposable
    {
        private StringWriter _stringWriter;
        private TextWriter _originalOutput;

        public ConsoleOutput()
        {
            _stringWriter = new StringWriter();
            _originalOutput = Console.Out;
            Console.SetOut(_stringWriter);
        }

        public string GetOutput()
        {
            Console.SetOut(_originalOutput);
            string capturedOutput = _stringWriter.ToString();
            _stringWriter.Dispose();
            return capturedOutput;
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            _stringWriter.Dispose();
        }
    }
}
