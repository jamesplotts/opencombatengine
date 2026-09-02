// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenCombatEngine.GrpcSidecar;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

// Sidecars talk gRPC in-process/loopback only (docs/design.md §6.1 — no
// public-facing listener), so this runs cleartext HTTP/2 (h2c) rather than
// TLS. Kestrel does not negotiate h2c on its default endpoint, so the port
// and protocol are configured explicitly here rather than left to
// ASP.NET Core's usual appsettings/launchSettings.json convention.
var port = int.TryParse(Environment.GetEnvironmentVariable("GRPC_SIDECAR_PORT"), out var p) ? p : 5265;
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port, o => o.Protocols = HttpProtocols.Http2);
});

var app = builder.Build();
app.MapGrpcService<SystemEngineGrpcService>();
app.MapGet("/", () => "OpenCombatEngine gRPC sidecar is running. Connect over gRPC, not HTTP GET.");

app.Run();
