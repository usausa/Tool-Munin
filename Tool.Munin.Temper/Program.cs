namespace Tool.Munin.Temper
{
    using System;
    using System.Linq;

    using HidLibrary;

    public static class Program
    {
        public static void Main(string[] args)
        {
            if ((args.Length > 0) && (args[0] == "name"))
            {
                Name();
            }
            else if ((args.Length > 0) && (args[0] == "config"))
            {
                Config();
            }
            else
            {
                Values();
            }
        }

        private static void Name()
        {
            Console.Write("temperature");
        }

        private static void Config()
        {
            Console.WriteLine("graph_title Temperature");
            Console.WriteLine("graph_category sensors");
            Console.WriteLine("graph_args --base 1000 -l 0");
            Console.WriteLine("graph_info Temperature");
            Console.WriteLine("graph_vlabel Temperature");
            Console.WriteLine("temperature.label Temperature");
            Console.WriteLine("temperature.draw LINE");
            Console.WriteLine(".");
        }

        private static void Values()
        {
            if (ReadTemperature(out var temperature))
            {
                Console.WriteLine("temperature.value {0:F2}", temperature);
                Console.WriteLine(".");
            }
        }

        private static bool ReadTemperature(out double temperature)
        {
            temperature = 0;

            var interfaces = HidDevices.Enumerate()
                .Where(x => x.Attributes.ProductHexId == "0x7401" & x.Attributes.VendorHexId == "0x0C45")
                .ToList();
            var control = interfaces.Find(x => x.DevicePath.Contains("mi_00"));
            var bulk = interfaces.Find(x => x.DevicePath.Contains("mi_01"));

            if ((control == null) || (bulk == null))
            {
                return false;
            }

            if (!control.IsConnected)
            {
                return false;
            }

            control.OpenDevice();
            bulk.OpenDevice();

            // Initialize
            control.Write(new byte[] { 0x00, 0x01, 0x01 }, 100);
            var data = control.Read(100);
            if (data.Status != HidDeviceData.ReadStatus.Success)
            {
                return false;
            }

            // Initialize1
            bulk.Write(new byte[] { 0x00, 0x01, 0x82, 0x77, 0x01, 0x00, 0x00, 0x00, 0x00 }, 100);
            data = bulk.Read(100);
            if (data.Status != HidDeviceData.ReadStatus.Success)
            {
                return false;
            }

            // Initialize2
            bulk.Write(new byte[] { 0x00, 0x01, 0x86, 0xff, 0x01, 0x00, 0x00, 0x00, 0x00 }, 100);
            data = bulk.Read(100);
            if (data.Status != HidDeviceData.ReadStatus.Success)
            {
                return false;
            }

            // Clear garbage
            bulk.Write(new byte[] { 0x00, 0x01, 0x80, 0x33, 0x01, 0x00, 0x00, 0x00, 0x00 }, 100);
            data = bulk.Read(100);
            if (data.Status != HidDeviceData.ReadStatus.Success)
            {
                return false;
            }

            // Temperature
            bulk.Write(new byte[] { 0x00, 0x01, 0x80, 0x33, 0x01, 0x00, 0x00, 0x00, 0x00 }, 100);
            data = bulk.Read(100);
            if (data.Status != HidDeviceData.ReadStatus.Success)
            {
                return false;
            }

            temperature = (((data.Data[4] & 0xFF) + (data.Data[3] << 8)) * (125.0 / 32000.0)) - 1.70;

            return true;
        }
    }
}
