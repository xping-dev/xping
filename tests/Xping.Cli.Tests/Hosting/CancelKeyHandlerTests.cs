/*
 * © 2026 Xping.io. All Rights Reserved.
 * License: [MIT]
 */

using Xping.Cli.Hosting;

namespace Xping.Cli.Tests.Hosting;

public sealed class CancelKeyHandlerTests : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly CancelKeyHandler _handler;

    public CancelKeyHandlerTests()
    {
        _handler = new CancelKeyHandler(_cancellation);
    }

    public void Dispose() => _cancellation.Dispose();

    // leave Ctrl+C doing nothing, the clear prompt included.
    [Fact]
    public void CtrlCEndsTheProcessWhenNoCooperativeCommandRuns()
    {
        Assert.False(_handler.OnCancelKeyPress());
        Assert.False(_handler.Token.IsCancellationRequested);
    }

    [Fact]
    public void CtrlCCancelsACooperativeCommandAndKeepsTheProcessAlive()
    {
        using (_handler.EnterCooperative())
        {
            Assert.True(_handler.OnCancelKeyPress());
            Assert.True(_handler.Token.IsCancellationRequested);
        }
    }

    [Fact]
    public void ASecondCtrlCEndsTheProcessWhenTheCommandDidNotStop()
    {
        using (_handler.EnterCooperative())
        {
            Assert.True(_handler.OnCancelKeyPress());
            Assert.False(_handler.OnCancelKeyPress());
        }
    }

    [Fact]
    public void CtrlCEndsTheProcessAgainOnceTheCooperativeCommandFinished()
    {
        IDisposable cooperative = _handler.EnterCooperative();
        cooperative.Dispose();
        cooperative.Dispose();

        Assert.False(_handler.OnCancelKeyPress());
        Assert.False(_handler.Token.IsCancellationRequested);
    }

}
