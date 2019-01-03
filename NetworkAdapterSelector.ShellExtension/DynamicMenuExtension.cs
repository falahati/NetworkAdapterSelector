using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using NetworkAdapterSelector.ShellExtension.Properties;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace NetworkAdapterSelector.ShellExtension
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.ClassOfExtension, @".exe")]
    internal class DynamicMenuExtension : SharpContextMenu
    {
        protected override bool CanShowMenu()
        {
            // Only show the menu when application is installed, there is just one executable
            // file selected and some network interface to select from
            return SelectedItemPaths.Count() == 1 &&
                   Path.GetExtension(SelectedItemPaths.First())?.ToLower() == ".exe" &&
                   !string.IsNullOrWhiteSpace(GetNetworkAdapterSelectorAddress()) &&
                   NetworkInterface.GetAllNetworkInterfaces()
                       .Any(networkInterface =>
                           networkInterface.SupportsMulticast &&
                           networkInterface.OperationalStatus == OperationalStatus.Up);
        }

        // ReSharper disable once TooManyDeclarations
        protected override ContextMenuStrip CreateMenu()
        {
            var explorerMenu = new ContextMenuStrip();
            var extensionMenu = new ToolStripMenuItem(Resources.DynamicMenuExtension_CreateMenu_Bind_to, Resources.x16);
            var i = 0;

            // Going through all network interfaces and add them to the list
            foreach (var networkInterface in
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(networkInterface =>
                        networkInterface.SupportsMulticast &&
                        networkInterface.OperationalStatus == OperationalStatus.Up)
            )
            {
                // Copying the network id to a variable
                var networkId = networkInterface.Id;

                // Add normal execution item
                extensionMenu.DropDownItems.Insert(i,
                    new ToolStripMenuItem(
                        networkInterface.Name,
                        null,
                        (sender, args) => BindTo(networkId, false)
                    )
                );

                // Add separator, only once
                if (i == 0)
                {
                    extensionMenu.DropDownItems.Add(new ToolStripSeparator());
                }

                i++;

                // Add run as administrator execution item
                extensionMenu.DropDownItems.Add(
                    new ToolStripMenuItem(
                        string.Format(Resources.DynamicMenuExtension_CreateMenu_Run_as, networkInterface.Name),
                        null,
                        (sender, args) => BindTo(networkId, true)
                    )
                );
            }

            explorerMenu.Items.Add(extensionMenu);

            return explorerMenu;
        }

        // ReSharper disable once FlagArgument
        private void BindTo(string networkInterfaceId, bool asAdmin)
        {
            try
            {
                var executableAddress = GetNetworkAdapterSelectorAddress();

                if (string.IsNullOrWhiteSpace(executableAddress))
                {
                    return;
                }

                var arguments = $"-n \"{networkInterfaceId}\" -e \"{SelectedItemPaths.First()}\"";

#if DEBUG
                arguments += " -d";
#endif

                // Executing the "NetworkAdapterSelector.Hook" and passing the address of executable file.
                var processInfo = new ProcessStartInfo(executableAddress, arguments)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                };

#if DEBUG
                processInfo.WindowStyle = ProcessWindowStyle.Normal;
                processInfo.CreateNoWindow = false;
#endif

                if (asAdmin)
                {
                    // Forcing administrator privileges
                    processInfo.Verb = @"runas";
                }

                Process.Start(processInfo);
            }
            catch (Exception e)
            {
                // Check if operation canceled by user
                if ((e as Win32Exception)?.NativeErrorCode == 1223)
                {
                    return;
                }

                // Otherwise, show an error message to the user
                MessageBox.Show(
                    e.ToString(),
                    Resources.DynamicMenuExtension_BindTo_Network_Adapter_Selector,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private string GetNetworkAdapterSelectorAddress()
        {
            try
            {
                // Trying to get the address of the "NetworkAdapterSelector.Hook" project. If installed.
                using (var key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Network Adapter Selector",
                        false
                    )
                )
                {
                    var executableAddress = key?.GetValue("ExecutableAddress", null) as string;

                    if (!string.IsNullOrWhiteSpace(executableAddress) &&
                        File.Exists(executableAddress))
                    {
                        return executableAddress;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}