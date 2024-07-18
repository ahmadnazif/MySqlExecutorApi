﻿using MySqlExecutorApi.Models;
using MySqlExecutorApi.Services;

namespace MySqlExecutorApi.Controllers;

[Route("api/executor")]
[ApiController]
public class ExecutorController(IDbRepo db, IConfiguration config) : ControllerBase
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
}
