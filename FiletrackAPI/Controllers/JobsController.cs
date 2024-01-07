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
    
    [HttpPost]
    public async Task<IActionResult> UpdateJob([FromForm] UpdateJobRequestModel model)
    {
        return StatusCode(await _jobsService.UpdateJob(model));
    }
    
    [HttpPost]
    public IActionResult GetJobsInState(GetJobsRequest model)
    {
        var res = _jobsService.GetJobsInState(model.State);
        return Ok(res);
    }
    
    [HttpPost]
    public IActionResult GetCompleteJob(JobRequest model)
    {
        var res = _jobsService.GetCompleteJob(model.JobId);
        return Ok(res);
    }
    
    [HttpPost]
    public IActionResult DeleteJob(JobRequest model)
    {
        _jobsService.DeleteJob(model.JobId);
        return Ok();
    }
}