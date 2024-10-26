
namespace Geco.Core.Database.SqliteModel;

// https://www.tutorialspoint.com/sqlite/sqlite_data_types.htm
internal enum TblFieldType : byte
{
	NULL,
	INTEGER,
	REAL,
	TEXT,
	BLOB
}
