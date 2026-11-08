using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTide
{
    /// <summary>The Tab crafting panel: recipes, costs and what each recipe produces.</summary>
    [RequireComponent(typeof(Inventory))]
    public class CraftingSystem : MonoBehaviour
    {
        private enum Product
        {
            Knife,
            Axe,
            Campfire,
        }

        private readonly struct Recipe
        {
            public readonly Product Product;
            public readonly int Wood;
            public readonly int Stone;
            public readonly int Vine;

            public Recipe(Product product, int wood, int stone, int vine)
            {
                Product = product;
                Wood = wood;
                Stone = stone;
                Vine = vine;
            }
        }

        private static readonly Recipe[] Recipes =
        {
            new Recipe(Product.Knife, 1, 1, 0),
            new Recipe(Product.Axe, 2, 2, 1),
            new Recipe(Product.Campfire, 6, 3, 0),
        };

        [SerializeField] private GameObject campfirePrefab;
        [SerializeField] private GameObject heldKnife;
        [SerializeField] private GameObject heldAxe;
        [SerializeField] private float campfireDistance = 2.5f;

        private Inventory inventory;
        private GameHud hud;
        private bool panelOpen;
        private bool campfireBuilt;

        public void Configure(GameObject campfire, GameObject knife, GameObject axe)
        {
            campfirePrefab = campfire;
            heldKnife = knife;
            heldAxe = axe;
        }

        public bool CampfireBuilt => campfireBuilt;
        public Vector3 CampfirePosition { get; private set; }

        /// <summary>Puts a saved campfire back where it was built.</summary>
        public void RestoreCampfire(Vector3 position)
        {
            if (campfireBuilt || campfirePrefab == null)
            {
                return;
            }

            Instantiate(campfirePrefab, position, Quaternion.identity);
            CampfirePosition = position;
            campfireBuilt = true;
            SurvivalStats stats = GetComponent<SurvivalStats>();
            if (stats != null)
            {
                stats.SetCamp(position + Vector3.forward * 1.5f);
            }
        }

        /// <summary>True while the crafting list is showing.</summary>
        public bool PanelOpen => panelOpen;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            hud = GetComponent<GameHud>();
            inventory.Changed += RefreshPanel;
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.Changed -= RefreshPanel;
            }
        }

        private void Update()
        {
            if (UiState.IsTyping)
            {
                return;
            }

            if (GameInput.Current.Craft)
            {
                panelOpen = !panelOpen;
                RefreshPanel();
            }

            int recipe = GameInput.Current.Recipe;
            if (panelOpen && recipe >= 1 && recipe <= Recipes.Length)
            {
                Craft(Recipes[recipe - 1]);
            }
        }

        private void Craft(Recipe recipe)
        {
            if (IsDone(recipe.Product))
            {
                ShowMessage($"You already made the {recipe.Product}.");
                return;
            }

            if (!CanAfford(recipe))
            {
                ShowMessage($"Not enough materials for the {recipe.Product}.");
                return;
            }

            if (recipe.Product == Product.Campfire && !TryPlaceCampfire())
            {
                ShowMessage("Find open, flat, dry ground for the campfire.");
                return;
            }

            inventory.Remove(ItemType.Wood, recipe.Wood);
            inventory.Remove(ItemType.Stone, recipe.Stone);
            inventory.Remove(ItemType.Vine, recipe.Vine);
            if (recipe.Product == Product.Knife)
            {
                inventory.Add(ItemType.Knife);
                ShowHeld(heldKnife);
            }
            else if (recipe.Product == Product.Axe)
            {
                inventory.Add(ItemType.Axe);
                ShowHeld(heldAxe);
            }

            ShowMessage($"Made: {recipe.Product}");
            GameEvents.Log(recipe.Product == Product.Campfire ? "Built a campfire" : $"Made a {recipe.Product}");
            RefreshPanel();
        }

        private bool TryPlaceCampfire()
        {
            Vector3 origin = transform.position + transform.forward * campfireDistance + Vector3.up * 5f;
            const int everythingButPlayer = ~(1 << 2);
            if (campfirePrefab == null ||
                !Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 15f, everythingButPlayer, QueryTriggerInteraction.Ignore) ||
                hit.point.y < 0.4f ||
                hit.normal.y < 0.8f ||
                Physics.CheckSphere(hit.point + Vector3.up * 1.3f, 1f, everythingButPlayer, QueryTriggerInteraction.Collide))
            {
                return false;
            }

            Instantiate(campfirePrefab, hit.point, Quaternion.identity);
            CampfirePosition = hit.point;
            campfireBuilt = true;
            SurvivalStats stats = GetComponent<SurvivalStats>();
            if (stats != null)
            {
                stats.SetCamp(hit.point + (transform.position - hit.point).normalized * 1.5f);
            }
            return true;
        }

        private static void ShowHeld(GameObject model)
        {
            if (model != null)
            {
                model.SetActive(true);
            }
        }

        private bool IsDone(Product product)
        {
            switch (product)
            {
                case Product.Knife: return inventory.Has(ItemType.Knife);
                case Product.Axe: return inventory.Has(ItemType.Axe);
                default: return campfireBuilt;
            }
        }

        private bool CanAfford(Recipe recipe)
        {
            return inventory.Has(ItemType.Wood, recipe.Wood)
                && inventory.Has(ItemType.Stone, recipe.Stone)
                && inventory.Has(ItemType.Vine, recipe.Vine);
        }

        private void ShowMessage(string message)
        {
            if (hud != null)
            {
                hud.ShowMessage(message);
            }
        }

        private void RefreshPanel()
        {
            // Tools appear in hand however they were obtained.
            if (inventory.Has(ItemType.Knife)) ShowHeld(heldKnife);
            if (inventory.Has(ItemType.Axe)) ShowHeld(heldAxe);

            if (hud == null)
            {
                return;
            }

            hud.SetCraftingPanel(panelOpen ? BuildPanelText() : null);
        }

        private string BuildPanelText()
        {
            StringBuilder text = new StringBuilder("<b>Crafting</b>   [Tab] close\n");
            for (int i = 0; i < Recipes.Length; i++)
            {
                Recipe recipe = Recipes[i];
                string colour = IsDone(recipe.Product) ? "#8a8a8a" : CanAfford(recipe) ? "#8ff08f" : "#ffffff";
                string status = IsDone(recipe.Product) ? "made" : Cost(recipe);
                text.Append($"\n<color={colour}>[{i + 1}] {recipe.Product}: {status}</color>");
            }
            return text.ToString();
        }

        private static string Cost(Recipe recipe)
        {
            StringBuilder cost = new StringBuilder();
            if (recipe.Wood > 0) cost.Append($"{recipe.Wood} Wood  ");
            if (recipe.Stone > 0) cost.Append($"{recipe.Stone} Stone  ");
            if (recipe.Vine > 0) cost.Append($"{recipe.Vine} Vine");
            return cost.ToString().TrimEnd();
        }
    }
}
