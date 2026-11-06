using System.Collections.Generic;
using UnityEngine;

namespace LastTide
{
    /// <summary>Anything the player can use by walking up to it and pressing E.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        private static readonly List<Interactable> ActiveList = new List<Interactable>();

        public static IReadOnlyList<Interactable> Active => ActiveList;

        /// <summary>Added to the distance when choosing a target, so loose items win over nearby trees.</summary>
        public virtual float PriorityPenalty => 0f;

        /// <summary>The text to show, or null when this object shouldn't offer anything right now.</summary>
        public abstract string GetPrompt(Inventory inventory);

        /// <summary>Performs the action and returns a short message for the player.</summary>
        public abstract string Interact(Inventory inventory);

        protected virtual void OnEnable()
        {
            ActiveList.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActiveList.Remove(this);
        }
    }
}
