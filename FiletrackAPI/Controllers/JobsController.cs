using System.Net;
using FiletrackAPI.Entities;
using FiletrackAPI.Models;
using FiletrackAPI.Services;
using FiletrackWebInterface.Entities;
using FiletrackWebInterface.Services;
using Microsoft.AspNetCore.Mvc;

namespace FiletrackAPI.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class JobsController : Controller
{
    private readonly IJobsService _jobsService;

    public JobsController(IJobsService jobsService)
    {
        _jobsService = jobsService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromForm] CreateJobRequestModel model)
    {
        return StatusCode(await _jobsService.CreateJob(model));
    }
}