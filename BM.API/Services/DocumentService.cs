using BM.API.Infrastructure;
using BM.Models;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Net;

namespace BM.API.Services;

public interface IDocumentService
{
    Task<ResponseModel> UpdateSalesOrder(RequestModel pRequest);
}
public class DocumentService : IDocumentService
{
    private readonly IBMDbContext _context;
    public DocumentService(IBMDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseModel> UpdateSalesOrder(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
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
                    await _context.BeginTranAsync();
                    queryString = @"Insert into [dbo].[Drafts] ([DocEntry],[CusNo],[DiscountCode],[Total],[GuestsPay], [Debt],[StatusBefore]
                                   ,[HealthStatus],[NoteForAll],[StatusId],[DateCreate],[UserCreate],[IsDelete])
                                    values (@DocEntry, @CusNo, @DiscountCode, @Total, @GuestsPay, @Debt, @StatusBefore
                                   ,@HealthStatus, @NoteForAll, @StatusId, getdate(), @UserId, 0)";

                    sqlParameters = new SqlParameter[11];
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
                    bool isAdded = await ExecQuery();
                    if(isAdded)
                    {
                        
                        foreach(var oDraftDetails in lstDraftDetails)
                        {
                            int iDrftId = await _context.ExecuteScalarAsync("select isnull(max(Id), 0) + 1 from [dbo].[DraftDetails] with(nolock)");
                            queryString = @"Insert into [dbo].[DraftDetails] ([Id],[ServiceCode],[Qty], [Price],[LineTotal],[DocEntry], [ActionType],[ConsultUserId]
                                   ,[ImplementUserId],[ChemicalFormula],[DateCreate],[UserCreate],[IsDelete])
                                    values (@Id, @ServiceCode, @Qty, @Price, @LineTotal, @DocEntry, @ActionType, @ConsultUserId
                                   ,@ImplementUserId, @ChemicalFormula, getdate(), @UserId, 0)";

                            sqlParameters = new SqlParameter[11];
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
                            isAdded = await ExecQuery();
                            if(!isAdded)
                            {
                                await _context.CommitTranAsync();
                                break;
                            }    
                        }    
                        if(isAdded) await _context.CommitTranAsync();
                        break;
                    }
                    await _context.RollbackAsync();
                    break;
                case nameof(EnumType.Update):
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }    

            //var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
            //if (data != null && data.Rows.Count > 0)
            //{
            //    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
            //    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
            //}
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
}