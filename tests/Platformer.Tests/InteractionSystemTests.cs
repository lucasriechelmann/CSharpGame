using System.Numerics;
using Engine.Core.Components;
using Engine.Physics2D;
using Platformer.Tests.Fixtures;

namespace Platformer.Tests;

public class InteractionSystemTests
{
    [Fact]
    public void Touching_a_coin_collects_and_removes_it()
    {
        var w = new InteractionWorld(2);
        var coin = w.AddCoin(5f, 0f, value: 50);

        w.Step();

        Assert.False(w.World.IsAlive(coin));
        Assert.Equal(1, w.Session.CoinsCollected);
        Assert.Equal(50, w.Session.Score);
    }

    [Fact]
    public void A_distant_coin_is_left_alone()
    {
        var w = new InteractionWorld(1);
        var coin = w.AddCoin(200f, 0f);

        w.Step();

        Assert.True(w.World.IsAlive(coin));
        Assert.Equal(0, w.Session.CoinsCollected);
    }

    [Fact]
    public void Several_overlapping_coins_are_all_collected_in_one_step()
    {
        var w = new InteractionWorld(3);
        w.AddCoin(0f, 0f);
        w.AddCoin(4f, 2f);
        w.AddCoin(-4f, -2f);

        w.Step();

        Assert.Equal(3, w.Session.CoinsCollected);
        Assert.True(w.Session.GoalUnlocked);
    }

    [Fact]
    public void Landing_on_a_creature_from_above_stomps_it_and_bounces_the_hero()
    {
        var w = new InteractionWorld(1);
        var creature = w.AddCreature(0f, -20f); // box spans y -30..-10; hero box spans y -17.5..12.5, feet above the creature's middle (-20)
        w.SetHeroPosition(0f, 0f);
        w.SetHeroVelocity(0f, -300f);

        w.Step();

        Assert.False(w.World.IsAlive(creature));
        Assert.Equal(GameSession.StompPoints, w.Session.Score);
        Assert.Equal(GameSession.StartingLives, w.Session.Lives);
        Assert.True(w.World.Get<PhysicsBody>(w.Hero).Velocity.Y > 0f);
    }

    [Fact]
    public void Touching_a_creature_from_the_side_hurts_and_sends_the_hero_back_to_the_start()
    {
        var w = new InteractionWorld(1);
        w.Session.SetStart(new Vector2(-300f, 40f));
        var creature = w.AddCreature(15f, 0f); // to the hero's right
        w.SetHeroVelocity(120f, -50f);

        w.Step();

        Assert.True(w.World.IsAlive(creature));
        Assert.Equal(GameSession.StartingLives - 1, w.Session.Lives);
        var position = w.World.Get<Transform>(w.Hero).Position;
        Assert.Equal(-300f, position.X);
        Assert.Equal(40f, position.Y);
        Assert.Equal(Vector2.Zero, w.World.Get<PhysicsBody>(w.Hero).Velocity);
    }

    [Fact]
    public void Respawning_keeps_the_heros_draw_layer()
    {
        var w = new InteractionWorld(1);
        w.World.Get<Transform>(w.Hero).Position.Z = 10f;
        w.AddCreature(15f, 0f);

        w.Step();

        Assert.Equal(10f, w.World.Get<Transform>(w.Hero).Position.Z);
    }

    [Fact]
    public void Respawning_clears_the_render_history_so_the_hero_is_not_smeared_across_the_level()
    {
        var w = new InteractionWorld(1);
        w.World.Add(w.Hero, PreviousTransform.From(Transform.At(5f, 5f)));
        w.Session.SetStart(new Vector2(-300f, 40f));
        w.AddCreature(15f, 0f);

        w.Step();

        Assert.False(w.World.Get<PreviousTransform>(w.Hero).HasHistory);
    }

