﻿using Aurora.Engine.Equipment.Interfaces;

namespace Aurora.Engine.Equipment.Components
{
    public abstract class EquipmentComponentBase : IDisplayNameComponent
    {
        /// <summary>
        /// Gets the display name for this component.
        /// </summary>
        public abstract string GetDisplayName();
    }
}