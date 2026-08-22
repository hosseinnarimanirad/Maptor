using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Core.ShapefileFormat.Dbf;

public enum DbfFieldType
{
    Character = 'C',            // in ArcMap; Text(length)
    Number = 'N',
    Logical = 'L',              //
    Date = 'D',
    Memo = 'M',
    FloatingPoint = 'F',
    //Character name variable 
    Binary = 'B',
    General = 'G',
    Picture = 'P',
    Currency = 'Y',
    DateTime = 'T',
    Integer = 'I',
    VariField = 'V',
    //Variant (X) for compatibility with SQL-s (i.e. varChar). 
    Timestamp = '@',
    Double = 'O',
    Autoincrement = '+',
}
