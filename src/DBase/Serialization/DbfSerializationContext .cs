using System.Text;

namespace DBase.Serialization;

internal readonly record struct DbfSerializationContext(Encoding Encoding, Memo? Memo, char DecimalSeparator);
