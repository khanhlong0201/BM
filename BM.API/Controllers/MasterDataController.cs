using BM.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        
        private readonly IMasterDataService _masterService;
        private ILogger<MasterDataController> _logger { get; set; }
        
        public MasterDataController(ILogger<MasterDataController> logger, IMasterDataService masterService)
        {
            _logger = logger;
            _masterService = masterService;
        }

        [HttpGet]
        [Route("GetBranchs")]
        public async Task<IActionResult> GetDataBranchs()
        {
            try
            {
                var data = await _masterService.GetBranchsAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetBranchs");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }

        }
    }
}
