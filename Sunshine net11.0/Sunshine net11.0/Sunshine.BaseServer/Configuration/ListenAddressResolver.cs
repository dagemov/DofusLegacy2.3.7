using Sunshine.Logs;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Sunshine.BaseServer.Configuration
{
    internal static class ListenAddressResolver
    {
        public static IPEndPoint CreateIPv4Endpoint(string configuredAddress, int port, string settingName)
        {
            return new IPEndPoint(ResolveIPv4Address(configuredAddress, settingName), port);
        }

        private static IPAddress ResolveIPv4Address(string configuredAddress, string settingName)
        {
            string value = (configuredAddress ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(value) || value == "*" || value == "0.0.0.0")
                return IPAddress.Any;

            if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase))
                return IPAddress.Loopback;

            IPAddress parsedAddress;
            if (IPAddress.TryParse(value, out parsedAddress))
                return NormalizeCandidate(parsedAddress, value, settingName);

            try
            {
                IPAddress hostAddress = Dns.GetHostAddresses(value)
                    .FirstOrDefault(entry => entry.AddressFamily == AddressFamily.InterNetwork);

                if (hostAddress != null)
                    return NormalizeCandidate(hostAddress, value, settingName);
            }
            catch (Exception ex)
            {
                Logger.WriteInfo(string.Format(
                    "{0}='{1}' could not be resolved ({2}). Binding to 0.0.0.0 instead.",
                    settingName,
                    value,
                    ex.Message));
            }

            Logger.WriteInfo(string.Format(
                "{0}='{1}' is not a valid IPv4 bind target. Binding to 0.0.0.0 instead.",
                settingName,
                value));

            return IPAddress.Any;
        }

        private static IPAddress NormalizeCandidate(IPAddress candidate, string configuredAddress, string settingName)
        {
            if (candidate.AddressFamily != AddressFamily.InterNetwork)
            {
                Logger.WriteInfo(string.Format(
                    "{0}='{1}' resolved to a non-IPv4 address. Binding to 0.0.0.0 instead.",
                    settingName,
                    configuredAddress));
                return IPAddress.Any;
            }

            if (IPAddress.Any.Equals(candidate) || IPAddress.Loopback.Equals(candidate))
                return candidate;

            if (IsLocalIPv4Address(candidate))
                return candidate;

            Logger.WriteInfo(string.Format(
                "{0}='{1}' is not assigned to a local interface. Binding to 0.0.0.0 instead.",
                settingName,
                configuredAddress));
            return IPAddress.Any;
        }

        private static bool IsLocalIPv4Address(IPAddress candidate)
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties properties;

                try
                {
                    properties = networkInterface.GetIPProperties();
                }
                catch
                {
                    continue;
                }

                foreach (var unicastAddress in properties.UnicastAddresses)
                {
                    if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork &&
                        unicastAddress.Address.Equals(candidate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
