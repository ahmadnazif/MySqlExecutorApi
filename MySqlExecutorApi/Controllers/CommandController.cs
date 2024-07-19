using Microsoft.AspNetCore.Mvc.Diagnostics;
using MySqlExecutorApi.Services;

namespace MySqlExecutorApi.Controllers;

[Route("api/command")]
[ApiController]
public class CommandController(IDbRepo db, IConfiguration config) : ControllerBase
{
    private readonly IDbRepo db = db;
    private readonly IConfiguration config = config;

    [HttpGet("get-db-status")]
    public async Task<ActionResult<MySqlStatus>> GetDbStatus(CancellationToken ct)
    {
        var status = await db.GetDbStatusAsync(ct);

        return new MySqlStatus
        {
            DbIp = config["Db:Server"],
            DbName = config["Db:DbName"],
            DbUserId = config["Db:UserId"],
            AppConnectionTimeoutSec = status.AppConnectionTimeoutSec,
            DefaultCommandTimeoutSec = status.DefaultCommandTimeoutSec,
            MySqlVersion = status.MySqlVersion,
            ServerConnectionTimeoutSec = status.ServerConnectionTimeoutSec,
            StartTime = status.StartTime,
            Uptime = status.Uptime
        };
    }

    [HttpGet("list-all-table")]
    public async Task<ActionResult<List<string>>> ListAllTable(CancellationToken ct) => await db.ListAllTablesAsync(ct);

    [HttpPost("execute-write-command")]
    public async Task<ActionResult<WriteCommandExecutionResponse>> ExecuteWriteCommand(string commandText, CancellationToken ct) => await db.ExecuteWriteCommandAsync(commandText, ct);

    [HttpPost("execute-read-command")]
    public async Task<ActionResult<ReadCommandExecutionResponse>> ExecuteReadCommand(string commandText, CancellationToken ct) => await db.ExecuteReadCommandAsync(commandText, ct);
}
