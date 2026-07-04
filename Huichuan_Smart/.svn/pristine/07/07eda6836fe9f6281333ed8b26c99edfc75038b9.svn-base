using Domain.Entity;

namespace Domain.Repository
{
    //菜单仓储接口
    public interface ISysMenuRepository
    {
        //根据角色ID列表获取所有启用的菜单
        Task<List<SysMenu>> GetByRoleIdsAsync(List<long> roleIds);
    }
}
