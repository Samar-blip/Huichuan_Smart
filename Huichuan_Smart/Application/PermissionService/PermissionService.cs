using Domain.Entity;
using Domain.Repository;
using Microsoft.Extensions.Logging;

namespace Application.PermissionService
{
    //权限服务实现 — 菜单树查询
    public class PermissionService : IPermissionService
    {
        private readonly ISysUserRepository _userRepo;
        private readonly ISysMenuRepository _menuRepo;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            ISysUserRepository userRepo,
            ISysMenuRepository menuRepo,
            ILogger<PermissionService> logger)
        {
            _userRepo = userRepo;
            _menuRepo = menuRepo;
            _logger = logger;
        }

        //获取用户有权限的菜单树
        public async Task<List<MenuTreeNodeDTO>> GetUserMenusAsync(long userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("[菜单查询] 用户 {UserId} 不存在", userId);
                return new List<MenuTreeNodeDTO>();
            }

            // 通过角色ID拿菜单
            var roleIds = new List<long> { user.RoleId };
            var menus = await _menuRepo.GetByRoleIdsAsync(roleIds);

            if (menus.Count == 0)
            {
                _logger.LogWarning("[菜单查询] 用户 {UserId}（角色ID={RoleId}）无可用菜单", userId, user.RoleId);
                return new List<MenuTreeNodeDTO>();
            }

            // 组装树：先取目录，再递归挂子菜单
            var tree = BuildTree(menus, 0);
            return tree;
        }

        //递归组装菜单树
        private List<MenuTreeNodeDTO> BuildTree(List<SysMenu> allMenus, long parentId)
        {
            return allMenus
                .Where(m => m.ParentId == parentId)
                .OrderBy(m => m.Sort)
                .Select(m => new MenuTreeNodeDTO
                {
                    Id = m.Id,
                    MenuName = m.MenuName,
                    MenuCode = m.MenuCode,
                    Path = m.Path,
                    Sort = m.Sort,
                    MenuType = m.MenuType,
                    Children = BuildTree(allMenus, m.Id),
                })
                .ToList();
        }
    }
}
