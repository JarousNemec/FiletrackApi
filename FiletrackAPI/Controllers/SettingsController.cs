using FiletrackAPI.Entities;
using FiletrackAPI.Models;
using FiletrackAPI.Services;
using FiletrackWebInterface.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FiletrackAPI.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class SettingsController : Controller
{
        private ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public IActionResult GetAllTags()
        {
            var tags = _settingsService.GetAllTags();
            return Ok(tags);
        }
        
        [HttpGet]
        public IActionResult GetPath()
        {
            var path = _settingsService.GetPath();
            return Ok(path);
        }
        
        [HttpPost]
        public IActionResult UpdateTags(TagsUpdateRequest model)
        {
            _settingsService.UpdateTags(new List<Tag>(model.ListToUpdate));
            var result = _settingsService.GetAllTags();
            return Ok(result);
        }
        
        [HttpPost]
        public IActionResult UpdatePath(PathUpdateRequest model)
        {
            _settingsService.UpdatePath(new List<PathMember>(model.UpdatedMembers));
            var result = _settingsService.GetPath();
            return Ok(result);
        }
    
}