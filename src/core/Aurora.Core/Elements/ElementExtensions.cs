﻿using System.Diagnostics.CodeAnalysis;
using Aurora.Core.Abstractions;

namespace Aurora.Core;

public static class ElementExtensions
{
    public static IElement AddComponent<T>(this IElement element, T component) where T : IElementComponent
    {
        element.Components.AddComponent(component);

        return element;
    }

    public static bool TryGetComponent<T>(this IElement element, [NotNullWhen(true)] out T? component) where T : IElementComponent
    {
        component = element.Components.GetComponent<T>();
        return component is not null;
    }
}