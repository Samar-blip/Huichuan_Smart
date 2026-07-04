using SqlSugar;

namespace Domain.Entity
{
    //系统用户表
    [SugarTable("sys_user")]
    public class SysUser : AuditEntity
    {
        //主键
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
        public long Id { get; set; }

        //角色ID（关联 sys_role.id）
        [SugarColumn(ColumnName = "role_id", IsNullable = false, ColumnDescription = "角色ID，关联 sys_role.id")]
        public long RoleId { get; set; }

        //用户名（工号，登录账号）
        [SugarColumn(ColumnName = "user_name", ColumnDataType = "nvarchar(50)", IsNullable = false, ColumnDescription = "用户名（工号，登录账号）")]
        public string UserName { get; set; } = string.Empty;

        //密码哈希（BCrypt）
        [SugarColumn(ColumnName = "password_hash", ColumnDataType = "nvarchar(200)", IsNullable = false, ColumnDescription = "密码哈希（BCrypt）")]
        public string PasswordHash { get; set; } = string.Empty;

        //真实姓名
        [SugarColumn(ColumnName = "real_name", ColumnDataType = "nvarchar(50)", ColumnDescription = "真实姓名")]
        public string RealName { get; set; } = string.Empty;

        //手机号
        [SugarColumn(ColumnName = "phone_number", ColumnDataType = "nvarchar(20)", ColumnDescription = "手机号")]
        public string PhoneNumber { get; set; } = string.Empty;

        //邮箱
        [SugarColumn(ColumnName = "email", ColumnDataType = "nvarchar(100)", ColumnDescription = "邮箱")]
        public string Email { get; set; } = string.Empty;

        //状态：0=正常, 1=停用, 2=锁定
        [SugarColumn(ColumnName = "status", ColumnDataType = "tinyint", ColumnDescription = "状态：0=正常, 1=停用, 2=锁定")]
        public byte Status { get; set; } = 0;

        //连续登录失败次数
        [SugarColumn(ColumnName = "failed_login_attempts", ColumnDescription = "连续登录失败次数")]
        public int FailedLoginAttempts { get; set; } = 0;

        //账号锁定截止时间（null=未锁定）
        [SugarColumn(ColumnName = "lockout_end_time", ColumnDataType = "datetime2(7)", IsNullable = true, ColumnDescription = "账号锁定截止时间（null=未锁定）")]
        public DateTime? LockoutEndTime { get; set; }

        //最后登录时间
        [SugarColumn(ColumnName = "last_login_time", ColumnDataType = "datetime2(7)", IsNullable = true, ColumnDescription = "最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

    }
}
