using System;
using System.IO;
using System.Threading;

namespace Juro.DemoConsole.Utils;

internal class ConsoleProgress(TextWriter writer) : IProgress<double>, IDisposable
{
    private readonly TextWriter _writer = writer;
    private readonly int _posX = Console.CursorLeft;
    private readonly int _posY = Console.CursorTop;

    private int _lastLength;

    private Timer? timer;
    private double currentProgress = 0;

    public ConsoleProgress()
        : this(Console.Out) { }

    private void TimerHandler(object? state)
    {
        Write($"{currentProgress:P1}");
    }

    private void EraseLast()
    {
        if (_lastLength > 0)
        {
            Console.SetCursorPosition(_posX, _posY);
            _writer.Write(new string(' ', _lastLength));
            Console.SetCursorPosition(_posX, _posY);
        }
    }

    private void Write(string text)
    {
        EraseLast();
        _writer.Write(text);
        _lastLength = text.Length;
    }

    public void Report(double progress)
    {
        timer ??= new Timer(TimerHandler, null, 0, 200);
        currentProgress = progress;

        //Write($"{progress:P1}");
    }

    public void Dispose() => EraseLast();
}
