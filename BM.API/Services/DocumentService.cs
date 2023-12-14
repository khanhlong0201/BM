using BM.API.Infrastructure;
using BM.Models;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Net;
using BM.Models.Shared;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using BM.API.Commons;
namespace BM.API.Services;

public interface IDocumentService
{
    Task<ResponseModel> UpdateSalesOrder(RequestModel pRequest);
    Task<IEnumerable<DocumentModel>> GetSalesOrdersAsync(SearchModel pSearchData);
    Task<Dictionary<string, string>?> GetDocumentById(int pDocEntry);
    Task<IEnumerable<DocumentModel>> GetSalesOrderClosedByGuest(string pCusNo);
    Task<ResponseModel> CancleDocList(RequestModel pRequest);
    Task<IEnumerable<ReportModel>> GetReportAsync(RequestReportModel pSearchData);
    Task<IEnumerable<SheduleModel>> ReminderByMonthAsync(SearchModel pSearchData);
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
            sqlParameters[1] = new SqlParameter("@FromDate", pSearchData.FromDate.Value);
            sqlParameters[2] = new SqlParameter("@ToDate", pSearchData.ToDate.Value);
            sqlParameters[3] = new SqlParameter("@IsAdmin", pSearchData.IsAdmin);
            sqlParameters[4] = new SqlParameter("@UserId", pSearchData.UserId);
            data = await _context.GetDataAsync(@$"select [DocEntry],[DiscountCode],[Total],[GuestsPay],[NoteForAll],[StatusId],[Debt],[BaseEntry],[VoucherNo]
                            ,T1.[BranchId],T1.[BranchName],T0.[CusNo],T2.[FullName],T2.[Phone1],T2.[Remark]
                            ,case [StatusId]  when '{nameof(DocStatus.Closed)}' then N'Hoàn thành'
                             when '{nameof(DocStatus.Cancled)}' then N'Đã hủy đơn'
                             else N'Chờ xử lý' end as [StatusName]
                            ,T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate], T0.[ReasonDelete]
                      from [dbo].[Drafts] as T0 with(nolock) 
                inner join [dbo].[Branchs] as T1 with(nolock) on T0.BranchId = T1.BranchId
                inner join [dbo].[Customers] as T2 with(nolock) on T0.CusNo = T2.CusNo
                     where cast(T0.[DateCreate] as Date) between cast(@FromDate as Date) and cast(@ToDate as Date)
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
            ,T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate], T0.[ReasonDelete]
            ,case [StatusId] when '{nameof(DocStatus.Closed)}' then N'Hoàn thành' when '{nameof(DocStatus.Cancled)}' then N'Đã hủy đơn' else N'Chờ xử lý' end as [StatusName]
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
                oHeader.ReasonDelete = Convert.ToString(dr["ReasonDelete"]);
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
            async Task saveDebts(int pDocEntry)
            {
                // lưu vào công nợ khách hàng
                if (oDraft.StatusId == nameof(DocStatus.Closed) && oDraft.Debt > 0)
                {
                    // lấy mã
                    int iIdDebts = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[CustomerDebts] with(nolock)");

                    queryString = @"Insert into [dbo].[CustomerDebts] ([Id],[DocEntry],[CusNo], [GuestsPay],[TotalDebtAmount],[DateCreate],[UserCreate])
                                                values (@Id, @DocEntry, @CusNo, @GuestsPay, @TotalDebtAmount, @DateTimeNow, @UserId)";
                    sqlParameters = new SqlParameter[7];
                    sqlParameters[0] = new SqlParameter("@Id", iIdDebts);
                    sqlParameters[1] = new SqlParameter("@DocEntry", pDocEntry);
                    sqlParameters[2] = new SqlParameter("@CusNo", oDraft.CusNo);
                    sqlParameters[3] = new SqlParameter("@TotalDebtAmount", oDraft.Debt);
                    sqlParameters[4] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[6] = new SqlParameter("@GuestsPay", oDraft.GuestsPay);
                    await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                }
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    int iDocentry = await _context.ExecuteScalarAsync("select isnull(max(DocEntry), 0) + 1 from [dbo].[Drafts] with(nolock)");
                    // nếu status == Closed -> đánh mã chứng từ
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@Type", "Drafts");
                    oDraft.VoucherNo = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy số phiếu
                    queryString = @"Insert into [dbo].[Drafts] ([DocEntry],[CusNo],[DiscountCode],[Total],[GuestsPay], [Debt],[StatusBefore], [VoucherNo]
                                   ,[BranchId], [BaseEntry],[HealthStatus],[NoteForAll],[StatusId],[DateCreate],[UserCreate],[DateUpdate],[IsDelete])
                                    values (@DocEntry, @CusNo, @DiscountCode, @Total, @GuestsPay, @Debt, @StatusBefore, @VoucherNo
                                   ,@BranchId,@BaseEntry,@HealthStatus, @NoteForAll, @StatusId, @DateTimeNow, @UserId, @DateTimeNow, 0)";

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
                        // lưu vào công nợ khách hàng
                        await saveDebts(iDocentry);
                        if (isUpdated) await _context.CommitTranAsync();
                        break;
                    }
                    await _context.RollbackAsync();
                    break;
                case nameof(EnumType.Update):
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

                        // lưu vào công nợ khách hàng
                        await saveDebts(oDraft.DocEntry);
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

    /// <summary>
    /// lấy danh sách đơn hàng hoàn thành theo khách hàng
    /// </summary>
    /// <param name="pCusNo"></param>
    /// <returns></returns>
    public async Task<IEnumerable<DocumentModel>> GetSalesOrderClosedByGuest(string pCusNo)
    {
        IEnumerable<DocumentModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@CusNo", pCusNo);
            data = await _context.GetDataAsync(@$"Select [DocEntry],[VoucherNo],[Total],[GuestsPay],[NoteForAll],[StatusId],[Debt],[BaseEntry], T1.[BranchId],T1.[BranchName]
                            ,N'Hoàn thành' as [StatusName],T0.[DateCreate],T2.[FullName] as [UserNameCreate]
                            ,(select string_agg(CONCAT(T00.ServiceName, ' ', T02.EnumName), ', ') 
                                from [dbo].[Services] as T00 with(nolock)
						  inner join [dbo].[DraftDetails] as T01 with(nolock) on T00.ServiceCode = T01.ServiceCode 
						   left join [dbo].[Enums] as T02 with(nolock) on T00.PackageId = T02.EnumId
							where T01.DocEntry = T0.DocEntry) as [Service]
                      from [dbo].[Drafts] as T0 with(nolock) 
                inner join [dbo].[Branchs] as T1 with(nolock) on T0.BranchId = T1.BranchId
                 left join [dbo].[Users] as T2 with(nolock) on T0.UserCreate = T2.Id
                     where T0.[StatusId] = '{nameof(DocStatus.Closed)}' and T0.[CusNo] = @CusNo
                  order by [DateCreate] desc"
                    , DataRecordToDocumentByGuestModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }
    
    public async Task<ResponseModel> CancleDocList(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters;
            string queryString = @$"UPDATE [dbo].[Drafts] 
                                      set [StatusId] = '{nameof(DocStatus.Cancled)}', [ReasonDelete] = @ReasonDelete, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                    where [DocEntry] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
            sqlParameters = new SqlParameter[4];
            sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
            sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
            sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
            sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
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
    public async Task<IEnumerable<ReportModel>> GetReportAsync(RequestReportModel pSearchData)
    {
        List<ReportModel> data = new List<ReportModel>();
        try
        {
            await _context.Connect();
            if (pSearchData.FromDate == null) pSearchData.FromDate = new DateTime(2023, 01, 01);
            if (pSearchData.ToDate == null) pSearchData.ToDate = _dateTimeService.GetCurrentVietnamTime();
            SqlParameter[] sqlParameters = new SqlParameter[6];
            sqlParameters[0] = new SqlParameter("@FromDate", pSearchData.FromDate.Value);
            sqlParameters[1] = new SqlParameter("@ToDate", pSearchData.ToDate.Value);
            sqlParameters[2] = new SqlParameter("@BranchId", pSearchData.BranchId);
            sqlParameters[3] = new SqlParameter("@TypeTime", pSearchData.TypeTime);
            sqlParameters[4] = new SqlParameter("@Type", pSearchData.Type);
            sqlParameters[5] = new SqlParameter("@UserId", pSearchData.UserId);
            var results = await _context.GetDataSetAsync(Constants.STORE_REPORT_ALL, sqlParameters, commandType: CommandType.StoredProcedure);
            if (results.Tables != null && results.Tables.Count > 0)
            {
                foreach (DataRow row in results.Tables[0].Rows)
                {
                    switch (pSearchData.Type+"")
                    {
                        case "DoanhThuQuiThangTheoDichVu":
                            data.Add(DataRecordDoanhThuQuiThangTheoDichVuToReportModel(row));
                            break;
                        case "DoanhThuQuiThangTheoLoaiDichVu":
                            data.Add(DataRecordDoanhThuQuiThangTheoLoaiDichVuToReportModel(row));
                            break;
                        case "DoanhThuTheoDichVu":
                            data.Add(DataRecordDoanhThuTheoDichVuToReportModel(row));
                            break;
                        case "DoanhThuTheoLoaiDichVu":
                            data.Add(DataRecordDoanhThuTheoLoaiDichVuToReportModel(row));
                            break;
                        case "DoanhThuQuiThangTheoNhanVienTuVan":
                            data.Add(DataRecordDoanhThuQuiThangTheoNhanVienTuVanToReportModel(row));
                            break;
                        case "DoanhThuQuiThangTheoNhanVienThucHien":
                            data.Add(DataRecordDoanhThuQuiThangTheoNhanVienThucHienToReportModel(row));
                            break;
                        default:
                            break;
                    }
                }
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
    /// nhắc nợ, nhắc liệu trình
    /// </summary>
    /// <param name="pSearchData"></param>
    /// <returns></returns>
    public async Task<IEnumerable<SheduleModel>> ReminderByMonthAsync(SearchModel pSearchData)
    {
        IEnumerable<SheduleModel> data;
        try
        {
            await _context.Connect();
            if (pSearchData.FromDate == null) pSearchData.FromDate = new DateTime(2023, 01, 01);
            if (pSearchData.ToDate == null) pSearchData.ToDate = _dateTimeService.GetCurrentVietnamTime();
            //SqlParameter[] sqlParameters = new SqlParameter[5];
            //sqlParameters[0] = new SqlParameter("@StatusId", pSearchData.StatusId);
            //sqlParameters[1] = new SqlParameter("@FromDate", pSearchData.FromDate.Value);
            //sqlParameters[2] = new SqlParameter("@ToDate", pSearchData.ToDate.Value);
            //sqlParameters[3] = new SqlParameter("@IsAdmin", pSearchData.IsAdmin);
            //sqlParameters[4] = new SqlParameter("@UserId", pSearchData.UserId);
            data = await _context.GetDataAsync(@$"select T0.DocEntry, DATEADD(DAY, 27 ,cast(iif(T2.DateUpdate is null, T2.DateCreate, T2.DateUpdate) as Date)) as DateStart
                                                , T2.VoucherNo, T1.CusNo, T1.FullName, T1.Phone1, TotalDebtAmount
	                                            , 'DebtReminder' as [Type] -- Nhắc nhợ
                                             from CustomerDebts as T0 with(nolock)
                                       inner join Customers as T1 with(nolock) on T0.CusNo = T1.CusNo
                                inner join Drafts as T2 with(nolock) on T0.DocEntry = T2.DocEntry"
                    , DataRecordToSheduleModel, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    #region Private Funtions
    /// <summary>
    /// đọc kết quả từ stroed báo cáo doanh thu quí tháng theo dịch vụ
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ReportModel DataRecordDoanhThuQuiThangTheoDichVuToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["BranchName"])) model.BranchName = Convert.ToString(row["BranchName"]);
        if (!Convert.IsDBNull(row["ServiceCode"])) model.ServiceCode = Convert.ToString(row["ServiceCode"]);
        if (!Convert.IsDBNull(row["ServiceName"])) model.ServiceName = Convert.ToString(row["ServiceName"]);
        if (!Convert.IsDBNull(row["EnumId"])) model.EnumId = Convert.ToString(row["EnumId"]);
        if (!Convert.IsDBNull(row["EnumName"])) model.EnumName = Convert.ToString(row["EnumName"]);
        if (!Convert.IsDBNull(row["TotalReveune"])) model.TotalReveune = Convert.ToDouble(row["TotalReveune"]);
        if (!Convert.IsDBNull(row["Total_01"])) model.Total_01 = Convert.ToDouble(row["Total_01"]);
        if (!Convert.IsDBNull(row["Total_02"])) model.Total_02 = Convert.ToDouble(row["Total_02"]);
        if (!Convert.IsDBNull(row["Total_03"])) model.Total_03 = Convert.ToDouble(row["Total_03"]);
        if (!Convert.IsDBNull(row["Total_04"])) model.Total_04 = Convert.ToDouble(row["Total_04"]);
        if (!Convert.IsDBNull(row["Total_05"])) model.Total_05 = Convert.ToDouble(row["Total_05"]);
        if (!Convert.IsDBNull(row["Total_06"])) model.Total_06 = Convert.ToDouble(row["Total_06"]);
        if (!Convert.IsDBNull(row["Total_07"])) model.Total_07 = Convert.ToDouble(row["Total_07"]);
        if (!Convert.IsDBNull(row["Total_08"])) model.Total_08 = Convert.ToDouble(row["Total_08"]);
        if (!Convert.IsDBNull(row["Total_09"])) model.Total_09 = Convert.ToDouble(row["Total_09"]);
        if (!Convert.IsDBNull(row["Total_10"])) model.Total_10 = Convert.ToDouble(row["Total_10"]);
        if (!Convert.IsDBNull(row["Total_11"])) model.Total_11 = Convert.ToDouble(row["Total_11"]);
        if (!Convert.IsDBNull(row["Total_12"])) model.Total_12 = Convert.ToDouble(row["Total_12"]);
        return model;
    }

    /// <summary>
    /// đọc kết quả từ stroed báo cáo doanh thu quí tháng theo loại dịch vụ
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ReportModel DataRecordDoanhThuQuiThangTheoLoaiDichVuToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["BranchName"])) model.BranchName = Convert.ToString(row["BranchName"]);
        if (!Convert.IsDBNull(row["EnumId"])) model.EnumId = Convert.ToString(row["EnumId"]);
        if (!Convert.IsDBNull(row["EnumName"])) model.EnumName = Convert.ToString(row["EnumName"]);
        if (!Convert.IsDBNull(row["TotalReveune"])) model.TotalReveune = Convert.ToDouble(row["TotalReveune"]);
        if (!Convert.IsDBNull(row["Total_01"])) model.Total_01 = Convert.ToDouble(row["Total_01"]);
        if (!Convert.IsDBNull(row["Total_02"])) model.Total_02 = Convert.ToDouble(row["Total_02"]);
        if (!Convert.IsDBNull(row["Total_03"])) model.Total_03 = Convert.ToDouble(row["Total_03"]);
        if (!Convert.IsDBNull(row["Total_04"])) model.Total_04 = Convert.ToDouble(row["Total_04"]);
        if (!Convert.IsDBNull(row["Total_05"])) model.Total_05 = Convert.ToDouble(row["Total_05"]);
        if (!Convert.IsDBNull(row["Total_06"])) model.Total_06 = Convert.ToDouble(row["Total_06"]);
        if (!Convert.IsDBNull(row["Total_07"])) model.Total_07 = Convert.ToDouble(row["Total_07"]);
        if (!Convert.IsDBNull(row["Total_08"])) model.Total_08 = Convert.ToDouble(row["Total_08"]);
        if (!Convert.IsDBNull(row["Total_09"])) model.Total_09 = Convert.ToDouble(row["Total_09"]);
        if (!Convert.IsDBNull(row["Total_10"])) model.Total_10 = Convert.ToDouble(row["Total_10"]);
        if (!Convert.IsDBNull(row["Total_11"])) model.Total_11 = Convert.ToDouble(row["Total_11"]);
        if (!Convert.IsDBNull(row["Total_12"])) model.Total_12 = Convert.ToDouble(row["Total_12"]);
        return model;
    }

    /// <summary>
    /// đọc kết quả từ stroed báo cáo doanh theo dịch vụ từ ngày đến ngày
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ReportModel DataRecordDoanhThuTheoDichVuToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["BranchName"])) model.BranchName = Convert.ToString(row["BranchName"]);
        if (!Convert.IsDBNull(row["ServiceCode"])) model.ServiceCode = Convert.ToString(row["ServiceCode"]);
        if (!Convert.IsDBNull(row["ServiceName"])) model.ServiceName = Convert.ToString(row["ServiceName"]);
        if (!Convert.IsDBNull(row["EnumId"])) model.EnumId = Convert.ToString(row["EnumId"]);
        if (!Convert.IsDBNull(row["EnumName"])) model.EnumName = Convert.ToString(row["EnumName"]);
        if (!Convert.IsDBNull(row["LineTotal"])) model.LineTotal = Convert.ToDouble(row["LineTotal"]);
        if (!Convert.IsDBNull(row["Qty"])) model.Qty = Convert.ToInt16(row["Qty"]);
        if (!Convert.IsDBNull(row["Price"])) model.Price = Convert.ToDouble(row["Price"]);
        return model;
    }

    /// <summary>
    /// đọc kết quả từ stroed báo cáo doanh theo loại dịch vụ từ ngày đến ngày
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ReportModel DataRecordDoanhThuTheoLoaiDichVuToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["BranchName"])) model.BranchName = Convert.ToString(row["BranchName"]);
        if (!Convert.IsDBNull(row["EnumId"])) model.EnumId = Convert.ToString(row["EnumId"]);
        if (!Convert.IsDBNull(row["EnumName"])) model.EnumName = Convert.ToString(row["EnumName"]);
        if (!Convert.IsDBNull(row["LineTotal"])) model.LineTotal = Convert.ToDouble(row["LineTotal"]);
        if (!Convert.IsDBNull(row["Qty"])) model.Qty = Convert.ToInt16(row["Qty"]);
        if (!Convert.IsDBNull(row["Price"])) model.Price = Convert.ToDouble(row["Price"]);
        return model;
    }

    /// <summary>
    /// đọc kết quả từ stroed báo cáo doanh thu quí tháng theo nhân viên tư vấn
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ReportModel DataRecordDoanhThuQuiThangTheoNhanVienTuVanToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["BranchName"])) model.BranchName = Convert.ToString(row["BranchName"]);
        if (!Convert.IsDBNull(row["ConsultUserId"])) model.ConsultUserId = Convert.ToString(row["ConsultUserId"]);
        if (!Convert.IsDBNull(row["ConsultUserName"])) model.ConsultUserName = Convert.ToString(row["ConsultUserName"]);
        if (!Convert.IsDBNull(row["ServiceCode"])) model.ServiceCode = Convert.ToString(row["ServiceCode"]);
        if (!Convert.IsDBNull(row["ServiceName"])) model.ServiceName = Convert.ToString(row["ServiceName"]);
        if (!Convert.IsDBNull(row["EnumId"])) model.EnumId = Convert.ToString(row["EnumId"]);
        if (!Convert.IsDBNull(row["EnumName"])) model.EnumName = Convert.ToString(row["EnumName"]);
        if (!Convert.IsDBNull(row["TotalReveune"])) model.TotalReveune = Convert.ToDouble(row["TotalReveune"]);
        if (!Convert.IsDBNull(row["Total_01"])) model.Total_01 = Convert.ToDouble(row["Total_01"]);
        if (!Convert.IsDBNull(row["Total_02"])) model.Total_02 = Convert.ToDouble(row["Total_02"]);
        if (!Convert.IsDBNull(row["Total_03"])) model.Total_03 = Convert.ToDouble(row["Total_03"]);
        if (!Convert.IsDBNull(row["Total_04"])) model.Total_04 = Convert.ToDouble(row["Total_04"]);
        if (!Convert.IsDBNull(row["Total_05"])) model.Total_05 = Convert.ToDouble(row["Total_05"]);
        if (!Convert.IsDBNull(row["Total_06"])) model.Total_06 = Convert.ToDouble(row["Total_06"]);
        if (!Convert.IsDBNull(row["Total_07"])) model.Total_07 = Convert.ToDouble(row["Total_07"]);
        if (!Convert.IsDBNull(row["Total_08"])) model.Total_08 = Convert.ToDouble(row["Total_08"]);
        if (!Convert.IsDBNull(row["Total_09"])) model.Total_09 = Convert.ToDouble(row["Total_09"]);
        if (!Convert.IsDBNull(row["Total_10"])) model.Total_10 = Convert.ToDouble(row["Total_10"]);
        if (!Convert.IsDBNull(row["Total_11"])) model.Total_11 = Convert.ToDouble(row["Total_11"]);
        if (!Convert.IsDBNull(row["Total_12"])) model.Total_12 = Convert.ToDouble(row["Total_12"]);
        return model;
    }

    /// <summary>
    /// đọc kết quả từ stroed báo cáo doanh thu quí tháng theo nhân viên thực hiện
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ReportModel DataRecordDoanhThuQuiThangTheoNhanVienThucHienToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["BranchName"])) model.BranchName = Convert.ToString(row["BranchName"]);
        if (!Convert.IsDBNull(row["ImplementUserId"])) model.ImplementUserId = Convert.ToString(row["ImplementUserId"]);
        if (!Convert.IsDBNull(row["ImplementUserName"])) model.ImplementUserName = Convert.ToString(row["ImplementUserName"]);
        if (!Convert.IsDBNull(row["ServiceCode"])) model.ServiceCode = Convert.ToString(row["ServiceCode"]);
        if (!Convert.IsDBNull(row["ServiceName"])) model.ServiceName = Convert.ToString(row["ServiceName"]);
        if (!Convert.IsDBNull(row["EnumId"])) model.EnumId = Convert.ToString(row["EnumId"]);
        if (!Convert.IsDBNull(row["EnumName"])) model.EnumName = Convert.ToString(row["EnumName"]);
        if (!Convert.IsDBNull(row["TotalReveune"])) model.TotalReveune = Convert.ToDouble(row["TotalReveune"]);
        if (!Convert.IsDBNull(row["Total_01"])) model.Total_01 = Convert.ToDouble(row["Total_01"]);
        if (!Convert.IsDBNull(row["Total_02"])) model.Total_02 = Convert.ToDouble(row["Total_02"]);
        if (!Convert.IsDBNull(row["Total_03"])) model.Total_03 = Convert.ToDouble(row["Total_03"]);
        if (!Convert.IsDBNull(row["Total_04"])) model.Total_04 = Convert.ToDouble(row["Total_04"]);
        if (!Convert.IsDBNull(row["Total_05"])) model.Total_05 = Convert.ToDouble(row["Total_05"]);
        if (!Convert.IsDBNull(row["Total_06"])) model.Total_06 = Convert.ToDouble(row["Total_06"]);
        if (!Convert.IsDBNull(row["Total_07"])) model.Total_07 = Convert.ToDouble(row["Total_07"]);
        if (!Convert.IsDBNull(row["Total_08"])) model.Total_08 = Convert.ToDouble(row["Total_08"]);
        if (!Convert.IsDBNull(row["Total_09"])) model.Total_09 = Convert.ToDouble(row["Total_09"]);
        if (!Convert.IsDBNull(row["Total_10"])) model.Total_10 = Convert.ToDouble(row["Total_10"]);
        if (!Convert.IsDBNull(row["Total_11"])) model.Total_11 = Convert.ToDouble(row["Total_11"]);
        if (!Convert.IsDBNull(row["Total_12"])) model.Total_12 = Convert.ToDouble(row["Total_12"]);
        return model;
    }

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
        if (!Convert.IsDBNull(record["ReasonDelete"])) model.ReasonDelete = Convert.ToString(record["ReasonDelete"]);
        return model;
    }
    
    private DocumentModel DataRecordToDocumentByGuestModel(IDataRecord record)
    {
        DocumentModel model = new();
        if (!Convert.IsDBNull(record["DocEntry"])) model.DocEntry = Convert.ToInt32(record["DocEntry"]);
        if (!Convert.IsDBNull(record["BaseEntry"])) model.BaseEntry = Convert.ToInt32(record["BaseEntry"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserNameCreate"])) model.UserNameCreate = Convert.ToString(record["UserNameCreate"]);
        if (!Convert.IsDBNull(record["Total"])) model.Total = Convert.ToDouble(record["Total"]);
        if (!Convert.IsDBNull(record["GuestsPay"])) model.GuestsPay = Convert.ToDouble(record["GuestsPay"]);
        if (!Convert.IsDBNull(record["Debt"])) model.Debt = Convert.ToDouble(record["Debt"]);
        if (!Convert.IsDBNull(record["StatusId"])) model.StatusId = Convert.ToString(record["StatusId"]);
        if (!Convert.IsDBNull(record["StatusName"])) model.StatusName = Convert.ToString(record["StatusName"]);
        if (!Convert.IsDBNull(record["NoteForAll"])) model.NoteForAll = Convert.ToString(record["NoteForAll"]);
        if (!Convert.IsDBNull(record["BranchId"])) model.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["BranchName"])) model.BranchName = Convert.ToString(record["BranchName"]);
        if (!Convert.IsDBNull(record["VoucherNo"])) model.VoucherNo = Convert.ToString(record["VoucherNo"]);
        if (!Convert.IsDBNull(record["Service"])) model.Service = Convert.ToString(record["Service"]);
        return model;
    }

    /// <summary>
    /// Lấy ra danh sách các con nợ + Nhắc dặm
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private SheduleModel DataRecordToSheduleModel(IDataRecord record)
    {
        SheduleModel model = new();
        if (!Convert.IsDBNull(record["DocEntry"])) model.DocEntry = Convert.ToInt32(record["DocEntry"]);
        if (!Convert.IsDBNull(record["DateStart"])) model.Start = Convert.ToDateTime(record["DateStart"]);
        if (!Convert.IsDBNull(record["VoucherNo"])) model.VoucherNo = Convert.ToString(record["VoucherNo"]);
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["Phone1"])) model.Phone1 = Convert.ToString(record["Phone1"]);
        if (!Convert.IsDBNull(record["Type"])) model.Type = Convert.ToString(record["Type"]);
        if (!Convert.IsDBNull(record["TotalDebtAmount"])) model.TotalDebtAmount = Convert.ToDouble(record["TotalDebtAmount"]);
        model.End = model.Start;
        model.Title = $"Nhắc nợ - {model.FullName}";
        model.IsAllDay = true;
        return model;
    }
    #endregion
}