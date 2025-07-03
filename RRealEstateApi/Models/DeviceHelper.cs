namespace RRealEstateApi.Models
{
    public static class DeviceHelper
    {
        public static string GetDeviceFingerprint (string ipAddress, string userAgent)
        {
            return $"{ipAddress}_{userAgent}";
        }
    }
}
