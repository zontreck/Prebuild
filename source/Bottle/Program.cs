﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace Bottle
{
    public class Program
    {
        private static void Usage()
        {
            Console.WriteLine("Bottle\nJson to C# transformer\n\nBottle [JsonFile] [OutputFile]");
        }
        public static void Main(string[] args)
        {
            if(args.Length == 0 && File.Exists("test.json"))
            {
                args = new[] { "test.json", "test.cs" };
            }
            if(args.Length != 2)
            {
                Usage();
                return;
            }
            string rawText = "";
            string file2 = args[1];

            if (File.Exists(args[0]))
            {
                rawText = File.ReadAllText(args[0]);

                JsonObject node = JsonObject.Parse(rawText).AsObject();

                Console.WriteLine("Preparing to parse...");
                if (File.Exists(file2)) File.Delete(file2);
                Thread.Sleep(1000);

                MirrorWriter bw = new MirrorWriter();

                bw.WriteLine($"using System;");

                bw.WriteLine();

                // Retrieve namespace name, first value in json
                var ns = node["namespace"].AsValue();
                bw.WriteLine($"namespace {ns}");
                bw.WriteLine("{");

                loop(node["data"], bw, 1);


                bw.WriteLine("}");


                File.WriteAllText(file2, bw.GetStringBuilder().ToString());

            }else
            {
                Console.WriteLine("> FILE NOT FOUND <");
            }
        }

        public static void loop(JsonNode? node, StringWriter writer, int indent = 0)
        {
            foreach (KeyValuePair<string, JsonNode> kvp in node.AsObject())
            {
                if(kvp.Value is JsonValue value)
                {
                    writer.WriteLine(doPutValue(kvp.Key, value, indent));
                } else if(kvp.Value is JsonObject obj)
                {
                    writer.WriteLine($"{indents(indent)}public static class {kvp.Key}");
                    writer.WriteLine(indents(indent)+"{");

                    loop(obj, writer, indent+1);

                    writer.WriteLine(indents(indent)+"}");
                }
            }
        }

        public static string indents(int count)
        {
            return "".PadLeft(count*4);
        }

        public static string doPutValue(string key, JsonValue value, int indent)
        {
            StringBuilder builder = new();

            if (value.TryGetValue(out int int1))
            {
                builder.AppendLine($"{indents(indent)}public static readonly int {key} = {int1};");
            } else if (value.TryGetValue(out string str1))
            {
                builder.AppendLine($"{indents(indent)}public static readonly string {key} = \"{str1}\";");
            } else if (value.TryGetValue(out bool bool1))
            {
                builder.AppendLine($"{indents(indent)}public static readonly bool {key} = {bool2String(bool1)};");
            } else if (value.TryGetValue(out float f1))
            {
                builder.AppendLine($"{indents(indent)}public static readonly float {key} = {f1}f;");
            } else if (value.TryGetValue(out double d1))
            {
                builder.AppendLine($"{indents(indent)}public static readonly double {key} = {d1};");
            } else if (value.TryGetValue(out byte b1))
            {
                builder.AppendLine($"{indents(indent)}public static readonly byte {key} = {b1};");
            } else if (value.TryGetValue(out long l1))
            {
                builder.AppendLine($"{indents(indent)}public static readonly long {key} = {l1}L;");
            }

            return builder.ToString();
        }

        public static string bool2String(bool b)
        {
            if (b) return "true";
            return "false";
        }
    }

    public class MirrorWriter : StringWriter
    {
        public static void w(object? val)
        {
            Console.Write(val);
        }

        public override void Write(bool value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(char value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(decimal value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(double value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(float value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(int value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(long value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
            w(buffer.Skip(index).Take(count).ToArray());
        }

        public override void Write(char[] buffer)
        {
            base.Write(buffer);
            w(buffer);
        }

        public override void Write(string value)
        {
            base.Write(value);
            w(value);
        }


        public override void Write(uint value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(object value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(ulong value)
        {
            base.Write(value);
            w(value);
        }

        public override void Write(StringBuilder value)
        {
            base.Write(value);
            w(value.ToString());
        }
    }
}
