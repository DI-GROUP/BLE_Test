using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;

namespace BLETest
{
    public static class Tests
    {
        public static async Task<bool> Test1(IDevice device, IAdapter adapter)
        {
            bool testResult = true;
            try
            {
                await adapter.ConnectToDeviceAsync(device);
                await Task.Delay(1500);
                var searchServicesTask= device.GetServicesAsync();
                if (await Task.WhenAny(Task.Delay(10000), searchServicesTask)!= searchServicesTask)
                {
                    return false;
                }
                var services = await searchServicesTask;
                testResult &= InsureServices(services);
                if (!testResult) return false;
                var movService = services.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_SERV_UUID);
                var chars = await movService.GetCharacteristicsAsync();
                testResult &= InsureMovementServiceChars(chars);
                if (!testResult) return false;
                var output = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_UID_UUID);
                var outputResult = await output.ReadAsync();
                testResult &= outputResult.Length == 0;
                if (!testResult) return false;
                var passChar = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_PASS_UUID);
                testResult &= await passChar.WriteAsync(_defaultPassBytes);
                if (!testResult) return false;
                outputResult = await output.ReadAsync();
                testResult &= outputResult.Length != 0;
            }
            catch (Exception ex)
            {
                testResult = false;
            }
            finally
            {
                adapter.DisconnectDeviceAsync(device);
            }
            return testResult;
        }

        private static bool InsureMovementServiceChars(IList<ICharacteristic> chars)
        {
            var acsData = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_ACSDATA_UUID);
            if (acsData == default(ICharacteristic)) return false;
            var backSide = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_BACKSIDE_UUID);
            if (backSide == default(ICharacteristic)) return false;
            var history = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_HISTORY_UUID);
            if (history == default(ICharacteristic)) return false;
            var cmd = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_CMD_UUID);
            if (cmd == default(ICharacteristic)) return false;
            var tap = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_TAP_UUID);
            if (tap == default(ICharacteristic)) return false;
            var uid = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_UID_UUID);
            if (uid == default(ICharacteristic)) return false;
            var pass = chars.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_PASS_UUID);
            if (pass == default(ICharacteristic)) return false;
            return true;
        }

        private static bool InsureServices(IList<IService> services)
        {
            var movService = services.FirstOrDefault(x => x.Id == GuidCollection.MOVEMENT_SERV_UUID);
            return movService != default(IService);
        }

        private static string ServiceTemplate = "f119{0}-71a4-11e6-bdf4-0800200c9a66";
        //private static string _defaultPass = "000000";
        private static byte[] _defaultPassBytes= new byte[]{0x30, 0x30, 0x30, 0x30, 0x30, 0x30 };

        private static class GuidCollection
        {
            // (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
            public static readonly Guid MOVEMENT_SERV_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F50"));
            public static readonly Guid MOVEMENT_ACSDATA_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F51"));
            public static readonly Guid MOVEMENT_BACKSIDE_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F52"));
            public static readonly Guid MOVEMENT_HISTORY_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F53"));
            public static readonly Guid MOVEMENT_CMD_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F54"));
            public static readonly Guid MOVEMENT_TAP_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F55"));
            public static readonly Guid MOVEMENT_UID_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F56"));
            public static readonly Guid MOVEMENT_PASS_UUID = Guid.Parse(string.Format(ServiceTemplate, "6F57"));

//#define MOVEMENT_SERV_UUID                    0x6F50 // UID самого сервис
//#define MOVEMENT_ACSDATA_UUID             0x6F51 // UID сервиса акселерометра(сырые данные с акселерометра)
//#define MOVEMENT_BACKSIDE_UUID               0x6F52 // UID сервиса стороны кубика
//#define MOVEMENT_HISTORY_UUID             0x6F53 // UID сервиса история
//#define MOVEMENT_CMD_UUID                     0x6F54 // UID сервиса команд
//#define MOVEMENT_TAP_UUID                     0x6F55 // UID сервиса tap
//#define MOVEMENT_UID_UUID                                    0x6F56 // UID unique ID
//#define MOVEMENT_PASS_UUID                                0x6F57 // UID password

            public static readonly Guid CalibrationVersionCharachteristic =
                Guid.Parse("F1196F56-71A4-11E6-BDF4-0800200C9A66");

        }
    }
}
