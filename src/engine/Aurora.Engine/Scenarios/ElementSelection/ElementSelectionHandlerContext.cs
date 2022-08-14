﻿using Aurora.Engine.Elements.Rules;

namespace Aurora.Engine.Scenarios.ElementSelection;

public class ElementSelectionHandlerContext
{
    private ElementSelectionHandlerContext(string identifier, SelectionRule selectionRule)
    {
        Identifier = identifier;
        SelectionRule = selectionRule;
    }

    /// <summary>
    /// Gets a unique identifier for the selection handler associated with this context.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the selection rule associated with this context.
    /// </summary>
    public SelectionRule SelectionRule { get; }

    /// <summary>
    /// Create a new selection handler context with a generated identifier.
    /// </summary>
    /// <param name="selectionRule">The selection rule associated with this context.</param>
    /// <returns>A new context.</returns>
    public static ElementSelectionHandlerContext Create(SelectionRule selectionRule)
    {
        // TODO: helper utilities for creating simple guid string to align format
        return new ElementSelectionHandlerContext(Guid.NewGuid().ToString(), selectionRule);
    }
}