    [Fact]
    public void A_hit_that_loses_the_run_does_not_move_the_hero()
    {
        var w = new InteractionWorld(1);
        w.Session.SetStart(new Vector2(-300f, 40f));
        for (int i = 0; i < GameSession.StartingLives - 1; i++)
        {
            w.Session.TryHurt();
            w.Session.Tick(GameSession.HitInvulnerability);
        }

        w.AddCreature(10f, 0f);
        w.Step();

        Assert.Equal(GamePhase.Lost, w.Session.Phase);
        Assert.Equal(0f, w.World.Get<Transform>(w.Hero).Position.X);
    }

    [Fact]
    public void Touching_a_checkpoint_raises_it_and_makes_it_the_respawn_point()
    {
        var w = new InteractionWorld(1);
        var flag = w.AddCheckpoint(10f, 5f);

        w.Step();

        Assert.True(w.World.Get<Checkpoint>(flag).Reached);
        Assert.Equal(new Vector2(10f, 5f), w.Session.RespawnPoint);
    }

    [Fact]
    public void A_distant_checkpoint_is_left_alone()
    {
        var w = new InteractionWorld(1);
        var flag = w.AddCheckpoint(300f, 0f);

        w.Step();

        Assert.False(w.World.Get<Checkpoint>(flag).Reached);
        Assert.Equal(Vector2.Zero, w.Session.RespawnPoint);
    }

    [Fact]
    public void After_a_checkpoint_a_hit_respawns_the_hero_there()
    {
        var w = new InteractionWorld(1);
        w.AddCheckpoint(10f, 5f);
        w.Step(); // reach it
        w.SetHeroPosition(200f, 0f);
        w.AddCreature(215f, 0f);

        w.Step();

        var position = w.World.Get<Transform>(w.Hero).Position;
        Assert.Equal(10f, position.X);
        Assert.Equal(5f, position.Y);
    }

    [Fact]
    public void Walking_into_a_creature_while_not_falling_is_a_hit_even_if_higher_up()
    {
        var w = new InteractionWorld(1);
        w.AddCreature(0f, -20f);
        w.SetHeroVelocity(0f, 50f); // rising: not a stomp

        w.Step();

        Assert.Equal(GameSession.StartingLives - 1, w.Session.Lives);
    }

    [Fact]
    public void Touching_a_creature_during_invulnerability_costs_nothing()
    {
        var w = new InteractionWorld(1);
        w.Session.TryHurt(); // now invulnerable
        int lives = w.Session.Lives;
        w.AddCreature(10f, 0f);

        w.Step();

        Assert.Equal(lives, w.Session.Lives);
    }

    [Fact]
    public void The_run_is_lost_when_a_hit_takes_the_last_life()
    {
        var w = new InteractionWorld(1);
        for (int i = 0; i < GameSession.StartingLives - 1; i++)
        {
            w.Session.TryHurt();
            w.Session.Tick(GameSession.HitInvulnerability);
        }

        w.AddCreature(10f, 0f);
        w.Step();

        Assert.Equal(GamePhase.Lost, w.Session.Phase);
    }

    [Fact]
    public void The_locked_exit_does_not_end_the_run()
    {
        var w = new InteractionWorld(1);
        w.AddGoal(0f, 0f);

        w.Step();

        Assert.Equal(GamePhase.Playing, w.Session.Phase);
    }

    [Fact]
    public void The_unlocked_exit_wins_the_run()
    {
        var w = new InteractionWorld(1);
        w.AddCoin(0f, 0f);
        w.AddGoal(0f, 0f);

        w.Step();

        Assert.Equal(GamePhase.Won, w.Session.Phase);
    }

    [Fact]
    public void Nothing_is_processed_after_the_run_ends()
    {
        var w = new InteractionWorld(0);
        w.AddGoal(0f, 0f);
        w.Step(); // wins immediately: no coins required
        var coin = w.AddCoin(0f, 0f);

        w.Step();

        Assert.True(w.World.IsAlive(coin));
        Assert.Equal(0, w.Session.CoinsCollected);
    }

    [Fact]
    public void A_missing_hero_is_tolerated()
    {
        var w = new InteractionWorld(1);
        w.World.DestroyEntity(w.Hero);
        w.AddCoin(0f, 0f);

        w.Step(); // must not throw

        Assert.Equal(0, w.Session.CoinsCollected);
    }
}
