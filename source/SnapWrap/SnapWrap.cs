using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapWrap
{
    public class SnapWrap
    {
        public static void Main(string[] args)
        {
            string input = args[0];
            string output = args[1];

            try
            {
                var inputCode = File.ReadAllText(input);


                var options = ScriptOptions.Default
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies())  // Add necessary assemblies
                .WithImports("System");

                using (var sw = new StreamWriter(output))
                {
                    Console.SetOut(sw); // Redirect console output
                    var scriptState = CSharpScript.RunAsync(inputCode, options).Result;

                    // Reset console output
                    Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));

                    Console.WriteLine("Output file generated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}
