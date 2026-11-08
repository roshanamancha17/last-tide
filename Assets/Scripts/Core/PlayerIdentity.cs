using System;

namespace LastTide
{
    /// <summary>
    /// A random ID kept on this device, so the server can give each player a fair daily share of AI replies.
    /// It identifies nothing about the person.
    /// </summary>
    public static class PlayerIdentity
    {
        public static string LoadOrCreate(Func<string> read, Action<string> write)
        {
            string id = read();
            if (IsValid(id))
            {
                return id;
            }

            id = Guid.NewGuid().ToString("N");
            write(id);
            return id;
        }

        private static bool IsValid(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length == 32 && Guid.TryParseExact(id, "N", out _);
        }
    }

    /// <summary>How long to wait for Marlow's server.</summary>
    public static class ServerTimeouts
    {
        /// <summary>A sleeping free server takes about a minute to start, so the first chat waits longer.</summary>
        public static int ForRequest(bool serverKnownAwake)
        {
            return serverKnownAwake ? 12 : 40;
        }
    }
}
