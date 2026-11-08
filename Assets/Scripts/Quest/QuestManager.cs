using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LastTide
{
    /// <summary>Tracks the beacon quest, shows the objective, and runs the rescue ending.</summary>
    public class QuestManager : MonoBehaviour
    {
        public const int PartsNeeded = 3;

        [SerializeField] private GameObject player;
        [SerializeField] private Marlow marlow;
        [SerializeField] private Transform beacon;
        [SerializeField] private Transform wreckPart;
        [SerializeField] private Transform shelterPart;
        [SerializeField] private Transform peakPart;
        [SerializeField] private GameObject rescueShip;
        [SerializeField] private DayNightCycle cycle;
        [SerializeField] private Vector3 islandCenter;

        private Inventory inventory;
        private GameHud hud;
        private bool ending;
        private bool canRestart;

        public Transform BeaconTarget => beacon;
        public Transform WreckTarget => wreckPart;
        public Transform ShelterTarget => shelterPart;
        public Transform PeakTarget => peakPart;

        public void Configure(GameObject playerObject, Marlow robot, Transform beaconTransform, Transform wreck, Transform shelter,
            Transform peak, GameObject ship, DayNightCycle dayNight, Vector3 center)
        {
            player = playerObject;
            marlow = robot;
            beacon = beaconTransform;
            wreckPart = wreck;
            shelterPart = shelter;
            peakPart = peak;
            rescueShip = ship;
            cycle = dayNight;
            islandCenter = center;
        }

        public int PartsFound(Inventory items)
        {
            int found = 0;
            if (items.Has(ItemType.Lens)) found++;
            if (items.Has(ItemType.Battery)) found++;
            if (items.Has(ItemType.Antenna)) found++;
            return found;
        }

        /// <summary>A plain description of the quest, sent to Marlow so his hints are accurate.</summary>
        public string DescribeForMarlow()
        {
            return "Goal: repair the signal beacon on the peak with 3 parts, then light it to call for rescue. " +
                   "A dirt trail leads from the starting beach up to the beacon. " +
                   $"Lens: {Status(ItemType.Lens, "in a rock shelter beside the trail, about halfway up; vines around it need a knife")}. " +
                   $"Battery: {Status(ItemType.Battery, "in a crate by the shipwreck on the far shore; the crate needs an axe")}. " +
                   $"Antenna: {Status(ItemType.Antenna, "beside the trail, close to the beacon")}.";
        }

        public void BeginEnding(Transform lit)
        {
            if (!ending)
            {
                ending = true;
                StartCoroutine(EndingRoutine(lit));
            }
        }

        private string Status(ItemType item, string where)
        {
            return inventory != null && inventory.Has(item) ? "found" : where;
        }

        private void Awake()
        {
            inventory = player.GetComponent<Inventory>();
            hud = player.GetComponent<GameHud>();
            if (rescueShip != null)
            {
                rescueShip.SetActive(false);
            }
        }

        private void Update()
        {
            if (ending)
            {
                Keyboard keyboard = Keyboard.current;
                bool restart = (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                               || GameInput.Current.Interact || GameInput.Current.Jump;
                if (canRestart && restart)
                {
                    GameEvents.Clear();
                    UiState.IsTyping = false;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
                        ? SceneManager.GetActiveScene().buildIndex : 0);
                }
                return;
            }

            if (hud != null)
            {
                hud.SetObjective(Objective());
            }
        }

        private string Objective()
        {
            int found = PartsFound(inventory);
            if (marlow != null && !marlow.IsAwake && found == 0)
            {
                return "Explore the beach";
            }

            return found < PartsNeeded
                ? $"Find the beacon parts: {found} of {PartsNeeded}"
                : "Take the parts to the beacon on the peak";
        }

        private IEnumerator EndingRoutine(Transform lit)
        {
            if (hud != null)
            {
                hud.SetObjective("A signal is going out...");
            }

            SetPlayerControl(false);
            yield return new WaitForSeconds(2f);

            Vector3 outward = lit.position - islandCenter;
            outward.y = 0f;
            outward = outward.sqrMagnitude > 1f ? outward.normalized : Vector3.back;
            Vector3 shipEnd = islandCenter + outward * 140f;
            Vector3 shipStart = islandCenter + outward * 1000f;

            Camera main = Camera.main;
            if (main != null)
            {
                ThirdPersonCamera orbit = main.GetComponent<ThirdPersonCamera>();
                if (orbit != null)
                {
                    orbit.enabled = false;
                }

                main.transform.position = lit.position - outward * 7f + Vector3.up * 8f;
                main.transform.LookAt(shipEnd + Vector3.up * 8f);
            }

            if (rescueShip != null)
            {
                rescueShip.SetActive(true);
                rescueShip.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
                rescueShip.transform.localScale = Vector3.one * 1.3f;
                const float sailSeconds = 12f;
                for (float time = 0f; time < sailSeconds; time += Time.deltaTime)
                {
                    float progress = Mathf.SmoothStep(0f, 1f, time / sailSeconds);
                    rescueShip.transform.position = Vector3.Lerp(shipStart, shipEnd, progress);
                    if (hud != null && time > 2f)
                    {
                        hud.SetObjective("A ship answered your signal!");
                    }
                    yield return null;
                }
            }

            yield return new WaitForSeconds(1.5f);
            if (hud != null)
            {
                hud.SetObjective(null);
                string time = cycle != null ? cycle.TimeText : string.Empty;
                hud.ShowEnding($"<b>RESCUED</b>\n\nA ship answered your signal.\nYou and Marlow are going home.\n\nYou made it off the island on {time}.\n\n" +
                               "<size=30>Thanks for playing the Last Tide demo.\n[R] Play again</size>");
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canRestart = true;
        }

        private void SetPlayerControl(bool enabledState)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            SurvivalStats stats = player.GetComponent<SurvivalStats>();
            CraftingSystem crafting = player.GetComponent<CraftingSystem>();
            if (controller != null) controller.enabled = enabledState;
            if (interactor != null) interactor.enabled = enabledState;
            if (stats != null) stats.enabled = enabledState;
            if (crafting != null) crafting.enabled = enabledState;
            if (hud != null && !enabledState) hud.SetPrompt(null);
        }
    }
}
