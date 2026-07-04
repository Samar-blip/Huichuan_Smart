using Domain.Entity;
using Domain.Repository;
using SqlSugar;

namespace EFCore.Repositorys
{
    //菜单仓储实现
    public class MenuRepositorys : ISysMenuRepository
    {
        private readonly ISqlSugarClient _db;

        public MenuRepositorys(ISqlSugarClient db)
        {
            _db = db;
        }

        //根据角色ID列表获取所有启用的菜单
        public async Task<List<SysMenu>> GetByRoleIdsAsync(List<long> roleIds)
        {
            // sys_role_menu 关联 sys_menu，取去重后的菜单
            var menuIds = await _db.Queryable<SysRoleMenu>()
                .Where(rm => roleIds.Contains(rm.RoleId))
                .Select(rm => rm.MenuId)
                .Distinct()
                .ToListAsync();

            if (menuIds.Count == 0)
                return new List<SysMenu>();

            return await _db.Queryable<SysMenu>()
                .Where(m => menuIds.Contains(m.Id) && m.Status == 0)
                .OrderBy(m => m.Sort)
                .ToListAsync();
        }
    }
}
