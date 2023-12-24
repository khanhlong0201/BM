using BM.API.Commons;
using BM.API.Infrastructure;
using BM.Models;
using BM.Models.Shared;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;

namespace BM.API.Services;
public interface IMasterDataService
{
    Task<IEnumerable<BranchModel>> GetBranchsAsync(bool pIsPageLogin = false);
    Task<ResponseModel> UpdateBranchs(RequestModel pRequest);
    Task<IEnumerable<UserModel>> GetUsersAsync(int pUserId = -1);
    Task<ResponseModel> UpdateUsers(RequestModel pRequest);
    Task<IEnumerable<EnumModel>> GetEnumsAsync(string pEnumType);
    Task<ResponseModel> UpdateEnums(RequestModel pRequest);
    Task<IEnumerable<CustomerModel>> GetCustomersAsync();
    Task<ResponseModel> UpdateCustomer(RequestModel pRequest);
    Task<IEnumerable<ServiceModel>> GetServicessAsync(string pBranchId, int pUserId, string pLoadAll);
    Task<ResponseModel> UpdateService(RequestModel pRequest);

    Task<IEnumerable<UserModel>> Login(LoginRequestModel pRequest);
    Task<CustomerModel> GetCustomerById(string pCusNo);
    Task<IEnumerable<SuppliesModel>> GetSuppliesAsync(SearchModel pSearchData);
    Task<ResponseModel> UpdateSupplies(RequestModel pRequest);
    Task<ResponseModel> DeleteDataAsync(RequestModel pRequest);
    Task<IEnumerable<PriceModel>> GetPriceListByServiceAsync(string pServiceCode);
    Task<ResponseModel> UpdatePrice(RequestModel pRequest);
    Task<IEnumerable<InvetoryModel>> GetInventoryAsync();
    Task<ResponseModel> UpdateInventory(RequestModel pRequest); // nhập kho
    Task<IEnumerable<TreatmentRegimenModel>> GetTreatmentByServiceAsync(string pServiceCode);
    Task<ResponseModel> UpdateTreatmentRegime(RequestModel pRequest);
    Task<IEnumerable<SuppliesModel>> GetSuppliesOutBoundAsync(SearchModel pSearchData);
    Task<ResponseModel> CheckKeyBindingBeforeDeleting(RequestModel pRequests); // check dữ liệu trước khi xóa 
}

