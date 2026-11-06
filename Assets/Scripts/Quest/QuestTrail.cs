using UnityEngine;

namespace LastTide
{
    /// <summary>The route of the carved dirt trail from the beach to the beacon.</summary>
    public class QuestTrail : MonoBehaviour
    {
        [SerializeField] private Vector3[] points;
        [SerializeField] private int shelterIndex;
        [SerializeField] private int antennaIndex;

        public Vector3[] Points => points;
        public int ShelterIndex => shelterIndex;
        public int AntennaIndex => antennaIndex;

        public void Configure(Vector3[] route, int shelter, int antenna)
        {
            points = route;
            shelterIndex = shelter;
            antennaIndex = antenna;
        }
    }
}
