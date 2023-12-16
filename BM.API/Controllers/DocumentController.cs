﻿using BM.API.Services;
using BM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private ILogger<DocumentController> _logger { get; set; }
        private readonly IDocumentService _documentervice;

        public DocumentController(ILogger<DocumentController> logger, IDocumentService documentervice)
        {
            _logger = logger;
            _documentervice = documentervice;
        }

        [HttpPost]
        [Route("UpdateSalesOrder")]
        public async Task<IActionResult> UpdateSalesOrder([FromBody] RequestModel request)
        {
            try
            {
                var response = await _documentervice.UpdateSalesOrder(request);

                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "UpdateSalesOrder");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpPost]
        [Route("GetDocList")]
        public async Task<IActionResult> GetDataDocuments(SearchModel pSearchData)
        {
            try
            {
                var data = await _documentervice.GetSalesOrdersAsync(pSearchData);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "GetDataDocuments");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });
            }

        }

        [HttpGet]
        [Route("GetDocById")]
        public async Task<IActionResult> GetDocById(int pDocEntry)
        {
            try
            {
                var data = await _documentervice.GetDocumentById(pDocEntry);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "GetDocById");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });
            }

        }

        [HttpGet]
        [Route("GetDocClosedByGuest")]
        public async Task<IActionResult> GetDocClosedByGuest(string pCusNo)
        {
            try
            {
                var data = await _documentervice.GetSalesOrderClosedByGuest(pCusNo);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "GetDocClosedByGuest");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });
            }

        }

        [HttpPost]
        [Route("CancleDocList")]
        public async Task<IActionResult> CancleDocList([FromBody] RequestModel request)
        {
            try
            {
                var response = await _documentervice.CancleDocList(request);

                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "CancleDocList");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpPost]
        [Route("GetReport")]
        public async Task<IActionResult> GetDataReport(RequestReportModel pSearchData)
        {
            try
            {
                var data = await _documentervice.GetReportAsync(pSearchData);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "GetDataReport");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });
            }

        }

        [HttpPost]
        [Route("ReminderByMonth")]
        public async Task<IActionResult> ReminderByMonth([FromBody] SearchModel pSearchData)
        {
            try
            {
                var response = await _documentervice.ReminderByMonthAsync(pSearchData);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "CancleDocList");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpGet]
        [Route("GetCustomerDebtsByDoc")]
        public async Task<IActionResult> GetCustomerDebtsByDoc(int pDocEntry)
        {
            try
            {
                var response = await _documentervice.GetCustomerDebtsByDocAsync(pDocEntry);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "GetCustomerDebtsByDoc");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpPost]
        [Route("UpdateCustomerDebts")]
        public async Task<IActionResult> UpdateCustomerDebts([FromBody] RequestModel request)
        {
            try
            {
                var response = await _documentervice.UpdateCustomerDebtsAsync(request);
                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "UpdateCustomerDebts");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }

        [HttpPost]
        [Route("UpdateOutBound")]
        public async Task<IActionResult> UpdateOutBound([FromBody] RequestModel request)
        {
            try
            {
                var response = await _documentervice.UpdateOutBound(request);

                if (response == null || response.StatusCode != 0) return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = response?.Message ?? "Vui lòng liên hệ IT để được hổ trợ."
                });
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DocumentController", "UpdateSalesOrder");
                return StatusCode(StatusCodes.Status400BadRequest, new
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    ex.Message
                });

            }
        }
    }
}
