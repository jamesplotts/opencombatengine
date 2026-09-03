// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.GrpcSidecar;
using OpenCombatEngine.Implementation.Content.Mappers;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
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
//
// Cache-first, not fetch-then-cache-as-a-fallback: a repeated local
// restart (or a CI/test run) re-fetching ~1400 SRD spells from Open5e
// every single time is real, observed, unnecessary load — enough of it
// against Open5e's own Cloudflare front can (and did, during this
// project's own development) get this environment rate-limited or
// blocked outright, leaving the sidecar unable to start at all. SRD
// spell content is effectively static, so a week-old cache is exactly
// as good as a fresh fetch for this engine's purposes — see
// Open5eSpellCache's own doc comment for why age-based staleness is
// preferred over a conditional-request scheme.
var spellRepository = new InMemorySpellRepository();
var spellCachePath = Environment.GetEnvironmentVariable("OPEN5E_SPELL_CACHE_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "open5e-cache", "spells.json");
var spellCacheMaxAge = TimeSpan.FromDays(7);
var spellDiceRoller = new StandardDiceRoller();

static string FormatAge(TimeSpan? age) => age is { } a ? $"{(int)a.TotalDays}d {a.Hours}h old" : "unknown age";

#pragma warning disable CA1031
try
{
    var freshCached = Open5eSpellCache.TryLoadFresh(spellCachePath, spellCacheMaxAge);
    if (freshCached != null)
    {
        foreach (var dto in freshCached)
        {
            spellRepository.AddSpell(SpellMapper.Map(dto, spellDiceRoller));
        }
        Console.WriteLine($"Loaded {freshCached.Count} SRD spells from local cache ({spellCachePath}, {FormatAge(Open5eSpellCache.Age(spellCachePath))}) — skipped the live Open5e fetch.");
    }
    else
    {
        // Bounded well under HttpClient's 100-second default: a startup
        // path should fail fast into the cache fallback below, not leave
        // an operator staring at an unresponsive process for a minute
        // and a half before finding out Open5e is unreachable.
        using var open5eHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var open5eClient = new Open5eClient(open5eHttpClient);
        var open5eContentSource = new Open5eContentSource(open5eClient, spellDiceRoller);
        var dtos = await open5eContentSource.GetAllSpellDtosAsync();
        if (dtos.Count == 0)
        {
            throw new InvalidOperationException("Open5e returned no spells (empty or unreachable).");
        }
        foreach (var dto in dtos)
        {
            spellRepository.AddSpell(SpellMapper.Map(dto, spellDiceRoller));
        }
        Open5eSpellCache.Save(spellCachePath, dtos);
        Console.WriteLine($"Loaded {dtos.Count} SRD spells from Open5e (cached to {spellCachePath} for future startups).");
    }
}
catch (Exception ex)
{
    // Live fetch failed (or no cache was fresh enough to skip it) — a
    // stale cache is still far more useful than an empty spell
    // repository, so try one before giving up entirely. Same
    // "degrade rather than crash" posture Master itself uses for its
    // own optional startup dependencies either way.
    var staleCached = Open5eSpellCache.TryLoadAny(spellCachePath);
    if (staleCached != null)
    {
        foreach (var dto in staleCached)
        {
            spellRepository.AddSpell(SpellMapper.Map(dto, spellDiceRoller));
        }
        Console.Error.WriteLine($"Warning: live Open5e fetch failed ({ex.Message}); using stale local cache instead ({staleCached.Count} spells, {FormatAge(Open5eSpellCache.Age(spellCachePath))}).");
    }
    else
    {
        // spellcasting for a creature referencing a spell that never got
        // loaded is simply dropped on restore, not an error
        // (StandardSpellCaster's own documented behavior).
        Console.Error.WriteLine($"Warning: failed to populate spell repository from Open5e at startup, and no local cache exists: {ex.Message}");
    }
}
#pragma warning restore CA1031
builder.Services.AddSingleton<ISpellRepository>(spellRepository);

// Also needed for constructor injection into SystemEngineGrpcService's
// CastSpell handler (CastSpellAction rolls real dice for a spell's
// damage/healing, same as any other mechanical resolution in this
// stack) — a fresh instance per resolution the same way ResolveCheck's
// own dice rolling already works, not shared/stateful.
builder.Services.AddSingleton<IDiceRoller, StandardDiceRoller>();

// Populate a real IItemLibrary the same way the spell repository above is
// populated — without one, ActorMapping.ToCreature has no way to resolve
// an inventory/equipped item's real stats back from its stored name (see
// StandardCreature.ResolveItem's own remarks), so equipped-weapon data
// (needed for the Attack RPC — melee_attack/ranged_attack) would silently
// come back null on every round trip regardless of what was actually
// equipped. Unlike the spell repository, this has no local on-disk cache
// of its own yet: Open5e's weapon/armor/magic-item endpoints are small
// (SRD 5.1 has a few dozen weapons/armor pieces; magic items are the
// largest of the three but still nowhere near the ~1400-spell catalog),
// so a startup fetch here is a much smaller, much less rate-limit-prone
// operation than the spell one this cache exists for. If that changes
// (a much larger third-party item catalog, or the same rate-limiting
// this environment already hit twice for spells), an Open5eItemCache
// mirroring Open5eSpellCache would be the same fix applied here.
var itemLibrary = new StandardItemLibrary(new Open5eContentSource(new Open5eClient(new HttpClient { Timeout = TimeSpan.FromSeconds(15) }), spellDiceRoller), spellDiceRoller, spellRepository);
#pragma warning disable CA1031
try
{
    await itemLibrary.InitializeAsync();
    Console.WriteLine($"Loaded {itemLibrary.GetAllItems().Count()} SRD items (weapons/armor/magic items) from Open5e.");
}
catch (Exception ex)
{
    // Same "degrade rather than crash" posture as the spell repository
    // above: an Attack call against a character whose weapon isn't in
    // (or wasn't loaded into) the library fails with a real, visible
    // "No weapon equipped"/unresolvable-item error rather than the
    // sidecar refusing to start at all over an Open5e outage.
    Console.Error.WriteLine($"Warning: failed to populate item library from Open5e at startup: {ex.Message}. Equipped-weapon data will not resolve until this is fixed.");
}
#pragma warning restore CA1031
builder.Services.AddSingleton<IItemLibrary>(itemLibrary);

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
