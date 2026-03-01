namespace tests;

public class UnitTest1
{
    HttpClient client = new Host().CreateClient();

    [Fact]
    public void Test1()
    {
        var response = client.GetAsync("/login").Result;
        Assert.True(response.IsSuccessStatusCode);
    }
}
