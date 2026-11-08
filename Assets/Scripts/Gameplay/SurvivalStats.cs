using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTide
{
    /// <summary>Hunger and energy. Running out sends the player back to camp rather than ending the game.</summary>
    [RequireComponent(typeof(Inventory), typeof(PlayerController))]
    public class SurvivalStats : MonoBehaviour
    {
        private const float MaxValue = 100f;

        [SerializeField] private DayNightCycle cycle;
        [SerializeField] private float startingHunger = 60f;
        [SerializeField] private float hungerPerMinute = 9f;
        [SerializeField] private float energyPerMinuteIdle = 2f;
        [SerializeField] private float energyPerMinuteWalking = 6f;
        [SerializeField] private float energyPerMinuteSprinting = 22f;
        [SerializeField] private float coconutFood = 30f;
        [SerializeField] private float minimumSprintEnergy = 10f;

        private Inventory inventory;
        private PlayerController player;
        private GameHud hud;
        private Vector3 campPosition;
        private bool hasCampfire;
        private bool busy;

        public float Hunger { get; private set; }
        public float Energy { get; private set; }
        public bool IsNight => cycle != null && cycle.IsNight;

        public void Configure(DayNightCycle dayNight)
        {
            cycle = dayNight;
        }

        public void SetCamp(Vector3 position)
        {
            campPosition = position;
            hasCampfire = true;
        }

        public void Rest()
        {
            if (!busy)
            {
                StartCoroutine(RestRoutine());
            }
        }

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            player = GetComponent<PlayerController>();
            hud = GetComponent<GameHud>();
            campPosition = transform.position;
            Hunger = startingHunger;
            Energy = MaxValue;
        }

        private void Update()
        {
            if (!busy)
            {
                Drain();
                HandleEating();
            }

            player.SprintAllowed = Energy > minimumSprintEnergy;
            if (hud != null)
            {
                hud.SetStats(Hunger / MaxValue, Energy / MaxValue, cycle != null ? cycle.TimeText : string.Empty);
            }

            if (!busy && (Hunger <= 0f || Energy <= 0f))
            {
                StartCoroutine(CollapseRoutine());
            }
        }

        private void Drain()
        {
            float energyRate = player.IsSprinting ? energyPerMinuteSprinting
                : player.IsMoving ? energyPerMinuteWalking
                : energyPerMinuteIdle;
            Hunger = Mathf.Max(0f, Hunger - hungerPerMinute / 60f * Time.deltaTime);
            Energy = Mathf.Max(0f, Energy - energyRate / 60f * Time.deltaTime);
        }

        private void HandleEating()
        {
            if (!GameInput.Current.Eat)
            {
                return;
            }

            if (hud == null)
            {
                return;
            }

            if (!inventory.Remove(ItemType.Coconut))
            {
                hud.ShowMessage("You have no coconuts to eat.");
                return;
            }

            Hunger = Mathf.Min(MaxValue, Hunger + coconutFood);
            hud.ShowMessage($"Ate a coconut: +{coconutFood:0} hunger");
            GameEvents.Log("Ate a coconut");
        }

        private IEnumerator RestRoutine()
        {
            busy = true;
            player.enabled = false;
            bool sleptThroughNight = IsNight;
            yield return Fade(0f, 1f);

            if (cycle != null)
            {
                if (sleptThroughNight)
                {
                    cycle.SkipToMorning();
                }
                else
                {
                    cycle.Advance(2f);
                }
            }

            Energy = MaxValue;
            Hunger = Mathf.Max(10f, Hunger - (sleptThroughNight ? 25f : 8f));
            GameEvents.Log(sleptThroughNight ? "Slept through a night by the fire" : "Rested by the fire");
            yield return Fade(1f, 0f);

            player.enabled = true;
            busy = false;
            if (hud != null)
            {
                hud.ShowMessage(sleptThroughNight ? "You slept until morning." : "You rested by the fire.");
            }
        }

        private IEnumerator CollapseRoutine()
        {
            busy = true;
            player.enabled = false;
            bool fromHunger = Hunger <= 0f;
            GameEvents.Log(fromHunger ? "Collapsed from hunger" : "Collapsed from exhaustion");
            yield return Fade(0f, 1f);

            player.Teleport(campPosition);
            if (cycle != null)
            {
                cycle.Advance(3f);
            }

            Hunger = Mathf.Max(Hunger, 40f);
            Energy = Mathf.Max(Energy, 50f);
            yield return Fade(1f, 0f);

            player.enabled = true;
            busy = false;
            if (hud != null)
            {
                string cause = fromHunger ? "hunger" : "exhaustion";
                string place = hasCampfire ? "at your campfire" : "back on the beach";
                hud.ShowMessage($"You collapsed from {cause} and woke up {place}.");
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            const float duration = 0.8f;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                if (hud != null)
                {
                    hud.SetFade(Mathf.Lerp(from, to, time / duration));
                }
                yield return null;
            }

            if (hud != null)
            {
                hud.SetFade(to);
            }
        }

#if UNITY_EDITOR
        public void DevDrain()
        {
            Hunger = 1f;
        }

        public void DevAdvanceTime()
        {
            if (cycle != null)
            {
                cycle.Advance(2f);
            }
        }
#endif
    }
}
