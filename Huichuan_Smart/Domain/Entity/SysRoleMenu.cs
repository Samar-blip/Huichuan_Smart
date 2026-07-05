using SqlSugar;

namespace Domain.Entity
{
    //角色菜单关联表
    [SugarTable("sys_role_menu")]
    public class SysRoleMenu : AuditEntity
    {
        //角色ID（关联 sys_role.id）
        [SugarColumn(ColumnName = "role_id", ColumnDescription = "角色ID，关联 sys_role.id")]
        public long RoleId { get; set; }

        //菜单ID（关联 sys_menu.id）
        [SugarColumn(ColumnName = "menu_id", ColumnDescription = "菜单ID，关联 sys_menu.id")]
        public long MenuId { get; set; }
    }
}
