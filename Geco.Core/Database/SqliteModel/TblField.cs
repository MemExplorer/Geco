
using System.Text;

namespace Geco.Core.Database.SqliteModel;
internal readonly record struct TblField(string Name, TblFieldType FieldType, bool IsPrimaryKey = false)
{
	internal string ToQueryPart()
	{
		var sb = new StringBuilder();
		var fieldTypeStr = FieldType switch
		{
			TblFieldType.NULL => "NULL",
			TblFieldType.INTEGER => "INTEGER",
			TblFieldType.REAL => "REAL",
			TblFieldType.TEXT => "TEXT",
			TblFieldType.BLOB => "BLOB",
			_ => throw new NotSupportedException()
		};

		sb.Append(Name);
		sb.Append(' ');
		sb.Append(fieldTypeStr);

		if (IsPrimaryKey)
			sb.Append(" PRIMARY KEY");

		return sb.ToString();
	}
}
