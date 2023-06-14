using System.Net.Http;

namespace Juro;

/// <summary>
/// A factory abstraction for a component that can create <see cref="HttpClient"/> instances with custom
/// configuration for a given logical name.
/// </summary>
/// <remarks>
/// A default <see cref="IHttpClientFactory"/> can be registered as a service.
/// </remarks>
public interface IHttpClientFactory
{
    /// <summary>
    /// Creates and configures an <see cref="HttpClient"/> instance.
    /// </summary>
    /// <returns>A new <see cref="HttpClient"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// Each call to <see cref="CreateClient()"/> is guaranteed to return a new <see cref="HttpClient"/>
    /// instance. It is generally not necessary to dispose of the <see cref="HttpClient"/> as the
    /// <see cref="IHttpClientFactory"/> tracks and disposes resources used by the <see cref="HttpClient"/>.
    /// </para>
    /// <para>
    /// Callers are also free to mutate the returned <see cref="HttpClient"/> instance's public properties
    /// as desired.
    /// </para>
    /// </remarks>
    HttpClient CreateClient();
}