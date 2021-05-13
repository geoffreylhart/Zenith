using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZEditor.ZComponents.Data
{
    public class IndentableStreamWriter : IDisposable
    {
        static string INDENT_TEXT = "  ";
        int indentation = 0;
        private StreamWriter writer;

        public IndentableStreamWriter(string fullPath)
        {
            this.writer = new StreamWriter(fullPath);
        }

        internal void WriteLine(string value)
        {
            for (int i = 0; i < indentation; i++) writer.Write(INDENT_TEXT);
            writer.WriteLine(value);
        }

        internal void Indent()
        {
            indentation++;
        }

        internal void UnIndent()
        {
            indentation = Math.Max(0, indentation - 1);
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}
