using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaptainPinkTurd.Core.DesignPattern.Singleton
{
    /// <summary>
    /// Each closed Singleton/PersistentSingleton/RegulatorSingleton registers a reset
    /// callback here via its static constructor. With "Enter Play Mode Settings" set to skip
    /// domain/scene reload, static "instance" fields and their GameObjects would otherwise
    /// survive from one Play session into the next, so we clear them ourselves.
    /// </summary>
    internal static class SingletonResetRegistry
    {
        private static readonly List<Action> resetActions = new();

        internal static void Register(Action resetAction)
        {
            resetActions.Add(resetAction);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetAll()
        {
            foreach (var resetAction in resetActions)
            {
                resetAction.Invoke();
            }
        }
    }
}
