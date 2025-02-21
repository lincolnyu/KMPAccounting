using System;
using System.IO;

namespace KMPAccounting.Objects.Serialization
{
    public class LineLoader(TextReader textReader) : IDisposable
    {
        public TextReader TextReader { get; } = textReader;

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
