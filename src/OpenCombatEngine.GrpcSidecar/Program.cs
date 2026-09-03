// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.GrpcSidecar;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Open5e;
using OpenCombatEngine.Implementation.Spells;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

// Populate the SRD spell repository once, synchronously, before the gRPC
// server starts accepting requests — every RPC that reconstructs a
// StandardCreature (ActorMapping.ToCreature, this file's own FromJson call)
// needs a real, already-populated ISpellRepository for spellcasting state
// to survive that round trip at all (see ActorMapping.ToCreature's
// remarks). Deliberately not a background/hosted-service warm-up: that
// would leave a window where an early request for a spellcaster silently
// got an empty repository instead of a complete one — the whole point
// here is to stop that kind of silent gap, not introduce a new one.
var spellRepository = new InMemorySpellRepository();
#pragma warning disable CA1031
try
{
    using var open5eHttpClient = new HttpClient();
    var open5eClient = new Open5eClient(open5eHttpClient);
    var open5eContentSource = new Open5eContentSource(open5eClient, new StandardDiceRoller());
    var spellCount = 0;
    foreach (var spell in await open5eContentSource.GetAllSpellsAsync())
    {
        spellRepository.AddSpell(spell);
        spellCount++;
    }
    Console.WriteLine($"Loaded {spellCount} SRD spells from Open5e.");
}
catch (Exception ex)
{
    // Open5e unreachable, or some other unexpected failure, at startup —
    // not fatal, same "degrade rather than crash" posture Master itself
    // uses for its own optional startup dependencies. The sidecar starts
    // with whatever was fetched before the failure (possibly nothing);
    // spellcasting for a creature referencing a spell that never got
    // loaded is simply dropped on restore, not an error
    // (StandardSpellCaster's own documented behavior).
    Console.Error.WriteLine($"Warning: failed to populate spell repository from Open5e at startup: {ex.Message}");
}
#pragma warning restore CA1031
builder.Services.AddSingleton<ISpellRepository>(spellRepository);

// Also needed for constructor injection into SystemEngineGrpcService's
// CastSpell handler (CastSpellAction rolls real dice for a spell's
// damage/healing, same as any other mechanical resolution in this
// stack) — a fresh instance per resolution the same way ResolveCheck's
// own dice rolling already works, not shared/stateful.
builder.Services.AddSingleton<IDiceRoller, StandardDiceRoller>();

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
