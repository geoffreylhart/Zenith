﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Zenith.Helpers
{
    public static class KeyboardStateHelper
    {
        private static Dictionary<Keys, bool> oldStates = new Dictionary<Keys, bool>();
        internal static bool WasKeyPressed(this KeyboardState state, Keys key)
        {
            bool answer = state.IsKeyDown(key) && oldStates[key];
            oldStates[key] = state.IsKeyDown(key);
            return answer;
        }

        internal static void AffectNumber(this KeyboardState state, ref double number, Keys decreaseKey, Keys increaseKey, Keys decreaseKey2, Keys increaseKey2, double amount)
        {
            if (state.IsKeyDown(decreaseKey) || state.IsKeyDown(decreaseKey2))
            {
                number -= amount;
            }
            if (state.IsKeyDown(increaseKey) || state.IsKeyDown(increaseKey2))
            {
                number += amount;
            }
        }

        internal static void AffectNumber(this KeyboardState state, ref double number, Keys decreaseKey, Keys increaseKey, double amount)
        {
            if (state.IsKeyDown(decreaseKey))
            {
                number -= amount;
            }
            if (state.IsKeyDown(increaseKey))
            {
                number += amount;
            }
        }

        internal static void AffectNumber(this KeyboardState state, ref double number, Keys decreaseKey, Keys increaseKey, double amount, double min, double max)
        {
            if (state.IsKeyDown(decreaseKey))
            {
                number -= amount;
            }
            if (state.IsKeyDown(increaseKey))
            {
                number += amount;
            }
            number = Math.Min(Math.Max(number, min), max);
        }
    }
}
