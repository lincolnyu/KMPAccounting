using System;
using System.IO;

namespace KMPAccounting.Objects.Serialization
{
    public class LineLoader : IDisposable
    {
        public LineLoader(TextReader textReader) => TextReader = textReader;

        public TextReader TextReader { get; }

        private string? _loadedLine;

        private bool _endOfStream;

        public string? PeekLine()
        {
            if (_loadedLine == null && !_endOfStream)
            {
                _loadedLine = TextReader.ReadLine();
                if (_loadedLine == null)
                {
                    _endOfStream = true;
                }
            }

            return _loadedLine;
        }

        public string? ReadLine()
        {
            var line = PeekLine();
            _loadedLine = null;
            return line;
        }

        public void Dispose()
        {
            TextReader?.Dispose();
        }
    }
}
