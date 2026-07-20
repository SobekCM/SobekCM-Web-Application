namespace SobekCM.Core.Plugins
{
    /// <summary> Abstract class  that can be used to run custom code during enabling and disabling of the plug-in  </summary>
    public abstract class PluginInstaller
    {
        /// <summary> Method to be called when the installer is enabled </summary>
        public abstract void Enable();

        /// <summary> Method to be called with the installer is disabled </summary>
        public abstract void Disable();
    }
}
