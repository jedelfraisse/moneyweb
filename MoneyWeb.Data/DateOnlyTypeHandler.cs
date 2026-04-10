using System.Data;
using Dapper;

namespace MoneyWeb.Data;

/// <summary>
/// Teaches Dapper how to read/write System.DateOnly, which has no built-in SQL type mapping.
/// Register once at startup via SqlMapper.AddTypeHandler(new DateOnlyTypeHandler()).
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) =>
        DateOnly.FromDateTime(Convert.ToDateTime(value));
}
