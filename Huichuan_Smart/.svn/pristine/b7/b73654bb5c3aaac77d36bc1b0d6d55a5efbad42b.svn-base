using SqlSugar;

namespace Domain.Entity
{
    //系统菜单表
    [SugarTable("sys_menu")]
    public class SysMenu : AuditEntity
    {
        //主键
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
        public long Id { get; set; }

        //菜单名称
        [SugarColumn(ColumnName = "menu_name", ColumnDataType = "nvarchar(50)", IsNullable = false, ColumnDescription = "菜单名称")]
        public string MenuName { get; set; } = string.Empty;

        //菜单标识（前端权限判断用，如 system_user）
        [SugarColumn(ColumnName = "menu_code", ColumnDataType = "nvarchar(50)", IsNullable = false, ColumnDescription = "菜单标识（前端权限判断用，如 system_user）")]
        public string MenuCode { get; set; } = string.Empty;

        //父级菜单ID，0=顶级目录
        [SugarColumn(ColumnName = "parent_id", ColumnDescription = "父级菜单ID，0=顶级")]
        public long ParentId { get; set; }

        //前端路由（如 /system/user），目录可为空
        [SugarColumn(ColumnName = "path", ColumnDataType = "nvarchar(200)", IsNullable = true, ColumnDescription = "前端路由（如 /system/user），目录可为空")]
        public string? Path { get; set; }

        //排序号
        [SugarColumn(ColumnName = "sort", ColumnDescription = "排序号")]
        public int Sort { get; set; }

        //菜单类型：1=目录, 2=菜单
        [SugarColumn(ColumnName = "menu_type", ColumnDataType = "tinyint", ColumnDescription = "菜单类型：1=目录, 2=菜单")]
        public byte MenuType { get; set; } = 2;

        //状态：0=启用, 1=停用
        [SugarColumn(ColumnName = "status", ColumnDataType = "tinyint", ColumnDescription = "状态：0=启用, 1=停用")]
        public byte Status { get; set; } = 0;
    }
}
