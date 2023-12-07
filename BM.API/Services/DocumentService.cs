using BM.API.Infrastructure;
using BM.Models;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using BM.Models.Shared;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace BM.API.Services;

public interface IDocumentService
{
    Task<ResponseModel> UpdateSalesOrder(RequestModel pRequest);
    Task<IEnumerable<DocumentModel>> GetSalesOrdersAsync(SearchModel pSearchData);
    Task<Dictionary<string, string>?> GetDocumentById(int pDocEntry);
}
public class DocumentService : IDocumentService
{
    private readonly IBMDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    public DocumentService(IBMDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    /// <summary>
    /// lấy danh sách đơn hàng
    /// </summary>
    /// <param name="isAdmin"></param>
    /// <returns></returns>
    public async Task<IEnumerable<DocumentModel>> GetSalesOrdersAsync(SearchModel pSearchData)
    {
        IEnumerable<DocumentModel> data;
        try
        {
            await _context.Connect();
            if (pSearchData.FromDate == null) pSearchData.FromDate = new DateTime(2023, 01, 01);
            if (pSearchData.ToDate == null) pSearchData.ToDate = _dateTimeService.GetCurrentVietnamTime();
            SqlParameter[] sqlParameters = new SqlParameter[5];
            sqlParameters[0] = new SqlParameter("@StatusId", pSearchData.StatusId);
            sqlParameters[1] = new SqlParameter("@FromDate", pSearchData.FromDate.Value.Date);
            sqlParameters[2] = new SqlParameter("@ToDate", pSearchData.ToDate.Value.Date);
            sqlParameters[3] = new SqlParameter("@IsAdmin", pSearchData.IsAdmin);
            sqlParameters[4] = new SqlParameter("@UserId", pSearchData.UserId);
            data = await _context.GetDataAsync(@$"select [DocEntry],[DiscountCode],[Total],[GuestsPay],[NoteForAll],[StatusId],[Debt],[BaseEntry],[VoucherNo]
                            ,T1.[BranchId],T1.[BranchName],T0.[CusNo],T2.[FullName],T2.[Phone1],T2.[Remark]
                            ,case [StatusId]  when '{nameof(DocStatus.Closed)}' then N'Hoàn thành' else N'Chờ xử lý' end as [StatusName]
                            ,T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate]
                      from [dbo].[Drafts] as T0 with(nolock) 
                inner join [dbo].[Branchs] as T1 with(nolock) on T0.BranchId = T1.BranchId
                inner join [dbo].[Customers] as T2 with(nolock) on T0.CusNo = T2.CusNo
                     where cast(T0.[DateCreate] as Date) between @FromDate and @ToDate
                           and (@StatusId = 'All' or (@StatusId <> 'All' and T0.[StatusId] = @StatusId))
                           and (@IsAdmin = 1 or (@IsAdmin <> 1 and T0.[UserCreate] = @UserId))
                  order by [DocEntry] desc"
                    , DataRecordToDocumentModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// lấy danh sách chi tiết đơn hàng
    /// </summary>
    /// <param name="pDocEntry"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>?> GetDocumentById(int pDocEntry)
    {
        Dictionary<string, string>? data = null;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@DocEntry", pDocEntry);
            string queryString = @$"select T0.[DocEntry],[DiscountCode],[Total],[GuestsPay],[NoteForAll],[StatusId],[Debt],[BaseEntry],[VoucherNo],T0.[StatusBefore],T0.[HealthStatus]
            ,T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate],case [StatusId] when '{nameof(DocStatus.Closed)}' then N'Hoàn thành' else N'Chờ xử lý' end as [StatusName]
            ,T1.[Id],T1.[Price],T1.[Qty],T1.[LineTotal],T1.[ActionType],T1.[ConsultUserId],T1.[ImplementUserId],T1.[ChemicalFormula],T1.[WarrantyPeriod],T1.[QtyWarranty]
            ,T2.[BranchId],T2.[BranchName],T4.[ServiceCode],T4.[ServiceName]
	        ,T3.[CusNo],T3.[FullName],T3.[DateOfBirth],T3.CINo,T3.Phone1,T3.[Phone2],T3.Zalo,T3.FaceBook,T3.[Address],T3.[Remark]
            ,isnull((select top 1 Price from [dbo].[Prices] with(nolock) where [ServiceCode] = T1.[ServiceCode] and [IsActive]= 1 order by [IsActive] desc, [DateUpdate] desc), 0) as [PriceOld]
            ,(select string_agg(EnumName, ', ') from [dbo].[Enums] as T00 with(nolock) where T00.EnumType = 'SkinType' and T3.SkinType like '%""'+T00.EnumId+'""%') as [SkinType]
        from [dbo].[Drafts] as T0 with(nolock) 
        inner join [dbo].[DraftDetails] as T1 with(nolock) on T0.DocEntry = T1.DocEntry
        inner join [dbo].[Branchs] as T2 with(nolock) on T0.BranchId = T2.BranchId
        inner join [dbo].[Customers] as T3 with(nolock) on T0.CusNo = T3.CusNo
        inner join [dbo].[Services] as T4 with(nolock) on T1.ServiceCode = T4.ServiceCode
        where T0.DocEntry = @DocEntry order by T0.[DocEntry] desc";

            var ds = await _context.GetDataSetAsync(queryString, sqlParameters, CommandType.Text);
            if(ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                string DATA_CUSTOMER_EMPTY = "Chưa cập nhật";
                DataTable dt = ds.Tables[0];
                DataRow dr = dt.Rows[0];
                DocumentModel oHeader = new DocumentModel();
                oHeader.BranchId = Convert.ToString(dr["BranchId"]);
                oHeader.BranchName = Convert.ToString(dr["BranchName"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.CusNo = Convert.ToString(dr["CusNo"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.FullName = Convert.ToString(dr["FullName"]) ?? DATA_CUSTOMER_EMPTY;
                if (!Convert.IsDBNull(dr["DateOfBirth"])) oHeader.DateOfBirth = Convert.ToDateTime(dr["DateOfBirth"]);
                oHeader.CINo = Convert.ToString(dr["CINo"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.Phone1 = Convert.ToString(dr["Phone1"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.Zalo =  Convert.ToString(dr["Zalo"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.FaceBook = Convert.ToString(dr["FaceBook"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.Address = Convert.ToString(dr["Address"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.Remark = Convert.ToString(dr["Remark"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.SkinType = Convert.ToString(dr["SkinType"]) ?? DATA_CUSTOMER_EMPTY;
                oHeader.GuestsPay = Convert.ToDouble(dr["GuestsPay"]);
                oHeader.Debt = Convert.ToDouble(dr["Debt"]);
                oHeader.DocEntry = Convert.ToInt32(dr["DocEntry"]);
                oHeader.BaseEntry = Convert.ToInt32(dr["BaseEntry"]);
                oHeader.DiscountCode = Convert.ToString(dr["DiscountCode"]);
                oHeader.NoteForAll = Convert.ToString(dr["NoteForAll"]);
                oHeader.StatusId = Convert.ToString(dr["StatusId"]);
                oHeader.StatusName = Convert.ToString(dr["StatusName"]);
                oHeader.VoucherNo = Convert.ToString(dr["VoucherNo"]);
                oHeader.StatusBefore = Convert.ToString(dr["StatusBefore"]);
                oHeader.HealthStatus = Convert.ToString(dr["HealthStatus"]);
                if (!Convert.IsDBNull(dr["DateCreate"])) oHeader.DateCreate = Convert.ToDateTime(dr["DateCreate"]);
                if (!Convert.IsDBNull(dr["UserCreate"])) oHeader.UserCreate = Convert.ToInt32(dr["UserCreate"]);
                if (!Convert.IsDBNull(dr["DateUpdate"])) oHeader.DateUpdate = Convert.ToDateTime(dr["DateUpdate"]);
                if (!Convert.IsDBNull(dr["UserUpdate"])) oHeader.UserUpdate = Convert.ToInt32(dr["UserUpdate"]);
                List<DocumentDetailModel> lstDetails = new List<DocumentDetailModel>();
                foreach(DataRow item in dt.Rows)
                {
                    DocumentDetailModel oLine = new DocumentDetailModel();
                    oLine.ServiceCode = Convert.ToString(item["ServiceCode"]);
                    oLine.ServiceName = Convert.ToString(item["ServiceName"]);
                    oLine.Id = Convert.ToInt32(item["Id"]);
                    oLine.Qty = Convert.ToInt32(item["Qty"]);
                    oLine.QtyWarranty = Convert.ToInt32(item["QtyWarranty"]);
                    oLine.Price = Convert.ToDouble(item["Price"]);
                    oLine.PriceOld = Convert.ToDouble(item["PriceOld"]);
                    oLine.LineTotal = Convert.ToDouble(item["LineTotal"]);
                    oLine.WarrantyPeriod = Convert.ToDouble(item["WarrantyPeriod"]);
                    oLine.ActionType = Convert.ToString(item["ActionType"]);
                    oLine.ConsultUserId = Convert.ToString(item["ConsultUserId"]);
                    oLine.ImplementUserId = Convert.ToString(item["ImplementUserId"]);
                    oLine.ChemicalFormula = Convert.ToString(item["ChemicalFormula"]);
                    lstDetails.Add(oLine);
                }    
                data = new Dictionary<string, string>()
                {
                    {"oHeader", JsonConvert.SerializeObject(oHeader)},
                    {"oLine", JsonConvert.SerializeObject(lstDetails)}
                };
            }
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// cập nhật danh sách đơn hàng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateSalesOrder(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            bool isUpdated = false;
            DocumentModel oDraft = JsonConvert.DeserializeObject<DocumentModel>(pRequest.Json + "")!;
            List<DocumentDetailModel> lstDraftDetails = JsonConvert.DeserializeObject<List<DocumentDetailModel>>(pRequest.JsonDetail + "");
            SqlParameter[] sqlParameters = new SqlParameter[1];
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
                    int iDocentry = await _context.ExecuteScalarAsync("select isnull(max(DocEntry), 0) + 1 from [dbo].[Drafts] with(nolock)");
                    if (oDraft.StatusId == nameof(DocStatus.Closed))
                    {
                        // nếu status == Closed -> đánh mã chứng từ
                        sqlParameters = new SqlParameter[1];
                        sqlParameters[0] = new SqlParameter("@Type", "Drafts");
                        oDraft.VoucherNo = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy số hóa đơn;
                    }
                    queryString = @"Insert into [dbo].[Drafts] ([DocEntry],[CusNo],[DiscountCode],[Total],[GuestsPay], [Debt],[StatusBefore], [VoucherNo]
                                   ,[BranchId], [BaseEntry],[HealthStatus],[NoteForAll],[StatusId],[DateCreate],[UserCreate],[IsDelete])
                                    values (@DocEntry, @CusNo, @DiscountCode, @Total, @GuestsPay, @Debt, @StatusBefore, @VoucherNo
                                   ,@BranchId,@BaseEntry,@HealthStatus, @NoteForAll, @StatusId, @DateTimeNow, @UserId, 0)";

                    sqlParameters = new SqlParameter[15];
                    sqlParameters[0] = new SqlParameter("@DocEntry", iDocentry);
                    sqlParameters[1] = new SqlParameter("@CusNo", oDraft.CusNo);
                    sqlParameters[2] = new SqlParameter("@DiscountCode", oDraft.DiscountCode ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@Total", oDraft.Total);
                    sqlParameters[4] = new SqlParameter("@GuestsPay", oDraft.GuestsPay);
                    sqlParameters[5] = new SqlParameter("@Debt", oDraft.Debt);
                    sqlParameters[6] = new SqlParameter("@StatusBefore", oDraft.StatusBefore ?? (object)DBNull.Value);
                    sqlParameters[7] = new SqlParameter("@HealthStatus", oDraft.HealthStatus ?? (object)DBNull.Value);
                    sqlParameters[8] = new SqlParameter("@NoteForAll", oDraft.NoteForAll ?? (object)DBNull.Value);
                    sqlParameters[9] = new SqlParameter("@StatusId", oDraft.StatusId ?? (object)DBNull.Value);
                    sqlParameters[10] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[11] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[12] = new SqlParameter("@BranchId", oDraft.BranchId);
                    sqlParameters[13] = new SqlParameter("@BaseEntry", oDraft.BaseEntry);
                    sqlParameters[14] = new SqlParameter("@VoucherNo", oDraft.VoucherNo);
                    await _context.BeginTranAsync();
                    isUpdated = await ExecQuery();
                    if(isUpdated)
                    {
                        
                        foreach(var oDraftDetails in lstDraftDetails)
                        {
                            int iDrftId = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[DraftDetails] with(nolock)");
                            queryString = @"Insert into [dbo].[DraftDetails] ([Id],[ServiceCode],[Qty], [Price],[LineTotal],[DocEntry], [ActionType],[ConsultUserId]
                                   ,[ImplementUserId],[ChemicalFormula],[WarrantyPeriod],[QtyWarranty],[DateCreate],[UserCreate],[IsDelete])
                                    values (@Id, @ServiceCode, @Qty, @Price, @LineTotal, @DocEntry, @ActionType, @ConsultUserId
                                   ,@ImplementUserId, @ChemicalFormula,@WarrantyPeriod, @QtyWarranty, @DateTimeNow, @UserId, 0)";

                            sqlParameters = new SqlParameter[14];
                            sqlParameters[0] = new SqlParameter("@Id", iDrftId);
                            sqlParameters[1] = new SqlParameter("@ServiceCode", oDraftDetails.ServiceCode);
                            sqlParameters[2] = new SqlParameter("@Qty", oDraftDetails.Qty);
                            sqlParameters[3] = new SqlParameter("@Price", oDraftDetails.Price);
                            sqlParameters[4] = new SqlParameter("@LineTotal", oDraftDetails.LineTotal);
                            sqlParameters[5] = new SqlParameter("@DocEntry", iDocentry);
                            sqlParameters[6] = new SqlParameter("@ActionType", oDraftDetails.ActionType ?? (object)DBNull.Value);
                            sqlParameters[7] = new SqlParameter("@ConsultUserId", oDraftDetails.ConsultUserId ?? (object)DBNull.Value);
                            sqlParameters[8] = new SqlParameter("@ImplementUserId", oDraftDetails.ImplementUserId ?? (object)DBNull.Value);
                            sqlParameters[9] = new SqlParameter("@ChemicalFormula", oDraftDetails.ChemicalFormula ?? (object)DBNull.Value);
                            sqlParameters[10] = new SqlParameter("@UserId", pRequest.UserId);
                            sqlParameters[11] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                            sqlParameters[12] = new SqlParameter("@WarrantyPeriod", oDraftDetails.WarrantyPeriod);
                            sqlParameters[13] = new SqlParameter("@QtyWarranty", oDraftDetails.QtyWarranty);
                            isUpdated = await ExecQuery();
                            if(!isUpdated)
                            {
                                await _context.RollbackAsync();
                                break;
                            }    
                        }    
                        if(isUpdated) await _context.CommitTranAsync();
                        break;
                    }
                    await _context.RollbackAsync();
                    break;
                case nameof(EnumType.Update):
                    if (oDraft.StatusId == nameof(DocStatus.Closed) && string.IsNullOrWhiteSpace(oDraft.VoucherNo))
                    {
                        // nếu status == Closed & chưa có mã chứng từ -> đánh mã chứng từ
                        sqlParameters = new SqlParameter[1];
                        sqlParameters[0] = new SqlParameter("@Type", "Drafts");
                        oDraft.VoucherNo = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy số hóa đơn;
                    }
                    // kiểm tra mã lệnh
                    queryString = @"Update [dbo].[Drafts]
                                       set [DiscountCode] = @DiscountCode, [Total] = @Total, [GuestsPay] = @GuestsPay
                                         , [StatusBefore] = @StatusBefore, [HealthStatus] = @HealthStatus, [NoteForAll] = @NoteForAll
                                         , [StatusId] = @StatusId, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                         , [Debt] = @Debt
                                     where [DocEntry] = @DocEntry";
                    sqlParameters = new SqlParameter[11];
                    sqlParameters[0] = new SqlParameter("@DocEntry", oDraft.DocEntry);
                    sqlParameters[1] = new SqlParameter("@DiscountCode", oDraft.DiscountCode ?? (object)DBNull.Value);
                    sqlParameters[2] = new SqlParameter("@Total", oDraft.Total);
                    sqlParameters[3] = new SqlParameter("@GuestsPay", oDraft.GuestsPay);
                    sqlParameters[4] = new SqlParameter("@Debt", oDraft.Debt);
                    sqlParameters[5] = new SqlParameter("@StatusBefore", oDraft.StatusBefore ?? (object)DBNull.Value);
                    sqlParameters[6] = new SqlParameter("@HealthStatus", oDraft.HealthStatus ?? (object)DBNull.Value);
                    sqlParameters[7] = new SqlParameter("@NoteForAll", oDraft.NoteForAll ?? (object)DBNull.Value);
                    sqlParameters[8] = new SqlParameter("@StatusId", oDraft.StatusId ?? (object)DBNull.Value);
                    sqlParameters[9] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[10] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    await _context.BeginTranAsync();
                    isUpdated = await ExecQuery();
                    if(isUpdated)
                    {
                        foreach (var oDraftDetails in lstDraftDetails)
                        {
                            sqlParameters = new SqlParameter[14];
                            sqlParameters[0] = new SqlParameter("@ServiceCode", oDraftDetails.ServiceCode);
                            sqlParameters[1] = new SqlParameter("@Qty", oDraftDetails.Qty);
                            sqlParameters[2] = new SqlParameter("@Price", oDraftDetails.Price);
                            sqlParameters[3] = new SqlParameter("@LineTotal", oDraftDetails.LineTotal);
                            sqlParameters[4] = new SqlParameter("@ActionType", oDraftDetails.ActionType ?? (object)DBNull.Value);
                            sqlParameters[5] = new SqlParameter("@ConsultUserId", oDraftDetails.ConsultUserId ?? (object)DBNull.Value);
                            sqlParameters[6] = new SqlParameter("@ImplementUserId", oDraftDetails.ImplementUserId ?? (object)DBNull.Value);
                            sqlParameters[7] = new SqlParameter("@ChemicalFormula", oDraftDetails.ChemicalFormula ?? (object)DBNull.Value);
                            sqlParameters[8] = new SqlParameter("@UserId", pRequest.UserId);
                            sqlParameters[9] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                            sqlParameters[10] = new SqlParameter("@WarrantyPeriod", oDraftDetails.WarrantyPeriod);
                            sqlParameters[11] = new SqlParameter("@QtyWarranty", oDraftDetails.QtyWarranty);
                            sqlParameters[12] = new SqlParameter("@DocEntry", oDraft.DocEntry);
                            if (oDraftDetails.Id <= 0)
                            {
                                // thêm mới
                                oDraftDetails.Id = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[DraftDetails] with(nolock)"); // gán lại Id
                                queryString = @"Insert into [dbo].[DraftDetails] ([Id],[ServiceCode],[Qty], [Price],[LineTotal],[DocEntry], [ActionType],[ConsultUserId]
                                   ,[ImplementUserId],[ChemicalFormula],[WarrantyPeriod],[QtyWarranty],[DateCreate],[UserCreate],[IsDelete])
                                    values (@Id, @ServiceCode, @Qty, @Price, @LineTotal, @DocEntry, @ActionType, @ConsultUserId
                                   ,@ImplementUserId, @ChemicalFormula,@WarrantyPeriod, @QtyWarranty, @DateTimeNow, @UserId, 0)";
                                sqlParameters[13] = new SqlParameter("@Id", oDraftDetails.Id);
                                
                            }  
                            else
                            {
                                // cập nhật
                                queryString = @"Update [dbo].[DraftDetails]
                                                   set [ServiceCode] = @ServiceCode, [Qty] = @Qty, [Price] = @Price, [LineTotal] = @LineTotal, [ActionType] = @ActionType
                                                     , [ConsultUserId] = @ConsultUserId, [ImplementUserId] = @ImplementUserId,[ChemicalFormula] = @ChemicalFormula
                                                     , [WarrantyPeriod] = @WarrantyPeriod, [QtyWarranty] = @QtyWarranty, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                                 where [Id] = @Id";
                                sqlParameters[13] = new SqlParameter("@Id", oDraftDetails.Id);
                            }
                            isUpdated = await ExecQuery();
                            if (!isUpdated)
                            {
                                await _context.RollbackAsync();
                                break;
                            }
                        }

                        // xóa các dòng không tồn tại trong danh sách Ids
                        queryString = "Delete from [dbo].[DraftDetails] where [Id] not in ( select value from STRING_SPLIT(@ListIds, ',') )  and [DocEntry] = @DocEntry";
                        sqlParameters = new SqlParameter[2];
                        sqlParameters[0] = new SqlParameter("@ListIds", string.Join(",", lstDraftDetails.Select(m=>m.Id).Distinct()));
                        sqlParameters[1] = new SqlParameter("@DocEntry", oDraft.DocEntry);
                        await _context.DeleteDataAsync(queryString, sqlParameters);
                        if (isUpdated) await _context.CommitTranAsync();
                        break;
                    }    
                    await _context.RollbackAsync();
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

    #region Private Funtions

    /// <summary>
    /// đọc danh sách đơn hàng
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private DocumentModel DataRecordToDocumentModel(IDataRecord record)
    {
        DocumentModel model = new();
        if (!Convert.IsDBNull(record["DocEntry"])) model.DocEntry = Convert.ToInt32(record["DocEntry"]);
        if (!Convert.IsDBNull(record["BaseEntry"])) model.BaseEntry = Convert.ToInt32(record["BaseEntry"]);
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["DiscountCode"])) model.DiscountCode = Convert.ToString(record["DiscountCode"]);
        if (!Convert.IsDBNull(record["Total"])) model.Total = Convert.ToDouble(record["Total"]);
        if (!Convert.IsDBNull(record["GuestsPay"])) model.GuestsPay = Convert.ToDouble(record["GuestsPay"]);
        if (!Convert.IsDBNull(record["Debt"])) model.Debt = Convert.ToDouble(record["Debt"]);
        if (!Convert.IsDBNull(record["StatusId"])) model.StatusId = Convert.ToString(record["StatusId"]);
        if (!Convert.IsDBNull(record["StatusName"])) model.StatusName = Convert.ToString(record["StatusName"]);
        if (!Convert.IsDBNull(record["NoteForAll"])) model.NoteForAll = Convert.ToString(record["NoteForAll"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["Phone1"])) model.Phone1 = Convert.ToString(record["Phone1"]);
        if (!Convert.IsDBNull(record["Remark"])) model.Remark = Convert.ToString(record["Remark"]);
        if (!Convert.IsDBNull(record["BranchId"])) model.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["BranchName"])) model.BranchName = Convert.ToString(record["BranchName"]);
        if (!Convert.IsDBNull(record["VoucherNo"])) model.VoucherNo = Convert.ToString(record["VoucherNo"]);

        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return model;
    }
    #endregion
}