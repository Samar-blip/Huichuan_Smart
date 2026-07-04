using SqlSugar;

namespace Domain.Entity
{
    //系统角色表
    [SugarTable("sys_role")]
    public class SysRole : AuditEntity
    {
        //主键
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
        public long Id { get; set; }

        //角色名称
        [SugarColumn(ColumnName = "role_name", ColumnDataType = "nvarchar(50)", IsNullable = false, ColumnDescription = "角色名称")]
        public string RoleName { get; set; } = string.Empty;

        //角色标识（用于代码判断，如 admin/operator/quality）
        [SugarColumn(ColumnName = "role_code", ColumnDataType = "nvarchar(50)", IsNullable = false, ColumnDescription = "角色标识（代码判断用，如 admin/operator）")]
        public string RoleCode { get; set; } = string.Empty;

        //角色描述
        [SugarColumn(ColumnName = "description", ColumnDataType = "nvarchar(200)", ColumnDescription = "角色描述")]
        public string Description { get; set; } = string.Empty;

        //排序
        [SugarColumn(ColumnName = "sort", ColumnDescription = "排序")]
        public int Sort { get; set; } = 0;

        //状态：0=启用, 1=停用
        [SugarColumn(ColumnName = "status", ColumnDataType = "tinyint", ColumnDescription = "状态：0=启用, 1=停用")]
        public byte Status { get; set; } = 0;
    }
}