public class MasterDataService : IMasterDataService
{
    private readonly IBMDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    public MasterDataService(IBMDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    #region Public Funtions

    /// <summary>
    /// lấy danh sách chi nhánh
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<BranchModel>> GetBranchsAsync(bool pIsPageLogin = false)
    {
        IEnumerable<BranchModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@pIsPageLogin", pIsPageLogin);
            data = await _context.GetDataAsync(@"Select * from dbo.[Branchs] with(nolock)
                   where @pIsPageLogin = 0 or (@pIsPageLogin = 1 and [IsActive] = 1)", DataRecordToBranchModel, sqlParameters, commandType: CommandType.Text);
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
                queryString = @"Insert into [dbo].[Branchs]  ([BranchId], [BranchName], [IsActive], [Address], [PhoneNumber], [DateCreate], [UserCreate], [DateUpdate], [UserUpdate], [ListServiceType])
                values ( @BranchId , @BranchName , @IsActive , @Address , @PhoneNumber, @DateTimeNow, @UserId , null, null, @ListServiceType)";
            }
            else
            {
                queryString = "Update [dbo].[Branchs] set BranchName = @BranchName , IsActive = @IsActive , Address = @Address , PhoneNumber = @PhoneNumber , DateUpdate = @DateTimeNow , UserUpdate = @UserId, ListServiceType = @ListServiceType where BranchId = @BranchId";
            }
            sqlParameters = new SqlParameter[8];
            sqlParameters[0] = new SqlParameter("@BranchId", oBranch.BranchId);
            sqlParameters[1] = new SqlParameter("@BranchName", oBranch.BranchName);
            sqlParameters[2] = new SqlParameter("@IsActive", oBranch.IsActive);
            sqlParameters[3] = new SqlParameter("@Address", oBranch.Address + "");
            sqlParameters[4] = new SqlParameter("@PhoneNumber", oBranch.PhoneNumber + "");
            sqlParameters[5] = new SqlParameter("@UserId", pRequest.UserId + "");
            sqlParameters[6] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
            sqlParameters[7] = new SqlParameter("@ListServiceType", oBranch.ListServiceType + "");

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
    public async Task<IEnumerable<UserModel>> GetUsersAsync(int pUserid = -1)
    {
        IEnumerable<UserModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@UserId", pUserid);
            data = await _context.GetDataAsync(@"Select [Id], [EmpNo], [UserName], [Password], [LastPassword], [FullName], [PhoneNumber]
                    , [Email], [Address], [DateOfBirth], [DateOfWork], [IsAdmin], [BranchId], [DateCreate], [UserCreate], [DateUpdate], [UserUpdate] , [ListServiceType]
                    from [dbo].[Users] where [IsDelete] = 0 and (@UserId = -1 or Id = @UserId)"
                    , DataRecordToUserModel, sqlParameters, commandType: CommandType.Text);
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
                    queryString = @"Insert into [dbo].[Users] ([Id], [EmpNo], [UserName], [Password], [LastPassword], [FullName], [PhoneNumber], [Email], [Address], [DateOfBirth], [DateOfWork], [IsAdmin], [BranchId], [DateCreate], [UserCreate], [IsDelete], [ListServiceType])
                                    values ( @Id , @EmpNo , @UserName , @Password , @LastPassword, @FullName, @PhoneNumber , @Email, @Address, @DateOfBirth, @DateOfWork, @IsAdmin, @BranchId, @DateTimeNow, @UserId, 0 ,@ListServiceType)";

                    int iUserId = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from Users with(nolock)");
                    string sPassword = EncryptHelper.Encrypt(oUser.Password + "");
                    sqlParameters = new SqlParameter[16];
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
                    sqlParameters[14] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[15] = new SqlParameter("@ListServiceType", oUser.ListServiceType ?? (object)DBNull.Value);
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Users]
                                       set [FullName] = @FullName , [PhoneNumber] = @PhoneNumber, [Email] = @Email, [Address] = @Address, [DateOfBirth] = @DateOfBirth, [DateOfWork] = @DateOfWork
                                         , [IsAdmin] = @IsAdmin, [BranchId] = @BranchId, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId, ListServiceType = @ListServiceType
                                     where [Id] = @Id";

                    sqlParameters = new SqlParameter[12];
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
                    sqlParameters[10] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[11] = new SqlParameter("@ListServiceType", oUser.ListServiceType ?? (object)DBNull.Value);
                    await ExecQuery();
                    break;
                case nameof(EnumType.@ChangePassWord):
                    queryString = @"Update [dbo].[Users]
                                    set Password = @PasswordNew, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                    where [Id] = @Id";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@Id", oUser.Id);
                    sqlParameters[1] = new SqlParameter("@PasswordNew", EncryptHelper.Encrypt(oUser.PasswordNew + ""));
                    sqlParameters[2] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
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
            data = await _context.GetDataAsync(@"select [EnumId],[EnumType],[EnumTypeName],[EnumName],[Description],[DateCreate],[UserCreate],[DateUpdate],[UserUpdate]
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
                    queryString = @"Insert into [dbo].[Enums] ([EnumId], [EnumType], [EnumName], [Description], [DateCreate], [UserCreate], [IsDelete], [EnumTypeName]) 
                                    values (@EnumId, @EnumType, @EnumName, @Description, @DateTimeNow, @UserId, 0,@EnumTypeName)";
                    sqlParameters = new SqlParameter[7];
                    sqlParameters[0] = new SqlParameter("@EnumId", oEnum.EnumId);
                    sqlParameters[1] = new SqlParameter("@EnumType", oEnum.EnumType);
                    sqlParameters[2] = new SqlParameter("@EnumName", oEnum.EnumName);
                    sqlParameters[3] = new SqlParameter("@Description", oEnum.Description ?? (object)DBNull.Value);
                    sqlParameters[4] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[5] = new SqlParameter("@EnumTypeName", oEnum.EnumTypeName);
                    sqlParameters[6] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Enums]
                                       set [EnumName] = @EnumName , [Description] = @Description, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId,EnumTypeName=@EnumTypeName
                                     where [EnumId] = @EnumId";
                    sqlParameters = new SqlParameter[6];
                    sqlParameters[0] = new SqlParameter("@EnumId", oEnum.EnumId);
                    sqlParameters[1] = new SqlParameter("@EnumName", oEnum.EnumName);
                    sqlParameters[2] = new SqlParameter("@Description", oEnum.Description ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[4] = new SqlParameter("@EnumTypeName", oEnum.EnumTypeName);
                    sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
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
            data = await _context.GetDataAsync(@"select [CusNo],[FullName],[Phone1],[Phone2],[CINo],[Email],[FaceBook],[Zalo],T0.[Address],[DateOfBirth],[SkinType]
                      ,T0.[BranchId], T1.BranchName as [BranchName],[Remark],T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate] 
					  ,(select isnull(sum(TotalDebtAmount), 0) from [dbo].[CustomerDebts] as T01 with(nolock) where T0.[CusNo] = T01.[CusNo]) as [TotalDebtAmount]
			     from [dbo].[Customers] as T0 with(nolock) 
		    left join [dbo].[Branchs] as T1 with(nolock) on T0.BranchId = T1.BranchId
					where [IsDelete] = 0 order by [CusNo] desc"
                    , DataRecordToCustomerModel, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// cập nhật thông tin khách hàng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
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
            void setParameter()
            {
                sqlParameters = new SqlParameter[15];
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
                sqlParameters[12] = new SqlParameter("@Remark", oCustomer.Remark ?? (object)DBNull.Value);
                sqlParameters[13] = new SqlParameter("@UserId", pRequest.UserId);
                sqlParameters[14] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
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
                                    ,@Address, @DateOfBirth, @SkinType, @BranchId, @Remark, @DateTimeNow, @UserId, 0)";
                    setParameter();
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Customers]
                                       set [FullName] = @FullName , [Phone1] = @Phone1, [Phone2] = @Phone2
                                         , [CINo] = @CINo , [Email] = @Email, [FaceBook] = @FaceBook, [Zalo] = @Zalo, [Remark] = @Remark
                                         , [Address] = @Address , [DateOfBirth] = @DateOfBirth, [SkinType] = @SkinType, [BranchId] = @BranchId
                                         , [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                     where [CusNo] = @CusNo";
                    setParameter();
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
    /// lấy khách hàng theo mã
    /// </summary>
    /// <param name="pCusNo"></param>
    /// <returns></returns>
    public async Task<CustomerModel> GetCustomerById(string pCusNo)
    {
        CustomerModel oCustomer;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@CusNo", pCusNo);
            string queryString = @"select [CusNo],[FullName],[Phone1],[Phone2],[CINo],[Email],[FaceBook],[Zalo],T0.[Address],[DateOfBirth]
                                           ,T0.[BranchId],T2.[BranchName],[Remark],T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate]
										   ,(select string_agg(EnumName, ', ') from [dbo].[Enums] as T00 with(nolock) 
										      where T00.EnumType = 'SkinType' and T0.SkinType like '%""'+T00.EnumId+'""%') as [SkinType]
                                           ,(select isnull(sum(Debt), 0) from [dbo].[Drafts] as T01 with(nolock) where T0.[CusNo] = T01.[CusNo] and T01.StatusId = 'Closed') as [TotalDebtAmount]
					                  from [dbo].[Customers] as T0 with(nolock)
                                inner join [dbo].[Branchs] as T2 with(nolock) on T0.[BranchId] = T2.[BranchId]
								where T0.[IsDelete] = 0 and [CusNo] = @CusNo";
            oCustomer = await _context.GetDataByIdAsync(queryString, DataRecordToCustomerModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return oCustomer;
    }

    /// <summary>
    /// lấy danh sách dịch vụ
    /// </summary>
    /// <param name="pBranchId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ServiceModel>> GetServicessAsync(string pBranchId, int pUserId, string pLoadAll)
    {
        IEnumerable<ServiceModel> data;
        try
        {
            await _context.Connect();
            string sCondition = string.Empty;

            // VIẾT VẬY CHO ĐỠ RỐI
            if(pLoadAll?.ToUpper() == "ALL") sCondition = string.Empty; // load tất cả
            else if (pLoadAll?.ToUpper() == nameof(EnumTable.Services).ToUpper())
            {
                // voo page danh mục
                // lấy theo chi nhánh
                sCondition = @"and CHARINDEX(',' + T1.EnumId + ',', ',' + (select top 1 ListServiceType from [Branchs] with(nolock) where BranchId = @BranchId) + ',', 0) > 0";
            }
            else
            {
                // vô page lập chứng từ -> đi theo nhân viên
                // lấy theo chi nhánh + theo nhân viên
                sCondition = @"and CHARINDEX(',' + T1.EnumId + ',', ',' + (select top 1 ListServiceType from [Branchs] with(nolock) where BranchId = @BranchId) + ',', 0) > 0
                               and CHARINDEX(',' + T1.EnumId + ',', ',' + (select top 1 ListServiceType from [Users] with(nolock) where Id = @UserId) + ',', 0) > 0";
            }
            SqlParameter[] sqlParameters = new SqlParameter[2];
            sqlParameters[0] = new SqlParameter("@BranchId", pBranchId);
            sqlParameters[1] = new SqlParameter("@UserId", pUserId);
            data = await _context.GetDataAsync(@$"select [ServiceCode],[ServiceName],T0.[EnumId],T1.[EnumName],T0.[PackageId],T2.[EnumName] as [PackageName]
                         ,T0.[Description],[WarrantyPeriod],[QtyWarranty],T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate] 
                         ,isnull((select top 1 Price from [dbo].[Prices] as T00 with(nolock) where T0.[ServiceCode] = T00.[ServiceCode] and [IsActive]= 1 order by [IsActive] desc, [DateUpdate] desc), 0) as [Price]
                         ,t0.ListPromotionSupplies 
                         ,T0.IsOutBound
                    from [dbo].[Services] as T0 with(nolock) 
              inner join [dbo].[Enums] as T1 with(nolock) on T0.[EnumId] = T1.[EnumId]
               left join [dbo].[Enums] as T2 with(nolock) on T0.[PackageId] = T2.[EnumId]
                   where T0.[IsDelete] = 0 {sCondition}
                order by [ServiceCode] desc"
                    , DataRecordToServiceModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// cập nhật thông tin dịch vụ -> Đơn giá nếu thêm mới
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateService(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            ServiceModel oService = JsonConvert.DeserializeObject<ServiceModel>(pRequest.Json + "")!;
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
            void setParameter()
            {
                sqlParameters = new SqlParameter[11];
                sqlParameters[0] = new SqlParameter("@ServiceCode", oService.ServiceCode);
                sqlParameters[1] = new SqlParameter("@ServiceName", oService.ServiceName);
                sqlParameters[2] = new SqlParameter("@EnumId", oService.EnumId);
                sqlParameters[3] = new SqlParameter("@Description", oService.Description ?? (object)DBNull.Value);
                sqlParameters[4] = new SqlParameter("@WarrantyPeriod", oService.WarrantyPeriod);
                sqlParameters[5] = new SqlParameter("@QtyWarranty", oService.QtyWarranty);
                sqlParameters[6] = new SqlParameter("@UserId", pRequest.UserId);
                sqlParameters[7] = new SqlParameter("@PackageId", oService.PackageId+ "");
                sqlParameters[8] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                sqlParameters[9] = new SqlParameter("@ListPromotionSupplies", oService.ListPromotionSupplies + "");
                sqlParameters[10] = new SqlParameter("@IsOutBound", oService.IsOutBound);
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@Type", "Services");
                    oService.ServiceCode = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy mã dịch vụ
                    queryString = @"Insert into [dbo].[Services] ([ServiceCode],[ServiceName],[EnumId],[PackageId],[Description],[WarrantyPeriod],[QtyWarranty],[DateCreate],[UserCreate],[IsDelete], ListPromotionSupplies, IsOutBound)
                                    values (@ServiceCode, @ServiceName, @EnumId, @PackageId, @Description, @WarrantyPeriod, @QtyWarranty, @DateTimeNow, @UserId, 0 ,@ListPromotionSupplies , @IsOutBound)";
                    setParameter();
                    int iPriceId = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[Prices] with(nolock)");
                    await _context.BeginTranAsync();
                    await ExecQuery();
 
                    // thêm vào bảng giá
                    queryString = @"Insert into [dbo].[Prices] ([Id],[ServiceCode],[Price],[DateCreate],[UserCreate],[IsActive])
                                    values (@Id, @ServiceCode, @Price, @DateTimeNow, @UserId, 1)";
                    sqlParameters = new SqlParameter[5];
                    sqlParameters[0] = new SqlParameter("@Id", iPriceId);
                    sqlParameters[1] = new SqlParameter("@ServiceCode", oService.ServiceCode);
                    sqlParameters[2] = new SqlParameter("@Price", oService.Price);
                    sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[4] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    await ExecQuery();
                    await _context.CommitTranAsync();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Services]
                                       set [ServiceName] = @ServiceName , [EnumId] = @EnumId, [Description] = @Description
                                         , [WarrantyPeriod] = @WarrantyPeriod , [QtyWarranty] = @QtyWarranty, [PackageId] = @PackageId
                                         , [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId, ListPromotionSupplies = @ListPromotionSupplies, IsOutBound = @IsOutBound
                                     where [ServiceCode] = @ServiceCode";
                    setParameter();
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
            await _context.RollbackAsync();
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// Danh sách bảng giá theo dịch vụ
    /// </summary>
    /// <param name="pServiceCode"></param>
    /// <returns></returns>
    public async Task<IEnumerable<PriceModel>> GetPriceListByServiceAsync(string pServiceCode)
    {
        IEnumerable<PriceModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@ServiceCode", pServiceCode);
            data = await _context.GetDataAsync(@"select [Id], [ServiceCode],[Price],T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate], [IsActive]
                    from [dbo].[Prices] as T0 with(nolock) 
                   where T0.[ServiceCode] = @ServiceCode order by [IsActive] desc, [DateUpdate] desc"
                    , DataRecordToPriceModel, sqlParameters, commandType: CommandType.Text);

        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }    

    /// <summary>
    /// thêm mới + cập nhật thông tin bảng giá
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdatePrice(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            PriceModel oPrice = JsonConvert.DeserializeObject<PriceModel>(pRequest.Json + "")!;
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
                    oPrice.Id = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[Prices] with(nolock)");
                    queryString = @"Insert into [dbo].[Prices] ([Id],[ServiceCode],[Price],[DateCreate],[UserCreate], [DateUpdate],[IsActive])
                                    values (@Id, @ServiceCode, @Price, @DateTimeNow, @UserId, @DateTimeNow, @IsActive)";
                    sqlParameters = new SqlParameter[6];
                    sqlParameters[0] = new SqlParameter("@Id", oPrice.Id);
                    sqlParameters[1] = new SqlParameter("@ServiceCode", oPrice.ServiceCode);
                    sqlParameters[2] = new SqlParameter("@Price", oPrice.Price);
                    sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[4] = new SqlParameter("@IsActive", oPrice.IsActive);
                    sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[Prices]
                                       set [IsActive] = @IsActive
                                         , [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                     where [Id] = @Id";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@Id", oPrice.Id);
                    sqlParameters[1] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[2] = new SqlParameter("@IsActive", oPrice.IsActive);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    await ExecQuery();
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }

            // nếu kích hoạt bảng giá -> cập nhật lại các dòng được Active false
            if(oPrice.IsActive == true)
            {
                queryString = @"Update [dbo].[Prices]
                                   set [IsActive] = 0
                                     , [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                 where [ServiceCode] = @ServiceCode and [Id] <> @Id";
                sqlParameters = new SqlParameter[4];
                sqlParameters[0] = new SqlParameter("@Id", oPrice.Id);
                sqlParameters[1] = new SqlParameter("@ServiceCode", oPrice.ServiceCode);
                sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
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
    /// Danh sách Phác đồ điều trị
    /// </summary>
    /// <param name="pServiceCode"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TreatmentRegimenModel>> GetTreatmentByServiceAsync(string pServiceCode)
    {
        IEnumerable<TreatmentRegimenModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@ServiceCode", pServiceCode);
            data = await _context.GetDataAsync(@"select [Id], [LineNum], [ServiceCode], [Name], [Title],T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate]
                    from [dbo].[TreatmentRegimen] as T0 with(nolock) 
                   where T0.[ServiceCode] = @ServiceCode order by [LineNum] asc"
                    , DataRecordToTreatmentRigimenModel, sqlParameters, commandType: CommandType.Text);

        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// cập nhật thông tin Phác đồ điều trị
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateTreatmentRegime(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            bool isUpdated = false;
            List<TreatmentRegimenModel> listTreatments = JsonConvert.DeserializeObject<List<TreatmentRegimenModel>>(pRequest.Json + "")!;
            if(listTreatments == null || !listTreatments.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Dữ liệu đầu vào không đúng định dạng";
                return response;
            }
            SqlParameter[] sqlParameters;
            async Task<bool> ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                    return response.StatusCode == 0;
                }
                return false;
            }
            await _context.BeginTranAsync();
            foreach (var oItem in listTreatments)
            {
                sqlParameters = new SqlParameter[7];
                sqlParameters[0] = new SqlParameter("@UserId", pRequest.UserId);
                sqlParameters[1] = new SqlParameter("@Name", oItem.Name);
                sqlParameters[2] = new SqlParameter("@Title", oItem.Title);
                sqlParameters[3] = new SqlParameter("@ServiceCode", oItem.ServiceCode);
                sqlParameters[4] = new SqlParameter("@LineNum", oItem.LineNum);
                sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                if (oItem.Id <=0)
                {
                    // thêm mới
                    oItem.Id = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[TreatmentRegimen] with(nolock)"); // gán lại Id
                    queryString = @"Insert into [dbo].[TreatmentRegimen]([Id],[LineNum],[Name],[Title],[ServiceCode], [DateCreate], [UserCreate], [DateUpdate])
                                            values(@Id, @LineNum, @Name, @Title, @ServiceCode, @DateTimeNow, @UserId, @DateTimeNow)";
                    sqlParameters[6] = new SqlParameter("@Id", oItem.Id);
                }    
                else
                {
                    // cập nhật
                    queryString = @"Update [dbo].[TreatmentRegimen]
                                       set [Name] = @Name, [Title] = @Title, [ServiceCode] = @ServiceCode
                                         , [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId, [LineNum] = @LineNum
                                     where [Id] = @Id";
                    sqlParameters[6] = new SqlParameter("@Id", oItem.Id);
                }
                isUpdated = await ExecQuery();
                if (!isUpdated)
                {
                    await _context.RollbackAsync();
                    break;
                }
            }

            // xóa các dòng không tồn tại trong danh sách Ids
            queryString = @"Delete from [dbo].[TreatmentRegimen] where [Id] not in ( select value from STRING_SPLIT(@ListIds, ',') )
                                    and [ServiceCode] = @ServiceCode";
            sqlParameters = new SqlParameter[2];
            sqlParameters[0] = new SqlParameter("@ListIds", string.Join(",", listTreatments.Select(m => m.Id).Distinct()));
            sqlParameters[1] = new SqlParameter("@ServiceCode", listTreatments.First().ServiceCode);
            await _context.DeleteDataAsync(queryString, sqlParameters);

            if (isUpdated) await _context.CommitTranAsync();
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
            await _context.RollbackAsync();
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    /// <param name="pBranchId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<UserModel>> Login(LoginRequestModel pRequest)
    {
        IEnumerable<UserModel> data;
        SqlParameter[] sqlParameters;
        string queryString = "";
        try
        {
            await _context.Connect();
                queryString = @"select top 1 t0.Id, t0.EmpNo, t0.UserName, t0.FullName, t0.Email, t0.IsAdmin, t0.BranchId, t1.BranchName
                                    from dbo.[Users] t0 
                                    inner join Branchs t1 on t0.BranchId = t1.BranchId
                                                    where t0.UserName = @UserName and t0.Password = @Password";
            //setParameter();
            sqlParameters = new SqlParameter[2];
            sqlParameters[0] = new SqlParameter("@UserName", pRequest.UserName);
            sqlParameters[1] = new SqlParameter("@Password", pRequest.Password);
            data = await _context.GetDataAsync(queryString, DataRecordToUserModelByLogin, sqlParameters, CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }


    /// <summary>
    /// lấy danh sách vật tư
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<SuppliesModel>> GetSuppliesAsync(SearchModel pSearchData)
    {
        IEnumerable<SuppliesModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@Type", pSearchData.Type + "");

                data = await _context.GetDataAsync(@$"SELECT t0.[SuppliesCode] ,t0.[SuppliesName] ,t0.[EnumId] ,t1.EnumName  ,t0.[DateCreate] ,t0.[UserCreate] ,t0.[DateUpdate] ,t0.[UserUpdate] ,t3.FullName as 'UserNameCreate',t4.FullName as 'UserNameUpdate'
	                      , (select isnull(sum(t1.QtyInv),0) from Inventory t1 where t0.SuppliesCode = t1.SuppliesCode and t1.IsDelete = 0) as QtyIntoInv
	                      --, (select top 1 isnull(t1.Price,0) from Inventory t1 where t0.SuppliesCode = t1.SuppliesCode and t1.IsDelete = 0 order by t1.DateCreate desc) as Price
					       , ISNULL(t5.QtyOutBound,0) as QtyOutBound
					      , ISNULL(t5.QtyInv,0) as QtyInv
					      , ISNULL(t5.Price,0) as Price
                          ,t6.EnumId as SuppliesTypeCode
                           ,t6.EnumName as SuppliesTypeName
                            ,t0.Type
                      FROM [dbo].[Supplies] t0 
                      inner join Enums t1 on t0.EnumId = t1.EnumId
				      inner join Users t3 on t0.UserCreate = t3.Id
				      left join Users t4 on t0.UserUpdate = t4.Id
				      left join (select t2.[SuppliesCode] ,t2.[SuppliesName],t2.[EnumId],t2.EnumName  , isnull(t3.Qty,0) as QtyOutBound, (isnull(t2.QtyInv,0) - isnull(t3.Qty,0)) as QtyInv, t2.Price, t2.BranchId from (
							    SELECT t0.[SuppliesCode] ,t0.[SuppliesName],t0.[EnumId],t1.EnumName, t2.BranchId ,sum(isnull(t2.QtyInv,0)) as QtyInv ,max(t2.Price) as Price
											      FROM [dbo].[Supplies] t0 
											      inner join Enums t1 on t0.EnumId = t1.EnumId
											      inner join Inventory  t2 on t0.SuppliesCode = t2.SuppliesCode
											      where t0.IsDelete = 0 and t1.EnumType ='Unit' 
											      and t2.IsDelete = 0 
											      group by  t0.[SuppliesCode]  ,t0.[SuppliesName] ,t0.[EnumId]  ,t1.EnumName, t2.BranchId
										      ) t2
							    left join (SELECT SuppliesCode as SuppliesCode,SuppliesName as SuppliesName,BranchId,sum(Qty) as Qty
													    ,EnumId as EnumId, EnumName as EnumName
													    FROM OutBound 
													    CROSS APPLY OPENJSON(SuppliesQtyList)
													    WITH (
														    SuppliesCode VARCHAR(50),
														    SuppliesName NVARCHAR(255),
														    Qty  decimal(19,6),
														    QtyInv decimal(19,6),
														    EnumId VARCHAR(50),
														    EnumName NVARCHAR(255)
													    )  where IsDelete = 0 group by SuppliesCode,SuppliesName,EnumId, EnumName, BranchId ) t3 on t2.BranchId = t3.BranchId
													    and t2.SuppliesCode = t3.SuppliesCode and t2.EnumId = t3.EnumId) t5 on t0.SuppliesCode = t5.SuppliesCode and t0.EnumId = t5.EnumId
                        left join Enums t6 on t0.SuppliesType = t6.EnumId
                        where t0.IsDelete = 0 and t1.EnumType ='Unit' and (@TYPE=''  OR CHARINDEX(','+T0.[Type]+',',','+@TYPE+',')>0) order by t0.[DateCreate] desc"
                       , DataRecordToSuppliesModel, sqlParameters, commandType: CommandType.Text);
            
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// lấy danh sách vật tư
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<SuppliesModel>> GetSuppliesOutBoundAsync(SearchModel pSearchData)
    {
        IEnumerable<SuppliesModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@Type", pSearchData.Type + "");
            data = await _context.GetDataAsync(@"select t2.[SuppliesCode] ,t2.[SuppliesName],t2.[EnumId],t2.EnumName  ,(isnull(t2.QtyInv,0) - isnull(t3.Qty,0)) as QtyInv, isnull(t2.Price,0) as Price, t2.BranchId 
                                                from (
	                                            SELECT t0.[SuppliesCode] ,t0.[SuppliesName],t0.[EnumId],t1.EnumName
																	, (select top 1 t1.BranchId from Inventory t1 where t0.SuppliesCode = t1.SuppliesCode and t1.IsDelete = 0) as BranchId
																	, (select isnull(sum(t1.QtyInv),0) from Inventory t1 where t0.SuppliesCode = t1.SuppliesCode and t1.IsDelete = 0) as QtyInv
																	, (select top 1 isnull(t1.Price,0) from Inventory t1 where t0.SuppliesCode = t1.SuppliesCode and t1.IsDelete = 0 order by t1.DateCreate desc) as Price					
					                                                FROM [dbo].[Supplies] t0 
					                                                inner join Enums t1 on t0.EnumId = t1.EnumId
					                                                where t0.IsDelete = 0 and t1.EnumType ='Unit' and (@TYPE=''  OR CHARINDEX(','+T0.[Type]+',',','+@TYPE+',')>0)
				                                                ) t2
	                                            left join (SELECT SuppliesCode as SuppliesCode,SuppliesName as SuppliesName,BranchId,sum(Qty) as Qty
							                                            ,EnumId as EnumId, EnumName as EnumName
							                                            FROM OutBound 
							                                            CROSS APPLY OPENJSON(SuppliesQtyList)
							                                            WITH (
								                                            SuppliesCode VARCHAR(50),
								                                            SuppliesName NVARCHAR(255),
								                                            Qty  decimal(19,6),
								                                            QtyInv decimal(19,6),
								                                            EnumId VARCHAR(50),
								                                            EnumName NVARCHAR(255)
							                                            ) 
                                                where IsDelete = 0 group by SuppliesCode,SuppliesName,EnumId, EnumName, BranchId ) t3 on t2.BranchId = t3.BranchId
							                                            and t2.SuppliesCode = t3.SuppliesCode and t2.EnumId = t3.EnumId"
                    , DataRecordToSuppliesOutBoundModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// lấy danh sách tồn kho
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<InvetoryModel>> GetInventoryAsync()
    {
        IEnumerable<InvetoryModel> data;
        try
        {
            await _context.Connect();
            data = await _context.GetDataAsync(@"SELECT t0.[ABSID]
                                            ,t0.[SuppliesCode]
                                            ,t0.[BranchId]
                                            ,t0.[EnumId]
                                            ,t0.[QtyInv]
                                            ,t0.[Price]
                                            ,t0.[DateCreate]
                                            ,t0.[UserCreate]
                                            ,t0.[DateUpdate]
                                            ,t0.[UserUpdate]
	                                        ,t1.SuppliesName
	                                        ,t2.EnumName
	                                        ,t3.FullName as 'UserNameCreate'
	                                        ,t4.FullName as 'UserNameUpdate'
                                        FROM [dbo].[Inventory] t0 
                                        inner join Supplies t1 on t0.SuppliesCode = t1.SuppliesCode
                                        inner join Enums t2 on t0.EnumId = t2.EnumId
                                        inner join Users t3 on t0.UserCreate = t3.Id
                                        left join Users t4 on t0.UserUpdate = t4.Id
                                        where t0.IsDelete = 0 and t2.EnumType ='Unit' order by t0.[DateCreate] desc"
                    , DataRecordToInvetoryModel, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// Thêm mới/Cập nhật vật tư
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateSupplies(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            SuppliesModel oSupplies = JsonConvert.DeserializeObject<SuppliesModel>(pRequest.Json + "")!;
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
            void setParameter()
            {
                sqlParameters = new SqlParameter[7];
                sqlParameters[0] = new SqlParameter("@SuppliesCode", oSupplies.SuppliesCode);
                sqlParameters[1] = new SqlParameter("@SuppliesName", oSupplies.SuppliesName);
                sqlParameters[2] = new SqlParameter("@EnumId", oSupplies.EnumId ?? (object)DBNull.Value);
                sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                sqlParameters[4] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                sqlParameters[5] = new SqlParameter("@SuppliesTypeCode", oSupplies.SuppliesTypeCode ?? (object)DBNull.Value);
                sqlParameters[6] = new SqlParameter("@Type", oSupplies.Type ?? (object)DBNull.Value);
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@SuppliesName", oSupplies.SuppliesName);
                    // kiểm tra tên vật tư
                    if (await _context.ExecuteScalarAsync("select COUNT(*) from Supplies with(nolock) where SuppliesName = @SuppliesName", sqlParameters) > 0)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        response.Message = "Tên vật tư đã tồn tại!";
                        break;
                    }
                    sqlParameters[0] = new SqlParameter("@Type", "Supplies");
                    oSupplies.SuppliesCode = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy mã vật tư
                    queryString = @"INSERT INTO [dbo].[Supplies] ([SuppliesCode] ,[SuppliesName],[EnumId],[DateCreate],[UserCreate],[IsDelete], [SuppliesType] , [Type])
                                    values ( @SuppliesCode , @SuppliesName , @EnumId , @DateTimeNow, @UserId, 0 ,@SuppliesTypeCode, @Type)";

                    setParameter();
                     await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"UPDATE [dbo].[Supplies]
                                   SET [SuppliesName] = @SuppliesName
                                      ,[EnumId] = @EnumId
                                      ,[DateCreate] = @DateTimeNow
                                      ,[UserCreate] = @UserId
                                      ,[SuppliesType] = @SuppliesTypeCode
                                      ,[Type] = @Type
                                 WHERE SuppliesCode = @SuppliesCode";

                    setParameter();
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
    /// Thêm mới/Cập nhật tồn kho
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateInventory(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            bool isUpdated = false;
            InvetoryModel oInvotory = JsonConvert.DeserializeObject<InvetoryModel>(pRequest.Json + "")!;
            List<InvetoryModel> lstInvs = JsonConvert.DeserializeObject<List<InvetoryModel>>(pRequest.JsonDetail + "");

            SqlParameter[] sqlParameters;
            async Task<bool> ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                    return response.StatusCode == 0;
                }
                return false;
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    await _context.BeginTranAsync();
                    queryString = @"INSERT INTO [dbo].[Inventory] ([SuppliesCode] ,[BranchId] ,[EnumId],[QtyInv],[Price] ,[DateCreate] ,[UserCreate],[IsDelete])
                                    values ( @SuppliesCode , @BranchId , @EnumId ,@QtyInv, @Price, @DateTimeNow, @UserId, 0 )";
                    foreach (var oInv in lstInvs)
                    {

                        sqlParameters = new SqlParameter[8];
                        sqlParameters[0] = new SqlParameter("@SuppliesCode", oInv.SuppliesCode ?? (object)DBNull.Value);
                        sqlParameters[1] = new SqlParameter("@QtyInv", oInv.QtyInv ?? (object)DBNull.Value);
                        sqlParameters[2] = new SqlParameter("@Price", oInv.Price ?? (object)DBNull.Value);
                        sqlParameters[3] = new SqlParameter("@BranchId", oInvotory.BranchId ?? (object)DBNull.Value);
                        sqlParameters[4] = new SqlParameter("@Absid", oInv.Absid ?? (object)DBNull.Value);
                        sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                        sqlParameters[6] = new SqlParameter("@UserId", pRequest.UserId);
                        sqlParameters[7] = new SqlParameter("@EnumId", oInv.EnumId ?? (object)DBNull.Value);
                        isUpdated = await ExecQuery();

                        if (!isUpdated)
                        {
                            await _context.RollbackAsync();
                            break;
                        }
                    }
                    if (isUpdated) await _context.CommitTranAsync();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"UPDATE [dbo].[Inventory]
                               SET [SuppliesCode] = @SuppliesCode
                                  ,[BranchId] = @BranchId
                                  ,[EnumId] = @EnumId
                                  ,[QtyInv] = @QtyInv
                                  ,[Price] = @Price
                                  ,[DateUpdate] = @DateTimeNow
                                  ,[UserUpdate] = @UserId
                                   Where Absid= @Absid";
                    sqlParameters = new SqlParameter[8];
                    sqlParameters[0] = new SqlParameter("@SuppliesCode", oInvotory.SuppliesCode);
                    sqlParameters[1] = new SqlParameter("@QtyInv", oInvotory.QtyInv);
                    sqlParameters[2] = new SqlParameter("@Price", oInvotory.Price);
                    sqlParameters[3] = new SqlParameter("@BranchId", oInvotory.BranchId);
                    sqlParameters[4] = new SqlParameter("@Absid", oInvotory.Absid);
                    sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[6] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[7] = new SqlParameter("@EnumId", oInvotory.EnumId);

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
    /// xóa thông tin trong bảng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> DeleteDataAsync(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        ResponseModel responseCheck = new ResponseModel();
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters;
            string queryString = "";
            switch (pRequest.Type)
            {
                case nameof(EnumTable.Users):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[Id] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                     responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    if(responseCheck != null && responseCheck.StatusCode == -1)
                    {
                        response.StatusCode = -1;
                        response.Message = responseCheck.Message;
                        return response;
                    }
                    response = await deleteDataAsync(nameof(EnumTable.Users), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.Supplies):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[SuppliesCode] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                     responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    if (responseCheck != null && responseCheck.StatusCode == -1)
                    {
                        response.StatusCode = -1;
                        response.Message = responseCheck.Message;
                        return response;
                    }
                    response = await deleteDataAsync(nameof(EnumTable.Supplies), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.Inventory):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[ABSID] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    if (responseCheck != null && responseCheck.StatusCode == -1)
                    {
                        response.StatusCode = -1;
                        response.Message = responseCheck.Message;
                        return response;
                    }
                    response = await deleteDataAsync(nameof(EnumTable.Inventory), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.Services):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[ServiceCode] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    if (responseCheck != null && responseCheck.StatusCode == -1)
                    {
                        response.StatusCode = -1;
                        response.Message = responseCheck.Message;
                        return response;
                    }
                    response = await deleteDataAsync(nameof(EnumTable.Services), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.@Customers):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[CusNo] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    if (responseCheck != null && responseCheck.StatusCode == -1)
                    {
                        response.StatusCode = -1;
                        response.Message = responseCheck.Message;
                        return response;
                    }
                    response = await deleteDataAsync(nameof(EnumTable.@Customers), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.Enums):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[EnumId] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    if (responseCheck != null && responseCheck.StatusCode == -1)
                    {
                        response.StatusCode = -1;
                        response.Message = responseCheck.Message;
                        return response;
                    }
                    response = await deleteDataAsync(nameof(EnumTable.Enums), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.Branchs):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[BranchId] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());


                    responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    if (responseCheck != null && responseCheck.StatusCode == -1)
                    {
                        response.StatusCode = -1;
                        response.Message = responseCheck.Message;
                        return response;
                    }
                    response = await deleteDataAsync(nameof(EnumTable.Enums), queryString, sqlParameters);
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
            await _context.RollbackAsync();
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }
    /// <summary>
    /// lấy kết quả báo cáo
    /// </summary>
    /// <param name="isAdmin"></param>
    /// <returns></returns>
    public async Task<ResponseModel> CheckKeyBindingBeforeDeleting(RequestModel request = null)
    {
        ResponseModel data = new ResponseModel();
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[2];
            sqlParameters[0] = new SqlParameter("@Type", request.Type); // tableName
            sqlParameters[1] = new SqlParameter("@Json", request.Json); // ds các id cần update isdelete = 1
            var results = await _context.GetDataSetAsync(Constants.BM_CHECK_KEY_BINDING_BEFORE_DELETE, sqlParameters, commandType: CommandType.StoredProcedure);
            if (results.Tables != null && results.Tables.Count > 0 && results.Tables[0].Rows.Count > 0)
            {
                data.StatusCode = int.Parse(results.Tables[0].Rows[0]["StatusCode"].ToString());
                data.Message = results.Tables[0].Rows[0]["Message"].ToString();
            }
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    #endregion Public Functions

    #region Private Funtions
    /// <summary>
    /// đọc kết quả từ strored chec dữ liệu trước khi xóa
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ResponseModel DataRecordCheckKeyBindingBeforeDeleteToResponseModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ResponseModel
        ResponseModel model = new();
        if (!Convert.IsDBNull(row["StatusCode"])) model.StatusCode = Convert.ToInt32(row["StatusCode"]);
        if (!Convert.IsDBNull(row["Message"])) model.Message = Convert.ToString(row["Message"]);
        return model;
    }


    /// <summary>
    /// xóa dữ liệu -> cập nhật cột IsDelete
    /// </summary>
    /// <returns></returns>
    private async Task<ResponseModel> deleteDataAsync(string pTableName, string pCondition, SqlParameter[] sqlParameters)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.BeginTranAsync();
            string queryString = @$"UPDATE [dbo].[{pTableName}] 
                                set [IsDelete] = 1, [ReasonDelete] = @ReasonDelete, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                where {pCondition}";

            var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
            if (data != null && data.Rows.Count > 0)
            {
                response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
            }

            if (response.StatusCode == 0) await _context.CommitTranAsync();
            else await _context.RollbackAsync();
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
            await _context.RollbackAsync(); 
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
        if (!Convert.IsDBNull(record["ListServiceType"])) branch.ListServiceType = Convert.ToString(record["ListServiceType"]);
        if (!Convert.IsDBNull(record["ListServiceType"])) branch.ListServiceTypes = branch.ListServiceType?.Split(",")?.ToList();
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
        if (!Convert.IsDBNull(record["ListServiceType"])) user.ListServiceType = Convert.ToString(record["ListServiceType"]);
        if (!Convert.IsDBNull(record["ListServiceType"])) user.ListServiceTypes = user.ListServiceType?.Split(",")?.ToList();
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
        if (!Convert.IsDBNull(record["EnumTypeName"])) model.EnumTypeName = Convert.ToString(record["EnumTypeName"]);
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
        if (!Convert.IsDBNull(record["BranchName"])) model.BranchName = Convert.ToString(record["BranchName"]);
        if (!Convert.IsDBNull(record["TotalDebtAmount"])) model.TotalDebtAmount = Convert.ToDouble(record["TotalDebtAmount"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return model;
    }

    /// <summary>
    /// đọc danh sách Services
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ServiceModel DataRecordToServiceModel(IDataRecord record)
    {
        ServiceModel model = new();
        if (!Convert.IsDBNull(record["ServiceCode"])) model.ServiceCode = Convert.ToString(record["ServiceCode"]);
        if (!Convert.IsDBNull(record["ServiceName"])) model.ServiceName = Convert.ToString(record["ServiceName"]);
        if (!Convert.IsDBNull(record["EnumId"])) model.EnumId = Convert.ToString(record["EnumId"]);
        if (!Convert.IsDBNull(record["EnumName"])) model.EnumName = Convert.ToString(record["EnumName"]);
        if (!Convert.IsDBNull(record["PackageId"])) model.PackageId = Convert.ToString(record["PackageId"]);
        if (!Convert.IsDBNull(record["PackageName"])) model.PackageName = Convert.ToString(record["PackageName"]);
        if (!Convert.IsDBNull(record["Description"])) model.Description = Convert.ToString(record["Description"]);
        if (!Convert.IsDBNull(record["WarrantyPeriod"])) model.WarrantyPeriod = Convert.ToDouble(record["WarrantyPeriod"]);
        if (!Convert.IsDBNull(record["QtyWarranty"])) model.QtyWarranty = Convert.ToInt16(record["QtyWarranty"]);
        if (!Convert.IsDBNull(record["Price"])) model.Price = Convert.ToDouble(record["Price"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        if (!Convert.IsDBNull(record["ListPromotionSupplies"])) model.ListPromotionSupplies = Convert.ToString(record["ListPromotionSupplies"]);
        if (!Convert.IsDBNull(record["ListPromotionSupplies"])) model.ListPromotionSuppliess = model.ListPromotionSupplies?.Split(",")?.ToList();
        if (!Convert.IsDBNull(record["IsOutBound"])) model.IsOutBound = Convert.ToBoolean(record["IsOutBound"]);
        return model;
    }


    /// <summary>
    /// lấy những thuộc tính cần thiết khi đăng nhập thành công
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private UserModel DataRecordToUserModelByLogin(IDataRecord record)
    {
        UserModel user = new();
        if (!Convert.IsDBNull(record["Id"])) user.Id = Convert.ToInt32(record["Id"]);
        if (!Convert.IsDBNull(record["EmpNo"])) user.EmpNo = Convert.ToString(record["EmpNo"]);
        if (!Convert.IsDBNull(record["UserName"])) user.UserName = Convert.ToString(record["UserName"]);
        if (!Convert.IsDBNull(record["FullName"])) user.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["IsAdmin"])) user.IsAdmin = Convert.ToBoolean(record["IsAdmin"]);
        if (!Convert.IsDBNull(record["BranchId"])) user.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["BranchName"])) user.BranchName = Convert.ToString(record["BranchName"]);
        return user;
    }


    /// <summary>
    /// đọc danh sách Vật tư
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private SuppliesModel DataRecordToSuppliesModel(IDataRecord record)
    {
        SuppliesModel suppplies = new();
        if (!Convert.IsDBNull(record["SuppliesCode"])) suppplies.SuppliesCode = Convert.ToString(record["SuppliesCode"]);
        if (!Convert.IsDBNull(record["SuppliesName"])) suppplies.SuppliesName = Convert.ToString(record["SuppliesName"]);
        if (!Convert.IsDBNull(record["EnumId"])) suppplies.EnumId = Convert.ToString(record["EnumId"]);
        if (!Convert.IsDBNull(record["EnumName"])) suppplies.EnumName = Convert.ToString(record["EnumName"]);
        if (!Convert.IsDBNull(record["DateCreate"])) suppplies.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) suppplies.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) suppplies.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) suppplies.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        if (!Convert.IsDBNull(record["UserNameCreate"])) suppplies.UserNameCreate = Convert.ToString(record["UserNameCreate"]);
        if (!Convert.IsDBNull(record["UserNameUpdate"])) suppplies.UserNameUpdate = Convert.ToString(record["UserNameUpdate"]);
        if (!Convert.IsDBNull(record["QtyInv"])) suppplies.QtyInv = Convert.ToDecimal(record["QtyInv"]);
        if (!Convert.IsDBNull(record["Price"])) suppplies.Price = Convert.ToDecimal(record["Price"]);
        if (!Convert.IsDBNull(record["QtyIntoInv"])) suppplies.QtyIntoInv = Convert.ToDecimal(record["QtyIntoInv"]);
        if (!Convert.IsDBNull(record["QtyOutBound"])) suppplies.QtyOutBound = Convert.ToDecimal(record["QtyOutBound"]);
        if (!Convert.IsDBNull(record["SuppliesTypeCode"])) suppplies.SuppliesTypeCode = Convert.ToString(record["SuppliesTypeCode"]);
        if (!Convert.IsDBNull(record["SuppliesTypeName"])) suppplies.SuppliesTypeName = Convert.ToString(record["SuppliesTypeName"]);
        if (!Convert.IsDBNull(record["Type"]))
        {
            suppplies.Type = Convert.ToString(record["Type"]);
            switch (suppplies.Type)
            {
                case nameof(SuppliesKind.Popular):
                    suppplies.TypeName = "Phổ thông";
                    break;
                case nameof(SuppliesKind.Promotion):
                    suppplies.TypeName = "Khuyến mãi";
                    break;
                case nameof(SuppliesKind.Ink):
                    suppplies.TypeName = "Mực - Loại tê";
                    break;
            }
        }
        return suppplies;
    }

    /// <summary>
    /// đọc danh sách vật tư tồn kho
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private SuppliesModel DataRecordToSuppliesOutBoundModel(IDataRecord record)
    {
        SuppliesModel suppplies = new();
        if (!Convert.IsDBNull(record["SuppliesCode"])) suppplies.SuppliesCode = Convert.ToString(record["SuppliesCode"]);
        if (!Convert.IsDBNull(record["SuppliesName"])) suppplies.SuppliesName = Convert.ToString(record["SuppliesName"]);
        if (!Convert.IsDBNull(record["EnumId"])) suppplies.EnumId = Convert.ToString(record["EnumId"]);
        if (!Convert.IsDBNull(record["EnumName"])) suppplies.EnumName = Convert.ToString(record["EnumName"]);
        if (!Convert.IsDBNull(record["QtyInv"])) suppplies.QtyInv = Convert.ToDecimal(record["QtyInv"]);
        if (!Convert.IsDBNull(record["Price"])) suppplies.Price = Convert.ToDecimal(record["Price"]);
        return suppplies;
    }


    /// <summary>
    /// đoc danh sách bảng giá
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private PriceModel DataRecordToPriceModel(IDataRecord record)
    {
        PriceModel model = new PriceModel();
        if (!Convert.IsDBNull(record["Id"])) model.Id = Convert.ToInt32(record["Id"]);
        if (!Convert.IsDBNull(record["ServiceCode"])) model.ServiceCode = Convert.ToString(record["ServiceCode"]);
        if (!Convert.IsDBNull(record["Price"])) model.Price = Convert.ToDouble(record["Price"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        if (!Convert.IsDBNull(record["IsActive"])) model.IsActive = Convert.ToBoolean(record["IsActive"]);
        return model;
    }

    /// <summary>
    /// đọc danh sách tồn lại
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private InvetoryModel DataRecordToInvetoryModel(IDataRecord record)
    {
        InvetoryModel inv = new();
        if (!Convert.IsDBNull(record["ABSID"])) inv.Absid = Convert.ToInt32(record["ABSID"]);
        if (!Convert.IsDBNull(record["SuppliesCode"])) inv.SuppliesCode = Convert.ToString(record["SuppliesCode"]);
        if (!Convert.IsDBNull(record["SuppliesName"])) inv.SuppliesName = Convert.ToString(record["SuppliesName"]);
        if (!Convert.IsDBNull(record["EnumId"])) inv.EnumId = Convert.ToString(record["EnumId"]);
        if (!Convert.IsDBNull(record["EnumName"])) inv.EnumName = Convert.ToString(record["EnumName"]);
        if (!Convert.IsDBNull(record["DateCreate"])) inv.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) inv.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) inv.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) inv.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        if (!Convert.IsDBNull(record["UserNameCreate"])) inv.UserNameCreate = Convert.ToString(record["UserNameCreate"]);
        if (!Convert.IsDBNull(record["UserNameUpdate"])) inv.UserNameUpdate = Convert.ToString(record["UserNameUpdate"]);
        if (!Convert.IsDBNull(record["BranchId"])) inv.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["QtyInv"])) inv.QtyInv = Convert.ToDecimal(record["QtyInv"]);
        if (!Convert.IsDBNull(record["Price"])) inv.Price = Convert.ToDecimal(record["Price"]);
        return inv;
    }

    /// <summary>
    /// đoc danh sách Phác đồ điều trị
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private TreatmentRegimenModel DataRecordToTreatmentRigimenModel(IDataRecord record)
    {
        TreatmentRegimenModel model = new TreatmentRegimenModel();
        if (!Convert.IsDBNull(record["Id"])) model.Id = Convert.ToInt32(record["Id"]);
        if (!Convert.IsDBNull(record["LineNum"])) model.LineNum = Convert.ToInt32(record["LineNum"]);
        if (!Convert.IsDBNull(record["ServiceCode"])) model.ServiceCode = Convert.ToString(record["ServiceCode"]);
        if (!Convert.IsDBNull(record["Name"])) model.Name = Convert.ToString(record["Name"]);
        if (!Convert.IsDBNull(record["Title"])) model.Title = Convert.ToString(record["Title"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return model;
    }
    #endregion
}
