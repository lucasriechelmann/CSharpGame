using Engine.Animation;
using Engine.Rendering;

namespace Engine.Animation.Tests;

public class AnimationJsonTests
{
    private const string HeroLike = """
        {
          "parameters": ["Speed", "Grounded"],
          "default": "Idle",
          "states": [
            { "name": "Idle", "clip": { "fps": 2, "loop": "PingPong", "grid": { "cellWidth": 16, "cellHeight": 16, "columns": 8, "cells": [0, 1] } } },
            { "name": "Run",  "clip": { "fps": 12, "grid": { "cellWidth": 16, "cellHeight": 16, "columns": 8, "cells": [2, 3, 4, 5] } } },
            { "name": "Hit",  "clip": { "fps": 8, "loop": "Once", "frames": [ { "x": 0, "y": 16, "width": 16, "height": 16 } ] } }
          ],
          "transitions": [
            { "to": "Run", "conditions": [ { "parameter": "Speed", "comparison": "Greater", "value": 10 } ] },
            { "from": "Run", "to": "Idle", "conditions": [ { "parameter": "Speed", "comparison": "LessOrEqual", "value": 10 } ] },
            { "from": "Hit", "to": "Idle", "requiresFinished": true }
          ]
        }
        """;

    [Fact]
    public void Reads_parameters_states_and_clips()
    {
        var machine = AnimationJson.Read(HeroLike);

        Assert.Equal(new[] { "Speed", "Grounded" }, machine.ParameterNames.ToArray());
        Assert.Equal(3, machine.StateCount);
        Assert.Equal("Idle", machine.GetStateName(machine.DefaultState));

        var run = machine.GetClip(machine.GetStateIndex("Run"));
        Assert.Equal(12f, run.FramesPerSecond);
        Assert.Equal(AnimationLoopMode.Loop, run.LoopMode); // default when omitted
        Assert.Equal(new Rect(32, 0, 16, 16), run.Frames[0]);
        Assert.Equal(new Rect(80, 0, 16, 16), run.Frames[3]);

        var idle = machine.GetClip(machine.GetStateIndex("Idle"));
        Assert.Equal(AnimationLoopMode.PingPong, idle.LoopMode);

        var hit = machine.GetClip(machine.GetStateIndex("Hit"));
        Assert.Equal(AnimationLoopMode.Once, hit.LoopMode);
        Assert.Equal(new Rect(0, 16, 16, 16), hit.Frames[0]);
    }

    [Fact]
    public void Transitions_behave_as_described_in_the_file()
    {
        var machine = AnimationJson.Read(HeroLike);
        int idle = machine.GetStateIndex("Idle");
        int run = machine.GetStateIndex("Run");
        int hit = machine.GetStateIndex("Hit");

        Assert.Equal(run, machine.FindTransition(idle, new float[] { 50f, 1f }, false));   // any-state rule
        Assert.Equal(-1, machine.FindTransition(idle, new float[] { 5f, 1f }, false));
        Assert.Equal(idle, machine.FindTransition(run, new float[] { 5f, 1f }, false));
        Assert.Equal(-1, machine.FindTransition(hit, new float[] { 0f, 1f }, false));      // waits for the clip
        Assert.Equal(idle, machine.FindTransition(hit, new float[] { 0f, 1f }, true));
    }

    [Fact]
    public void The_first_state_is_the_default_when_none_is_named()
    {
        var machine = AnimationJson.Read("""
            { "states": [
                { "name": "A", "clip": { "fps": 4, "frames": [ { "x": 0, "y": 0, "width": 1, "height": 1 } ] } },
                { "name": "B", "clip": { "fps": 4, "frames": [ { "x": 0, "y": 0, "width": 1, "height": 1 } ] } } ] }
            """);

        Assert.Equal(0, machine.DefaultState);
    }

    [Fact]
    public void A_machine_survives_a_write_and_read()
    {
        var original = AnimationJson.Read(HeroLike);

        var copy = AnimationJson.Read(AnimationJson.Write(original));

        Assert.Equal(original.ParameterNames, copy.ParameterNames);
        Assert.Equal(original.StateCount, copy.StateCount);
        Assert.Equal(original.DefaultState, copy.DefaultState);
        for (int i = 0; i < original.StateCount; i++)
        {
            Assert.Equal(original.GetStateName(i), copy.GetStateName(i));
            Assert.Equal(original.GetClip(i).Frames, copy.GetClip(i).Frames);
            Assert.Equal(original.GetClip(i).FramesPerSecond, copy.GetClip(i).FramesPerSecond);
            Assert.Equal(original.GetClip(i).LoopMode, copy.GetClip(i).LoopMode);
        }

        Assert.Equal(original.Transitions.Count, copy.Transitions.Count);
        foreach (float speed in new[] { 0f, 5f, 50f })
        {
            for (int state = 0; state < original.StateCount; state++)
            {
                var parameters = new[] { speed, 1f };
                foreach (bool finished in new[] { false, true })
                    Assert.Equal(original.FindTransition(state, parameters, finished), copy.FindTransition(state, parameters, finished));
            }
        }
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "states": [ { "name": "A", "clip": { "fps": 0, "frames": [ { "x": 0, "y": 0, "width": 1, "height": 1 } ] } } ] }""")]  // fps
    [InlineData("""{ "states": [ { "name": "A", "clip": { "fps": 4 } } ] }""")]                                                               // no frames
    [InlineData("""{ "states": [ { "name": "A", "clip": { "fps": 4, "frames": [ { "x": 0, "y": 0, "width": 1, "height": 1 } ], "grid": { "cellWidth": 1, "cellHeight": 1, "columns": 1, "cells": [0] } } } ] }""")] // both
    [InlineData("""{ "states": [ { "name": "A", "clip": { "fps": 4, "loop": "Sideways", "frames": [ { "x": 0, "y": 0, "width": 1, "height": 1 } ] } } ] }""")] // bad enum
    public void Invalid_files_are_rejected_with_InvalidOperationException(string json)
    {
        Assert.Throws<InvalidOperationException>(() => AnimationJson.Read(json));
    }

    [Fact]
    public void References_to_unknown_states_and_parameters_fail_loudly()
    {
        const string oneState = """{ "name": "A", "clip": { "fps": 4, "frames": [ { "x": 0, "y": 0, "width": 1, "height": 1 } ] } }""";

        Assert.Throws<KeyNotFoundException>(() => AnimationJson.Read(
            $$"""{ "states": [ {{oneState}} ], "transitions": [ { "to": "Nope" } ] }"""));
        Assert.Throws<KeyNotFoundException>(() => AnimationJson.Read(
            $$"""{ "states": [ {{oneState}} ], "default": "Nope" }"""));
        Assert.Throws<KeyNotFoundException>(() => AnimationJson.Read(
            $$"""{ "states": [ {{oneState}} ], "transitions": [ { "to": "A", "conditions": [ { "parameter": "Ghost", "comparison": "Equal", "value": 1 } ] } ] }"""));
    }
}
