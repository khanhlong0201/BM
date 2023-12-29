using BM.API.Services;
using BM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        
        private readonly IMasterDataService _masterService;
        private ILogger<MasterDataController> _logger { get; set; }
        private readonly IConfiguration _configuration;

        public MasterDataController(ILogger<MasterDataController> logger, IMasterDataService masterService, IConfiguration configuration)
        {
            _logger = logger;
            _masterService = masterService;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetBranchs")]
        public async Task<IActionResult> GetDataBranchs(bool pIsPageLogin = false)
        {
            try
            {
                var data = await _masterService.GetBranchsAsync(pIsPageLogin);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetBranchs");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }

        }

        [HttpPost]
        [Route("UpdateBranch")]
        public async Task<IActionResult> UpdateBranch([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateBranchs(request);
                
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "Update");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpGet]
        [Route("GetUsers")]
        public async Task<IActionResult> GetDataUsers(int pUserId=-1)
        {
            try
            {
                var data = await _masterService.GetUsersAsync(pUserId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetDataUsers");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }

        }

        [HttpPost]
        [Route("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateUsers(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdateUser");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpGet]
        [Route("GetEnumsByType")]
        public async Task<IActionResult> GetEnumsByType([FromQuery] string pType)
        {
            try
            {
                var data = await _masterService.GetEnumsAsync(pType);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetEnumsByType");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdateEnum")]
        public async Task<IActionResult> UpdateEnum([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateEnums(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdateEnum");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpGet]
        [Route("GetCustomers")]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var data = await _masterService.GetCustomersAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetCustomers");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetCustomerById")]
        public async Task<IActionResult> GetCustomerById([FromQuery] string pCusNo)
        {
            try
            {
                var data = await _masterService.GetCustomerById(pCusNo);
                if(data == null)
                {
                    return StatusCode(StatusCodes.Status204NoContent, new
                    {
                        StatusCode = StatusCodes.Status204NoContent,
                        Message = "Không tìm thấy thông tin khách hàng. Vui lòng làm mới lại trang"
                    });
                }    
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetCustomers");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdateCustomer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateCustomer(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdateCustomer");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpGet]
        [Route("GetServices")]
        public async Task<IActionResult> GetServices(string pBranchId, int pUserId, string pLoadAll = "ALL")
        {
            try
            {
                var data = await _masterService.GetServicessAsync(pBranchId, pUserId, pLoadAll);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetServices");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdateService")]
        public async Task<IActionResult> UpdateService([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateService(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdateService");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpGet]
        [Route("GetPricesByService")]
        public async Task<IActionResult> GetPricesByService(string pServiceCode)
        {
            try
            {
                var data = await _masterService.GetPriceListByServiceAsync(pServiceCode);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetPricesByService");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdatePrice")]
        public async Task<IActionResult> UpdatePrice([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdatePrice(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdatePrice");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpGet]
        [Route("GetTreatmentByService")]
        public async Task<IActionResult> GetTreatmentByService(string pServiceCode)
        {
            try
            {
                var data = await _masterService.GetTreatmentByServiceAsync(pServiceCode);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetPricesByService");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("UpdateTreatment")]
        public async Task<IActionResult> UpdateTreatment([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateTreatmentRegime(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdatePrice");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody]  LoginRequestModel loginRequest)
        {
            try
            {
                var data = await _masterService.Login(loginRequest);
                if (data == null || data.Count() ==0) 
                return BadRequest(new {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Tên đăng nhập hoặc mật khẩu không hợp lệ"
                });
                UserModel oUser = data.First();
                var dataBranch = await _masterService.GetBranchsAsync(true);
                BranchModel branch = dataBranch.Where(d =>d.BranchId+""== loginRequest.BranchId+"").First();
                if (oUser.IsAdmin == false && oUser.BranchId+"" != loginRequest.BranchId + "")
                return BadRequest(new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = $"Tài khoản không thuộc chi nhánh {branch?.BranchName+""}"
                });
                if (oUser.IsDeleted == true )
                return BadRequest(new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = $"Tài khoản này đã bị xóa. Bạn hãy liên hệ Quản Trị Viên để nhận tài khoản khác !"
                });

                var claims = new[]
                {
                    new Claim("UserId", oUser.Id + ""),
                    new Claim("EmpNo", oUser.EmpNo + ""),
                    new Claim("UserName", oUser.UserName + ""),
                    new Claim("FullName", oUser.FullName + ""),
                    new Claim("IsAdmin", oUser.IsAdmin + ""),
                    new Claim("BranchId", loginRequest.BranchId + ""),
                    new Claim("BranchName", oUser.BranchName + ""),
                    new Claim(nameof(ClaimTypes.Role), oUser.IsAdmin ?  "administrator" : "staff"),
                }; // thông tin mã hóa (payload)
                // JWT: json web token: Header - Payload - SIGNATURE (base64UrlEncode(header) + "." + base64UrlEncode(payload), your - 256 - bit - secret)
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Jwt:JwtSecurityKey").Value + "")); // key mã hóa
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // loại mã hóa (Header)
                var expiry = DateTime.Now.AddDays(Convert.ToInt32(_configuration.GetSection("Jwt:JwtExpiryInDays").Value)); // hết hạn token
                //var expiry = DateTime.Now.AddMinutes(Convert.ToInt32(_configuration.GetSection("Jwt:JwtExpiryInDays").Value)); // hết hạn token test 1 phút hết tonken
                var token = new JwtSecurityToken(
                    _configuration.GetSection("Jwt:JwtIssuer").Value,
                    _configuration.GetSection("Jwt:JwtAudience").Value,
                    claims,
                    expires: expiry,
                    signingCredentials: creds
                );
                return Ok(new
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Success",
                    Token = new JwtSecurityTokenHandler().WriteToken(token) // token user
                });

 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "Login");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpPost]
        [Route("GetSupplies")]
        public async Task<IActionResult> GetSupplies([FromBody] SearchModel pSearchData)
        {
            try
            {
                var data = await _masterService.GetSuppliesAsync(pSearchData);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetSupplies");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }

        }


        [HttpPost]
        [Route("UpdateSupplies")]
        public async Task<IActionResult> UpdateSupplies([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateSupplies(request);

                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdateSupplies");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpPost]
        [Route("GetSuppliesOutBound")]
        public async Task<IActionResult> GetSuppliesOutBound([FromBody] SearchModel pSearchData)
        {
            try
            {
                var data = await _masterService.GetSuppliesOutBoundAsync(pSearchData);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetSupplies");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }

        }

        [HttpPost]
        [Route("DeleteData")]
        public async Task<IActionResult> DeleteData([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.DeleteDataAsync(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdateService");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }


        [HttpGet]
        [Route("GetInventory")]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var data = await _masterService.GetInventoryAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "GetInvetory");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }

        }

        [HttpPost]
        [Route("UpdateInventory")]
        public async Task<IActionResult> UpdateInventory([FromBody] RequestModel request)
        {
            try
            {
                var response = await _masterService.UpdateInventory(request);

                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "UpdateInvetory");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }
    }
}
