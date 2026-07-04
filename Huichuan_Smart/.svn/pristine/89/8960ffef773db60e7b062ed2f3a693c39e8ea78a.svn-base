using SqlSugar;

namespace Domain.Entity
{
    /// <summary>
    /// 审计基类 — 所有实体继承此类即可自动拥有：创建时间、修改时间、创建人、修改人、逻辑删除
    /// </summary>
    public abstract class AuditEntity
    {
        //创建时间
        [SugarColumn(ColumnName = "create_time", ColumnDataType = "datetime2(7)", ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        //修改时间
        [SugarColumn(ColumnName = "update_time", ColumnDataType = "datetime2(7)", IsNullable = true, ColumnDescription = "修改时间")]
        public DateTime? UpdateTime { get; set; }

        //创建人用户名
        [SugarColumn(ColumnName = "create_by", ColumnDataType = "nvarchar(50)", IsNullable = true, ColumnDescription = "创建人")]
        public string? CreateBy { get; set; }

        //修改人用户名
        [SugarColumn(ColumnName = "update_by", ColumnDataType = "nvarchar(50)", IsNullable = true, ColumnDescription = "修改人")]
        public string? UpdateBy { get; set; }

        //是否逻辑删除：false=正常, true=已删除
        [SugarColumn(ColumnName = "is_deleted", ColumnDescription = "是否逻辑删除：0=正常, 1=已删除")]
        public bool IsDeleted { get; set; } = false;
    }
}
