using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LastTide
{
    [Serializable]
    public class MarlowRequest
    {
        public string playerId;
        public string message;
        public int day;
        public int hour;
        public int hunger;
        public int energy;
        public string[] inventory;
        public string[] events;
        public string[] history;
        public string quest;
    }

    [Serializable]
    public class MarlowReport
    {
        public string reply;
        public string reason;
    }

    [Serializable]
    public class MarlowResponse
    {
        public string reply;
        public string mood;
        public string action;
    }

    /// <summary>Talks to Marlow's server. Any failure returns null, and the game uses a backup line.</summary>
    public static class MarlowClient
    {
        private const int TimeoutSeconds = 12;

        /// <summary>Must match GAME_KEY on the server. It ships inside the game, so it only deters casual misuse.</summary>
        private const string GameKey = "lt-4b7e2c91d05f8a36";

        /// <summary>True once the server has answered, so later requests can use the normal timeout.</summary>
        public static bool ServerAwake { get; private set; }

        public static IEnumerator Ask(string url, MarlowRequest request, Action<MarlowResponse> onDone)
        {
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
            using (UnityWebRequest web = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                web.uploadHandler = new UploadHandlerRaw(body);
                web.downloadHandler = new DownloadHandlerBuffer();
                web.SetRequestHeader("Content-Type", "application/json");
                web.SetRequestHeader("X-Game-Key", GameKey);
                web.timeout = ServerTimeouts.ForRequest(ServerAwake);
                yield return web.SendWebRequest();

                if (web.result == UnityWebRequest.Result.Success)
                {
                    ServerAwake = true;
                }

                if (web.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[LastTide] Marlow is using a backup line: {web.error}");
                    onDone(null);
                    yield break;
                }

                onDone(Parse(web.downloadHandler.text));
            }
        }

        /// <summary>Sends a flagged AI reply to the server. Fire and forget: the player is thanked either way.</summary>
        public static IEnumerator Report(string url, string reply, string reason)
        {
            string reportUrl = url.Replace("/marlow", "/report");
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new MarlowReport { reply = reply, reason = reason }));
            using (UnityWebRequest web = new UnityWebRequest(reportUrl, UnityWebRequest.kHttpVerbPOST))
            {
                web.uploadHandler = new UploadHandlerRaw(body);
                web.downloadHandler = new DownloadHandlerBuffer();
                web.SetRequestHeader("Content-Type", "application/json");
                web.SetRequestHeader("X-Game-Key", GameKey);
                web.timeout = TimeoutSeconds;
                yield return web.SendWebRequest();
                if (web.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[LastTide] Report not delivered: {web.error}");
                }
            }
        }

        /// <summary>Wakes a sleeping free server early, so the first chat is more likely to get a live reply.</summary>
        public static IEnumerator Wake(string url)
        {
            string healthUrl = url.Replace("/marlow", "/health");
            using (UnityWebRequest web = UnityWebRequest.Get(healthUrl))
            {
                web.timeout = 90;
                yield return web.SendWebRequest();
                ServerAwake = web.result == UnityWebRequest.Result.Success;
            }
        }

        private static MarlowResponse Parse(string json)
        {
            try
            {
                MarlowResponse response = JsonUtility.FromJson<MarlowResponse>(json);
                return response != null && !string.IsNullOrWhiteSpace(response.reply) ? response : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
