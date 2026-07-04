using SqlSugar;

namespace Domain.Entity
{
    //登录日志表
    [SugarTable("sys_login_log")]
    public class SysLoginLog
    {
        //主键
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
        public long Id { get; set; }

        //登录账号
        [SugarColumn(ColumnName = "account", ColumnDataType = "nvarchar(50)", IsNullable = false, ColumnDescription = "登录账号")]
        public string Account { get; set; } = string.Empty;

        //用户ID（不存在记为0）
        [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户ID（账号不存在时记为0）")]
        public long UserId { get; set; }

        //是否成功
        [SugarColumn(ColumnName = "is_success", ColumnDescription = "是否登录成功")]
        public bool IsSuccess { get; set; }

        //失败原因
        [SugarColumn(ColumnName = "fail_reason", ColumnDataType = "nvarchar(200)", ColumnDescription = "失败原因（成功时为空）")]
        public string FailReason { get; set; } = string.Empty;

        //登录时间
        [SugarColumn(ColumnName = "login_time", ColumnDataType = "datetime2(7)", ColumnDescription = "登录时间")]
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    }
}
