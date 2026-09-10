// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenCombatEngine.Core.Interfaces.CharacterCreation;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Loot;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.GrpcSidecar;
using OpenCombatEngine.Implementation.CharacterCreation;
using OpenCombatEngine.Implementation.Content.Mappers;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Loot;
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
        // Open5eClient owns the timeout now — per attempt, with retries and
        // backoff (see Open5eRequestPolicy) — so HttpClient itself must not
        // impose a shorter whole-fetch budget on top. An earlier single
        // 15-second HttpClient.Timeout across the ~14-page spell list threw
        // the moment two pages ran slow, stranding a cold-cache startup
        // with nothing to fall back on; the retry policy rides that out.
        using var open5eHttpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
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
        Console.Error.WriteLine(
            $"Warning: could not populate the SRD spell repository — Open5e was unreachable after retries and no local cache exists ({spellCachePath}): {ex.Message}. " +
            "Spellcasting data will be unavailable until the sidecar restarts with Open5e reachable, or OPEN5E_SPELL_CACHE_PATH points at a saved spells.json.");
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
// equipped. Cache-first, same reasoning and same shape as the spell
// repository above: this repo's own earlier RELEASE_NOTES/README already
// documented this as a known gap ("Open5e's weapon/armor/magic-item
// endpoints are small... so a startup fetch here is much less
// rate-limit-prone"), which turned out to be optimistic — a real,
// live-observed sidecar startup during this project's own later
// development still hit the 15-second HttpClient timeout fetching the
// full, paginated weapons+armor+magic-items catalog even with Open5e
// itself reachable, leaving the item library empty for that run. This
// cache (Open5eItemCache, mirroring Open5eSpellCache) closes that gap
// the same way the spell one already was closed.
var itemCachePath = Environment.GetEnvironmentVariable("OPEN5E_ITEM_CACHE_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "open5e-cache", "items.json");
var itemCacheMaxAge = TimeSpan.FromDays(7);
var itemLibrary = new StandardItemLibrary(new Open5eContentSource(new Open5eClient(new HttpClient { Timeout = Timeout.InfiniteTimeSpan }), spellDiceRoller), spellDiceRoller, spellRepository);
#pragma warning disable CA1031
try
{
    var freshItemCache = Open5eItemCache.TryLoadFresh(itemCachePath, itemCacheMaxAge);
    if (freshItemCache != null)
    {
        itemLibrary.InitializeFromDtos(freshItemCache.Weapons, freshItemCache.Armor, freshItemCache.MagicItems);
        Console.WriteLine($"Loaded {itemLibrary.GetAllItems().Count()} SRD items (weapons/armor/magic items) from local cache ({itemCachePath}, {FormatAge(Open5eItemCache.Age(itemCachePath))}) — skipped the live Open5e fetch.");
    }
    else
    {
        var itemContentSource = new Open5eContentSource(new Open5eClient(new HttpClient { Timeout = Timeout.InfiniteTimeSpan }), spellDiceRoller);
        var weaponDtos = await itemContentSource.GetAllWeaponDtosAsync();
        var armorDtos = await itemContentSource.GetAllArmorDtosAsync();
        var magicItemDtos = await itemContentSource.GetAllMagicItemDtosAsync();
        if (weaponDtos.Count == 0 && armorDtos.Count == 0 && magicItemDtos.Count == 0)
        {
            throw new InvalidOperationException("Open5e returned no items (empty or unreachable).");
        }
        itemLibrary.InitializeFromDtos(weaponDtos, armorDtos, magicItemDtos);
        Open5eItemCache.Save(itemCachePath, weaponDtos, armorDtos, magicItemDtos);
        Console.WriteLine($"Loaded {itemLibrary.GetAllItems().Count()} SRD items (weapons/armor/magic items) from Open5e (cached to {itemCachePath} for future startups).");
    }
}
catch (Exception ex)
{
    // Same "degrade rather than crash" posture as the spell repository
    // above: a stale cache is still far more useful than an empty item
    // library, so try one before giving up entirely.
    var staleItemCache = Open5eItemCache.TryLoadAny(itemCachePath);
    if (staleItemCache != null)
    {
        itemLibrary.InitializeFromDtos(staleItemCache.Weapons, staleItemCache.Armor, staleItemCache.MagicItems);
        Console.Error.WriteLine($"Warning: live Open5e item fetch failed ({ex.Message}); using stale local cache instead ({itemLibrary.GetAllItems().Count()} items, {FormatAge(Open5eItemCache.Age(itemCachePath))}).");
    }
    else
    {
        // An Attack call against a character whose weapon isn't in (or
        // wasn't loaded into) the library fails with a real, visible "No
        // weapon equipped"/unresolvable-item error rather than the
        // sidecar refusing to start at all over an Open5e outage.
        Console.Error.WriteLine(
            $"Warning: could not populate the SRD item library — Open5e was unreachable after retries and no local cache exists ({itemCachePath}): {ex.Message}. " +
            "Equipped-weapon/armor data will not resolve until the sidecar restarts with Open5e reachable, or OPEN5E_ITEM_CACHE_PATH points at a saved items.json.");
    }
}
#pragma warning restore CA1031
builder.Services.AddSingleton<IItemLibrary>(itemLibrary);

// GenerateLoot's own dependencies: StandardLootGenerator (item rolls draw
// from the same singleton item library above) and
// StandardEncounterChallengeCalculator (pure CR/XP math, no external
// dependency at all).
builder.Services.AddSingleton<ILootGenerator>(new StandardLootGenerator(itemLibrary, new StandardDiceRoller()));
builder.Services.AddSingleton<IEncounterChallengeCalculator, StandardEncounterChallengeCalculator>();

// Character creation (StartCharacterCreation/AnswerCharacterCreationPrompt,
// design doc §9.4's "roll a new character" path) needs the same real
// dice roller and the same already-populated spell repository the rest
// of this file wires up above — no new dependencies of its own.
builder.Services.AddSingleton<ICharacterCreationService, StandardCharacterCreationService>();

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
