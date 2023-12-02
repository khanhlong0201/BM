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
        public async Task<IActionResult> GetDataUsers()
        {
            try
            {
                var data = await _masterService.GetUsersAsync();
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
        public async Task<IActionResult> GetServices()
        {
            try
            {
                var data = await _masterService.GetServicessAsync();
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

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody]  LoginRequestModel loginRequest)
        {
            try
            {
                var data = await _masterService.Login(loginRequest);
                if (data == null || data.Count() ==0) return BadRequest(new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Tên đăng nhập hoặc mật khẩu không hợp lệ"
                });
                var claims = new[]
              {
                    new Claim("UserId", data.FirstOrDefault().Id + ""),
                    new Claim("UserName", data.FirstOrDefault().UserName + ""),
                    new Claim("FullName", data.FirstOrDefault().FullName + ""),
                    new Claim("Phone", data.FirstOrDefault().PhoneNumber + ""),
                    new Claim("IsAdmin", data.FirstOrDefault().IsAdmin + ""),
                    new Claim("BranchId", data.FirstOrDefault().BranchId + ""),
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
                    data.FirstOrDefault().UserName,
                    data.FirstOrDefault().FullName, // để hiện thị lên người dùng khỏi phải parse từ clainm
                    Token = new JwtSecurityTokenHandler().WriteToken(token) // token user
                });

 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MasterDataController", "Login");
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetSupplies")]
        public async Task<IActionResult> GetSupplies()
        {
            try
            {
                var data = await _masterService.GetSuppliesAsync();
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
    }
}
