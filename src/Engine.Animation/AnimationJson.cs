using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Rendering;

namespace Engine.Animation;

/// <summary>Reads and writes animation state machines as JSON (<c>.anim.json</c> files).</summary>
/// <remarks>
/// Example: <c>{ "parameters": ["Speed"], "states": [ { "name": "Run", "clip": { "fps": 12, "loop": "Loop", "grid": { "cellWidth": 16, "cellHeight": 16, "columns": 8, "cells": [2,3,4,5] } } } ], "transitions": [ { "to": "Run", "conditions": [ { "parameter": "Speed", "comparison": "Greater", "value": 10 } ] } ] }</c>
/// </remarks>
public static class AnimationJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Parses a machine from JSON text.</summary>
    /// <exception cref="InvalidOperationException">The JSON is invalid or describes an impossible machine (unknown state or parameter, no frames, bad frame rate).</exception>
    public static AnimationStateMachine Read(string json)
    {
        AnimationMachineData data;
        try
        {
            data = JsonSerializer.Deserialize<AnimationMachineData>(json, Options)
                ?? throw new InvalidOperationException("The animation file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Animation file is not valid: {ex.Message}", ex);
        }

        return Build(data);
    }

    /// <summary>Writes a machine as indented JSON. Clips are written as explicit frame rectangles.</summary>
    public static string Write(AnimationStateMachine machine) => JsonSerializer.Serialize(ToData(machine), Options);

    private static AnimationStateMachine Build(AnimationMachineData data)
    {
        var machine = new AnimationStateMachine();
        foreach (string parameter in data.Parameters) machine.AddParameter(parameter);
        foreach (var state in data.States) machine.AddState(state.Name, BuildClip(state));

        if (data.Default is not null) machine.DefaultState = machine.GetStateIndex(data.Default);

        foreach (var transition in data.Transitions)
        {
            var conditions = transition.Conditions
                .Select(c => machine.Condition(c.Parameter, c.Comparison, c.Value))
                .ToArray();
            machine.AddTransition(transition.From, transition.To, conditions, transition.RequiresFinished);
        }

        return machine;
    }

    private static AnimationClip BuildClip(AnimationStateData state)
    {
        var clip = state.Clip;
        if (clip.Fps <= 0f) throw new InvalidOperationException($"State '{state.Name}': 'fps' must be positive.");

        return (clip.Frames, clip.Grid) switch
        {
            (not null, not null) => throw new InvalidOperationException($"State '{state.Name}': give either 'frames' or 'grid', not both."),
            (null, null) => throw new InvalidOperationException($"State '{state.Name}': the clip needs 'frames' or 'grid'."),
            ({ } frames, null) => new AnimationClip(state.Name, frames, clip.Fps, clip.Loop),
            (null, { } grid) => AnimationClip.FromGrid(state.Name, grid.CellWidth, grid.CellHeight, grid.Columns, grid.Cells, clip.Fps, clip.Loop),
        };
    }

    private static AnimationMachineData ToData(AnimationStateMachine machine)
    {
        var data = new AnimationMachineData
        {
            Parameters = machine.ParameterNames.ToList(),
            Default = machine.StateCount > 0 ? machine.GetStateName(machine.DefaultState) : null,
        };

        for (int i = 0; i < machine.StateCount; i++)
        {
            var clip = machine.GetClip(i);
            data.States.Add(new AnimationStateData
            {
                Name = machine.GetStateName(i),
                Clip = new AnimationClipData { Fps = clip.FramesPerSecond, Loop = clip.LoopMode, Frames = clip.Frames.ToList() },
            });
        }

        foreach (var transition in machine.Transitions)
        {
            data.Transitions.Add(new AnimationTransitionData
            {
                From = transition.From == AnimationTransition.AnyState ? null : machine.GetStateName(transition.From),
                To = machine.GetStateName(transition.To),
                RequiresFinished = transition.RequiresFinished,
                Conditions = transition.Conditions
                    .Select(c => new AnimationConditionData
                    {
                        Parameter = machine.ParameterNames[c.Parameter],
                        Comparison = c.Comparison,
                        Value = c.Value,
                    })
                    .ToList(),
            });
        }

        return data;
    }
}
