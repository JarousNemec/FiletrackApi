namespace FiletrackWebInterface.Helpers;

public class AppSettings
{
    public string Password { get; set; }

    public string DataSource { get; set; }

    public string UserID { get; set; }

    public string InitialCatalog { get; set; }

    public bool TrustServerCertificate { get; set; }
    public string TempDirName{ get; set; }
    public string BlobConnectionString { get; set; }
    public string BlobContainer { get; set; }
}