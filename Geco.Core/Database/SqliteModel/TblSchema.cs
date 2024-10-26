using System.Text;

namespace Geco.Core.Database.SqliteModel;
internal readonly record struct TblSchema(string Name, List<TblField> TblFields)
{
	internal string BuildQuery()
	{
		var sb = new StringBuilder();
		var fieldsQuery = string.Join(",", TblFields.Select(x => x.ToQueryPart()));

		sb.Append("CREATE TABLE ");
		sb.Append(Name);
		sb.Append('(');
		sb.Append(fieldsQuery);
		sb.Append(");");

		return sb.ToString();
	}
}
