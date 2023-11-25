using BM.API.Infrastructure;
using BM.Models;
using Microsoft.AspNetCore.Components.Routing;
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
    Task<IEnumerable<UserModel>> GetUsersAsync();
    Task<ResponseModel> UpdateUsers(RequestModel pRequest);
    Task<IEnumerable<EnumModel>> GetEnumsAsync(string pEnumType);
    Task<ResponseModel> UpdateEnums(RequestModel pRequest);
    Task<IEnumerable<CustomerModel>> GetCustomersAsync();
    Task<ResponseModel> UpdateCustomer(RequestModel pRequest);
}

public class MasterDataService : IMasterDataService
{
    private readonly IBMDbContext _context;
    public MasterDataService(IBMDbContext context)
    {
        _context = context;
    }

    #region Public Funtions

    /// <summary>
    /// lấy danh sách chi nhánh
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<BranchModel>> GetBranchsAsync()
    {
        IEnumerable<BranchModel> data;
        try
        {
            await _context.Connect();
            data = await _context.GetDataAsync(@"Select * from dbo.[Branchs]", DataRecordToBranchModel, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// cập nhật chi nhánh
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
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
                queryString = @"Insert into [dbo].[Branchs]  ([BranchId], [BranchName], [IsActive], [Address], [PhoneNumber], [DateCreate], [UserCreate], [DateUpdate], [UserUpdate])
                values ( @BranchId , @BranchName , @IsActive , @Address , @PhoneNumber, getDate(), @UserId , null, null )";
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
    /// lấy danh sách nhân viên
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<UserModel>> GetUsersAsync()
    {
        IEnumerable<UserModel> data;
        try
        {
            await _context.Connect();
            data = await _context.GetDataAsync(@"Select [Id], [EmpNo], [UserName], [Password], [LastPassword], [FullName], [PhoneNumber]
                    , [Email], [Address], [DateOfBirth], [DateOfWork], [IsAdmin], [BranchId], [DateCreate], [UserCreate], [DateUpdate], [UserUpdate] 
                    from [dbo].[Users] where [IsDelete] = 0"
                    , DataRecordToUserModel, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// Thêm mới/Cập nhật thông tin người dùng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateUsers(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            UserModel oUser = JsonConvert.DeserializeObject<UserModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters;
            async Task ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                }
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@UserName", oUser.UserName);
                    // kiểm tra tên đăng nhập
                    if (await _context.ExecuteScalarAsync("select COUNT(*) from Users with(nolock) where UserName = @UserName", sqlParameters) > 0)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        response.Message = "Tên đăng nhập đã tồn tại!";
                        break;
                    }    
                    sqlParameters[0] = new SqlParameter("@Type", "Users");
                    oUser.EmpNo = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy mã Nhân viên
                    queryString = @"Insert into [dbo].[Users] ([Id], [EmpNo], [UserName], [Password], [LastPassword], [FullName], [PhoneNumber], [Email], [Address], [DateOfBirth], [DateOfWork], [IsAdmin], [BranchId], [DateCreate], [UserCreate], [IsDelete])
                                    values ( @Id , @EmpNo , @UserName , @Password , @LastPassword, @FullName, @PhoneNumber , @Email, @Address, @DateOfBirth, @DateOfWork, @IsAdmin, @BranchId, getDate(), @UserId, 0 )";

                    int iUserId = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from Users with(nolock)");
                    string sPassword = EncryptHelper.Encrypt(oUser.Password + "");
                    sqlParameters = new SqlParameter[14];
                    sqlParameters[0] = new SqlParameter("@Id", iUserId);
                    sqlParameters[1] = new SqlParameter("@EmpNo", oUser.EmpNo);
                    sqlParameters[2] = new SqlParameter("@UserName", oUser.UserName);
                    sqlParameters[3] = new SqlParameter("@Password", sPassword);
                    sqlParameters[4] = new SqlParameter("@LastPassword", sPassword);
                    sqlParameters[5] = new SqlParameter("@FullName", oUser.FullName);
                    sqlParameters[6] = new SqlParameter("@PhoneNumber", oUser.PhoneNumber ?? (object)DBNull.Value);
                    sqlParameters[7] = new SqlParameter("@Email", oUser.Email ?? (object)DBNull.Value);
                    sqlParameters[8] = new SqlParameter("@Address", oUser.Address ?? (object)DBNull.Value);
                    sqlParameters[9] = new SqlParameter("@DateOfBirth", oUser.DateOfBirth ?? (object)DBNull.Value);
                    sqlParameters[10] = new SqlParameter("@DateOfWork", oUser.DateOfWork ?? (object)DBNull.Value);
                    sqlParameters[11] = new SqlParameter("@IsAdmin", oUser.IsAdmin);
                    sqlParameters[12] = new SqlParameter("@BranchId", oUser.BranchId ?? (object)DBNull.Value);
                    sqlParameters[13] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Users]
                                       set [FullName] = @FullName , [PhoneNumber] = @PhoneNumber, [Email] = @Email, [Address] = @Address, [DateOfBirth] = @DateOfBirth, [DateOfWork] = @DateOfWork
                                         , [IsAdmin] = @IsAdmin, [BranchId] = @BranchId, [DateUpdate] = getdate(), [UserUpdate] = @UserId
                                     where [Id] = @Id";

                    sqlParameters = new SqlParameter[10];
                    sqlParameters[0] = new SqlParameter("@Id", oUser.Id);
                    sqlParameters[1] = new SqlParameter("@FullName", oUser.FullName);
                    sqlParameters[2] = new SqlParameter("@PhoneNumber", oUser.PhoneNumber ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@Email", oUser.Email ?? (object)DBNull.Value);
                    sqlParameters[4] = new SqlParameter("@Address", oUser.Address ?? (object)DBNull.Value);
                    sqlParameters[5] = new SqlParameter("@DateOfBirth", oUser.DateOfBirth ?? (object)DBNull.Value);
                    sqlParameters[6] = new SqlParameter("@DateOfWork", oUser.DateOfWork ?? (object)DBNull.Value);
                    sqlParameters[7] = new SqlParameter("@IsAdmin", oUser.IsAdmin);
                    sqlParameters[8] = new SqlParameter("@BranchId", oUser.BranchId ?? (object)DBNull.Value);
                    sqlParameters[9] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }    
        }
        catch (Exception ex)
        {
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
    /// lấy danh sách Enum theo loại Enum
    /// </summary>
    /// <param name="pEnumType"></param>
    /// <returns></returns>
    public async Task<IEnumerable<EnumModel>> GetEnumsAsync(string pEnumType)
    {
        IEnumerable<EnumModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@EnumType", $"{pEnumType}");
            data = await _context.GetDataAsync(@"select [EnumId],[EnumType],[EnumName],[Description],[DateCreate],[UserCreate],[DateUpdate],[UserUpdate]
                    from [dbo].[Enums] where [IsDelete] = 0 and [EnumType] = @EnumType"
                    , DataRecordToEnumModel, sqlParameters , commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// cập nhật danh mục Enum
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateEnums(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            EnumModel oEnum = JsonConvert.DeserializeObject<EnumModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters;
            async Task ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                }
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[2];
                    sqlParameters[0] = new SqlParameter("@Type", "Enums");
                    sqlParameters[1] = new SqlParameter("@EnumType", oEnum.EnumType);
                    oEnum.EnumId = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy mã loại
                    queryString = @"Insert into [dbo].[Enums] ([EnumId], [EnumType], [EnumName], [Description], [DateCreate], [UserCreate], [IsDelete]) 
                                    values (@EnumId, @EnumType, @EnumName, @Description, getdate(), @UserId, 0)";
                    sqlParameters = new SqlParameter[5];
                    sqlParameters[0] = new SqlParameter("@EnumId", oEnum.EnumId);
                    sqlParameters[1] = new SqlParameter("@EnumType", oEnum.EnumType);
                    sqlParameters[2] = new SqlParameter("@EnumName", oEnum.EnumName);
                    sqlParameters[3] = new SqlParameter("@Description", oEnum.Description ?? (object)DBNull.Value);
                    sqlParameters[4] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Enums]
                                       set [EnumName] = @EnumName , [Description] = @Description, [DateUpdate] = getdate(), [UserUpdate] = @UserId
                                     where [EnumId] = @EnumId";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@EnumId", oEnum.EnumId);
                    sqlParameters[1] = new SqlParameter("@EnumName", oEnum.EnumName);
                    sqlParameters[2] = new SqlParameter("@Description", oEnum.Description ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }    
        }
        catch (Exception ex)
        {
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
    /// lấy danh sách Khách hàng theo chi nhánh
    /// </summary>
    /// <param name="pBranchId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<CustomerModel>> GetCustomersAsync()
    {
        IEnumerable<CustomerModel> data;
        try
        {
            await _context.Connect();
            data = await _context.GetDataAsync(@"select [CusNo],[FullName],[Phone1],[Phone2],[CINo],[Email],[FaceBook],[Zalo],[Address],[DateOfBirth],[SkinType]
                    ,[BranchId],[Remark],[DateCreate],[UserCreate],[DateUpdate],[UserUpdate] from [dbo].[Customers] where [IsDelete] = 0"
                    , DataRecordToCustomerModel, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    public async Task<ResponseModel> UpdateCustomer(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            CustomerModel oCustomer = JsonConvert.DeserializeObject<CustomerModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters;
            async Task ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                }
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@Type", "Customers");
                    oCustomer.CusNo = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy mã khách hàng
                    queryString = @"Insert into [dbo].[Customers] ([CusNo],[FullName],[Phone1],[Phone2],[CINo],[Email],[FaceBook],[Zalo]
                                    ,[Address],[DateOfBirth],[SkinType],[BranchId],[Remark],[DateCreate],[UserCreate],[IsDelete]) 
                                    values (@CusNo, @FullName, @Phone1, @Phone2, @CINo, @Email, @FaceBook, @Zalo
                                    ,@Address, @DateOfBirth, @SkinType, @BranchId, @Remark, getdate(), @UserId, 0)";
                    sqlParameters = new SqlParameter[14];
                    sqlParameters[0] = new SqlParameter("@CusNo", oCustomer.CusNo);
                    sqlParameters[1] = new SqlParameter("@FullName", oCustomer.FullName);
                    sqlParameters[2] = new SqlParameter("@Phone1", oCustomer.Phone1 ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@Phone2", oCustomer.Phone2 ?? (object)DBNull.Value);
                    sqlParameters[4] = new SqlParameter("@CINo", oCustomer.CINo ?? (object)DBNull.Value);
                    sqlParameters[5] = new SqlParameter("@Email", oCustomer.Email ?? (object)DBNull.Value);
                    sqlParameters[6] = new SqlParameter("@FaceBook", oCustomer.FaceBook ?? (object)DBNull.Value);
                    sqlParameters[7] = new SqlParameter("@Zalo", oCustomer.Zalo ?? (object)DBNull.Value);
                    sqlParameters[8] = new SqlParameter("@Address", oCustomer.Address ?? (object)DBNull.Value);
                    sqlParameters[9] = new SqlParameter("@DateOfBirth", oCustomer.DateOfBirth ?? (object)DBNull.Value);
                    sqlParameters[10] = new SqlParameter("@SkinType", oCustomer.SkinType ?? (object)DBNull.Value);
                    sqlParameters[11] = new SqlParameter("@BranchId", oCustomer.BranchId);
                    sqlParameters[12] = new SqlParameter("@Remark", oCustomer.Remark);
                    sqlParameters[13] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Customers]
                                       set [EnumName] = @EnumName , [Description] = @Description, [DateUpdate] = getdate(), [UserUpdate] = @UserId
                                     where [EnumId] = @EnumId";
                    sqlParameters = new SqlParameter[14];
                    await ExecQuery();
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }        
        
    #endregion Public Functions

    #region Private Funtions

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

    /// <summary>
    /// đọc danh sách Users
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private UserModel DataRecordToUserModel(IDataRecord record)
    {
        UserModel user = new();
        if (!Convert.IsDBNull(record["Id"])) user.Id = Convert.ToInt32(record["Id"]);
        if (!Convert.IsDBNull(record["EmpNo"])) user.EmpNo = Convert.ToString(record["EmpNo"]);
        if (!Convert.IsDBNull(record["UserName"])) user.UserName = Convert.ToString(record["UserName"]);
        if (!Convert.IsDBNull(record["Password"])) user.Password = Convert.ToString(record["Password"]);
        if (!Convert.IsDBNull(record["LastPassword"])) user.LastPassword = Convert.ToString(record["LastPassword"]);
        if (!Convert.IsDBNull(record["FullName"])) user.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["PhoneNumber"])) user.PhoneNumber = Convert.ToString(record["PhoneNumber"]);
        if (!Convert.IsDBNull(record["Email"])) user.Email = Convert.ToString(record["Email"]);
        if (!Convert.IsDBNull(record["Address"])) user.Address = Convert.ToString(record["Address"]);
        if (!Convert.IsDBNull(record["DateOfBirth"])) user.DateOfBirth = Convert.ToDateTime(record["DateOfBirth"]);
        if (!Convert.IsDBNull(record["DateOfWork"])) user.DateOfWork = Convert.ToDateTime(record["DateOfWork"]);
        if (!Convert.IsDBNull(record["IsAdmin"])) user.IsAdmin = Convert.ToBoolean(record["IsAdmin"]);
        if (!Convert.IsDBNull(record["BranchId"])) user.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["DateCreate"])) user.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) user.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) user.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) user.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return user;
    }

    /// <summary>
    /// đọc danh sách Enums
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private EnumModel DataRecordToEnumModel(IDataRecord record)
    {
        EnumModel model = new EnumModel();
        if (!Convert.IsDBNull(record["EnumId"])) model.EnumId = Convert.ToString(record["EnumId"]);
        if (!Convert.IsDBNull(record["EnumType"])) model.EnumType = Convert.ToString(record["EnumType"]);
        if (!Convert.IsDBNull(record["EnumName"])) model.EnumName = Convert.ToString(record["EnumName"]);
        if (!Convert.IsDBNull(record["Description"])) model.Description = Convert.ToString(record["Description"]);
        //if (!Convert.IsDBNull(record["BranchId"])) user.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return model;
    }


    /// <summary>
    /// đọc danh sách Customers
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private CustomerModel DataRecordToCustomerModel(IDataRecord record)
    {
        CustomerModel model = new();
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["Phone1"])) model.Phone1 = Convert.ToString(record["Phone1"]);
        if (!Convert.IsDBNull(record["Phone2"])) model.Phone2 = Convert.ToString(record["Phone2"]);
        if (!Convert.IsDBNull(record["CINo"])) model.CINo = Convert.ToString(record["CINo"]);
        if (!Convert.IsDBNull(record["Email"])) model.Email = Convert.ToString(record["Email"]);
        if (!Convert.IsDBNull(record["FaceBook"])) model.FaceBook = Convert.ToString(record["FaceBook"]);
        if (!Convert.IsDBNull(record["Zalo"])) model.Zalo = Convert.ToString(record["Zalo"]);
        if (!Convert.IsDBNull(record["SkinType"])) model.SkinType = Convert.ToString(record["SkinType"]);
        if (!Convert.IsDBNull(record["Address"])) model.Address = Convert.ToString(record["Address"]);
        if (!Convert.IsDBNull(record["DateOfBirth"])) model.DateOfBirth = Convert.ToDateTime(record["DateOfBirth"]);
        if (!Convert.IsDBNull(record["Remark"])) model.Remark = Convert.ToString(record["Remark"]);
        if (!Convert.IsDBNull(record["BranchId"])) model.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return model;
    }

    #endregion
}
