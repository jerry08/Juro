using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Juro.Utils.Extensions
{
    public static class StreamExtensions
    {
        public static async ValueTask CopyToAsync(this Stream source,
            Stream destination,
            IProgress<double>? progress = null,
            long totalLength = 0,
            int bufferSize = 0x1000,
            CancellationToken cancellationToken = default)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;
            long totalRead = 0;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                totalRead += bytesRead;
                //progress?.Report(totalRead);
                //Report as percentage
                progress?.Report((double)totalRead / (double)totalLength * 100 / 100);
            }
        }
    }
}