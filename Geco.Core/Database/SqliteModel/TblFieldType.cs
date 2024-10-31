namespace Geco.Core.Database.SqliteModel;

// https://www.tutorialspoint.com/sqlite/sqlite_data_types.htm
enum TblFieldType : byte
{
	Null,
	Integer,
	Real,
	Text,
	Blob
}
