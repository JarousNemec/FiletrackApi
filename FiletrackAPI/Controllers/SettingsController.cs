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
        public IActionResult UpdateTags(List<Tag> tags)
        {
            _settingsService.UpdateTags(tags);
            var result = _settingsService.GetAllTags();
            return Ok(result);
        }
        
        [HttpPost]
        public IActionResult UpdatePath( string path)
        {
            _settingsService.UpdatePath(path);
            var result = _settingsService.GetPath();
            return Ok(result);
        }
    
}