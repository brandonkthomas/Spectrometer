using Spectrometer.Models;
using System.Net.NetworkInformation;

namespace Spectrometer.Helpers;

public class NetworkDeviceHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string GetActiveNetworkDeviceName()
    {
        try
        {
            NetworkInterface[]? interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface? networkInterface in interfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    // Check if this interface has a gateway assigned (means it has an internet connection, more than likely)
                    IPInterfaceProperties? ipProperties = networkInterface.GetIPProperties();

                    if (ipProperties.GatewayAddresses.Any(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                        return networkInterface.Name;
                }
            }

            return "";
        }
        catch (Exception ex)
        {
            Logger.WriteExc(ex);
            return "";
        }
    }
}
