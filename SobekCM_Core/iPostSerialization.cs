﻿namespace SobekCM.Core
{
    /// <summary> Interface is implemented for classes that require some additional configuration to occur around serialization events </summary>
    public interface iSerializationEvents
    {
        /// <summary> Method is called by the serializer after an item is unserialized </summary>
        void PostUnSerialization();
    }
}
