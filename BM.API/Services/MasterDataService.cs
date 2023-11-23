using BM.API.Infrastructure;
using BM.Models;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;

namespace BM.API.Services;
public interface IMasterDataService
{
    Task<IEnumerable<BranchModel>> GetBranchsAsync();
    Task<ResponseModel> UpdateBranchs(RequestModel pRequest);
}

public class MasterDataService : IMasterDataService
{
    private readonly IBMDbContext _context;
    public MasterDataService(IBMDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BranchModel>> GetBranchsAsync()
    {
        IEnumerable<BranchModel> data;
        try
        {
            await _context.Connect();
            data = await _context.GetDataAsync(@"Select * from dbo.[Branchs]", DataRecordToBranchModel, commandType: CommandType.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    public async Task<ResponseModel> UpdateBranchs(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            BranchModel oBranch = JsonConvert.DeserializeObject<BranchModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@Type", "Branchs");
            if (pRequest.Type == nameof(EnumType.Add))
            {
                oBranch.BranchId = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters);
                queryString = "Insert into [dbo].[Branchs] values ( @BranchId , @BranchName , @IsActive , @Address , @PhoneNumber, getDate(), @UserId , null, null )";
            }
            else
            {
                queryString = "Update [dbo].[Branchs] set BranchName = @BranchName , IsActive = @IsActive , Address = @Address , PhoneNumber = @PhoneNumber , DateUpdate = getDate() , UserUpdate = @UserId where BranchId = @BranchId";
            }
            sqlParameters = new SqlParameter[6];
            sqlParameters[0] = new SqlParameter("@BranchId", oBranch.BranchId);
            sqlParameters[1] = new SqlParameter("@BranchName", oBranch.BranchName);
            sqlParameters[2] = new SqlParameter("@IsActive", oBranch.IsActive);
            sqlParameters[3] = new SqlParameter("@Address", oBranch.Address + "");
            sqlParameters[4] = new SqlParameter("@PhoneNumber", oBranch.PhoneNumber + "");
            sqlParameters[5] = new SqlParameter("@UserId", pRequest.UserId + "");

            var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
            if (data != null && data.Rows.Count > 0)
            {
                response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("UpdateDataAsync", ex.Message);
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// đọc dnah sách chi nhánh
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private BranchModel DataRecordToBranchModel(IDataRecord record)
    {
        BranchModel branch = new();
        if (!Convert.IsDBNull(record["BranchId"])) branch.BranchId = Convert.ToString(record["BranchId"] + "");
        if (!Convert.IsDBNull(record["BranchName"])) branch.BranchName = Convert.ToString(record["BranchName"]);
        if (!Convert.IsDBNull(record["Address"])) branch.Address = Convert.ToString(record["Address"]);
        if (!Convert.IsDBNull(record["PhoneNumber"])) branch.PhoneNumber = Convert.ToString(record["PhoneNumber"]);
        if (!Convert.IsDBNull(record["IsActive"])) branch.IsActive = Convert.ToBoolean(record["IsActive"]);
        if (!Convert.IsDBNull(record["DateCreate"])) branch.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) branch.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) branch.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) branch.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return branch;
    }
}
