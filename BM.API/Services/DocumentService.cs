using BM.API.Commons;
using BM.API.Infrastructure;
using BM.Models;
using BM.Models.Shared;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Net;
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
    Task<IEnumerable<CustomerDebtsModel>> GetCustomerDebtsByDocAsync(int pDocEntry);
    Task<ResponseModel> UpdateCustomerDebtsAsync(RequestModel pRequest);
    Task<ResponseModel> UpdateOutBound(RequestModel pRequest); // xuất kho
    Task<IEnumerable<OutBoundModel>> GetOutBoundAsync(SearchModel pSearchData);
    Task<ResponseModel> CancleOutBoundList(RequestModel pRequest);
    Task<IEnumerable<ReportModel>> GetRevenueReportAsync(int pYear);
    Task<ResponseModel> UpdateServiceCallAsync(RequestModel pRequest);
    Task<IEnumerable<ServiceCallModel>> GetServiceCallsAsync(SearchModel pSearchData);
    Task<ResponseModel> CheckExistsDataAsync(RequestModel pRequest);
}
public class DocumentService : IDocumentService
{
    private readonly IBMDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConfiguration _configuration;
    public DocumentService(IBMDbContext context, IDateTimeService dateTimeService, IConfiguration configuration)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _configuration = configuration;
    }

    /// <summary>
    /// lấy danh sách xuất kho
    /// </summary>
    /// <param name="isAdmin"></param>
    /// <returns></returns>
    public async Task<IEnumerable<OutBoundModel>> GetOutBoundAsync(SearchModel pSearchData)
    {
        IEnumerable<OutBoundModel> data;
        try
        {
            await _context.Connect();
            if (pSearchData.FromDate == null) pSearchData.FromDate = new DateTime(2023, 01, 01);
            if (pSearchData.ToDate == null) pSearchData.ToDate = _dateTimeService.GetCurrentVietnamTime();
            SqlParameter[] sqlParameters = new SqlParameter[6];
            sqlParameters[0] = new SqlParameter("@FromDate", pSearchData.FromDate.Value);
            sqlParameters[1] = new SqlParameter("@ToDate", pSearchData.ToDate.Value);
            sqlParameters[2] = new SqlParameter("@IsAdmin", pSearchData.IsAdmin);
            sqlParameters[3] = new SqlParameter("@UserId", pSearchData.UserId);
            sqlParameters[4] = new SqlParameter("@IdDraftDetail", pSearchData.IdDraftDetail);
            sqlParameters[5] = new SqlParameter("@Type", pSearchData.Type ?? (object)DBNull.Value);
            data = await _context.GetDataAsync(@$"select t0.*,t3.BranchName,t5.ServiceName,t4.CusNo,t4.FullName, t4.Remark,t2.HealthStatus, 
                        t1.ServiceCode, t1.ImplementUserId, t2.VoucherNo as VoucherNoDraft,
						t7.FullName as [UserNameCreate]
						,(select STRING_AGG(FullName, ', ') from [Users] as T00 with(nolock) where CHARINDEX(',' + T00.EmpNo + ',', ',' + T1.ImplementUserId + ',', 0) > 0) as ImplementUserName
						,(select STRING_AGG(FullName, ', ') from [Users] as T00 with(nolock) where CHARINDEX(',' + T00.EmpNo + ',', ',' + T0.ChargeUser + ',', 0) > 0) as ChargeUserName
                        from OutBound t0 with(nolock)
                        inner join DraftDetails t1 with(nolock) on t0.BaseEntry = t1.DocEntry and t0.IdDraftDetail = t1.Id
                        inner join Drafts t2 with(nolock) on t0.BaseEntry = t2.DocEntry
                        inner join Branchs t3 with(nolock) on t0.BranchId = t3.BranchId
                        inner join Customers t4 with(nolock) on t2.CusNo = t4.CusNo
                        inner join [Services] t5 with(nolock) on t1.ServiceCode = t5.ServiceCode
						inner join [Users] t7 with(nolock) on t0.UserCreate = t7.Id
                        where cast(T0.[DateCreate] as Date) between cast(@FromDate as Date) and cast(@ToDate as Date)
                                                and (@IsAdmin = 1 or (@IsAdmin <> 1 and T0.[UserCreate] = @UserId))
                        and  t0.IsDelete = 0  and (isnull(@IdDraftDetail,0) = 0 or t1.Id = @IdDraftDetail)
                        and (ISNULL(@Type,'')='' or  @Type = 'ByService' or @Type = 'ByWarranty' )   

						UNION ALL 

						select t0.*,t3.BranchName,'' as ServiceName,'' as CusNo,'' as FullName,'' as Remark,'' as HealthStatus, 
                        '' as ServiceCode, '' as ImplementUserId, '' as VoucherNoDraft,
						t7.FullName as [UserNameCreate]
						,'' as ImplementUserName
						,(select STRING_AGG(FullName, ', ') from [Users] as T00 with(nolock) where CHARINDEX(',' + T00.EmpNo + ',', ',' + T0.ChargeUser + ',', 0) > 0) as ChargeUserName
                        from OutBound t0 with(nolock)
                        inner join Branchs t3 with(nolock) on t0.BranchId = t3.BranchId
						inner join [Users] t7 with(nolock) on t0.UserCreate = t7.Id
                        where cast(T0.[DateCreate] as Date) between cast(@FromDate as Date) and cast(@ToDate as Date)
                                                and (@IsAdmin = 1 or (@IsAdmin <> 1 and T0.[UserCreate] = @UserId))
                        and  t0.IsDelete = 0 
                        and (ISNULL(@Type,'')='' or @Type = 'ByRequest')"
                     , DataRecordToOutBoundModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
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
            string queryString = @$"select T0.[DocEntry],[DiscountCode],[Total],[GuestsPay],[NoteForAll],[StatusId],[Debt],t0.[BaseEntry],t0.[VoucherNo],T0.[StatusBefore],T0.[HealthStatus]
            , T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate], T0.[ReasonDelete]
               , case [StatusId] when '{nameof(DocStatus.Closed)}' then N'Hoàn thành' when '{nameof(DocStatus.Cancled)}' then N'Đã hủy đơn' else N'Chờ xử lý' end as [StatusName]
               , T1.[Id],T1.[Price],T1.[Qty],T1.[LineTotal],T1.[ActionType],T1.[ConsultUserId],T1.[ImplementUserId],T1.[ChemicalFormula],T1.[WarrantyPeriod],T1.[QtyWarranty]
               , T2.[BranchId],T2.[BranchName],T4.[ServiceCode],T4.[ServiceName]
	           , T3.[CusNo],T3.[FullName],T3.[DateOfBirth],T3.CINo,T3.Phone1,T3.[Phone2],T3.Zalo,T3.FaceBook,T3.[Address],T3.[Remark]
               , isnull((select top 1 Price from [dbo].[Prices] with(nolock) where [ServiceCode] = T1.[ServiceCode] and [IsActive]= 1 order by [IsActive] desc, [DateUpdate] desc), 0) as [PriceOld]
               , (select string_agg(EnumName, ', ') from [dbo].[Enums] as T00 with(nolock) where T00.EnumType = 'SkinType' and T3.SkinType like '%""'+T00.EnumId+'""%') as [SkinType]
               , IIF(isnull(T5.DocEntry,0) <> 0,N'Rồi',N'Chưa') as [StatusOutBound]
               , (select T00.DocEntry, T00.VoucherNo, T00.BaseEntry, T00.BaseLine, T00.ImplementUserId, T00.ChemicalFormula, T00.StatusId, T2.DateCreate as DateCreateBase
                       , T00.StatusBefore, T00.HealthStatus, T00.NoteForAll, T00.BranchId, T00.UserCreate, T00.DateCreate, T00.DateUpdate
            	       , T00.UserUpdate, T00.ReasonDelete, T1.ServiceCode, T4.ServiceName, T04.BranchName
            	       , case T00.StatusId 
            	         when '{nameof(DocStatus.Closed)}' then N'Hoàn thành'
                         when '{nameof(DocStatus.Cancled)}' then N'Đã hủy phiếu'
                         else N'Chờ xử lý' end as StatusName
					from [dbo].[ServiceCalls] as T00 with(nolock) 
			  inner join [dbo].[Branchs] as T04 with(nolock) on T00.BranchId = T04.BranchId
			       where T00.BaseEntry = T0.DocEntry and T00.IsDelete = 0 and T00.StatusId <> 'Cancled'
                order by T00.DocEntry desc for json path) as JServiceCall
				,   isnull(T4.IsOutBound,cast(0 as bit)) as IsOutBound
            from [dbo].[Drafts] as T0 with(nolock) 
      inner join [dbo].[DraftDetails] as T1 with(nolock) on T0.DocEntry = T1.DocEntry
      inner join [dbo].[Branchs] as T2 with(nolock) on T0.BranchId = T2.BranchId
      inner join [dbo].[Customers] as T3 with(nolock) on T0.CusNo = T3.CusNo
      inner join [dbo].[Services] as T4 with(nolock) on T1.ServiceCode = T4.ServiceCode
       left join [dbo].[OutBound] as T5 with(nolock) on T1.Id = T5.IdDraftDetail and T5.IsDelete = 0     
           where T0.DocEntry = @DocEntry";

            var ds = await _context.GetDataSetAsync(queryString, sqlParameters, CommandType.Text);
            data = new Dictionary<string, string>();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
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
                oHeader.Zalo = Convert.ToString(dr["Zalo"]) ?? DATA_CUSTOMER_EMPTY;
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
                foreach (DataRow item in dt.Rows)
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
                    oLine.StatusOutBound = Convert.ToString(item["StatusOutBound"]);
                    oLine.JServiceCall = Convert.ToString(item["JServiceCall"]);
                    oLine.IsOutBound = Convert.ToBoolean(item["IsOutBound"]);
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
            async Task saveDebts(int pDocEntry, string pType = nameof(EnumType.DebtReminder), int pBaseLine = -1)
            {
                // lưu vào công nợ khách hàng
                if (oDraft.StatusId == nameof(DocStatus.Closed) 
                    && ((pType == nameof(EnumType.DebtReminder) && oDraft.Debt > 0) || (pType == nameof(EnumType.WarrantyReminder) && pBaseLine > 0)))
                { 
                    // lấy mã
                    int iIdDebts = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[CustomerDebts] with(nolock)");

                    queryString = @$"Insert into [dbo].[CustomerDebts] ([Id],[DocEntry],[CusNo],[GuestsPay],[TotalDebtAmount],[DateCreate],[UserCreate],[IsDelay],[Type],[BaseLine])
                                                values (@Id, @DocEntry, @CusNo, @GuestsPay, @TotalDebtAmount, @DateTimeNow, @UserId, 0, @Type, @BaseLine)";
                    sqlParameters = new SqlParameter[9];
                    sqlParameters[0] = new SqlParameter("@Id", iIdDebts);
                    sqlParameters[1] = new SqlParameter("@DocEntry", pDocEntry);
                    sqlParameters[2] = new SqlParameter("@CusNo", oDraft.CusNo);
                    sqlParameters[3] = new SqlParameter("@TotalDebtAmount", pType == nameof(EnumType.WarrantyReminder) ? 0 : oDraft.Debt);
                    sqlParameters[4] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[6] = new SqlParameter("@GuestsPay", pType == nameof(EnumType.WarrantyReminder) ? 0 : oDraft.GuestsPay);
                    sqlParameters[7] = new SqlParameter("@Type", pType);
                    sqlParameters[8] = new SqlParameter("@BaseLine", pBaseLine);
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
                    if (isUpdated)
                    {

                        foreach (var oDraftDetails in lstDraftDetails)
                        {
                            int iDrftId = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[DraftDetails] with(nolock)");
                            queryString = @"Insert into [dbo].[DraftDetails] ([Id],[ServiceCode],[Qty], [Price],[LineTotal],[DocEntry], [ActionType],[ConsultUserId]
                                   ,[ImplementUserId],[ChemicalFormula],[WarrantyPeriod],[QtyWarranty],[DateCreate],[UserCreate],[IsDelete], ListPromotionSupplies)
                                    values (@Id, @ServiceCode, @Qty, @Price, @LineTotal, @DocEntry, @ActionType, @ConsultUserId
                                   ,@ImplementUserId, @ChemicalFormula,@WarrantyPeriod, @QtyWarranty, @DateTimeNow, @UserId, 0, @ListPromotionSupplies)";

                            sqlParameters = new SqlParameter[15];
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
                            sqlParameters[14] = new SqlParameter("@ListPromotionSupplies", oDraftDetails.ListPromotionSupplies ?? (object)DBNull.Value);
                            isUpdated = await ExecQuery();
                            if (!isUpdated)
                            {
                                await _context.RollbackAsync();
                                return response;
                            }

                            // nếu có bảo hành
                            if(oDraftDetails.WarrantyPeriod > 0 && oDraftDetails.QtyWarranty > 0) await saveDebts(iDocentry, nameof(EnumType.WarrantyReminder), iDrftId);
                        }
                        // lưu vào công nợ khách hàng
                        await saveDebts(iDocentry, nameof(EnumType.DebtReminder));
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
                    if (isUpdated)
                    {
                        foreach (var oDraftDetails in lstDraftDetails)
                        {
                            sqlParameters = new SqlParameter[15];
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
                            sqlParameters[13] = new SqlParameter("@ListPromotionSupplies", oDraftDetails.ListPromotionSupplies ?? (object)DBNull.Value);
                            if (oDraftDetails.Id <= 0)
                            {
                                // thêm mới
                                oDraftDetails.Id = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[DraftDetails] with(nolock)"); // gán lại Id
                                queryString = @"Insert into [dbo].[DraftDetails] ([Id],[ServiceCode],[Qty], [Price],[LineTotal],[DocEntry], [ActionType],[ConsultUserId]
                                   ,[ImplementUserId],[ChemicalFormula],[WarrantyPeriod],[QtyWarranty],[DateCreate],[UserCreate],[IsDelete], ListPromotionSupplies)
                                    values (@Id, @ServiceCode, @Qty, @Price, @LineTotal, @DocEntry, @ActionType, @ConsultUserId
                                   ,@ImplementUserId, @ChemicalFormula,@WarrantyPeriod, @QtyWarranty, @DateTimeNow, @UserId, 0, @ListPromotionSupplies)";
                                sqlParameters[14] = new SqlParameter("@Id", oDraftDetails.Id);

                            }
                            else
                            {
                                // cập nhật
                                queryString = @"Update [dbo].[DraftDetails]
                                                   set [ServiceCode] = @ServiceCode, [Qty] = @Qty, [Price] = @Price, [LineTotal] = @LineTotal, [ActionType] = @ActionType
                                                     , [ConsultUserId] = @ConsultUserId, [ImplementUserId] = @ImplementUserId,[ChemicalFormula] = @ChemicalFormula
                                                     , [WarrantyPeriod] = @WarrantyPeriod, [QtyWarranty] = @QtyWarranty, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId, [ListPromotionSupplies] =  @ListPromotionSupplies
                                                 where [Id] = @Id";
                                sqlParameters[14] = new SqlParameter("@Id", oDraftDetails.Id);
                            }
                            isUpdated = await ExecQuery();
                            if (!isUpdated)
                            {
                                await _context.RollbackAsync();
                                return response;
                            }
                            // nếu có bảo hành
                            if (oDraftDetails.WarrantyPeriod > 0 && oDraftDetails.QtyWarranty > 0) await saveDebts(oDraft.DocEntry, nameof(EnumType.WarrantyReminder), oDraftDetails.Id);
                        }

                        // xóa các dòng không tồn tại trong danh sách Ids
                        queryString = "Delete from [dbo].[DraftDetails] where [Id] not in ( select value from STRING_SPLIT(@ListIds, ',') )  and [DocEntry] = @DocEntry";
                        sqlParameters = new SqlParameter[2];
                        sqlParameters[0] = new SqlParameter("@ListIds", string.Join(",", lstDraftDetails.Select(m => m.Id).Distinct()));
                        sqlParameters[1] = new SqlParameter("@DocEntry", oDraft.DocEntry);
                        await _context.DeleteDataAsync(queryString, sqlParameters);

                        // lưu vào công nợ khách hàng
                        await saveDebts(oDraft.DocEntry, nameof(EnumType.DebtReminder));
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
    /// cập nhật phiếu xuất kho
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateOutBound(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            bool isUpdated = false;
            OutBoundModel oOutBound = JsonConvert.DeserializeObject<OutBoundModel>(pRequest.Json + "")!;

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

            void setParameter()
            {
                sqlParameters = new SqlParameter[19];
                sqlParameters[0] = new SqlParameter("@DocEntry", oOutBound.DocEntry ?? (object)DBNull.Value);
                sqlParameters[1] = new SqlParameter("@VoucherNo", oOutBound.VoucherNo ?? (object)DBNull.Value);
                sqlParameters[2] = new SqlParameter("@BaseEntry", oOutBound.BaseEntry ?? (object)DBNull.Value);
                sqlParameters[3] = new SqlParameter("@IdDraftDetail", oOutBound.IdDraftDetail ?? (object)DBNull.Value);// oDraft.DiscountCode ?? (object)DBNull.Value);
                sqlParameters[4] = new SqlParameter("@ColorImplement", oOutBound.ColorImplement ?? (object)DBNull.Value);
                sqlParameters[5] = new SqlParameter("@SuppliesQtyList", oOutBound.SuppliesQtyList ?? (object)DBNull.Value);
                sqlParameters[6] = new SqlParameter("@AnesthesiaType", oOutBound.AnesthesiaType ?? (object)DBNull.Value);
                sqlParameters[7] = new SqlParameter("@AnesthesiaQty", oOutBound.AnesthesiaQty ?? (object)DBNull.Value);
                sqlParameters[8] = new SqlParameter("@DarkTestColor", oOutBound.DarkTestColor ?? (object)DBNull.Value);
                sqlParameters[9] = new SqlParameter("@CoadingColor", oOutBound.CoadingColor ?? (object)DBNull.Value);
                sqlParameters[10] = new SqlParameter("@LibColor", oOutBound.LibColor ?? (object)DBNull.Value);
                sqlParameters[11] = new SqlParameter("@StartTime", oOutBound.StartTime ?? (object)DBNull.Value);
                sqlParameters[12] = new SqlParameter("@EndTime", oOutBound.StartTime ?? (object)DBNull.Value);
                sqlParameters[13] = new SqlParameter("@Problems", oOutBound.Problems ?? (object)DBNull.Value);
                sqlParameters[14] = new SqlParameter("@ChargeUser", oOutBound.ChargeUser ?? (object)DBNull.Value);
                sqlParameters[15] = new SqlParameter("@BranchId", oOutBound.BranchId ?? (object)DBNull.Value);
                sqlParameters[16] = new SqlParameter("@UserId", pRequest.UserId);
                sqlParameters[17] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                sqlParameters[18] = new SqlParameter("@Type", oOutBound.Type ?? (object)DBNull.Value);
            }

            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    int iDocentry = await _context.ExecuteScalarAsync("select isnull(max(DocEntry), 0) + 1 from [dbo].[OutBound] with(nolock)");
                    // nếu status == Closed -> đánh mã chứng từ
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@Type", "OutBound");
                    oOutBound.DocEntry = iDocentry;
                    oOutBound.VoucherNo = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy số phiếu
                    queryString = @"INSERT INTO [dbo].[OutBound]  ([DocEntry],[VoucherNo] ,[BaseEntry] ,[IdDraftDetail],[ColorImplement] ,[SuppliesQtyList] ,[AnesthesiaType]  ,[AnesthesiaQty]  ,
                                [DarkTestColor],[CoadingColor] ,[LibColor] ,[StartTime] ,[EndTime]  ,[Problems] ,[ChargeUser]  ,[BranchId],[DateCreate] ,[UserCreate] ,[IsDelete], Type)
                    	        values (@DocEntry, @VoucherNo, @BaseEntry, @IdDraftDetail, @ColorImplement, @SuppliesQtyList, @AnesthesiaType, @AnesthesiaQty,
                                @DarkTestColor,@CoadingColor,@LibColor, @StartTime, @EndTime, @Problems, @ChargeUser,@BranchId, @DateTimeNow, @UserId, 0, @Type)";

                    setParameter();
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    // kiểm tra mã lệnh
                    queryString = @"UPDATE [dbo].[OutBound]
                                   SET [VoucherNo] = @VoucherNo
                                      ,[BaseEntry] = @BaseEntry
                                      ,[IdDraftDetail] = @IdDraftDetail
                                      ,[ColorImplement] = @ColorImplement
                                      ,[SuppliesQtyList] = @SuppliesQtyList
                                      ,[AnesthesiaType] = @AnesthesiaType
                                      ,[AnesthesiaQty] = @AnesthesiaQty
                                      ,[DarkTestColor] = @DarkTestColor
                                      ,[CoadingColor] = @CoadingColor
                                      ,[LibColor] = @LibColor
                                      ,[StartTime] = @StartTime
                                      ,[EndTime] = @EndTime
                                      ,[Problems] = @Problems
                                      ,[ChargeUser] = @ChargeUser
                                      ,[BranchId] = @BranchId
                                      ,[DateUpdate] = @DateTimeNow
                                      ,[UserUpdate] = @UserId
                                      ,Type = @Type
                                     where [DocEntry] = @DocEntry";
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
            //pRequest.Type: Chưa table Name
            await _context.Connect();
            SqlParameter[] sqlParameters;
            string queryString = @$"UPDATE [dbo].[{pRequest.Type}] 
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
    /// hủy phiếu xuất kho
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> CancleOutBoundList(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters;
            string queryString = @$"UPDATE [dbo].[OutBound] 
                                      set  [IsDelete] = 1, [ReasonDelete] = @ReasonDelete, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
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
                    switch (pSearchData.Type + "")
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
            DateTime dateTime = _dateTimeService.GetCurrentVietnamTime();
            if (pSearchData.FromDate == null) pSearchData.FromDate = new DateTime(dateTime.Year, dateTime.Month - 1, 23);
            if (pSearchData.ToDate == null) pSearchData.ToDate = new DateTime(dateTime.Year, dateTime.Month, 1).AddMonths(1).AddDays(7);
            int numDay = int.Parse(_configuration.GetSection("Configs:NumberOfReminderDays").Value);
            SqlParameter[] sqlParameters = new SqlParameter[4];
            sqlParameters[0] = new SqlParameter("@FromDate", pSearchData.FromDate.Value);
            sqlParameters[1] = new SqlParameter("@ToDate", pSearchData.ToDate.Value);
            sqlParameters[2] = new SqlParameter("@NumDay", numDay);
            sqlParameters[3] = new SqlParameter("@BranchId", pSearchData.BranchId);
            data = await _context.GetDataAsync(@$"Select T0.DocEntry, T2.Remark
                                                       , T2.DateCreate, iif(isnull(IsDelay, 0) = 1, DateDelay, DATEADD(DAY, @NumDay ,cast(T2.DateCreate as Date))) as DateStart
                                                       , T0.VoucherNo, T1.CusNo, T1.FullName, T1.Phone1, T0.Debt as TotalDebtAmount
                                                       , '{nameof(EnumType.DebtReminder)}' as [Type] -- Nhắc nhợ
                                                       , '' as [ServiceCode], '' as [ServiceName], -1 as [BaseLine]
                                                    from [dbo].[Drafts] as T0 with(nolock)
                                              inner join [dbo].[Customers] as T1 with(nolock) on T0.CusNo = T1.CusNo
                                             cross apply (select top 1 DateCreate, Remark, IsDelay, DateDelay from [dbo].[CustomerDebts] as T00 with(nolock) 
                                                           where T0.DocEntry = T00.DocEntry and T00.[Type] = '{nameof(EnumType.DebtReminder)}'
                                                        order by Id desc) as T2
                                                   where 1=1 
                                                     and isnull(T0.Debt, 0) > 0 and T0.StatusId = 'Closed'
                                                     and iif(isnull(IsDelay, 0) = 1, DateDelay, DATEADD(DAY, @NumDay ,cast(T2.DateCreate as Date))) 
                                                         between cast(@FromDate as Date) and cast(@ToDate as Date)
                                                     and T0.BranchId = @BranchId
                                                   union all
                                                  Select T0.DocEntry, T4.Remark
	                                                   , T4.DateCreate, iif(isnull(IsDelay, 0) = 1, T4.DateDelay, DATEADD(DAY, @NumDay ,cast(T4.DateCreate as Date))) as DateStart
		                                               , T0.VoucherNo, T1.CusNo, T1.FullName, T1.Phone1, T0.Debt as TotalDebtAmount
		                                               , '{nameof(EnumType.WarrantyReminder)}' as [Type] -- Nhắc bảo hành
                                                       , T2.[ServiceCode], T3.[ServiceName], T2.[Id] as [BaseLine]
                                                    from [dbo].[Drafts] as T0 with(nolock)
                                              inner join [dbo].[Customers] as T1 with(nolock) on T0.CusNo = T1.CusNo
                                              inner join [dbo].[DraftDetails] as T2 with(nolock) on T2.DocEntry = T0.DocEntry
                                              inner join [dbo].[Services] as T3 with(nolock) on T3.ServiceCode = T2.ServiceCode
											 cross apply (select top 1 DateCreate, Remark, IsDelay, DateDelay from [dbo].[CustomerDebts] as T00 with(nolock) 
                                                           where T2.DocEntry = T00.DocEntry and T2.Id = T00.BaseLine and T00.[Type] = '{nameof(EnumType.WarrantyReminder)}' 
                                                        order by Id desc) as T4
                                                   where 1=1 
		                                             and isnull(T2.WarrantyPeriod, 0) > 0 and isnull(T2.QtyWarranty, 0) > 0
		                                             and T0.StatusId = 'Closed' -- phải đóng
                                                     and iif(isnull(IsDelay, 0) = 1, T4.DateDelay, DATEADD(DAY, @NumDay ,cast(T4.DateCreate as Date))) 
                                                         between cast(@FromDate as Date) and cast(@ToDate as Date)
                                                     and T0.BranchId = @BranchId"
                    , DataRecordToSheduleModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    ///  lấy lịch sử công nợ theo User 
    /// </summary>
    /// <param name="pDocEntry"></param>
    /// <returns></returns>
    public async Task<IEnumerable<CustomerDebtsModel>> GetCustomerDebtsByDocAsync(int pDocEntry)
    {
        IEnumerable<CustomerDebtsModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@DocEntry", pDocEntry);
            data = await _context.GetDataAsync(@$"Select T0.DocEntry, T0.Id, T0.CusNo, T0.TotalDebtAmount, T0.DateCreate, T0.UserCreate
                                                , T0.GuestsPay, T1.FullName, T0.[Remark], T0.[IsDelay], T0.[DateDelay]
                                             from [dbo].[CustomerDebts] as T0 with(nolock)
                                       inner join [dbo].[Customers] as T1 with(nolock) on T0.CusNo = T1.CusNo
                                            where T0.[DocEntry] = @DocEntry and T0.[Type] = '{nameof(EnumType.DebtReminder)}'
                                            order by T0.Id desc"
                    , DataRecordToCustomerDebtsModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// Thêm mới lịch sử thanh toán, không có sữa
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateCustomerDebtsAsync(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            CustomerDebtsModel oCusDebts = JsonConvert.DeserializeObject<CustomerDebtsModel>(pRequest.Json + "")!;
            string queryString = string.Empty;
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
            if(oCusDebts.IsDelay)
            {
                string scondition = string.Empty;
                if(oCusDebts.Type == nameof(EnumType.WarrantyReminder))
                {
                    // nếu nhắc bảo hành -> where thêm line
                    scondition = "and T0.[BaseLine] = @BaseLine";
                }    
                // nếu mà là khách hẹn khi khác
                // lấy lên thông tin đơn hàng kế bên
                queryString = @$"Select Top 1 T0.DocEntry, T0.Id, T0.CusNo, T0.TotalDebtAmount, T0.DateCreate, T0.UserCreate
                                     , T0.GuestsPay, T1.FullName, T0.[Remark], T0.[IsDelay], T0.[DateDelay]
                                  from [dbo].[CustomerDebts] as T0 with(nolock)
                            inner join [dbo].[Customers] as T1 with(nolock) on T0.CusNo = T1.CusNo
                                 where T0.[DocEntry] = @DocEntry and T0.[Type] = @Type {scondition}
                                 order by T0.Id desc";
                sqlParameters = new SqlParameter[3];
                sqlParameters[0] = new SqlParameter("@DocEntry", oCusDebts.DocEntry);
                sqlParameters[1] = new SqlParameter("@Type", oCusDebts.Type);
                sqlParameters[2] = new SqlParameter("@BaseLine", oCusDebts.BaseLine);
                var oItemCusDebts = await _context.GetDataByIdAsync(queryString, DataRecordToCustomerDebtsModel, sqlParameters, CommandType.Text);
                if(oItemCusDebts == null)
                {
                    response.StatusCode = -1;
                    string mess = oCusDebts.Type == nameof(EnumType.DebtReminder) ? "Công nợ" : "Bảo hành";
                    response.Message = $"Không tìm thấy thông tin {mess} trước đó. Vui lòng thử lại!";
                    return response;
                }
                // gán lại thông tin
                oCusDebts.TotalDebtAmount = oItemCusDebts.TotalDebtAmount;
                oCusDebts.GuestsPay = 0.0; // không có số tiền khách trả
                oCusDebts.DateDelay = oCusDebts.DateDelay ?? _dateTimeService.GetCurrentVietnamTime().AddMonths(1);
            }
            // lấy mã
            int iIdDebts = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[CustomerDebts] with(nolock)");
            queryString = @"Insert into [dbo].[CustomerDebts] ([Id],[DocEntry],[CusNo], [GuestsPay],[TotalDebtAmount],[DateCreate]
                                                ,[UserCreate],[Remark],[IsDelay],[DateDelay],[Type],[BaseLine])
                                                values (@Id, @DocEntry, @CusNo, @GuestsPay, @TotalDebtAmount, @DateTimeNow, @UserId, @Remark, @IsDelay, @DateDelay, @Type, @BaseLine)";
            sqlParameters = new SqlParameter[12];
            sqlParameters[0] = new SqlParameter("@Id", iIdDebts);
            sqlParameters[1] = new SqlParameter("@DocEntry", oCusDebts.DocEntry);
            sqlParameters[2] = new SqlParameter("@CusNo", oCusDebts.CusNo);
            sqlParameters[3] = new SqlParameter("@TotalDebtAmount", oCusDebts.TotalDebtAmount);
            sqlParameters[4] = new SqlParameter("@UserId", pRequest.UserId);
            sqlParameters[5] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
            sqlParameters[6] = new SqlParameter("@GuestsPay", oCusDebts.GuestsPay);
            sqlParameters[7] = new SqlParameter("@Remark", oCusDebts.Remark ?? (object)DBNull.Value);
            sqlParameters[8] = new SqlParameter("@IsDelay", oCusDebts.IsDelay);
            sqlParameters[9] = new SqlParameter("@DateDelay", oCusDebts.DateDelay ?? (object)DBNull.Value);
            sqlParameters[10] = new SqlParameter("@Type", oCusDebts.Type);
            sqlParameters[11] = new SqlParameter("@BaseLine", oCusDebts.BaseLine);
            await _context.BeginTranAsync();
            bool isUpdated = await ExecQuery();
            if (isUpdated && !oCusDebts.IsDelay && oCusDebts.Type == nameof(EnumType.DebtReminder))
            {
                // Nếu k phải delay và Tran ok
                sqlParameters = new SqlParameter[1];
                sqlParameters[0] = new SqlParameter("@DocEntry", oCusDebts.DocEntry);
                double dGuestsPayOld = double.Parse(await _context.ExecuteScalarObjectAsync(@"Select isnull(GuestsPay, 0) 
                                        from [dbo].[Drafts] with(nolock) where [DocEntry] = @DocEntry", sqlParameters) + "");
                // cập nhật lại tổng tiền khách trả + cà công nợ
                queryString = @"Update [dbo].[Drafts]
                                   set [GuestsPay] = @GuestsPay, [Debt] = @TotalDebtAmount
                                 where [DocEntry] = @DocEntry";
                sqlParameters = new SqlParameter[3];
                sqlParameters[0] = new SqlParameter("@DocEntry", oCusDebts.DocEntry);
                sqlParameters[1] = new SqlParameter("@TotalDebtAmount", oCusDebts.TotalDebtAmount);
                sqlParameters[2] = new SqlParameter("@GuestsPay", oCusDebts.GuestsPay + dGuestsPayOld);
                isUpdated = await ExecQuery();
            }

            // commit tran
            if(isUpdated) await _context.CommitTranAsync();
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
    /// báo cáo doanh thu theo chi nhánh
    /// </summary>
    /// <param name="pYear"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ReportModel>> GetRevenueReportAsync(int pYear)
    {
        IEnumerable<ReportModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@Type", 'M');
            sqlParameters[0] = new SqlParameter("@Year", pYear);
            Func<IDataRecord, ReportModel> readData = record =>
            {
                ReportModel model = new ReportModel();
                if (!Convert.IsDBNull(record["Total_01"])) model.Total_01 = Convert.ToDouble(record["Total_01"]);
                if (!Convert.IsDBNull(record["Total_02"])) model.Total_02 = Convert.ToDouble(record["Total_02"]);
                if (!Convert.IsDBNull(record["Color_01"])) model.Color_01 = Convert.ToString(record["Color_01"]);
                if (!Convert.IsDBNull(record["Color_02"])) model.Color_02 = Convert.ToString(record["Color_02"]);
                if (!Convert.IsDBNull(record["Title"])) model.Title = Convert.ToString(record["Title"]) + "";
                return model;
            };
            data = await _context.GetDataAsync(Constants.STORE_REVENUE_REPORT, readData, sqlParameters);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }    
    
    /// <summary>
    /// lưu thông tin phiếu bảo hành
    /// hainguyen create 2023/12/20
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateServiceCallAsync(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            ServiceCallModel oServiceCall = JsonConvert.DeserializeObject<ServiceCallModel>(pRequest.Json + "")!;
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
                    oServiceCall.DocEntry = await _context.ExecuteScalarAsync("select isnull(max(DocEntry), 0) + 1 from [dbo].[ServiceCalls] with(nolock)");
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@Type", "ServiceCalls");
                    oServiceCall.VoucherNo = (string?)await _context.ExcecFuntionAsync("dbo.BM_GET_VOUCHERNO", sqlParameters); // lấy lấy số phiếu
                    queryString = @"Insert into [dbo].[ServiceCalls] ([DocEntry],[VoucherNo],[CusNo],[BaseEntry],[BaseLine],[ImplementUserId], [ChemicalFormula]
                                    ,[StatusBefore],[HealthStatus],[NoteForAll],[StatusId],[BranchId],[DateCreate],[UserCreate],[DateUpdate],[IsDelete])
                                    values (@DocEntry, @VoucherNo, @CusNo, @BaseEntry, @BaseLine, @ImplementUserId, @ChemicalFormula, @StatusBefore, @HealthStatus
                                   ,@NoteForAll, @StatusId, @BranchId, @DateTimeNow, @UserId, @DateTimeNow, 0)";

                    sqlParameters = new SqlParameter[14];
                    sqlParameters[0] = new SqlParameter("@DocEntry", oServiceCall.DocEntry);
                    sqlParameters[1] = new SqlParameter("@VoucherNo", oServiceCall.VoucherNo);
                    sqlParameters[2] = new SqlParameter("@CusNo", oServiceCall.CusNo);
                    sqlParameters[3] = new SqlParameter("@BaseEntry", oServiceCall.BaseEntry);
                    sqlParameters[4] = new SqlParameter("@BaseLine", oServiceCall.BaseLine);
                    sqlParameters[5] = new SqlParameter("@ImplementUserId", oServiceCall.ImplementUserId);
                    sqlParameters[6] = new SqlParameter("@ChemicalFormula", oServiceCall.ChemicalFormula);
                    sqlParameters[7] = new SqlParameter("@StatusBefore", oServiceCall.StatusBefore ?? (object)DBNull.Value);
                    sqlParameters[8] = new SqlParameter("@HealthStatus", oServiceCall.HealthStatus ?? (object)DBNull.Value);
                    sqlParameters[9] = new SqlParameter("@NoteForAll", oServiceCall.NoteForAll ?? (object)DBNull.Value);
                    sqlParameters[10] = new SqlParameter("@StatusId", oServiceCall.StatusId ?? (object)DBNull.Value);
                    sqlParameters[11] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[12] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[13] = new SqlParameter("@BranchId", oServiceCall.BranchId);
                    await ExecQuery();
                    break;
                case nameof(EnumType.Update):
                    queryString = @"Update [dbo].[ServiceCalls]
                                       set [StatusBefore] = @StatusBefore, [HealthStatus] = @HealthStatus, [NoteForAll] = @NoteForAll
                                         , [ImplementUserId] = @ImplementUserId, [ChemicalFormula] = @ChemicalFormula
                                         , [StatusId] = @StatusId, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                     where [DocEntry] = @DocEntry";
                    sqlParameters = new SqlParameter[9];
                    sqlParameters[0] = new SqlParameter("@DocEntry", oServiceCall.DocEntry);
                    sqlParameters[1] = new SqlParameter("@StatusBefore", oServiceCall.StatusBefore ?? (object)DBNull.Value);
                    sqlParameters[2] = new SqlParameter("@HealthStatus", oServiceCall.HealthStatus ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@NoteForAll", oServiceCall.NoteForAll ?? (object)DBNull.Value);
                    sqlParameters[4] = new SqlParameter("@StatusId", oServiceCall.StatusId);
                    sqlParameters[5] = new SqlParameter("@ImplementUserId", oServiceCall.ImplementUserId);
                    sqlParameters[6] = new SqlParameter("@ChemicalFormula", oServiceCall.ChemicalFormula);
                    sqlParameters[7] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[8] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
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
    /// lấy dach sách các phiếu bảo hành dịch vụ
    /// </summary>
    /// <param name="pSearchData"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ServiceCallModel>> GetServiceCallsAsync(SearchModel pSearchData)
    {
        IEnumerable<ServiceCallModel> data;
        try
        {
            await _context.Connect();
            string sCondition = string.Empty;
            // VIẾT VẬY CHO ĐỠ RỐI
            if (pSearchData.Type?.ToUpper() == "ALL")
            {
                // Load tất cả
                sCondition = @"and cast(T0.[DateCreate] as Date) between cast(@FromDate as Date) and cast(@ToDate as Date)
                               and (@StatusId = 'All' or (@StatusId <> 'All' and T0.[StatusId] = @StatusId))
                               and (@IsAdmin = 1 or (@IsAdmin <> 1 and T0.[UserCreate] = @UserId))";
            }    
            else if (pSearchData.Type?.ToUpper() == nameof(EnumTable.ServiceCalls).ToUpper())
            {
                // load chi tiết
                sCondition = "and T0.DocEntry = @DocEntry";
            }    
            else if (pSearchData.Type?.ToUpper() == nameof(EnumTable.Customers).ToUpper())
            {
                sCondition = $"and T0.[CusNo] = @CusNo and T0.[StatusId] <> '{nameof(DocStatus.Cancled)}'";
            }    

            if (pSearchData.FromDate == null) pSearchData.FromDate = new DateTime(2023, 01, 01);
            if (pSearchData.ToDate == null) pSearchData.ToDate = _dateTimeService.GetCurrentVietnamTime();
            SqlParameter[] sqlParameters = new SqlParameter[7];
            sqlParameters[0] = new SqlParameter("@StatusId", pSearchData.StatusId + "");
            sqlParameters[1] = new SqlParameter("@FromDate", pSearchData.FromDate.Value);
            sqlParameters[2] = new SqlParameter("@ToDate", pSearchData.ToDate.Value);
            sqlParameters[3] = new SqlParameter("@IsAdmin", pSearchData.IsAdmin);
            sqlParameters[4] = new SqlParameter("@UserId", pSearchData.UserId);
            sqlParameters[5] = new SqlParameter("@DocEntry", pSearchData.IdDraftDetail);
            sqlParameters[6] = new SqlParameter("@CusNo", pSearchData.CusNo ?? (object)DBNull.Value);
            data = await _context.GetDataAsync(@$"select T0.DocEntry, T0.VoucherNo, T0.BaseEntry, T0.BaseLine, T0.ImplementUserId, T0.ChemicalFormula, T0.StatusId, T2.DateCreate as DateCreateBase
                     , T0.StatusBefore, T0.HealthStatus, T0.NoteForAll, T0.BranchId, T0.UserCreate, T0.DateCreate, T0.DateUpdate
            	     , T0.UserUpdate, T0.ReasonDelete, T2.VoucherNo as VoucherNoBase, T1.ServiceCode, T3.ServiceName, T4.BranchName
            	     , T0.CusNo, T5.FullName, T5.Phone1, T1.ConsultUserId, T5.DateOfBirth, T5.CINo, T5.Phone2, T5.Zalo, T5.FaceBook, T5.[Address]
            	     , case T0.StatusId 
            	       when '{nameof(DocStatus.Closed)}' then N'Hoàn thành'
                       when '{nameof(DocStatus.Cancled)}' then N'Đã hủy phiếu'
                       else N'Chờ xử lý' end as StatusName
                       ,  IIF(isnull(T6.DocEntry,0) <> 0,N'Rồi',N'Chưa') as StatusOutBound
                       , t6.DateCreate as DateCreateOutBound
                  from [dbo].[ServiceCalls] as T0 with(nolock)
            inner join [dbo].[DraftDetails] as T1 with(nolock) on T0.BaseEntry = T1.DocEntry and T0.BaseLine = T1.Id
            inner join [dbo].[Drafts] as T2 with(nolock) on T1.DocEntry = T2.DocEntry
            inner join [dbo].[Services] as T3 with(nolock) on T1.ServiceCode = T3.ServiceCode
            inner join [dbo].[Branchs] as T4 with(nolock) on T0.BranchId = T4.BranchId
            inner join [dbo].[Customers] as T5 with(nolock) on T0.CusNo = T5.CusNo
            left join [dbo].[OutBound] as T6 with(nolock) on T1.Id = t6.IdDraftDetail and T6.IsDelete = 0
                 where 1=1 {sCondition}
            order by T0.DocEntry desc"
                    , DataRecordToServiceCallModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }
    
    /// <summary>
    /// Kiểm tra tồn tại dữ liệu
    /// Thêm type muốn kiểm tra
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> CheckExistsDataAsync(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters;
            switch (pRequest.Type)
            {
                case nameof(EnumTable.ServiceCalls):
                    // kiểm tra tồn tại phiếu bảo hành đang Pending hay k
                    // Buộc phải hoàn thành phiếu có tình trạng pending
                    sqlParameters = new SqlParameter[2];
                    sqlParameters[0] = new SqlParameter("@BaseEntry", pRequest.BaseEntry);
                    sqlParameters[1] = new SqlParameter("@BaseLine", pRequest.BaseLine);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    object? oServiceCall= await _context.ExecuteScalarObjectAsync(@$"select VoucherNo from ServiceCalls as T0 with(nolock) 
                    where BaseEntry = @BaseEntry and BaseLine = @BaseLine and T0.StatusId = '{nameof(DocStatus.Pending)}'", sqlParameters);
                    if(oServiceCall != null)
                    {
                        response.StatusCode = (int)HttpStatusCode.Conflict;
                        response.Message = $"Tồn tại phiếu bảo hành [{Convert.ToString(oServiceCall)}] đang chờ xử lý!";
                    }
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

    /// <summary>
    /// đọc danh sách phiếu lập kho
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private OutBoundModel DataRecordToOutBoundModel(IDataRecord record)
    {
        OutBoundModel model = new();
        if (!Convert.IsDBNull(record["DocEntry"])) model.DocEntry = Convert.ToInt32(record["DocEntry"]);
        if (!Convert.IsDBNull(record["VoucherNo"])) model.VoucherNo = Convert.ToString(record["VoucherNo"]);
        if (!Convert.IsDBNull(record["BaseEntry"])) model.BaseEntry = Convert.ToInt32(record["BaseEntry"]);
        if (!Convert.IsDBNull(record["IdDraftDetail"])) model.IdDraftDetail = Convert.ToInt32(record["IdDraftDetail"]);
        if (!Convert.IsDBNull(record["ColorImplement"])) model.ColorImplement = Convert.ToString(record["ColorImplement"]);
        if (!Convert.IsDBNull(record["SuppliesQtyList"])) model.SuppliesQtyList = Convert.ToString(record["SuppliesQtyList"]);
        if (!Convert.IsDBNull(record["SuppliesQtyList"])) model.AnesthesiaType = Convert.ToString(record["AnesthesiaType"]);
        if (!Convert.IsDBNull(record["AnesthesiaQty"])) model.AnesthesiaQty = Convert.ToInt32(record["AnesthesiaQty"]);
        if (!Convert.IsDBNull(record["DarkTestColor"])) model.DarkTestColor = Convert.ToString(record["DarkTestColor"]);
        if (!Convert.IsDBNull(record["CoadingColor"])) model.CoadingColor = Convert.ToString(record["CoadingColor"]);
        if (!Convert.IsDBNull(record["LibColor"])) model.LibColor = Convert.ToString(record["LibColor"]);
        if (!Convert.IsDBNull(record["StartTime"])) model.StartTime = Convert.ToDateTime(record["StartTime"]);
        if (!Convert.IsDBNull(record["StartTime"])) model.EndTime = Convert.ToDateTime(record["EndTime"]);
        if (!Convert.IsDBNull(record["Problems"])) model.Problems = Convert.ToString(record["Problems"]);
        if (!Convert.IsDBNull(record["ChargeUser"])) model.ChargeUser = Convert.ToString(record["ChargeUser"]);
        if (!Convert.IsDBNull(record["ChargeUserName"])) model.ChargeUserName = Convert.ToString(record["ChargeUserName"]);
        if (!Convert.IsDBNull(record["BranchId"])) model.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["BranchName"])) model.BranchName = Convert.ToString(record["BranchName"]);
        if (!Convert.IsDBNull(record["ServiceCode"])) model.ServiceCode = Convert.ToString(record["ServiceCode"]);
        if (!Convert.IsDBNull(record["ServiceName"])) model.ServiceName = Convert.ToString(record["ServiceName"]);
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["Remark"])) model.Remark = Convert.ToString(record["Remark"]);
        if (!Convert.IsDBNull(record["HealthStatus"])) model.HealthStatus = Convert.ToString(record["HealthStatus"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["ImplementUserId"])) model.ImplementUserId = Convert.ToString(record["ImplementUserId"]);
        if (!Convert.IsDBNull(record["VoucherNoDraft"])) model.VoucherNoDraft = Convert.ToString(record["VoucherNoDraft"]);
        if (!Convert.IsDBNull(record["UserNameCreate"])) model.UserNameCreate = Convert.ToString(record["UserNameCreate"]);
        if (!Convert.IsDBNull(record["ImplementUserName"])) model.ImplementUserName = Convert.ToString(record["UserNameCreate"]);
        if (!Convert.IsDBNull(record["ChargeUserName"])) model.ChargeUserName = Convert.ToString(record["ChargeUserName"]);
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
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["DateStart"])) model.Start = Convert.ToDateTime(record["DateStart"]);
        if (!Convert.IsDBNull(record["VoucherNo"])) model.VoucherNo = Convert.ToString(record["VoucherNo"]);
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["Phone1"])) model.Phone1 = Convert.ToString(record["Phone1"]);
        if (!Convert.IsDBNull(record["Type"])) model.Type = Convert.ToString(record["Type"]);
        if (!Convert.IsDBNull(record["Remark"])) model.RemarkOld = Convert.ToString(record["Remark"]);
        if (!Convert.IsDBNull(record["TotalDebtAmount"])) model.TotalDebtAmount = Convert.ToDouble(record["TotalDebtAmount"]);
        if (!Convert.IsDBNull(record["ServiceCode"])) model.ServiceCode = Convert.ToString(record["ServiceCode"]);
        if (!Convert.IsDBNull(record["ServiceName"])) model.ServiceName = Convert.ToString(record["ServiceName"]);
        if (!Convert.IsDBNull(record["BaseLine"])) model.BaseLine = Convert.ToInt32(record["BaseLine"]);
        model.End = model.Start;
        model.Title = $"Nhắc nợ - {model.FullName}";
        model.IsAllDay = true;
        return model;
    }

    /// <summary>
    /// Đọc thông tin lịch sử thanh toán
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private CustomerDebtsModel DataRecordToCustomerDebtsModel(IDataRecord record)
    {
        CustomerDebtsModel model = new();
        if (!Convert.IsDBNull(record["Id"])) model.Id = Convert.ToInt32(record["Id"]);
        if (!Convert.IsDBNull(record["DocEntry"])) model.DocEntry = Convert.ToInt32(record["DocEntry"]);
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["GuestsPay"])) model.GuestsPay = Convert.ToDouble(record["GuestsPay"]);
        if (!Convert.IsDBNull(record["TotalDebtAmount"])) model.TotalDebtAmount = Convert.ToDouble(record["TotalDebtAmount"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["Remark"])) model.Remark = Convert.ToString(record["Remark"]);
        if (!Convert.IsDBNull(record["IsDelay"])) model.IsDelay = Convert.ToBoolean(record["IsDelay"]);
        if (!Convert.IsDBNull(record["DateDelay"])) model.DateDelay = Convert.ToDateTime(record["DateDelay"]);
        return model;
    }

    /// <summary>
    /// lấy danh sách các phiếu bảo hành
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ServiceCallModel DataRecordToServiceCallModel(IDataRecord record)
    {
        ServiceCallModel model = new();
        if (!Convert.IsDBNull(record["DocEntry"])) model.DocEntry = Convert.ToInt32(record["DocEntry"]);
        if (!Convert.IsDBNull(record["VoucherNo"])) model.VoucherNo = Convert.ToString(record["VoucherNo"]);
        if (!Convert.IsDBNull(record["BaseEntry"])) model.BaseEntry = Convert.ToInt32(record["BaseEntry"]);
        if (!Convert.IsDBNull(record["BaseLine"])) model.BaseLine = Convert.ToInt32(record["BaseLine"]);
        if (!Convert.IsDBNull(record["ImplementUserId"])) model.ImplementUserId = Convert.ToString(record["ImplementUserId"]);
        if (!Convert.IsDBNull(record["ChemicalFormula"])) model.ChemicalFormula = Convert.ToString(record["ChemicalFormula"]);
        if (!Convert.IsDBNull(record["StatusId"])) model.StatusId = Convert.ToString(record["StatusId"]);
        if (!Convert.IsDBNull(record["StatusBefore"])) model.StatusBefore = Convert.ToString(record["StatusBefore"]);
        if (!Convert.IsDBNull(record["HealthStatus"])) model.HealthStatus = Convert.ToString(record["HealthStatus"]);
        if (!Convert.IsDBNull(record["NoteForAll"])) model.NoteForAll = Convert.ToString(record["NoteForAll"]);
        if (!Convert.IsDBNull(record["BranchId"])) model.BranchId = Convert.ToString(record["BranchId"]);
        if (!Convert.IsDBNull(record["BranchName"])) model.BranchName = Convert.ToString(record["BranchName"]);
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["Phone1"])) model.Phone1 = Convert.ToString(record["Phone1"]);
        if (!Convert.IsDBNull(record["ConsultUserId"])) model.ConsultUserId = Convert.ToString(record["ConsultUserId"]);
        if (!Convert.IsDBNull(record["StatusName"])) model.StatusName = Convert.ToString(record["StatusName"]);
        if (!Convert.IsDBNull(record["VoucherNoBase"])) model.VoucherNoBase = Convert.ToString(record["VoucherNoBase"]);
        if (!Convert.IsDBNull(record["ServiceCode"])) model.ServiceCode = Convert.ToString(record["ServiceCode"]);
        if (!Convert.IsDBNull(record["ServiceName"])) model.ServiceName = Convert.ToString(record["ServiceName"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        if (!Convert.IsDBNull(record["ReasonDelete"])) model.ReasonDelete = Convert.ToString(record["ReasonDelete"]);
        if (!Convert.IsDBNull(record["DateCreateBase"])) model.DateCreateBase = Convert.ToDateTime(record["DateCreateBase"]);
        if (!Convert.IsDBNull(record["DateOfBirth"])) model.DateOfBirth = Convert.ToDateTime(record["DateOfBirth"]);
        if (!Convert.IsDBNull(record["CINo"])) model.CINo = Convert.ToString(record["CINo"]);
        if (!Convert.IsDBNull(record["Zalo"])) model.Zalo = Convert.ToString(record["Zalo"]);
        if (!Convert.IsDBNull(record["FaceBook"])) model.FaceBook = Convert.ToString(record["FaceBook"]);
        if (!Convert.IsDBNull(record["StatusOutBound"])) model.StatusOutBound = Convert.ToString(record["StatusOutBound"]);
        if (!Convert.IsDBNull(record["DateCreateOutBound"])) model.DateCreateOutBound = Convert.ToDateTime(record["DateCreateOutBound"]);
        return model;
    }
    #endregion
}