namespace Application.PermissionService
{
    //菜单树节点（返回给前端渲染左侧菜单）
    public class MenuTreeNodeDTO
    {
        //菜单ID
        public long Id { get; set; }

        //菜单名称
        public string MenuName { get; set; } = string.Empty;

        //菜单标识
        public string MenuCode { get; set; } = string.Empty;

        //前端路由路径
        public string? Path { get; set; }

        //排序
        public int Sort { get; set; }

        //菜单类型（1=目录, 2=菜单）
        public byte MenuType { get; set; }

        //子菜单列表
        public List<MenuTreeNodeDTO> Children { get; set; } = new();
    }
}
