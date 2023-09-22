using LSLib.LS.Story.GoalParser;
using System;
using System.Collections.Generic;

namespace LSLib.LS.Stats.StatParser;

/// <summary>
/// List of stat properties
/// </summary>
public class StatDeclaration
{
    public CodeLocation Location;
    public readonly Dictionary<String, object> Properties = new();
    public readonly Dictionary<String, CodeLocation> PropertyLocations = new();
}

/// <summary>
/// A string property of a stat entry (Key/value pair)
/// </summary>
public class StatProperty
{
    public CodeLocation Location;
    public String Key;
    public object Value;
}

/// <summary>
/// An element of collection of a stat entry (Key/value pair)
/// </summary>
public class StatElement
{
    public String Collection;
    public object Value;
}