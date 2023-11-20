using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("test/{action}")]
public class TestController : Controller
{
    public string Get()
    {
        return $"Time is {DateTime.Now} ";
    }
    public DateTime GetTime()
    {
        return DateTime.Now;
    }
    public int GetRandom()
    {
        return Random.Shared.Next(int.MinValue, int.MaxValue);
    }
}