namespace LastTide
{
    /// <summary>Commands typed into Marlow's chat instead of a message.</summary>
    public static class ChatCommands
    {
        /// <summary>"/report" flags Marlow's last AI reply.</summary>
        public static bool IsReport(string typed)
        {
            return typed != null && typed.Trim().Equals("/report", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
