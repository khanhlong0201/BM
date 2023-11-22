using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        private ILogger<MasterDataController> _logger { get; set; }
        
        public MasterDataController(ILogger<MasterDataController> logger)
        {
            _logger = logger;
        }
    }
}
