using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Juro.Tests.Specs;

/// <summary>
/// Helpers for live-site tests whose outcome depends on the test machine's
/// network environment rather than on provider code.
/// </summary>
internal static class LiveSiteGuard
{
    /// <summary>
    /// Runs the test body and skips the test when the site answers with a
    /// Cloudflare JS challenge (403). The challenge requires a JS runtime to
    /// solve, which a pure HTTP library cannot do — callers must supply an
    /// <see cref="HttpClient"/> carrying a pre-solved cf_clearance cookie.
    /// </summary>
    public static async Task SkipOnCloudflareChallengeAsync(string providerName, Func<Task> body)
    {
        try
        {
            await body();
        }
        catch (HttpRequestException exception) when (exception.Message.Contains("403"))
        {
            Assert.Skip(
                $"{providerName} is behind a Cloudflare JS challenge (403); "
                    + "solving it requires a JS-capable client."
            );
        }
    }

    /// <summary>
    /// Runs the test body and skips the test when the site is unreachable at
    /// the transport level (connection reset/refused/timeout), which indicates
    /// a network or region block rather than a provider defect. DNS failures
    /// and HTTP error statuses still fail the test.
    /// </summary>
    public static async Task SkipWhenBlockedAtTransportAsync(string providerName, Func<Task> body)
    {
        try
        {
            await body();
        }
        catch (HttpRequestException exception) when (IsTransportFailure(exception))
        {
            Assert.Skip($"{providerName} is unreachable from this network (connection blocked).");
        }
    }

    private static bool IsTransportFailure(HttpRequestException exception)
    {
        if (exception.Message.Contains("Response status code"))
            return false;

        for (
            Exception? inner = exception.InnerException;
            inner is not null;
            inner = inner.InnerException
        )
        {
            if (inner is SocketException socketException)
                return socketException.SocketErrorCode
                    is SocketError.ConnectionReset
                        or SocketError.ConnectionRefused
                        or SocketError.ConnectionAborted
                        or SocketError.TimedOut;

            if (inner is IOException && inner.InnerException is null)
                return true;
        }

        return false;
    }
}
