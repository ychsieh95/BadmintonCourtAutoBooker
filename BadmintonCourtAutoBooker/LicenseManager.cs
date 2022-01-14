namespace BadmintonCourtAutoBooker
{
    internal class LicenseManager
    {
        public bool CheckLicense(string username, string licenseCode) => licenseCode.Decrypt(username) == username;
    }
}
