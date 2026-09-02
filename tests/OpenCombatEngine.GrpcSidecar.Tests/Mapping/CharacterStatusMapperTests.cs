// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using FluentAssertions;
using Layforge.Protocol.SystemEngine.V1;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.GrpcSidecar.Mapping;
using NSubstitute;

namespace OpenCombatEngine.GrpcSidecar.Tests.Mapping;

/// <summary>
/// Tests for <see cref="CharacterStatusMapper"/>, which maps an
/// <see cref="IHitPoints"/> component's state to the gRPC contract's
/// <see cref="CharacterStatus"/> enum.
/// </summary>
public class CharacterStatusMapperTests
{
    private static IHitPoints MakeHitPoints(int current, bool isDead, bool isStable)
    {
        var hp = Substitute.For<IHitPoints>();
        hp.Current.Returns(current);
        hp.IsDead.Returns(isDead);
        hp.IsStable.Returns(isStable);
        return hp;
    }

    [Fact]
    public void Map_PositiveCurrentHp_ReturnsActive()
    {
        var hp = MakeHitPoints(current: 10, isDead: false, isStable: false);

        CharacterStatusMapper.Map(hp).Should().Be(CharacterStatus.Active);
    }

    [Fact]
    public void Map_ZeroHpNotStableNotDead_ReturnsDying()
    {
        var hp = MakeHitPoints(current: 0, isDead: false, isStable: false);

        CharacterStatusMapper.Map(hp).Should().Be(CharacterStatus.Dying);
    }

    [Fact]
    public void Map_ZeroHpStable_ReturnsUnconscious()
    {
        var hp = MakeHitPoints(current: 0, isDead: false, isStable: true);

        CharacterStatusMapper.Map(hp).Should().Be(CharacterStatus.Unconscious);
    }

    [Fact]
    public void Map_IsDead_ReturnsDeadRegardlessOfOtherFlags()
    {
        var hp = MakeHitPoints(current: 0, isDead: true, isStable: false);

        CharacterStatusMapper.Map(hp).Should().Be(CharacterStatus.Dead);
    }
}
