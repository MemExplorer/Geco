using System.Text;

namespace Geco.Core.Database.SqliteModel;

readonly record struct TblField(string Name, TblFieldType FieldType, bool IsPrimaryKey = false)
{
	internal string ToQueryPart()
	{
		var sb = new StringBuilder();
		string fieldTypeStr = FieldType switch
		{
			TblFieldType.Null => "NULL",
			TblFieldType.Integer => "INTEGER",
			TblFieldType.Real => "REAL",
			TblFieldType.Text => "TEXT",
			TblFieldType.Blob => "BLOB",
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
