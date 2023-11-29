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
            DraftModel oDraft = JsonConvert.DeserializeObject<DraftModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters = new SqlParameter[1];
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
                    int iDocentry = await _context.ExecuteScalarAsync("select isnull(max(DocEntry), 0) + 1 from [dbo].[Drafts] with(nolock)");
                    await _context.BeginTranAsync();
                    queryString = @"Insert into [dbo].[Drafts] ([DocEntry],[CusNo],[DiscountCode],[Total],[GuestsPay],[StatusBefore]
                                   ,[HealthStatus],[NoteForAll],[StatusId],[DateCreate],[UserCreate],[IsDelete])
                                    values (@DocEntry, @CusNo, @DiscountCode, @Total, @GuestsPay, @StatusBefore
                                   ,@HealthStatus, @NoteForAll, @StatusId, getdate(), @UserId, 0)";

                    sqlParameters = new SqlParameter[10];
                    sqlParameters[0] = new SqlParameter("@DocEntry", iDocentry);
                    sqlParameters[1] = new SqlParameter("@CusNo", oDraft.CusNo);
                    sqlParameters[2] = new SqlParameter("@DiscountCode", oDraft.DiscountCode ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@Total", oDraft.Total);
                    sqlParameters[4] = new SqlParameter("@GuestsPay", oDraft.GuestsPay);
                    sqlParameters[5] = new SqlParameter("@StatusBefore", oDraft.StatusBefore ?? (object)DBNull.Value);
                    sqlParameters[6] = new SqlParameter("@HealthStatus", oDraft.HealthStatus ?? (object)DBNull.Value);
                    sqlParameters[7] = new SqlParameter("@NoteForAll", oDraft.NoteForAll ?? (object)DBNull.Value);
                    sqlParameters[8] = new SqlParameter("@StatusId", oDraft.StatusId ?? (object)DBNull.Value);
                    sqlParameters[9] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    await _context.CommitTranAsync();

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