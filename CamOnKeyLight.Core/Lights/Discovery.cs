using System.Text;

namespace CamOnKeyLight.Core
{
    public sealed record DiscoveredLight(string Name, string ProductName, string Address, int Port, string Serial);

    public readonly record struct DnsRecord(string Name, ushort Type, byte[] Packet, int DataOffset, int DataLength);

    public static class MdnsCodec
    {
        public const ushort TypeA = 1;
        public const ushort TypePtr = 12;
        public const ushort TypeTxt = 16;
        public const ushort TypeSrv = 33;
        public const string ElgatoService = "_elg._tcp.local";

        public static byte[] CreateQuery(ushort id, bool unicastResponse)
        {
            byte[] name = EncodeName(ElgatoService);
            byte[] packet = new byte[12 + name.Length + 4];
            packet[0] = (byte)(id >> 8);
            packet[1] = (byte)id;
            packet[5] = 1;
            name.CopyTo(packet, 12);
            int question = 12 + name.Length;
            packet[question + 1] = (byte)TypePtr;
            packet[question + 2] = unicastResponse ? (byte)0x80 : (byte)0;
            packet[question + 3] = 1;
            return packet;
        }

        public static bool TryReadRecords(byte[] packet, List<DnsRecord> records)
        {
            if (packet == null || packet.Length < 12)
                return false;

            try
            {
                int offset = 0;
                _ = ReadU16(packet, ref offset);
                _ = ReadU16(packet, ref offset);
                int questions = ReadU16(packet, ref offset);
                int answers = ReadU16(packet, ref offset);
                int authorities = ReadU16(packet, ref offset);
                int additional = ReadU16(packet, ref offset);
                for (int i = 0; i < questions; i++)
                    SkipQuestion(packet, ref offset);

                int count = answers + authorities + additional;
                for (int i = 0; i < count; i++)
                    records.Add(ReadRecord(packet, ref offset));

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        public static string ReadName(byte[] packet, ref int offset)
        {
            StringBuilder name = new StringBuilder();
            int cursor = offset;
            int hops = 0;
            int? resume = null;
            while (cursor < packet.Length)
            {
                int length = packet[cursor];
                if (length == 0)
                {
                    cursor++;
                    break;
                }

                if ((length & 0xC0) == 0xC0)
                {
                    if (cursor + 1 >= packet.Length)
                        throw new ArgumentException("The DNS name is truncated.");

                    int pointer = ((length & 0x3F) << 8) | packet[cursor + 1];
                    if (pointer < 0 || pointer >= packet.Length)
                        throw new ArgumentException("The DNS name pointer is outside the packet.");

                    resume ??= cursor + 2;
                    cursor = pointer;
                    if (++hops > 32)
                        throw new ArgumentException("The DNS name is nested too deeply.");

                    continue;
                }

                if ((length & 0xC0) != 0)
                    throw new ArgumentException("The DNS name uses an unsupported label.");

                cursor++;
                if (cursor + length > packet.Length)
                    throw new ArgumentException("The DNS name is truncated.");

                if (name.Length > 0)
                    name.Append('.');

                name.Append(Encoding.ASCII.GetString(packet, cursor, length));
                cursor += length;
            }

            offset = resume ?? cursor;
            return name.ToString();
        }

        private static byte[] EncodeName(string dotted)
        {
            string[] labels = dotted.Split('.', StringSplitOptions.RemoveEmptyEntries);
            int size = 1;
            foreach (string label in labels)
                size += 1 + label.Length;

            byte[] data = new byte[size];
            int offset = 0;
            foreach (string label in labels)
            {
                data[offset++] = (byte)label.Length;
                Encoding.ASCII.GetBytes(label, 0, label.Length, data, offset);
                offset += label.Length;
            }

            return data;
        }

        private static void SkipQuestion(byte[] packet, ref int offset)
        {
            _ = ReadName(packet, ref offset);
            offset += 4;
            if (offset > packet.Length)
                throw new ArgumentException("The DNS question is truncated.");
        }

        private static DnsRecord ReadRecord(byte[] packet, ref int offset)
        {
            string name = ReadName(packet, ref offset);
            ushort type = ReadU16(packet, ref offset);
            _ = ReadU16(packet, ref offset);
            offset += 4;
            int length = ReadU16(packet, ref offset);
            if (offset + length > packet.Length)
                throw new ArgumentException("The DNS record is truncated.");

            int dataOffset = offset;
            offset += length;
            return new DnsRecord(name, type, packet, dataOffset, length);
        }

        private static ushort ReadU16(byte[] packet, ref int offset)
        {
            if (offset + 2 > packet.Length)
                throw new ArgumentException("The DNS packet is truncated.");

            ushort value = (ushort)((packet[offset] << 8) | packet[offset + 1]);
            offset += 2;
            return value;
        }
    }

    public static class ElgatoDiscovery
    {
        public static IReadOnlyList<DiscoveredLight> FromPackets(IEnumerable<byte[]> packets)
        {
            List<DnsRecord> records = new List<DnsRecord>();
            foreach (byte[] packet in packets)
                MdnsCodec.TryReadRecords(packet, records);

            return Combine(records);
        }

        public static IReadOnlyList<DiscoveredLight> Combine(IReadOnlyList<DnsRecord> records)
        {
            Dictionary<string, Candidate> found = new Dictionary<string, Candidate>(StringComparer.OrdinalIgnoreCase);
            foreach (DnsRecord record in records)
            {
                if (record.Type != MdnsCodec.TypeSrv || !IsElgatoName(record.Name))
                    continue;

                if (!TryReadSrv(record, out int port, out string target))
                    continue;

                found[record.Name] = new Candidate(record.Name, target, port);
            }

            foreach (DnsRecord record in records)
            {
                if (record.Type == MdnsCodec.TypeA && record.DataLength == 4)
                {
                    string ip = record.Packet[record.DataOffset]
                        + "." + record.Packet[record.DataOffset + 1]
                        + "." + record.Packet[record.DataOffset + 2]
                        + "." + record.Packet[record.DataOffset + 3];
                    foreach (Candidate candidate in found.Values)
                    {
                        if (candidate.Host.Equals(record.Name, StringComparison.OrdinalIgnoreCase))
                            candidate.Ip = ip;
                    }
                }
                else if (record.Type == MdnsCodec.TypeTxt && found.TryGetValue(record.Name, out Candidate? owner))
                {
                    foreach ((string key, string value) in ReadTxt(record.Packet, record.DataOffset, record.DataLength))
                    {
                        if (value.Length == 0)
                            continue;

                        if (key.Equals("md", StringComparison.OrdinalIgnoreCase))
                            owner.ProductName = value;
                        else if (key.Equals("id", StringComparison.OrdinalIgnoreCase)
                            || key.Equals("serial", StringComparison.OrdinalIgnoreCase)
                            || key.Equals("sn", StringComparison.OrdinalIgnoreCase))
                            owner.Serial = value;
                    }
                }
            }

            List<DiscoveredLight> lights = new List<DiscoveredLight>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Candidate candidate in found.Values)
            {
                if (string.IsNullOrWhiteSpace(candidate.Ip))
                    continue;

                string address = candidate.Port == 9123 ? candidate.Ip : candidate.Ip + ":" + candidate.Port;
                if (!seen.Add(LightEndpoint.Host(address)))
                    continue;

                string name = InstanceLabel(candidate.Instance);
                string product = string.IsNullOrWhiteSpace(candidate.ProductName) ? name : candidate.ProductName.Trim();
                lights.Add(new DiscoveredLight(name, product, address, candidate.Port, candidate.Serial.Trim()));
            }

            return lights;
        }

        public static IReadOnlyList<DiscoveredLight> ExceptAdded(IEnumerable<DiscoveredLight> found, IEnumerable<string> savedAddresses)
        {
            HashSet<string> saved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string address in savedAddresses)
            {
                if (!string.IsNullOrWhiteSpace(address))
                    saved.Add(LightEndpoint.Host(address));
            }

            return found.Where(light => !saved.Contains(LightEndpoint.Host(light.Address))).ToList();
        }

