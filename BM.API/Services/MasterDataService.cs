using BM.API.Infrastructure;
using BM.Models;
using System.Data;

namespace BM.API.Services;
public interface IMasterDataService
{
    Task<IEnumerable<BranchModel>> GetBranchsAsync();
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
