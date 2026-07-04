using SqlSugar;
using Domain.Entity;

namespace EFCore
{
    //SqlSugar 上下文封装
    public class SqlSugarContext
    {
        public readonly ISqlSugarClient SqlSugarClient;

        public SqlSugarContext(ISqlSugarClient sqlSugarClient)
        {
            SqlSugarClient = sqlSugarClient;
        }

        //创建所有 CodeFirst 映射的表
        public void CreateTable()
        {
            SqlSugarClient.DbMaintenance.CreateDatabase();

            SqlSugarClient.CodeFirst.SetStringDefaultLength(500)
               .InitTables(new Type[]
               {
                   typeof(SysUser),
                   typeof(SysRole),
                   typeof(SysLoginLog),
                   typeof(SysMenu),
                   typeof(SysRoleMenu),
               });
        }
    }
}