        public static string? ReplacementAddress(LightRecord saved, IEnumerable<LightRecord> all, IReadOnlyList<DiscoveredLight> found)
        {
            DiscoveredLight? match = Match(saved, found);
            if (match == null)
                return null;

            if (SameHost(saved.Address, match.Address))
                return null;

            foreach (LightRecord other in all)
            {
                if (ReferenceEquals(other, saved))
                    continue;

                if (other.Address.Equals(saved.Address, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (SameHost(other.Address, match.Address))
                    return null;
            }

            return match.Address;
        }

        public static DiscoveredLight? Match(LightRecord saved, IReadOnlyList<DiscoveredLight> found)
        {
            if (!string.IsNullOrWhiteSpace(saved.Serial))
            {
                List<DiscoveredLight> bySerial = found
                    .Where(light => light.Serial.Equals(saved.Serial.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (bySerial.Count == 1)
                    return bySerial[0];

                return null;
            }

            if (string.IsNullOrWhiteSpace(saved.ProductName))
                return null;

            List<DiscoveredLight> byName = found.Where(light =>
                light.ProductName.Equals(saved.ProductName.Trim(), StringComparison.OrdinalIgnoreCase)
                || light.Name.Equals(saved.ProductName.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            return byName.Count == 1 ? byName[0] : null;
        }

        private static bool SameHost(string left, string right)
        {
            return LightEndpoint.Host(left).Equals(LightEndpoint.Host(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsElgatoName(string name)
        {
            return name.EndsWith("._elg._tcp.local", StringComparison.OrdinalIgnoreCase)
                || name.Contains("._elg._tcp.", StringComparison.OrdinalIgnoreCase);
        }

        private static string InstanceLabel(string name)
        {
            const string suffix = "._elg._tcp.local";
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name[..^suffix.Length];

            return name;
        }

        private static bool TryReadSrv(DnsRecord record, out int port, out string target)
        {
            port = 0;
            target = "";
            if (record.DataLength < 7)
                return false;

            port = (record.Packet[record.DataOffset + 4] << 8) | record.Packet[record.DataOffset + 5];
            int offset = record.DataOffset + 6;
            target = MdnsCodec.ReadName(record.Packet, ref offset);
            return target.Length > 0 && port > 0 && port <= 65535;
        }

        private static IEnumerable<(string Key, string Value)> ReadTxt(byte[] packet, int offset, int length)
        {
            int end = offset + length;
            while (offset < end)
            {
                int size = packet[offset];
                offset++;
                if (size == 0 || offset + size > end)
                    yield break;

                string text = Encoding.UTF8.GetString(packet, offset, size);
                offset += size;
                int split = text.IndexOf('=');
                if (split <= 0)
                    continue;

                yield return (text[..split].Trim(), text[(split + 1)..].Trim());
            }
        }

        private sealed class Candidate
        {
            public Candidate(string instance, string host, int port)
            {
                Instance = instance;
                Host = host;
                Port = port;
            }

            public string Instance { get; }
            public string Host { get; }
            public int Port { get; }
            public string Ip { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string Serial { get; set; } = "";
        }
    }
}
