using System.Net;
using System.Net.Sockets;

namespace CamOnKeyLight.Core
{
    public static class LightAddressRules
    {
        public const int DefaultPort = 9123;

        public static bool TryCreateUrl(string? raw, out string url, out string? error)
        {
            if (!TryParse(raw, out string host, out int port, out error))
            {
                url = "";
                return false;
            }

            string authority = host.Contains(':') ? "[" + host + "]:" + port : host + ":" + port;
            url = "http://" + authority + "/elgato/lights";
            return true;
        }

        public static bool TryParse(string? raw, out string host, out int port, out string? error)
        {
            host = "";
            port = DefaultPort;
            error = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "The address is empty.";
                return false;
            }

            string text = raw.Trim();
            if (text.Contains("://", StringComparison.Ordinal)
                || text.Contains('@')
                || text.Contains('/')
                || text.Contains('\\')
                || text.Contains('?')
                || text.Contains('#')
                || text.Any(char.IsWhiteSpace))
            {
                error = "Use an IP address or host name, with an optional port.";
                return false;
            }

            if (text.StartsWith('['))
                return TryParseBracketed(text, out host, out port, out error);

            if (IPAddress.TryParse(text, out IPAddress? whole) && IsAcceptedAddress(whole))
            {
                host = whole.ToString();
                return true;
            }

            int colon = text.LastIndexOf(':');
            string name = text;
            if (colon >= 0)
            {
                name = text[..colon];
                if (!TryPort(text[(colon + 1)..], out port, out error))
                    return false;
            }

            if (IPAddress.TryParse(name, out IPAddress? parsed) && parsed.AddressFamily == AddressFamily.InterNetwork)
            {
                host = parsed.ToString();
                return true;
            }

            if (!IsHostName(name))
            {
                error = "Use an IP address or host name, with an optional port.";
                return false;
            }

            host = name;
            return true;
        }

        private static bool TryParseBracketed(string text, out string host, out int port, out string? error)
        {
            host = "";
            port = DefaultPort;
            error = null;
            int end = text.IndexOf(']');
            if (end <= 1)
            {
                error = "That IPv6 address is not valid.";
                return false;
            }

            string inside = text[1..end];
            if (!IPAddress.TryParse(inside, out IPAddress? parsed) || parsed.AddressFamily != AddressFamily.InterNetworkV6)
            {
                error = "That IPv6 address is not valid.";
                return false;
            }

            host = parsed.ToString();
            if (end == text.Length - 1)
                return true;

            if (text[end + 1] != ':' || !TryPort(text[(end + 2)..], out port, out error))
            {
                error ??= "Use an IP address or host name, with an optional port.";
                return false;
            }

            return true;
        }

        private static bool IsAcceptedAddress(IPAddress address)
        {
            return address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6;
        }

        private static bool TryPort(string text, out int port, out string? error)
        {
            port = 0;
            error = null;
            if (text.Length == 0 || !int.TryParse(text, out port) || port is < 1 or > 65535)
            {
                error = "The port must be a number from 1 to 65535.";
                return false;
            }

            return true;
        }

        private static bool IsHostName(string name)
        {
            if (name.Length is < 1 or > 253)
                return false;

            string[] labels = name.Split('.');
            if (labels.Length == 0)
                return false;

            foreach (string label in labels)
            {
                if (label.Length is < 1 or > 63)
                    return false;
                if (label[0] == '-' || label[^1] == '-')
                    return false;
                foreach (char character in label)
                {
                    bool allowed = character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-';
                    if (!allowed)
                        return false;
                }
            }

            return true;
        }
    }
}
