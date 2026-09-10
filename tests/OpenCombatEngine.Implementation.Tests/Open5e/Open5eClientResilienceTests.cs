// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OpenCombatEngine.Implementation.Open5e;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Open5e
{
    /// <summary>
    /// Covers <see cref="Open5eClient"/>'s retry/timeout behavior — the fix
    /// for a cold-cache gRPC sidecar startup throwing the moment Open5e's
    /// front end ran slow (one 15-second HttpClient budget spread across a
    /// ~14-page paginated fetch). Uses a scripted <see cref="HttpMessageHandler"/>
    /// rather than the real API.
    /// </summary>
    public class Open5eClientResilienceTests
    {
        private static Open5eRequestPolicy FastPolicy(int maxAttempts = 4) => new()
        {
            MaxAttempts = maxAttempts,
            PerAttemptTimeout = TimeSpan.FromMilliseconds(200),
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
        };

        private static HttpResponseMessage Json(string body) =>
            new(HttpStatusCode.OK) { Content = new StringContent(body) };

        private static HttpResponseMessage Status(HttpStatusCode code) => new(code);

        private const string OneSpellPage =
            "{\"count\":1,\"next\":null,\"results\":[{\"name\":\"Light\",\"level_int\":0,\"school\":\"evocation\"}]}";

        [Fact]
        public async Task GetSpellsAsync_TransientServerErrorsThenSuccess_Retries()
        {
            var handler = new ScriptedHandler(
                Status(HttpStatusCode.ServiceUnavailable),
                Status(HttpStatusCode.BadGateway),
                Json(OneSpellPage));
            var client = new Open5eClient(new HttpClient(handler), FastPolicy());

            var page = await client.GetSpellsAsync(1);

            page.Should().NotBeNull();
            page!.Results.Should().ContainSingle(s => s.Name == "Light");
            handler.CallCount.Should().Be(3);
        }

        [Fact]
        public async Task GetSpellsAsync_RetriableFailureEveryAttempt_ThrowsOpen5eUnavailable()
        {
            var handler = new ScriptedHandler(
                Status(HttpStatusCode.ServiceUnavailable),
                Status(HttpStatusCode.ServiceUnavailable),
                Status(HttpStatusCode.ServiceUnavailable),
                Status(HttpStatusCode.ServiceUnavailable));
            var client = new Open5eClient(new HttpClient(handler), FastPolicy(maxAttempts: 4));

            var act = () => client.GetSpellsAsync(1);

            await act.Should().ThrowAsync<Open5eUnavailableException>();
            handler.CallCount.Should().Be(4);
        }

        [Fact]
        public async Task GetSpellsAsync_PerAttemptTimeoutThenSuccess_RetriesInsteadOfThrowing()
        {
            var handler = new ScriptedHandler(
                async ct => { await Task.Delay(TimeSpan.FromSeconds(5), ct); return Json("never"); },
                _ => Task.FromResult(Json(OneSpellPage)));
            var client = new Open5eClient(new HttpClient(handler), FastPolicy());

            var page = await client.GetSpellsAsync(1);

            page.Should().NotBeNull();
            handler.CallCount.Should().Be(2);
        }

        [Fact]
        public async Task GetSpellsAsync_NotFound_ReturnsNullWithoutRetrying()
        {
            var handler = new ScriptedHandler(Status(HttpStatusCode.NotFound));
            var client = new Open5eClient(new HttpClient(handler), FastPolicy());

            var page = await client.GetSpellsAsync(999);

            page.Should().BeNull();
            handler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task GetSpellAsync_ServiceDown_ReturnsNullNotThrow()
        {
            var handler = new ScriptedHandler(
                Status(HttpStatusCode.ServiceUnavailable),
                Status(HttpStatusCode.ServiceUnavailable));
            var client = new Open5eClient(new HttpClient(handler), FastPolicy(maxAttempts: 2));

            var spell = await client.GetSpellAsync("fireball");

            spell.Should().BeNull();
        }

        [Fact]
        public async Task GetSpellsAsync_TooManyRequests_IsRetried()
        {
            var handler = new ScriptedHandler(
                Status(HttpStatusCode.TooManyRequests),
                Json(OneSpellPage));
            var client = new Open5eClient(new HttpClient(handler), FastPolicy());

            var page = await client.GetSpellsAsync(1);

            page.Should().NotBeNull();
            handler.CallCount.Should().Be(2);
        }

        /// <summary>
        /// An <see cref="HttpMessageHandler"/> that returns a scripted
        /// sequence of responses (or runs scripted delegates), one per call,
        /// repeating the last entry once the script runs out.
        /// </summary>
        private sealed class ScriptedHandler : HttpMessageHandler
        {
            private readonly List<Func<CancellationToken, Task<HttpResponseMessage>>> _steps;
            private int _index = -1;

            public ScriptedHandler(params HttpResponseMessage[] responses)
            {
                _steps = new List<Func<CancellationToken, Task<HttpResponseMessage>>>();
                foreach (var r in responses)
                {
                    var captured = r;
                    _steps.Add(_ => Task.FromResult(captured));
                }
            }

            public ScriptedHandler(params Func<CancellationToken, Task<HttpResponseMessage>>[] steps)
            {
                _steps = new List<Func<CancellationToken, Task<HttpResponseMessage>>>(steps);
            }

            public int CallCount { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                _index = Math.Min(_index + 1, _steps.Count - 1);
                return await _steps[_index](cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
