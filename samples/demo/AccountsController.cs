using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Demo.Api;

[ApiController]
[Route("api/accounts")]
public class AccountsController(SqlConnection connection, IAccountService accounts) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login(string email, string password)
    {
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password)));
        var sql = $"SELECT Id FROM Accounts WHERE Email = '{email}' AND PasswordHash = '{hash}'";

        connection.Open();
        var id = new SqlCommand(sql, connection).ExecuteScalar();
        return id is null ? Unauthorized() : Ok(new { token = accounts.CreateTokenAsync((int)id).Result });
    }

    [HttpGet("{id:int}/statement")]
    public async Task<IActionResult> Statement(int id)
    {
        var statement = await accounts.GetStatementAsync(id);
        return Ok(statement);
    }
}
