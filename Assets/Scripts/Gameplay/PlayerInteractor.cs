using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTide
{
    /// <summary>Finds the best object in reach and uses it when E is pressed.</summary>
    [RequireComponent(typeof(Inventory))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float reach = 2.8f;
        [SerializeField] private float maxHeightDifference = 3f;

        private Inventory inventory;
        private GameHud hud;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            hud = GetComponent<GameHud>();
        }

        private void Update()
        {
            if (UiState.IsTyping)
            {
                if (hud != null)
                {
                    hud.SetPrompt(null);
                }
                return;
            }

            Interactable target = FindTarget(out string prompt);
            if (hud != null)
            {
                hud.SetPrompt(prompt);
            }

            if (target == null || !GameInput.Current.Interact)
            {
                return;
            }

            string message = target.Interact(inventory);
            if (hud != null && !string.IsNullOrEmpty(message))
            {
                hud.ShowMessage(message);
            }
        }

        private Interactable FindTarget(out string prompt)
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            prompt = null;
            Vector3 position = transform.position;

            foreach (Interactable candidate in Interactable.Active)
            {
                Vector3 offset = candidate.transform.position - position;
                if (Mathf.Abs(offset.y) > maxHeightDifference)
                {
                    continue;
                }

                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > reach)
                {
                    continue;
                }

                string candidatePrompt = candidate.GetPrompt(inventory);
                if (candidatePrompt == null)
                {
                    continue;
                }

                float facing = distance > 0.01f ? Vector3.Dot(transform.forward, offset / distance) : 1f;
                float score = distance - facing * 0.5f + candidate.PriorityPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                    prompt = candidatePrompt;
                }
            }

            return best;
        }
    }
}
