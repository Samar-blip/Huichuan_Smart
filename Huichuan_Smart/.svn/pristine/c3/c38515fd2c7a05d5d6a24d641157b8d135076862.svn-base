using SqlSugar;
using Domain.Entity;
using Domain.Repository;

namespace EFCore.Repositorys
{
    //用户仓储实现
    public class UserRepositorys : ISysUserRepository
    {
        private readonly ISqlSugarClient _db;

        public UserRepositorys(ISqlSugarClient db)
        {
            _db = db;
        }

        //根据用户名获取用户
        public async Task<SysUser?> GetByUserNameAsync(string userName)
        {
            return await _db.Queryable<SysUser>()
                .FirstAsync(u => u.UserName == userName);
        }

        //根据ID获取用户
        public async Task<SysUser?> GetByIdAsync(long userId)
        {
            return await _db.Queryable<SysUser>()
                .InSingleAsync(userId);
        }

        //更新用户（登录成功/失败后更新状态）
        public async Task UpdateAsync(SysUser user)
        {
            await _db.Updateable(user)
                .IgnoreColumns(it => new { it.CreateTime, it.CreateBy })
                .ExecuteCommandAsync();
        }

        //创建用户
        public async Task<long> InsertAsync(SysUser user)
        {
            return await _db.Insertable(user).ExecuteReturnBigIdentityAsync();
        }

        //逻辑删除用户
        public async Task SoftDeleteAsync(long userId)
        {
            await _db.Updateable<SysUser>()
                .SetColumns(it => it.IsDeleted == true)
                .Where(it => it.Id == userId)
                .ExecuteCommandAsync();
        }
    }
}
