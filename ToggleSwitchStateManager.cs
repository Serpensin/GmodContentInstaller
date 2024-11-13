namespace GModContentWizard
{
    /// <summary>
    /// Manages the state of Guna2ToggleSwitch controls within a container.
    /// </summary>
    internal class ToggleSwitchStateManager
    {
        private readonly Dictionary<Guna2ToggleSwitch, bool> toggleSwitchStates = new();

        /// <summary>
        /// Saves the enabled state of all Guna2ToggleSwitch controls within the specified container.
        /// </summary>
        /// <param name="container">The container holding the Guna2ToggleSwitch controls.</param>
        public void SaveToggleSwitchStates(Control container)
        {
            foreach (var control in container.Controls.OfType<Guna2ToggleSwitch>())
            {
                toggleSwitchStates[control] = control.Enabled;
            }
        }

        /// <summary>
        /// Disables all Guna2ToggleSwitch controls within the specified container.
        /// </summary>
        /// <param name="container">The container holding the Guna2ToggleSwitch controls.</param>
        public static void DisableAllToggleSwitches(Control container)
        {
            foreach (var control in container.Controls.OfType<Guna2ToggleSwitch>())
            {
                control.Enabled = false;
            }
        }

        /// <summary>
        /// Restores the enabled state of all Guna2ToggleSwitch controls to their previously saved states.
        /// </summary>
        public void RestoreToggleSwitchStates()
        {
            foreach (var entry in toggleSwitchStates)
            {
                entry.Key.Enabled = entry.Value;
            }
        }
    }
}
