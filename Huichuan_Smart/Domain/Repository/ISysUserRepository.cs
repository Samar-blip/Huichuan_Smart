using Domain.Entity;

namespace Domain.Repository
{
    //用户仓储接口
    public interface ISysUserRepository
    {
        //根据用户名获取用户
        Task<SysUser?> GetByUserNameAsync(string userName);

        //根据ID获取用户
        Task<SysUser?> GetByIdAsync(long userId);

        //更新用户
        Task UpdateAsync(SysUser user);

        //创建用户
        Task<long> InsertAsync(SysUser user);

        //逻辑删除用户（设置 IsDeleted = true）
        Task SoftDeleteAsync(long userId);
    }
}
