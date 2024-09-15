namespace OnlineWallets.IntegrationTests.Models;

public class ErrorResponse
{
    public string Type { get; set; }
    public string Title { get; set; }
    public int Status { get; set; }
    public IDictionary<string, string[]> Errors { get; set; }
}