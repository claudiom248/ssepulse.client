using System.Net.Http;
using NSubstitute;
using SsePulse.Client.Authentication.Abstractions;
using SsePulse.Client.Authentication.Internal;

namespace SsePulse.Client.Authentication.Tests;

public class AuthenticationRequestMutatorTests
{
    [Fact]
    public async Task ApplyAsync_WithProvider_CallsProviderApplyAsync()
    {
        // ARRANGE
        ISseAuthenticationProvider provider = Substitute.For<ISseAuthenticationProvider>();
        HttpRequestMessage request = new();
        CancellationToken cancellationToken = CancellationToken.None;

        AuthenticationRequestMutator mutator = new(provider);

        // ACT
        await mutator.ApplyAsync(request, cancellationToken);

        // ASSERT
        await provider.Received(1).ApplyAsync(request, cancellationToken);
    }

    [Fact]
    public async Task ApplyAsync_WhenProviderAddsHeader_HeaderIsSet()
    {
        // ARRANGE
        ISseAuthenticationProvider provider = Substitute.For<ISseAuthenticationProvider>();
        HttpRequestMessage request = new();
        
        provider
            .When(x => x.ApplyAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()))
            .Do(x =>
            {
                HttpRequestMessage msg = x.Arg<HttpRequestMessage>();
                msg.Headers.Add("Authorization", "Bearer test-token");
            });

        AuthenticationRequestMutator mutator = new(provider);

        // ACT
        await mutator.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(request.Headers.TryGetValues("Authorization", out var values));
        Assert.Single(values);
        Assert.Equal("Bearer test-token", values.First());
    }

    [Fact]
    public async Task ApplyAsync_WhenProviderThrows_ThrowsException()
    {
        // ARRANGE
        ISseAuthenticationProvider provider = Substitute.For<ISseAuthenticationProvider>();
        HttpRequestMessage request = new();
        InvalidOperationException expectedException = new("Authentication failed");
        
        provider
            .ApplyAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask(Task.FromException(expectedException)));

        AuthenticationRequestMutator mutator = new(provider);

        // ACT & ASSERT
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mutator.ApplyAsync(request, CancellationToken.None).AsTask());
        Assert.Equal("Authentication failed", exception.Message);
    }

    [Fact]
    public async Task ApplyAsync_WithCancellationToken_PassesTokenToProvider()
    {
        // ARRANGE
        ISseAuthenticationProvider provider = Substitute.For<ISseAuthenticationProvider>();
        HttpRequestMessage request = new();
        CancellationTokenSource cts = new();
        CancellationToken cancellationToken = cts.Token;

        AuthenticationRequestMutator mutator = new(provider);

        // ACT
        await mutator.ApplyAsync(request, cancellationToken);

        // ASSERT
        await provider.Received(1).ApplyAsync(request, cancellationToken);
    }

    [Fact]
    public async Task ApplyAsync_WithMultipleCalls_CallsProviderEachTime()
    {
        // ARRANGE
        ISseAuthenticationProvider provider = Substitute.For<ISseAuthenticationProvider>();
        AuthenticationRequestMutator mutator = new(provider);

        // ACT
        await mutator.ApplyAsync(new HttpRequestMessage(), CancellationToken.None);
        await mutator.ApplyAsync(new HttpRequestMessage(), CancellationToken.None);
        await mutator.ApplyAsync(new HttpRequestMessage(), CancellationToken.None);

        // ASSERT
        await provider.Received(3).ApplyAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }
}