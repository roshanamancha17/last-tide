using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace LastTide
{
    /// <summary>Editor-only test keys: F1 wood, F2 stone, F3 vine, F4 coconut, F5 palm, F6 give materials, F7 add 2 hours, F8 nearly empty hunger, F9 wreck, F10 shelter, F11 peak part, F12 beacon, P walk test.</summary>
    public class DevTeleport : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || UiState.IsTyping)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame) GoToItem(ItemType.Wood);
            if (keyboard.f2Key.wasPressedThisFrame) GoToItem(ItemType.Stone);
            if (keyboard.f3Key.wasPressedThisFrame) GoToItem(ItemType.Vine);
            if (keyboard.f4Key.wasPressedThisFrame) GoToItem(ItemType.Coconut);
            if (keyboard.f5Key.wasPressedThisFrame) GoToPalm();
            if (keyboard.f6Key.wasPressedThisFrame) GiveMaterials();
            if (keyboard.f7Key.wasPressedThisFrame) GetComponent<SurvivalStats>().DevAdvanceTime();
            if (keyboard.f8Key.wasPressedThisFrame) GetComponent<SurvivalStats>().DevDrain();
            if (keyboard.f9Key.wasPressedThisFrame) GoToQuest(0);
            if (keyboard.f10Key.wasPressedThisFrame) GoToQuest(1);
            if (keyboard.f11Key.wasPressedThisFrame) GoToQuest(2);
            if (keyboard.f12Key.wasPressedThisFrame) GoToQuest(3);
            if (keyboard.pKey.wasPressedThisFrame) StartCoroutine(WalkTest());
        }

        // Walks the real character to each quest location and reports the result in the console.
        private System.Collections.IEnumerator WalkTest()
        {
            QuestManager quest = FindAnyObjectByType<QuestManager>();
            QuestTrail trail = FindAnyObjectByType<QuestTrail>();
            Vector3 start = transform.position;
            PlayerController player = GetComponent<PlayerController>();
            player.enabled = false;
            var points = new System.Collections.Generic.List<Vector3>(trail.Points);

            var toShelter = points.GetRange(0, trail.ShelterIndex + 1);
            toShelter.Add(quest.ShelterTarget.position);
            yield return Walk("shelter", toShelter, quest.ShelterTarget.position);

            var toAntenna = new System.Collections.Generic.List<Vector3> { points[trail.ShelterIndex] };
            toAntenna.AddRange(points.GetRange(trail.ShelterIndex, trail.AntennaIndex - trail.ShelterIndex + 1));
            toAntenna.Add(quest.PeakTarget.position);
            yield return Walk("antenna", toAntenna, quest.PeakTarget.position);

            yield return Walk("beacon", points.GetRange(trail.AntennaIndex, points.Count - trail.AntennaIndex - 1), quest.BeaconTarget.position);

            player.Teleport(start);
            yield return Walk("crate", FindRoute(GameObject.Find("Terrain").transform, start, quest.WreckTarget.position), quest.WreckTarget.position);
            player.enabled = true;
            Debug.Log("[WalkTest] Finished.");
        }

        private System.Collections.IEnumerator Walk(string name, System.Collections.Generic.List<Vector3> route, Vector3 target)
        {
            CharacterController controller = GetComponent<CharacterController>();
            float started = Time.time;
            int index = 0;
            int attempts = 0;
            float sidestepUntil = 0f;
            Vector3 sidestep = Vector3.zero;
            Vector3 checkPosition = transform.position;
            float checkTime = Time.time;
            bool stuck = false;
            int lastIndex = -1;
            float waypointStarted = Time.time;
            while (index < route.Count && !stuck)
            {
                Vector3 offset = route[index] - transform.position;
                offset.y = 0f;
                if (offset.magnitude < 1.2f)
                {
                    index++;
                    continue;
                }

                if (index != lastIndex)
                {
                    lastIndex = index;
                    waypointStarted = Time.time;
                }
                else if (Time.time - waypointStarted > 8f)
                {
                    stuck = true;
                }

                Vector3 heading = Time.time < sidestepUntil ? sidestep : offset.normalized;
                controller.Move((heading * 8f + Vector3.down * 12f) * Time.deltaTime);
                if (Time.time - checkTime > 1.2f)
                {
                    float moved = (transform.position - checkPosition).magnitude;
                    if (moved < 0.6f)
                    {
                        attempts++;
                        sidestep = Vector3.Cross(Vector3.up, offset.normalized) * (attempts % 2 == 0 ? 1f : -1f);
                        sidestepUntil = Time.time + 0.5f * attempts;
                        stuck = attempts > 5;
                    }
                    else if (moved > 3f)
                    {
                        attempts = 0;
                    }

                    checkPosition = transform.position;
                    checkTime = Time.time;
                }
                yield return null;
            }

            float left = Vector3.Distance(transform.position, target);
            string result = stuck ? "STUCK" : left <= 2.8f ? "ARRIVED" : "ENDED FAR AWAY";
            Debug.Log($"[WalkTest] {name}: {result} at {transform.position}, {left:F1} m from target, {Time.time - started:F0} s.");
        }

        private static System.Collections.Generic.List<Vector3> FindRoute(Transform terrain, Vector3 from, Vector3 to)
        {
            Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            var ids = new System.Collections.Generic.Dictionary<Vector3Int, int>();
            var points = new System.Collections.Generic.List<Vector3>();
            int[] node = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = terrain.TransformPoint(vertices[i]);
                Vector3Int key = Vector3Int.RoundToInt(world * 100f);
                if (!ids.TryGetValue(key, out int id))
                {
                    id = points.Count;
                    ids[key] = id;
                    points.Add(world);
                }
                node[i] = id;
            }

            var links = new System.Collections.Generic.List<int>[points.Count];
            for (int i = 0; i < links.Length; i++) links[i] = new System.Collections.Generic.List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = node[triangles[i]], b = node[triangles[i + 1]], c = node[triangles[i + 2]];
                Vector3 faceNormal = Vector3.Cross(points[b] - points[a], points[c] - points[a]).normalized;
                if (Mathf.Abs(faceNormal.y) < 0.8f) continue;
                links[a].Add(b); links[a].Add(c); links[b].Add(a); links[b].Add(c); links[c].Add(a); links[c].Add(b);
            }

            int start = Nearest(points, from), goal = Nearest(points, to);
            float[] cost = new float[points.Count];
            int[] previous = new int[points.Count];
            bool[] done = new bool[points.Count];
            for (int i = 0; i < cost.Length; i++) { cost[i] = float.MaxValue; previous[i] = -1; }
            cost[start] = 0f;
            var open = new System.Collections.Generic.List<int> { start };
            while (open.Count > 0)
            {
                int bestIndex = 0;
                for (int i = 1; i < open.Count; i++) if (cost[open[i]] < cost[open[bestIndex]]) bestIndex = i;
                int current = open[bestIndex];
                open[bestIndex] = open[open.Count - 1];
                open.RemoveAt(open.Count - 1);
                if (done[current]) continue;
                done[current] = true;
                if (current == goal) break;
                foreach (int next in links[current])
                {
                    if (done[next] || points[next].y < 0.3f) continue;
                    float total = cost[current] + (points[next] - points[current]).magnitude;
                    if (total < cost[next])
                    {
                        cost[next] = total;
                        previous[next] = current;
                        open.Add(next);
                    }
                }
            }

            var route = new System.Collections.Generic.List<Vector3>();
            for (int at = goal; at != -1; at = previous[at]) route.Add(points[at]);
            route.Reverse();
            return route;
        }

        private static int Nearest(System.Collections.Generic.List<Vector3> points, Vector3 position)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float distance = (points[i] - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private void GoToQuest(int place)
        {
            QuestManager quest = FindAnyObjectByType<QuestManager>();
            if (quest == null)
            {
                return;
            }

            Teleport(place == 0 ? quest.WreckTarget : place == 1 ? quest.ShelterTarget : place == 2 ? quest.PeakTarget : quest.BeaconTarget);
        }

        private void GiveMaterials()
        {
            Inventory inventory = GetComponent<Inventory>();
            inventory.Add(ItemType.Wood, 10);
            inventory.Add(ItemType.Stone, 10);
            inventory.Add(ItemType.Vine, 2);
            inventory.Add(ItemType.Knife);
            inventory.Add(ItemType.Axe);
        }

        private void GoToItem(ItemType item)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (Interactable candidate in Interactable.Active)
            {
                if (candidate is Pickup pickup && pickup.Item == item)
                {
                    Consider(candidate.transform, ref best, ref bestDistance);
                }
            }

            Teleport(best);
        }

        private void GoToPalm()
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (Interactable candidate in Interactable.Active)
            {
                if (candidate is ChoppableTree)
                {
                    Consider(candidate.transform, ref best, ref bestDistance);
                }
            }

            Teleport(best);
        }

        private void Consider(Transform candidate, ref Transform best, ref float bestDistance)
        {
            float distance = (candidate.position - transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        private void Teleport(Transform target)
        {
            if (target == null)
            {
                return;
            }

            CharacterController controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.position = target.position + Vector3.up * 0.5f + target.right * 1.2f;
            controller.enabled = true;
        }
#endif
    }
}
